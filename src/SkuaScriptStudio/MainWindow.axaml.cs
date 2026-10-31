using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Platform.Storage;
using SkuaScriptStudio.Models;
using SkuaScriptStudio.Services;

namespace SkuaScriptStudio;

public partial class MainWindow : Window
{
    private readonly ScriptFileService _fileService = new();
    private readonly ScriptBuilderService _builderService = new();
    private ScriptDocument _document = new();

    public MainWindow()
    {
        InitializeComponent();
        EditorTextBox.Text = _document.Source;
    }

    private void HomeNav_OnClick(object? sender, RoutedEventArgs e) => SelectTab(0);
    private void EditorNav_OnClick(object? sender, RoutedEventArgs e) => SelectTab(1);
    private void BuilderNav_OnClick(object? sender, RoutedEventArgs e) => SelectTab(2);
    private void LibraryNav_OnClick(object? sender, RoutedEventArgs e) => SelectTab(3);
    private void DiagnosticsNav_OnClick(object? sender, RoutedEventArgs e) => SelectTab(4);
    private void SnippetsNav_OnClick(object? sender, RoutedEventArgs e) => SelectTab(5);

    private void SelectTab(int index)
    {
        if (WorkspaceTabs is not null)
            WorkspaceTabs.SelectedIndex = index;
    }

    private async void OpenButton_OnClick(object? sender, RoutedEventArgs e)
    {
        IReadOnlyList<IStorageFile> files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Open Skua script",
            AllowMultiple = false,
            FileTypeFilter =
            [
                new FilePickerFileType("C# scripts") { Patterns = ["*.cs"] },
                FilePickerFileTypes.All
            ]
        });

        IStorageFile? file = files.FirstOrDefault();
        if (file is null)
            return;

        try
        {
            string? path = file.TryGetLocalPath();
            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidOperationException("The selected file does not have a local filesystem path.");

            _document = await _fileService.OpenAsync(path);
            EditorTextBox.Text = _document.Source;
            DocumentNameText.Text = _document.Name;
            EditorStatusText.Text = $"Opened · {path}";
            SelectTab(1);
        }
        catch (Exception exception)
        {
            await ShowErrorAsync("Unable to open script", exception.Message);
        }
    }

    private async void SaveButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _document.Source = EditorTextBox.Text ?? string.Empty;

        string? path = _document.FilePath;
        if (string.IsNullOrWhiteSpace(path))
        {
            IStorageFile? file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
            {
                Title = "Save Skua script",
                SuggestedFileName = _document.Name,
                DefaultExtension = "cs",
                FileTypeChoices = [new FilePickerFileType("C# scripts") { Patterns = ["*.cs"] }]
            });

            path = file?.TryGetLocalPath();
        }

        if (string.IsNullOrWhiteSpace(path))
            return;

        try
        {
            await _fileService.SaveAsync(_document, path);
            DocumentNameText.Text = _document.Name;
            EditorStatusText.Text = $"Saved · {path}";
        }
        catch (Exception exception)
        {
            await ShowErrorAsync("Unable to save script", exception.Message);
        }
    }

    private void GenerateButton_OnClick(object? sender, RoutedEventArgs e)
    {
        string map = MapInput.Text?.Trim() ?? string.Empty;
        string monster = MonsterInput.Text?.Trim() ?? string.Empty;
        string item = ItemInput.Text?.Trim() ?? string.Empty;
        int quantity = decimal.ToInt32(QuantityInput.Value ?? 1);
        string classType = (ClassTypeInput.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Farm";

        try
        {
            string generated = _builderService.GenerateHuntMonster(map, monster, item, quantity, classType);
            _document = new ScriptDocument
            {
                Name = "GeneratedHuntMonster.cs",
                Source = generated,
                IsDirty = true
            };

            EditorTextBox.Text = generated;
            DocumentNameText.Text = _document.Name + " *";
            EditorStatusText.Text = "Generated code · Review and edit before saving";
            SelectTab(1);
        }
        catch (ArgumentException exception)
        {
            _ = ShowErrorAsync("Missing builder information", exception.Message);
        }
    }

    private async Task ShowErrorAsync(string title, string message)
    {
        Window dialog = new()
        {
            Title = title,
            Width = 460,
            Height = 190,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(20),
                Spacing = 16,
                Children =
                {
                    new TextBlock { Text = message, TextWrapping = Avalonia.Media.TextWrapping.Wrap },
                    new Button
                    {
                        Content = "Close",
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right
                    }
                }
            }
        };

        if (dialog.Content is StackPanel panel && panel.Children.LastOrDefault() is Button closeButton)
            closeButton.Click += (_, _) => dialog.Close();

        await dialog.ShowDialog(this);
    }
}
