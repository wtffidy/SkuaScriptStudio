using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using SkuaScriptStudio.Models;
using SkuaScriptStudio.Services;

namespace SkuaScriptStudio;

public partial class MainWindow : Window
{
    private readonly ScriptFileService _fileService = new();
    private readonly ScriptBuilderService _builderService = new();
    private readonly ScriptLibraryService _libraryService = new();
    private readonly ScriptDiagnosticsService _diagnosticsService = new();
    private readonly ScriptSnippetService _snippetService = new();
    private ScriptDocument _document = new();
    private bool _isUpdatingEditor;
    private long _documentLoadVersion;
    private long _documentEditVersion;
    private CancellationTokenSource? _documentLoadCancellation;
    private CancellationTokenSource? _diagnosticsCancellation;
    private CancellationTokenSource? _libraryRefreshCancellation;
    private ScriptLibraryEntry[] _libraryEntries = [];
    private long _libraryRefreshVersion;
    private bool _allowClose;
    private bool _isConfirmingClose;

    public MainWindow()
    {
        InitializeComponent();
        SetEditorSource(_document.Source);
        RefreshSnippets();
        _ = LoadLibraryAsync();
    }

    private void MainWindow_OnKeyDown(object? sender, KeyEventArgs e)
    {
        if (!e.KeyModifiers.HasFlag(KeyModifiers.Control))
            return;

        switch (e.Key)
        {
            case Key.S:
                SaveButton_OnClick(this, new RoutedEventArgs());
                e.Handled = true;
                break;
            case Key.O:
                OpenButton_OnClick(this, new RoutedEventArgs());
                e.Handled = true;
                break;
            case Key.F5:
                AnalyzeButton_OnClick(this, new RoutedEventArgs());
                e.Handled = true;
                break;
        }
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
        _documentLoadCancellation?.Cancel();
        using CancellationTokenSource cancellation = new();
        _documentLoadCancellation = cancellation;
        long loadVersion = ++_documentLoadVersion;

        try
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
            if (file is null || loadVersion != _documentLoadVersion || !await ConfirmDocumentReplacementAsync("opening another script"))
                return;

            long editVersion = _documentEditVersion;
            string? path = file.TryGetLocalPath();
            if (string.IsNullOrWhiteSpace(path))
                throw new InvalidOperationException("The selected file does not have a local filesystem path.");

            ScriptDocument document = await _fileService.OpenAsync(path, cancellation.Token);
            if (loadVersion != _documentLoadVersion || editVersion != _documentEditVersion)
                return;

            _document = document;
            SetEditorSource(document.Source);
            DocumentNameText.Text = document.Name;
            EditorStatusText.Text = $"Opened · {path}";
            RefreshLibrary();
            SelectTab(1);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            if (loadVersion == _documentLoadVersion)
                await ShowErrorAsync("Unable to open script", exception.Message);
        }
        finally
        {
            if (ReferenceEquals(_documentLoadCancellation, cancellation))
                _documentLoadCancellation = null;
        }
    }

    private async void SaveButton_OnClick(object? sender, RoutedEventArgs e) => await SaveDocumentAsync(_document);

    private async Task<bool> SaveDocumentAsync(ScriptDocument document, bool refreshLibrary = true)
    {
        if (ReferenceEquals(_document, document))
            document.Source = EditorTextBox.Text ?? string.Empty;

        try
        {
            string? path = document.FilePath;
            if (string.IsNullOrWhiteSpace(path))
            {
                IStorageFile? file = await StorageProvider.SaveFilePickerAsync(new FilePickerSaveOptions
                {
                    Title = "Save Skua script",
                    SuggestedFileName = document.Name,
                    DefaultExtension = "cs",
                    FileTypeChoices = [new FilePickerFileType("C# scripts") { Patterns = ["*.cs"] }]
                });

                path = file?.TryGetLocalPath();
            }

            if (string.IsNullOrWhiteSpace(path))
                return false;

            await _fileService.SaveAsync(document, path);
            if (!ReferenceEquals(_document, document))
                return !document.IsDirty;

            DocumentNameText.Text = document.Name + (document.IsDirty ? " *" : string.Empty);
            EditorStatusText.Text = document.IsDirty
                ? $"Saved snapshot · newer edits remain unsaved · {path}"
                : $"Saved · {path}";
            if (!document.IsDirty && refreshLibrary)
                await LoadLibraryAsync();

            return !document.IsDirty;
        }
        catch (Exception exception)
        {
            if (ReferenceEquals(_document, document))
                await ShowErrorAsync("Unable to save script", exception.Message);

            return false;
        }
    }

    private async void MainWindow_OnClosing(object? sender, WindowClosingEventArgs e)
    {
        if (_allowClose || !_document.IsDirty)
        {
            ++_documentLoadVersion;
            _documentLoadCancellation?.Cancel();
            _libraryRefreshCancellation?.Cancel();
            _diagnosticsCancellation?.Cancel();
            return;
        }

        e.Cancel = true;
        if (_isConfirmingClose)
            return;

        _isConfirmingClose = true;
        try
        {
            UnsavedChangesChoice choice = await ShowUnsavedChangesDialogAsync("closing the studio");
            if (choice == UnsavedChangesChoice.Save && !await SaveDocumentAsync(_document, refreshLibrary: false))
                return;

            if (choice is UnsavedChangesChoice.Save or UnsavedChangesChoice.Discard)
            {
                _allowClose = true;
                Close();
            }
        }
        finally
        {
            _isConfirmingClose = false;
        }
    }

    private async Task<bool> ConfirmDocumentReplacementAsync(string action)
    {
        if (!_document.IsDirty)
            return true;

        UnsavedChangesChoice choice = await ShowUnsavedChangesDialogAsync(action);
        return choice switch
        {
            UnsavedChangesChoice.Save => await SaveDocumentAsync(_document, refreshLibrary: false),
            UnsavedChangesChoice.Discard => true,
            _ => false
        };
    }

    private async Task<UnsavedChangesChoice> ShowUnsavedChangesDialogAsync(string action)
    {
        Window dialog = new()
        {
            Title = "Unsaved changes",
            Width = 440,
            Height = 210,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Content = new StackPanel
            {
                Margin = new Avalonia.Thickness(20),
                Spacing = 16,
                Children =
                {
                    new TextBlock
                    {
                        Text = $"Save changes to {_document.Name} before {action}?",
                        TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                        FontSize = 15
                    },
                    new StackPanel
                    {
                        Orientation = Avalonia.Layout.Orientation.Horizontal,
                        HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Right,
                        Spacing = 8,
                        Children =
                        {
                            new Button { Content = "Cancel", MinWidth = 82, Tag = UnsavedChangesChoice.Cancel },
                            new Button { Content = "Discard", MinWidth = 82, Tag = UnsavedChangesChoice.Discard },
                            new Button { Content = "Save", MinWidth = 82, Tag = UnsavedChangesChoice.Save }
                        }
                    }
                }
            }
        };

        if (dialog.Content is StackPanel panel && panel.Children.LastOrDefault() is StackPanel buttons)
        {
            foreach (Button button in buttons.Children.OfType<Button>())
                button.Click += (_, _) => dialog.Close((UnsavedChangesChoice)button.Tag!);
        }

        return await dialog.ShowDialog<UnsavedChangesChoice>(this);
    }

    private enum UnsavedChangesChoice
    {
        Cancel,
        Save,
        Discard
    }

    private async void GenerateButton_OnClick(object? sender, RoutedEventArgs e)
    {
        string map = MapInput.Text?.Trim() ?? string.Empty;
        string monster = MonsterInput.Text?.Trim() ?? string.Empty;
        string item = ItemInput.Text?.Trim() ?? string.Empty;
        int quantity = decimal.ToInt32(QuantityInput.Value ?? 1);
        string classType = (ClassTypeInput.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Farm";

        try
        {
            string generated = _builderService.GenerateHuntMonster(map, monster, item, quantity, classType);
            if (!await ConfirmDocumentReplacementAsync("replacing the current script"))
                return;

            _documentLoadCancellation?.Cancel();
            ++_documentLoadVersion;
            _document = new ScriptDocument
            {
                Name = "GeneratedHuntMonster.cs",
                Source = generated,
                IsDirty = true
            };

            SetEditorSource(generated);
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
        try
        {
            IReadOnlyList<IStorageFile> files = await StorageProvider.OpenFilePickerAsync(new FilePickerOpenOptions
            {
                Title = "Import scripts into library",
                AllowMultiple = true,
                FileTypeFilter = [new FilePickerFileType("C# scripts") { Patterns = ["*.cs"] }]
            });

            int importedCount = 0;
            foreach (IStorageFile file in files)
            {
                string? path = file.TryGetLocalPath();
                if (string.IsNullOrWhiteSpace(path))
                    continue;

                await _libraryService.ImportAsync(path);
                importedCount++;
            }

            await LoadLibraryAsync();
            EditorStatusText.Text = $"Library updated · {importedCount} file(s) imported";
        }
        catch (Exception exception)
        {
            await ShowErrorAsync("Unable to import scripts", exception.Message);
        }
    }

    private void RefreshLibrary()
    {
        string filter = LibrarySearchInput?.Text?.Trim() ?? string.Empty;
        IEnumerable<ScriptLibraryEntry> entries = _libraryEntries;

        if (!string.IsNullOrWhiteSpace(filter))
            entries = entries.Where(entry => entry.Name.Contains(filter, StringComparison.OrdinalIgnoreCase));

        LibraryList.ItemsSource = entries.ToArray();
        LibraryCountText.Text = $"{LibraryList.Items.Count} / {_libraryEntries.Length} SCRIPT(S)";
    }

    private async Task LoadLibraryAsync()
    {
        _libraryRefreshCancellation?.Cancel();
        CancellationTokenSource cancellation = new();
        _libraryRefreshCancellation = cancellation;
        long refreshVersion = ++_libraryRefreshVersion;

        try
        {
            IReadOnlyList<ScriptLibraryEntry> entries = await _libraryService.GetEntriesAsync(cancellation.Token);
            if (cancellation.IsCancellationRequested || refreshVersion != _libraryRefreshVersion)
                return;

            _libraryEntries = entries.ToArray();
            RefreshLibrary();
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            if (!cancellation.IsCancellationRequested)
                await ShowErrorAsync("Unable to load script library", exception.Message);
        }
        finally
        {
            if (ReferenceEquals(_libraryRefreshCancellation, cancellation))
                _libraryRefreshCancellation = null;

            cancellation.Dispose();
        }
    }

    private void LibrarySearchInput_OnTextChanged(object? sender, Avalonia.Controls.TextChangedEventArgs e) => RefreshLibrary();

    private async void OpenLibraryButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (LibraryList.SelectedItem is not ScriptLibraryEntry entry)
            return;

        _documentLoadCancellation?.Cancel();
        using CancellationTokenSource cancellation = new();
        _documentLoadCancellation = cancellation;
        long loadVersion = ++_documentLoadVersion;

        try
        {
            if (!await ConfirmDocumentReplacementAsync("opening another script") || cancellation.IsCancellationRequested || loadVersion != _documentLoadVersion)
                return;

            long editVersion = _documentEditVersion;
            ScriptDocument document = await _fileService.OpenAsync(entry.FilePath, cancellation.Token);
            if (loadVersion != _documentLoadVersion || editVersion != _documentEditVersion)
                return;

            _document = document;
            SetEditorSource(document.Source);
            DocumentNameText.Text = document.Name;
            EditorStatusText.Text = $"Opened from library · {entry.FilePath}";
            SelectTab(1);
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            if (loadVersion == _documentLoadVersion)
                await ShowErrorAsync("Unable to open library script", exception.Message);
        }
        finally
        {
            if (ReferenceEquals(_documentLoadCancellation, cancellation))
                _documentLoadCancellation = null;
        }
    }

    private async void RemoveLibraryButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (LibraryList.SelectedItem is not ScriptLibraryEntry entry)
            return;

        try
        {
            await Task.Run(() => _libraryService.Remove(entry.FilePath));
            await LoadLibraryAsync();
        }
        catch (Exception exception)
        {
            await ShowErrorAsync("Unable to remove library script", exception.Message);
        }
    }


    private void SnippetSearchInput_OnTextChanged(object? sender, Avalonia.Controls.TextChangedEventArgs e) => RefreshSnippets();

    private void RefreshSnippets()
    {
        string filter = SnippetSearchInput?.Text?.Trim() ?? string.Empty;
        IEnumerable<ScriptSnippet> snippets = _snippetService.GetSnippets();

        if (!string.IsNullOrWhiteSpace(filter))
            snippets = snippets.Where(snippet =>
                snippet.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                snippet.Description.Contains(filter, StringComparison.OrdinalIgnoreCase));

        SnippetsList.ItemsSource = snippets.ToArray();
    }

    private void SnippetsList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (SnippetsList.SelectedItem is not ScriptSnippet snippet)
            return;

        SnippetDescriptionText.Text = snippet.Description;
        SnippetPreviewText.Text = snippet.Code;
    }

    private void InsertSnippetButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (SnippetsList.SelectedItem is not ScriptSnippet snippet)
            return;

        string source = EditorTextBox.Text ?? string.Empty;
        string updatedSource = string.IsNullOrWhiteSpace(source)
            ? snippet.Code
            : $"{source.TrimEnd()}{Environment.NewLine}{Environment.NewLine}{snippet.Code}";

        _document.Source = updatedSource;
        _document.IsDirty = true;
        SetEditorSource(updatedSource);
        DocumentNameText.Text = _document.Name + " *";
        EditorStatusText.Text = $"Inserted snippet · {snippet.Name}";
        SelectTab(1);
    }

    private void EditorTextBox_OnTextChanged(object? sender, Avalonia.Controls.TextChangedEventArgs e)
    {
        if (_isUpdatingEditor)
            return;

        _document.Source = EditorTextBox.Text ?? string.Empty;
        _document.IsDirty = true;
        _documentEditVersion++;
        InvalidateDiagnostics();
        DocumentNameText.Text = _document.Name + " *";
        EditorStatusText.Text = "Unsaved changes";
    }

    private void SetEditorSource(string source)
    {
        _documentEditVersion++;
        InvalidateDiagnostics();
        _isUpdatingEditor = true;
        try
        {
            EditorTextBox.Text = source;
        }
        finally
        {
            _isUpdatingEditor = false;
        }
    }

    private async void AnalyzeButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _diagnosticsCancellation?.Cancel();
        CancellationTokenSource cancellation = new();
        _diagnosticsCancellation = cancellation;
        string source = EditorTextBox.Text ?? string.Empty;
        DiagnosticsCountText.Text = "ANALYZING...";
        SelectTab(4);

        try
        {
            IReadOnlyList<ScriptDiagnostic> diagnostics = await _diagnosticsService.AnalyzeAsync(source, cancellation.Token);
            if (cancellation.IsCancellationRequested || !ReferenceEquals(_diagnosticsCancellation, cancellation))
                return;

            DiagnosticsList.ItemsSource = diagnostics;
            DiagnosticsCountText.Text = diagnostics.Count == 0
                ? "NO SYNTAX ISSUES FOUND"
                : $"{diagnostics.Count} ISSUE(S) FOUND";
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            if (!cancellation.IsCancellationRequested)
                await ShowErrorAsync("Unable to analyze script", exception.Message);
        }
        finally
        {
            if (ReferenceEquals(_diagnosticsCancellation, cancellation))
                _diagnosticsCancellation = null;

            cancellation.Dispose();
        }
    }

    private void InvalidateDiagnostics()
    {
        _diagnosticsCancellation?.Cancel();
        _diagnosticsCancellation = null;
        DiagnosticsList.ItemsSource = null;
        DiagnosticsCountText.Text = "SCRIPT CHANGED · RUN ANALYSIS AGAIN";
    }

    private void DiagnosticsList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (DiagnosticsList.SelectedItem is not ScriptDiagnostic diagnostic)
            return;

        string source = EditorTextBox.Text ?? string.Empty;
        int offset = 0;

        for (int line = 1; line < diagnostic.Line && offset < source.Length; line++)
        {
            int newlineIndex = source.IndexOf('\n', offset);
            if (newlineIndex < 0)
            {
                offset = source.Length;
                break;
            }

            offset = newlineIndex + 1;
        }

        EditorTextBox.CaretIndex = Math.Min(offset + diagnostic.Column - 1, source.Length);
        SelectTab(1);
        EditorTextBox.Focus();
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
