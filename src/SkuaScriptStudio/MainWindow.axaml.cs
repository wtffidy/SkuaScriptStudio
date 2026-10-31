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
    private readonly ScriptLibraryService _libraryService = new();
    private ScriptDocument _document = new();

    public MainWindow()
    {
        InitializeComponent();
        EditorTextBox.Text = _document.Source;
        RefreshLibrary();
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
            RefreshLibrary();
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
            RefreshLibrary();
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


    private async void ImportToLibraryButton_OnClick(object? sender, RoutedEventArgs e)
    {
        IReadOnlyList<IStorageFile> files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
        {
            Title = "Import scripts into library",
            AllowMultiple = true,
            FileTypeFilter = [new FilePickerFileType("C# scripts") { Patterns = ["*.cs"] }]
        });

        try
        {
            foreach (IStorageFile file in files)
            {
                string? path = file.TryGetLocalPath();
                if (!string.IsNullOrWhiteSpace(path))
                    await _libraryService.ImportAsync(path);
            }

            RefreshLibrary();
            EditorStatusText.Text = $"Library updated · {files.Count} file(s) imported";
        }
        catch (Exception exception)
        {
            await ShowErrorAsync("Unable to import scripts", exception.Message);
        }
    }

    private void RefreshLibrary()
    {
        if (LibraryList is null)
            return;

        string filter = LibrarySearchInput?.Text?.Trim() ?? string.Empty;
        IEnumerable<ScriptLibraryEntry> entries = _libraryService.GetEntries();

        if (!string.IsNullOrWhiteSpace(filter))
            entries = entries.Where(entry => entry.Name.Contains(filter, StringComparison.OrdinalIgnoreCase));

        LibraryList.ItemsSource = entries.ToArray();
        LibraryCountText.Text = $"{LibraryList.ItemCount} SCRIPT(S)";
    }

    private void LibrarySearchInput_OnTextChanged(object? sender, Avalonia.Controls.TextChangedEventArgs e) => RefreshLibrary();

    private async void OpenLibraryButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (LibraryList.SelectedItem is not ScriptLibraryEntry entry)
            return;

        try
        {
            _document = await _fileService.OpenAsync(entry.FilePath);
            EditorTextBox.Text = _document.Source;
            DocumentNameText.Text = _document.Name;
            EditorStatusText.Text = $"Opened from library · {entry.FilePath}";
            SelectTab(1);
        }
        catch (Exception exception)
        {
            await ShowErrorAsync("Unable to open library script", exception.Message);
        }
    }

    private async void RemoveLibraryButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (LibraryList.SelectedItem is not ScriptLibraryEntry entry)
            return;

        try
        {
            _libraryService.Remove(entry.FilePath);
            RefreshLibrary();
        }
        catch (Exception exception)
        {
            await ShowErrorAsync("Unable to remove library script", exception.Message);
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
