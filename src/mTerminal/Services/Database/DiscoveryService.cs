using System.Net;
using System.Net.Sockets;
using System.Text;
using Microsoft.Data.SqlClient;
using mTerminal.Models;
using Npgsql;

namespace mTerminal.Services.Database;

public sealed class DiscoveryService : IDisposable
{
    private readonly DbRegistry _registry;
    private readonly DbLogger _logger;
    private readonly DatabaseSettings _settings;
    private Timer? _timer;
    private int _running;
    private CancellationTokenSource? _cts;

    public event Action<string>? StatusChanged;

    public DiscoveryService(DbRegistry registry, DbLogger logger, DatabaseSettings settings)
    {
        _registry = registry;
        _logger = logger;
        _settings = settings;
    }

    public void Start()
    {
        _cts = new CancellationTokenSource();
        int intervalMs = _settings.DiscoveryIntervalMinutes * 60 * 1000;
        _timer = new Timer(OnTimer, null, 0, intervalMs);
    }

    public void Stop()
    {
        _cts?.Cancel();
        _cts = null;
        _timer?.Dispose();
        _timer = null;
    }

    public void RunNow()
    {
        if (_cts == null) return;
        ThreadPool.QueueUserWorkItem(_ => OnTimer(null));
    }

    public void Dispose() => Stop();

    private void OnTimer(object? state)
    {
        if (Interlocked.CompareExchange(ref _running, 1, 0) != 0) return;
        try
        {
            var cts = _cts;
            if (cts is { IsCancellationRequested: false })
                RunDiscovery(cts.Token);
        }
        catch (OperationCanceledException) { }
        catch (Exception ex)
        {
            _logger.Write($"[Discovery] Error: {ex.Message}", "Discovery");
        }
        finally
        {
            Interlocked.Exchange(ref _running, 0);
            StatusChanged?.Invoke("Idle");
        }
    }

    private void RunDiscovery(CancellationToken ct)
    {
        int registered = 0;

        if (_settings.SqlServer.Enabled)
        {
            ct.ThrowIfCancellationRequested();
            StatusChanged?.Invoke("Scanning SQL Server...");
            try { registered += DiscoverSqlServer(ct); }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { _logger.Write($"[Discovery] SQL Server error: {ex.Message}", "Discovery"); }
        }

        if (_settings.PostgreSql.Enabled)
        {
            ct.ThrowIfCancellationRequested();
            StatusChanged?.Invoke("Scanning PostgreSQL...");
            try { registered += DiscoverPostgreSql(ct); }
            catch (OperationCanceledException) { throw; }
            catch (Exception ex) { _logger.Write($"[Discovery] PostgreSQL error: {ex.Message}", "Discovery"); }
        }

        var maxAge = TimeSpan.FromMinutes(_settings.DiscoveryIntervalMinutes * _settings.StaleCycles);
        int removed = _registry.RemoveStale(maxAge);

        if (registered > 0 || removed > 0)
            _logger.Write($"[Discovery] Done: {registered} new, {removed} removed, {_registry.Count} total", "Discovery");

        StatusChanged?.Invoke($"Done: {registered} new, {removed} removed");
    }

    private struct SqlInstanceInfo
    {
        public IPAddress Ip;
        public string Host;
        public string InstanceName;
        public string TcpPort;
    }

    private int DiscoverSqlServer(CancellationToken ct)
    {
        int registered = 0;
        byte[] request = [0x02];
        var responses = new List<(IPAddress Address, byte[] Data)>();
        var broadcastTargets = new List<IPAddress> { IPAddress.Broadcast };

        foreach (var subnet in SubnetScanner.GetLocalSubnets())
        {
            var bcast = SubnetScanner.GetBroadcastAddress(subnet);
            if (!broadcastTargets.Contains(bcast))
                broadcastTargets.Add(bcast);
        }

        try
        {
            using var udp = new UdpClient();
            udp.EnableBroadcast = true;
            udp.Client.ReceiveTimeout = 3000;
            foreach (var target in broadcastTargets)
                udp.Send(request, request.Length, new IPEndPoint(target, 1434));
            while (true)
            {
                try
                {
                    var remote = new IPEndPoint(IPAddress.Any, 0);
                    byte[] data = udp.Receive(ref remote);
                    responses.Add((remote.Address, data));
                }
                catch (SocketException) { break; }
            }
        }
        catch (SocketException ex)
        {
            _logger.Write($"[Discovery] SQL Browser UDP error: {ex.Message}", "Discovery");
        }

        ct.ThrowIfCancellationRequested();

        var instances = new List<SqlInstanceInfo>();
        foreach (var resp in responses)
        {
            try { ParseSqlBrowserResponse(resp.Address, resp.Data, instances); }
            catch (Exception ex) { _logger.Write($"[Discovery] Failed to parse SQL Browser response: {ex.Message}", "Discovery"); }
        }

        Parallel.ForEach(instances, new ParallelOptions { CancellationToken = ct }, inst =>
        {
            try
            {
                int r = TryRegisterSqlServerDatabases(inst.Ip, inst.Host, inst.InstanceName, inst.TcpPort);
                Interlocked.Add(ref registered, r);
            }
            catch (Exception ex)
            {
                _logger.Write($"[Discovery] SQL Server register {inst.Host}/{inst.InstanceName} failed: {ex.Message}", "Discovery");
            }
        });

        return registered;
    }

    private static void ParseSqlBrowserResponse(IPAddress sourceIp, byte[] data, List<SqlInstanceInfo> result)
    {
        if (data.Length < 3 || data[0] != 0x05) return;
        string text = Encoding.ASCII.GetString(data, 3, data.Length - 3);
        string[] instances = text.Split([";;"], StringSplitOptions.RemoveEmptyEntries);

        foreach (string instance in instances)
        {
            string[] parts = instance.Split(';');
            var dict = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
            for (int i = 0; i + 1 < parts.Length; i += 2)
                dict[parts[i]] = parts[i + 1];

            dict.TryGetValue("ServerName", out var serverName);
            dict.TryGetValue("InstanceName", out var instanceName);
            dict.TryGetValue("tcp", out var tcpPort);

            if (string.IsNullOrEmpty(tcpPort)) continue;

            result.Add(new SqlInstanceInfo
            {
                Ip = sourceIp,
                Host = serverName ?? sourceIp.ToString(),
                InstanceName = string.IsNullOrEmpty(instanceName) || instanceName == "MSSQLSERVER" ? "" : instanceName,
                TcpPort = tcpPort
            });
        }
    }

    private string BuildSqlServerConnStr(IPAddress ip, string tcpPort, string database)
    {
        var builder = new SqlConnectionStringBuilder
        {
            DataSource = $"{ip},{tcpPort}",
            InitialCatalog = database,
            ConnectTimeout = 15,
            Encrypt = false,
            TrustServerCertificate = true
        };
        if (_settings.SqlServer.UseIntegratedSecurity || string.IsNullOrEmpty(_settings.SqlServer.Username))
            builder.IntegratedSecurity = true;
        else
        {
            builder.UserID = _settings.SqlServer.Username;
            builder.Password = _settings.SqlServer.Password;
        }
        return builder.ConnectionString;
    }

    private int TryRegisterSqlServerDatabases(IPAddress ip, string host, string instanceName, string tcpPort)
    {
        int registered = 0;
        string baseConnStr = BuildSqlServerConnStr(ip, tcpPort, "master");

        var databases = new List<string>();
        try
        {
            var provider = new SqlServerProvider(baseConnStr);
            using var conn = provider.CreateConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT name FROM sys.databases WHERE database_id > 4";
            cmd.CommandTimeout = 5;
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) databases.Add(reader.GetString(0));
        }
        catch (Exception ex)
        {
            _logger.Write($"[Discovery] SQL Server DB list from {host}/{instanceName} failed: {ex.Message}", "Discovery");
            return 0;
        }

        foreach (string db in databases)
        {
            var info = new DatabaseInstance
            {
                Server = host,
                Instance = instanceName,
                Database = db,
                Provider = DbProviderType.SqlServer,
                ConnectionString = BuildSqlServerConnStr(ip, tcpPort, db),
                Source = DbSourceType.Discovered
            };

            if (_registry.TryRegister(info))
            {
                _logger.Write($"[Discovery] Registered: {info.DisplayName}", "Discovery");
                registered++;
            }
        }
        return registered;
    }

    private int DiscoverPostgreSql(CancellationToken ct)
    {
        int registered = 0;
        var targets = new List<(IPAddress Ip, int Port)>();

        foreach (var ip in SubnetScanner.GetLoopbackAddresses())
            foreach (int port in _settings.PostgreSql.Ports)
                targets.Add((ip, port));

        foreach (var subnet in SubnetScanner.GetLocalSubnets())
            foreach (var ip in SubnetScanner.GetAddressesInSubnet(subnet))
                foreach (int port in _settings.PostgreSql.Ports)
                    targets.Add((ip, port));

        Parallel.ForEach(targets, new ParallelOptions { MaxDegreeOfParallelism = 32, CancellationToken = ct }, target =>
        {
            try
            {
                if (SubnetScanner.ScanPort(target.Ip, target.Port, 2000))
                {
                    int r = TryRegisterPostgreSqlDatabases(target.Ip, target.Port);
                    Interlocked.Add(ref registered, r);
                }
            }
            catch (Exception ex)
            {
                _logger.Write($"[Discovery] PG scan {target.Ip}:{target.Port} error: {ex.Message}", "Discovery");
            }
        });

        return registered;
    }

    private string BuildPostgreSqlConnStr(IPAddress ip, int port, string database)
    {
        var builder = new NpgsqlConnectionStringBuilder
        {
            Host = ip.ToString(),
            Port = port,
            Database = database,
            Timeout = 15
        };
        if (!string.IsNullOrEmpty(_settings.PostgreSql.Username))
        {
            builder.Username = _settings.PostgreSql.Username;
            builder.Password = _settings.PostgreSql.Password;
        }
        return builder.ConnectionString;
    }

    private int TryRegisterPostgreSqlDatabases(IPAddress ip, int port)
    {
        int registered = 0;
        string baseConnStr = BuildPostgreSqlConnStr(ip, port, "postgres");

        var databases = new List<string>();
        try
        {
            var provider = new PostgreSqlProvider(baseConnStr);
            using var conn = provider.CreateConnection();
            conn.Open();
            using var cmd = conn.CreateCommand();
            cmd.CommandText = "SELECT datname FROM pg_database WHERE datistemplate = false";
            cmd.CommandTimeout = 5;
            using var reader = cmd.ExecuteReader();
            while (reader.Read()) databases.Add(reader.GetString(0));
        }
        catch (Exception ex)
        {
            _logger.Write($"[Discovery] PG DB list from {ip}:{port} failed: {ex.Message}", "Discovery");
            return 0;
        }

        string hostName = ResolveHostName(ip);
        string instance = port == 5432 ? "" : port.ToString();

        foreach (string db in databases)
        {
            var info = new DatabaseInstance
            {
                Server = hostName,
                Instance = instance,
                Database = db,
                Provider = DbProviderType.PostgreSQL,
                ConnectionString = BuildPostgreSqlConnStr(ip, port, db),
                Source = DbSourceType.Discovered
            };

            if (_registry.TryRegister(info))
            {
                _logger.Write($"[Discovery] Registered: {info.DisplayName}", "Discovery");
                registered++;
            }
        }
        return registered;
    }

    private static string ResolveHostName(IPAddress ip)
    {
        try
        {
            var entry = Dns.GetHostEntry(ip);
            if (!string.IsNullOrEmpty(entry.HostName))
            {
                int dot = entry.HostName.IndexOf('.');
                return dot > 0 ? entry.HostName[..dot] : entry.HostName;
            }
        }
        catch { }
        return ip.ToString();
    }
}
