using Avalonia.Controls;
using Avalonia.Platform.Storage;
using MTerminal.ViewModels;

namespace MTerminal.Views;

public partial class WorkspacesPanelView : UserControl
{
    public WorkspacesPanelView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is WorkspacesPanelViewModel vm)
        {
            vm.FolderPicker = async () =>
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel == null) return null;

                var folders = await topLevel.StorageProvider.OpenFolderPickerAsync(
                    new FolderPickerOpenOptions { Title = "Select workspace directory", AllowMultiple = false });

                return folders.Count > 0 ? folders[0].TryGetLocalPath() : null;
            };
        }
    }
}
