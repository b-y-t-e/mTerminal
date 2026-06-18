using System.Collections.ObjectModel;
using CommunityToolkit.Mvvm.ComponentModel;
using CommunityToolkit.Mvvm.Input;
using MTerminal.Models;
using MTerminal.Services;

namespace MTerminal.ViewModels;

public partial class SettingsViewModel : ObservableObject
{
    public static string[] Themes { get; } = ["Dark", "Light"];
    public static string CustomShellOption => "Custom...";
    public static string[] ColorThemeNames { get; } = TerminalTheme.BuiltIn.Select(t => t.Name).ToArray();

    private readonly SettingsService _settingsService;

    [ObservableProperty]
    private int _selectedTab;

    public bool IsGeneralTab => SelectedTab == 0;
    public bool IsProfilesTab => SelectedTab == 1;

    partial void OnSelectedTabChanged(int value)
    {
        OnPropertyChanged(nameof(IsGeneralTab));
        OnPropertyChanged(nameof(IsProfilesTab));
    }

    [RelayCommand]
    private void SelectTab(int tab) => SelectedTab = tab;

    public ObservableCollection<string> ShellOptions { get; } = [];
    public List<string> ProfileShellOptions { get; } = [];
    public ObservableCollection<UserShellProfile> ShellProfiles { get; } = [];

    [ObservableProperty]
    private string _colorThemeName;

    [ObservableProperty]
    private string _terminalFontFamily;

    [ObservableProperty]
    private double _terminalFontSize;

    [ObservableProperty]
    private string _fontFamily;

    [ObservableProperty]
    private double _fontSize;

    [ObservableProperty]
    private string _theme;

    [ObservableProperty]
    private string _selectedShell;

    [ObservableProperty]
    private string _customShellPath;

    [ObservableProperty]
    private string _customShellArgs;

    [ObservableProperty]
    private bool _isCustomShell;

    [ObservableProperty]
    private bool _isEditingProfile;

    [ObservableProperty]
    private string _editProfileName = "";

    [ObservableProperty]
    private string _editProfileShell = "";

    [ObservableProperty]
    private string _editProfileScript = "";

    private UserShellProfile? _editingProfile;

    public SettingsViewModel(SettingsService settingsService)
    {
        _settingsService = settingsService;
        var s = settingsService.Settings;
        _colorThemeName = s.ColorThemeName;
        _terminalFontFamily = s.TerminalFontFamily;
        _terminalFontSize = s.TerminalFontSize;
        _fontFamily = s.FontFamily;
        _fontSize = s.FontSize;
        _theme = s.Theme;
        _customShellPath = s.CustomShellPath;
        _customShellArgs = s.CustomShellArgs;

        var detected = ShellDetector.Detect();
        foreach (var shell in detected)
        {
            ShellOptions.Add(shell.Name);
            ProfileShellOptions.Add(shell.Name);
        }
        ShellOptions.Add(CustomShellOption);

        if (!string.IsNullOrEmpty(s.CustomShellPath))
        {
            _selectedShell = CustomShellOption;
            _isCustomShell = true;
        }
        else if (!string.IsNullOrEmpty(s.DefaultShellName) && ShellOptions.Contains(s.DefaultShellName))
        {
            _selectedShell = s.DefaultShellName;
        }
        else
        {
            _selectedShell = ShellOptions.Count > 1 ? ShellOptions[0] : CustomShellOption;
        }

        foreach (var p in s.ShellProfiles)
            ShellProfiles.Add(p);
    }

    partial void OnColorThemeNameChanged(string value) { _settingsService.Settings.ColorThemeName = value; _settingsService.NotifyChanged(); }
    partial void OnTerminalFontFamilyChanged(string value) { _settingsService.Settings.TerminalFontFamily = value; _settingsService.NotifyChanged(); }
    partial void OnTerminalFontSizeChanged(double value) { _settingsService.Settings.TerminalFontSize = value; _settingsService.NotifyChanged(); }
    partial void OnFontFamilyChanged(string value) { _settingsService.Settings.FontFamily = value; _settingsService.NotifyChanged(); }
    partial void OnFontSizeChanged(double value) { _settingsService.Settings.FontSize = value; _settingsService.NotifyChanged(); }
    partial void OnThemeChanged(string value) { _settingsService.Settings.Theme = value; _settingsService.NotifyChanged(); }

    partial void OnSelectedShellChanged(string value)
    {
        IsCustomShell = value == CustomShellOption;
        if (IsCustomShell)
        {
            _settingsService.Settings.DefaultShellName = "";
        }
        else
        {
            _settingsService.Settings.DefaultShellName = value;
            _settingsService.Settings.CustomShellPath = "";
            _settingsService.Settings.CustomShellArgs = "";
        }
        _settingsService.NotifyChanged();
    }

    partial void OnCustomShellPathChanged(string value) { _settingsService.Settings.CustomShellPath = value; _settingsService.NotifyChanged(); }
    partial void OnCustomShellArgsChanged(string value) { _settingsService.Settings.CustomShellArgs = value; _settingsService.NotifyChanged(); }

    [RelayCommand]
    private void AddProfile()
    {
        var defaultShell = IsCustomShell
            ? (ProfileShellOptions.Count > 0 ? ProfileShellOptions[0] : "")
            : SelectedShell;
        _editingProfile = new UserShellProfile { Name = "New Profile", ShellName = defaultShell };
        EditProfileName = _editingProfile.Name;
        EditProfileShell = _editingProfile.ShellName;
        EditProfileScript = "";
        IsEditingProfile = true;
    }

    [RelayCommand]
    private void EditProfile(UserShellProfile profile)
    {
        _editingProfile = profile;
        EditProfileName = profile.Name;
        EditProfileShell = profile.ShellName;
        EditProfileScript = profile.StartupScript;
        IsEditingProfile = true;
    }

    [RelayCommand]
    private void DeleteProfile(UserShellProfile profile)
    {
        ShellProfiles.Remove(profile);
        _settingsService.Settings.ShellProfiles.Remove(profile);
        if (_editingProfile == profile)
            IsEditingProfile = false;
        _settingsService.NotifyChanged();
    }

    [RelayCommand]
    private void SaveProfile()
    {
        if (_editingProfile == null) return;

        _editingProfile.Name = EditProfileName;
        _editingProfile.ShellName = EditProfileShell;
        _editingProfile.StartupScript = EditProfileScript;

        if (!ShellProfiles.Contains(_editingProfile))
        {
            ShellProfiles.Add(_editingProfile);
            _settingsService.Settings.ShellProfiles.Add(_editingProfile);
        }
        else
        {
            var idx = ShellProfiles.IndexOf(_editingProfile);
            ShellProfiles.RemoveAt(idx);
            ShellProfiles.Insert(idx, _editingProfile);
        }

        IsEditingProfile = false;
        _editingProfile = null;
        _settingsService.NotifyChanged();
    }

    [RelayCommand]
    private void CancelEditProfile()
    {
        IsEditingProfile = false;
        _editingProfile = null;
    }
}
