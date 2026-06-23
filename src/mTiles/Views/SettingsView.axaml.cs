using Avalonia.Controls;
using Avalonia.Platform.Storage;
using MsgBox = MsBox.Avalonia.MessageBoxManager;
using MsBox.Avalonia.Enums;
using mTiles.ViewModels;

namespace mTiles.Views;

public partial class SettingsView : UserControl
{
    public SettingsView()
    {
        InitializeComponent();
        DataContextChanged += OnDataContextChanged;
    }

    private void OnDataContextChanged(object? sender, EventArgs e)
    {
        if (DataContext is SettingsViewModel vm)
        {
            vm.ConfirmAction = async message =>
            {
                var window = TopLevel.GetTopLevel(this) as Window;
                if (window == null) return true;
                var box = MsgBox.GetMessageBoxStandard("Confirm", message, ButtonEnum.YesNo, Icon.Question);
                var result = await box.ShowWindowDialogAsync(window);
                return result == ButtonResult.Yes;
            };
            vm.BrowseAiToolFile = async () =>
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel == null) return null;

                var files = await topLevel.StorageProvider.OpenFilePickerAsync(
                    new FilePickerOpenOptions
                    {
                        Title = "Select AI tool executable",
                        AllowMultiple = false
                    });

                return files.Count > 0 ? files[0].TryGetLocalPath() : null;
            };

            vm.BrowseGitFile = async () =>
            {
                var topLevel = TopLevel.GetTopLevel(this);
                if (topLevel == null) return null;

                var files = await topLevel.StorageProvider.OpenFilePickerAsync(
                    new FilePickerOpenOptions
                    {
                        Title = "Select git executable",
                        AllowMultiple = false
                    });

                return files.Count > 0 ? files[0].TryGetLocalPath() : null;
            };
        }
    }
}
