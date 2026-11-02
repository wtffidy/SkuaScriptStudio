using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Input;
using Avalonia.Platform.Storage;
using SkuaScriptStudio.Models;
using SkuaScriptStudio.Services;

namespace SkuaScriptStudio;

public partial class MainWindow : Window
{
    private readonly UpdateService _updateService = new();
    private readonly ScriptFileService _fileService = new();
    private readonly ScriptBuilderService _builderService = new();
    private readonly ScriptLibraryService _libraryService = new();
    private readonly ScriptDiagnosticsService _diagnosticsService = new();
    private readonly ScriptSnippetService _snippetService = new();
    private readonly List<ScriptBuilderStep> _builderSteps = [];
    private int _editingBuilderStepIndex = -1;
    private int _builderDragSourceIndex = -1;
    private Point _builderDragStart;
    private ScriptDocument _document = new();
    private string _savedSource = string.Empty;
    private bool _isUpdatingEditor;
    private long _documentLoadVersion;
    private long _documentEditVersion;
    private CancellationTokenSource? _documentLoadCancellation;
    private CancellationTokenSource? _diagnosticsCancellation;
    private CancellationTokenSource? _libraryRefreshCancellation;
    private CancellationTokenSource? _libraryImportCancellation;
    private CancellationTokenSource? _libraryRemoveCancellation;
    private ScriptLibraryEntry[] _libraryEntries = [];
    private long _libraryRefreshVersion;
    private bool _allowClose;
    private bool _isClosing;
    private bool _isConfirmingClose;
    private bool _isInstallingUpdate;

    public MainWindow()
    {
        InitializeComponent();
        _savedSource = _document.Source;
        UpdateBuilderActionFields();
        SnippetCoreFilter.ItemsSource = _snippetService.GetCores();
        SnippetCoreFilter.SelectedIndex = 0;
        SnippetCategoryFilter.ItemsSource = _snippetService.GetCategories();
        SnippetCategoryFilter.SelectedIndex = 0;
        SetEditorSource(_document.Source);
        RefreshSnippets();
        _ = LoadLibraryAsync();
    }

    private void TitleBar_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        if (e.Source is Button)
            return;

        if (e.GetCurrentPoint(this).Properties.IsLeftButtonPressed)
            BeginMoveDrag(e);
    }

    private void MinimizeWindow_OnClick(object? sender, RoutedEventArgs e) => WindowState = WindowState.Minimized;

    private void ToggleMaximizeWindow_OnClick(object? sender, RoutedEventArgs e) =>
        WindowState = WindowState == WindowState.Maximized ? WindowState.Normal : WindowState.Maximized;

    private void CloseWindow_OnClick(object? sender, RoutedEventArgs e) => Close();

    private async void UpdateButton_OnClick(object? sender, RoutedEventArgs e)
    {
        UpdateButton.IsEnabled = false;
        UpdateButton.Content = "Checking...";
        try
        {
            UpdateCheckResult result = await _updateService.CheckForUpdatesAsync();
            if (!result.IsUpdateAvailable)
            {
                UpdateButton.Content = "Up to date";
                EditorStatusText.Text = $"You're running the latest version · {result.CurrentVersion}";
                return;
            }

            UpdateButton.Content = $"Install v{result.LatestVersion}";
            bool install = await ShowUpdatePromptAsync(result);
            if (!install || !await ConfirmDocumentReplacementAsync("installing an update"))
                return;

            UpdateButton.Content = "Downloading...";
            await _updateService.InstallUpdateAsync(result);
            _isInstallingUpdate = true;
            _allowClose = true;
            Close();
        }
        catch (Exception exception)
        {
            await ShowErrorAsync("Unable to check for updates", exception.Message);
        }
        finally
        {
            if (!_isInstallingUpdate)
            {
                UpdateButton.IsEnabled = true;
                if (UpdateButton.Content?.ToString() is "Checking..." or "Downloading...")
                    UpdateButton.Content = "Check for updates";
            }
        }
    }

    private async Task<bool> ShowUpdatePromptAsync(UpdateCheckResult update)
    {
        Window dialog = new()
        {
            Title = "Update available",
            Width = 460,
            Height = 220,
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
                        Text = $"Version {update.LatestVersion} is available. You're currently running {update.CurrentVersion}. Install it now?",
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
                            new Button { Content = "Later", MinWidth = 82, Tag = false },
                            new Button { Content = "Install", MinWidth = 82, Tag = true }
                        }
                    }
                }
            }
        };

        if (dialog.Content is StackPanel panel && panel.Children.LastOrDefault() is StackPanel buttons)
        {
            foreach (Button button in buttons.Children.OfType<Button>())
                button.Click += (_, _) => dialog.Close((bool)button.Tag!);
        }

        return await dialog.ShowDialog<bool>(this);
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
            _savedSource = document.Source;
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

            string sourceSnapshot = document.Source;
            await _fileService.SaveAsync(document, path);
            if (ReferenceEquals(_document, document))
                _savedSource = sourceSnapshot;

            if (_isClosing || !ReferenceEquals(_document, document))
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
            _isClosing = true;
            _documentLoadCancellation?.Cancel();
            _libraryRefreshCancellation?.Cancel();
            _libraryImportCancellation?.Cancel();
            _libraryRemoveCancellation?.Cancel();
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

    private void BuilderStepsList_OnPointerPressed(object? sender, PointerPressedEventArgs e)
    {
        _builderDragSourceIndex = BuilderStepsList.SelectedIndex;
        _builderDragStart = e.GetPosition(BuilderStepsList);
    }

    private async void BuilderStepsList_OnPointerMoved(object? sender, PointerEventArgs e)
    {
        if (_builderDragSourceIndex < 0 || !e.GetCurrentPoint(BuilderStepsList).Properties.IsLeftButtonPressed)
            return;

        Point current = e.GetPosition(BuilderStepsList);
        if (Math.Abs(current.X - _builderDragStart.X) < 6 && Math.Abs(current.Y - _builderDragStart.Y) < 6)
            return;

        int sourceIndex = _builderDragSourceIndex;
        _builderDragSourceIndex = -1;
        DataTransfer data = new();
        data.Add(DataTransferItem.CreateText(sourceIndex.ToString(System.Globalization.CultureInfo.InvariantCulture)));
        await DragDrop.DoDragDropAsync(e, data, DragDropEffects.Move);
    }

    private void BuilderStepsList_OnDragOver(object? sender, DragEventArgs e)
    {
        e.DragEffects = int.TryParse(e.DataTransfer.TryGetText(), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out _) ? DragDropEffects.Move : DragDropEffects.None;
        e.Handled = true;
    }

    private void BuilderStepsList_OnDrop(object? sender, DragEventArgs e)
    {
        if (!int.TryParse(e.DataTransfer.TryGetText(), System.Globalization.NumberStyles.Integer, System.Globalization.CultureInfo.InvariantCulture, out int sourceIndex) || sourceIndex < 0 || sourceIndex >= _builderSteps.Count)
            return;

        Control? target = e.Source as Control;
        while (target is not null && target is not ListBoxItem)
            target = target.Parent as Control;

        int targetIndex = target is null ? _builderSteps.Count - 1 : BuilderStepsList.IndexFromContainer(target);
        if (targetIndex < 0 || targetIndex >= _builderSteps.Count || targetIndex == sourceIndex)
            return;

        ScriptBuilderStep step = _builderSteps[sourceIndex];
        _builderSteps.RemoveAt(sourceIndex);
        if (targetIndex > sourceIndex)
            targetIndex--;

        _builderSteps.Insert(targetIndex, step);
        CancelBuilderEdit();
        RefreshBuilderSteps();
        BuilderStepsList.SelectedIndex = targetIndex;
        BuilderStatusText.Text = $"Moved block to position {targetIndex + 1}.";
        e.Handled = true;
    }

    private void BuilderActionInput_OnSelectionChanged(object? sender, SelectionChangedEventArgs e) => UpdateBuilderActionFields();

    private void UpdateBuilderActionFields()
    {
        if (BuilderMapField is null || BuilderTargetField is null || BuilderItemField is null || BuilderQuantityField is null || BuilderValueField is null || BuilderClassField is null || BuilderActionHelpText is null || BuilderCellFields is null || BuilderEnabledInput is null || BuilderValueLabel is null || BuilderValueInput is null)
            return;

        string action = (BuilderActionInput?.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty;
        bool isHunt = action == "Hunt for an item";
        bool isGenericFarm = action is "Generic farming preset" or "Simple farming preset";
        bool isQuest = action is "Accept a quest" or "Complete a quest" or "Register quests";
        bool isEquip = action == "Equip a class";
        bool isTravel = action == "Travel to a map";
        bool isInventory = action == "Check inventory";
        bool isLog = action == "Log a message";
        bool isWait = action is "Wait" or "Wait for an item";
        bool isCell = action == "Move to a cell";
        bool isItemAction = action is "Wait for an item" or "Add drops" or "Remove drops" or "Bank items" or "Unbank items" or "Log farming progress" or "If item missing";
        bool isGear = action == "Equip gear";
        bool isAggro = action == "Configure aggro";

        BuilderMapField.IsVisible = isHunt || isTravel;
        BuilderTargetField.IsVisible = isHunt;
        BuilderItemField.IsVisible = isHunt || isInventory || isItemAction || isGenericFarm;
        BuilderHuntsField.IsVisible = isGenericFarm;
        BuilderTemporaryInput.IsVisible = isHunt || isGenericFarm;
        BuilderQuantityField.IsVisible = isHunt || isInventory || isWait || isGenericFarm || action is "Log farming progress" or "If item missing" or "Repeat N times";
        BuilderQuantityLabel.Text = action == "Repeat N times" ? "Repeat count" : isWait && action == "Wait" ? "Seconds" : "Item quantity";
        BuilderValueField.IsVisible = isQuest || isLog || isGear || isGenericFarm;
        BuilderValueLabel.Text = isGenericFarm || action == "Register quests" ? "Quest IDs (comma separated, optional for simple farming)" : isGear ? "Gear item name(s), comma separated" : isQuest ? "Quest ID" : "Message";
        BuilderValueInput.Watermark = isGear ? "e.g. Blade of Awe, Cape of Awe" : isGenericFarm || action == "Register quests" ? "e.g. 12345, 12346 (optional for simple farming)" : isQuest ? "e.g. 12345" : "Message to show in the bot log";
        BuilderClassField.IsVisible = isEquip;
        BuilderCellFields.IsVisible = isCell;
        BuilderEnabledInput.IsVisible = isAggro;
        BuilderActionHelpText.Text = action switch
        {
            "Generic farming preset" => "Check the item, return if already owned, log progress, register quests, then hunt until the target quantity is reached.",
            "Simple farming preset" => "Same inventory-aware farming loop without requiring quest IDs. Add one hunt per line as map|mob|item|quant|isTemp (isTemp optional).",
            "Hunt for an item" => "Tell us where to go, which monster to fight, and which item to collect.",
            "Accept a quest" or "Complete a quest" => "Enter the numeric quest ID. You do not need to write any code.",
            "Register quests" => "Register one or more quest IDs. Separate multiple IDs with commas.",
            "Cancel registered quests" => "Clear the currently registered quest list.",
            "Equip a class" => "Choose the class the bot should equip before continuing.",
            "Travel to a map" => "Enter the map name the bot should travel to.",
            "Check inventory" => "Enter the item and minimum quantity the bot should check for.",
            "Log a message" => "Enter the message you want shown in the bot log.",
            "Wait" => "Choose how many seconds the bot should pause before the next step.",
            "Wait for an item" => "The script safely waits until you own the requested amount, or until you stop the bot.",
            "Equip gear" => "Enter one or more item names, separated by commas.",
            "Move to a cell" => "Choose the room cell and pad to move to.",
            "Add drops" => "Add an item to the drop list so the bot collects it.",
            "Remove drops" => "Remove an item from the drop list.",
            "Bank items" => "Move the named item from your inventory into the bank.",
            "Unbank items" => "Retrieve the named item from your bank.",
            "Log farming progress" => "Log the item name and target quantity using the farming logger.",
            "Rest" => "Restore health and mana before the next action.",
            "Configure aggro" => "Choose whether the script should handle monster aggro.",
            "If item missing" => "Opens an IF block. The blocks you add next run only when you do not own the requested quantity. Close it with End block.",
            "Repeat N times" => "Repeats the blocks inside this section the number of times entered. It also stops when the bot is stopped. Close it with End repeat.",
            "End block" => "Closes the most recently opened IF block.",
            "End repeat" => "Closes the most recently opened REPEAT block.",
            _ => "Choose a block to see exactly what information it needs."
        };
    }

    private void EditBuilderStepButton_OnClick(object? sender, RoutedEventArgs e)
    {
        int index = BuilderStepsList.SelectedIndex;
        if (index < 0 || index >= _builderSteps.Count)
        {
            BuilderStatusText.Text = "Select a step first, then choose Edit selected.";
            return;
        }

        ScriptBuilderStep step = _builderSteps[index];
        _editingBuilderStepIndex = index;
        BuilderActionInput.SelectedItem = BuilderActionInput.Items
            .OfType<ComboBoxItem>()
            .FirstOrDefault(item => string.Equals(item.Content?.ToString(), step.Action, StringComparison.Ordinal));
        UpdateBuilderActionFields();
        BuilderMapInput.Text = step.Map;
        BuilderTargetInput.Text = step.Target;
        BuilderItemInput.Text = step.Item;
        BuilderHuntsInput.Text = step.Hunts;
        BuilderTemporaryInput.IsChecked = step.IsTemporary;
        BuilderValueInput.Text = step.Value;
        BuilderQuantityInput.Value = step.Quantity;
        BuilderCellInput.Text = step.Cell;
        BuilderPadInput.Text = step.Pad;
        BuilderEnabledInput.IsChecked = step.Enabled;
        BuilderClassInput.SelectedItem = BuilderClassInput.Items
            .OfType<ComboBoxItem>()
            .FirstOrDefault(item => string.Equals(item.Content?.ToString(), step.ClassType, StringComparison.Ordinal));
        AddBuilderStepButton.Content = "Update selected step";
        BuilderStatusText.Text = $"Editing step {index + 1}. Update it when you're done.";
    }

    private void CancelBuilderEdit()
    {
        _editingBuilderStepIndex = -1;
        AddBuilderStepButton.Content = "＋ Add this step";
    }

    private async void AddBuilderStepButton_OnClick(object? sender, RoutedEventArgs e)
    {
        string action = (BuilderActionInput.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? string.Empty;
        string map = BuilderMapInput.Text?.Trim() ?? string.Empty;
        string target = BuilderTargetInput.Text?.Trim() ?? string.Empty;
        string item = BuilderItemInput.Text?.Trim() ?? string.Empty;
        string value = BuilderValueInput.Text?.Trim() ?? string.Empty;
        int quantity = decimal.ToInt32(BuilderQuantityInput.Value ?? 1);
        string classType = (BuilderClassInput.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Farm";
        string cell = BuilderCellInput.Text?.Trim() ?? string.Empty;
        string pad = BuilderPadInput.Text?.Trim() ?? string.Empty;

        ScriptBuilderStep step = new()
        {
            Action = action,
            Map = map,
            Target = target,
            Item = item,
            Hunts = BuilderHuntsInput.Text?.Trim() ?? string.Empty,
            IsTemporary = BuilderTemporaryInput.IsChecked ?? true,
            Value = value,
            Quantity = quantity,
            ClassType = classType,
            Cell = cell,
            Pad = pad,
            Enabled = BuilderEnabledInput.IsChecked ?? true
        };

        try
        {
            if (action is "Generic farming preset" or "Simple farming preset" && (string.IsNullOrWhiteSpace(step.Item) || string.IsNullOrWhiteSpace(step.Hunts) || (action == "Generic farming preset" && string.IsNullOrWhiteSpace(step.Value))))
                throw new ArgumentException(action == "Generic farming preset" ? "The quest farming preset requires an item, quantity, quest IDs, and at least one hunt." : "The simple farming preset requires an item, quantity, and at least one hunt.");
            if (action == "If item missing")
            {
                if (string.IsNullOrWhiteSpace(step.Item))
                    throw new ArgumentException($"The block '{action}' requires an item name.");
            }
            else if (action is not "End block" and not "End repeat")
                _ = _builderService.GenerateScript([step]);

            bool wasEditing = _editingBuilderStepIndex >= 0 && _editingBuilderStepIndex < _builderSteps.Count;
            int editedIndex = _editingBuilderStepIndex;
            if (wasEditing)
            {
                _builderSteps[editedIndex] = step;
                CancelBuilderEdit();
            }
            else
                _builderSteps.Add(step);

            RefreshBuilderSteps();
            if (wasEditing)
                BuilderStepsList.SelectedIndex = editedIndex;

            BuilderMapInput.Text = string.Empty;
            BuilderTargetInput.Text = string.Empty;
            BuilderItemInput.Text = string.Empty;
            BuilderHuntsInput.Text = string.Empty;
            BuilderTemporaryInput.IsChecked = true;
            BuilderValueInput.Text = string.Empty;
            BuilderQuantityInput.Value = 1;
            BuilderCellInput.Text = "Enter";
            BuilderPadInput.Text = "Spawn";
            BuilderStatusText.Text = wasEditing
                ? $"Updated step {editedIndex + 1}. Your script has {_builderSteps.Count} step(s)."
                : $"Added step {_builderSteps.Count}. Add more actions to build the full routine.";
        }
        catch (ArgumentException exception)
        {
            await ShowErrorAsync("Check this step", exception.Message);
        }
    }

    private void RemoveBuilderStepButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (BuilderStepsList.SelectedItem is not ScriptBuilderStep step)
            return;

        _builderSteps.Remove(step);
        CancelBuilderEdit();
        RefreshBuilderSteps();
        BuilderStatusText.Text = "Step removed.";
    }

    private void MoveBuilderStepUpButton_OnClick(object? sender, RoutedEventArgs e) => MoveBuilderStep(-1);

    private void MoveBuilderStepDownButton_OnClick(object? sender, RoutedEventArgs e) => MoveBuilderStep(1);

    private void MoveBuilderStep(int offset)
    {
        int index = BuilderStepsList.SelectedIndex;
        int newIndex = index + offset;
        if (index < 0 || newIndex < 0 || newIndex >= _builderSteps.Count)
            return;

        CancelBuilderEdit();
        ScriptBuilderStep step = _builderSteps[index];
        _builderSteps.RemoveAt(index);
        _builderSteps.Insert(newIndex, step);
        RefreshBuilderSteps();
        BuilderStepsList.SelectedIndex = newIndex;
    }

    private void RefreshBuilderSteps()
    {
        BuilderStepsList.ItemsSource = null;
        BuilderStepsList.ItemsSource = _builderSteps.ToArray();
        BuilderStepCountText.Text = $"{_builderSteps.Count} STEP(S)";
        BuildScriptButton.IsEnabled = _builderSteps.Count > 0;
        ClearBuilderButton.IsEnabled = _builderSteps.Count > 0;
    }

    private void ClearBuilderButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _builderSteps.Clear();
        CancelBuilderEdit();
        RefreshBuilderSteps();
        BuilderStatusText.Text = "Add action blocks and wrap them in IF / END IF or REPEAT / END REPEAT blocks.";
    }

    private async void BuildScriptButton_OnClick(object? sender, RoutedEventArgs e)
    {
        try
        {
            string generated = _builderService.GenerateScript(_builderSteps);
            if (!await ConfirmDocumentReplacementAsync("creating a script from the visual builder"))
                return;

            _documentLoadCancellation?.Cancel();
            ++_documentLoadVersion;
            _document = new ScriptDocument
            {
                Name = "MySkuaScript.cs",
                Source = generated,
                IsDirty = true
            };
            _savedSource = string.Empty;

            SetEditorSource(generated);
            DocumentNameText.Text = _document.Name + " *";
            EditorStatusText.Text = $"Built a complete script from {_builderSteps.Count} step(s) · Review and save";
            SelectTab(1);
        }
        catch (ArgumentException exception)
        {
            await ShowErrorAsync("Your script needs a little more work", exception.Message);
        }
    }


    private async void ImportToLibraryButton_OnClick(object? sender, RoutedEventArgs e)
    {
        _libraryImportCancellation?.Cancel();
        using CancellationTokenSource cancellation = new();
        _libraryImportCancellation = cancellation;

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
                cancellation.Token.ThrowIfCancellationRequested();
                string? path = file.TryGetLocalPath();
                if (string.IsNullOrWhiteSpace(path))
                    continue;

                await _libraryService.ImportAsync(path, cancellation.Token);
                importedCount++;
            }

            if (_isClosing || cancellation.IsCancellationRequested)
                return;

            await LoadLibraryAsync();
            if (!_isClosing)
                EditorStatusText.Text = $"Library updated · {importedCount} file(s) imported";
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            if (!_isClosing)
                await ShowErrorAsync("Unable to import scripts", exception.Message);
        }
        finally
        {
            if (ReferenceEquals(_libraryImportCancellation, cancellation))
                _libraryImportCancellation = null;
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
            if (_isClosing || cancellation.IsCancellationRequested || refreshVersion != _libraryRefreshVersion)
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

        _libraryRemoveCancellation?.Cancel();
        using CancellationTokenSource cancellation = new();
        _libraryRemoveCancellation = cancellation;

        try
        {
            await Task.Run(() => _libraryService.Remove(entry.FilePath), cancellation.Token);
            if (!_isClosing && !cancellation.IsCancellationRequested)
                await LoadLibraryAsync();
        }
        catch (OperationCanceledException) when (cancellation.IsCancellationRequested)
        {
        }
        catch (Exception exception)
        {
            if (!_isClosing)
                await ShowErrorAsync("Unable to remove library script", exception.Message);
        }
        finally
        {
            if (ReferenceEquals(_libraryRemoveCancellation, cancellation))
                _libraryRemoveCancellation = null;
        }
    }


    private void SnippetSearchInput_OnTextChanged(object? sender, Avalonia.Controls.TextChangedEventArgs e) => RefreshSnippets();

    private void SnippetFilter_OnSelectionChanged(object? sender, SelectionChangedEventArgs e) => RefreshSnippets();

    private void RefreshSnippets()
    {
        if (SnippetsList is null)
            return;

        string filter = SnippetSearchInput?.Text?.Trim() ?? string.Empty;
        string core = SnippetCoreFilter?.SelectedItem?.ToString() ?? "All cores";
        string category = SnippetCategoryFilter?.SelectedItem?.ToString() ?? "All categories";
        IEnumerable<ScriptSnippet> snippets = _snippetService.GetSnippets();

        if (!string.Equals(core, "All cores", StringComparison.OrdinalIgnoreCase))
            snippets = snippets.Where(snippet => string.Equals(snippet.Core, core, StringComparison.OrdinalIgnoreCase));

        if (!string.Equals(category, "All categories", StringComparison.OrdinalIgnoreCase))
            snippets = snippets.Where(snippet => string.Equals(snippet.Category, category, StringComparison.OrdinalIgnoreCase));

        if (!string.IsNullOrWhiteSpace(filter))
            snippets = snippets.Where(snippet =>
                snippet.Name.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                snippet.Description.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                snippet.Code.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                snippet.Core.Contains(filter, StringComparison.OrdinalIgnoreCase) ||
                snippet.Category.Contains(filter, StringComparison.OrdinalIgnoreCase));

        ScriptSnippet[] results = snippets.ToArray();
        SnippetsList.ItemsSource = results;
        SnippetCountText.Text = $"{results.Length} / {_snippetService.GetSnippets().Count} SNIPPETS";
    }

    private void SnippetsList_OnSelectionChanged(object? sender, SelectionChangedEventArgs e)
    {
        if (SnippetsList.SelectedItem is not ScriptSnippet snippet)
            return;

        string includes = string.Join(", ", snippet.RequiredIncludes);
        SnippetDescriptionText.Text = $"{snippet.Core} · {snippet.Category}{Environment.NewLine}{snippet.Description}{Environment.NewLine}Required includes: {includes}";
        SnippetPreviewText.Text = snippet.Code;
    }

    private void InsertSnippetButton_OnClick(object? sender, RoutedEventArgs e)
    {
        if (SnippetsList.SelectedItem is not ScriptSnippet snippet)
            return;

        string source = EditorTextBox.Text ?? string.Empty;
        int originalCaretIndex = Math.Clamp(EditorTextBox.CaretIndex, 0, source.Length);
        string sourceWithIncludes = AddRequiredIncludes(source, snippet.RequiredIncludes);
        int includeLength = sourceWithIncludes.Length - source.Length;
        int insertionIndex = Math.Clamp(originalCaretIndex + includeLength, 0, sourceWithIncludes.Length);
        string before = sourceWithIncludes[..insertionIndex];
        string after = sourceWithIncludes[insertionIndex..];
        string separatorBefore = before.Length > 0 && !before.EndsWith(Environment.NewLine, StringComparison.Ordinal) ? Environment.NewLine : string.Empty;
        string separatorAfter = after.Length > 0 && !after.StartsWith(Environment.NewLine, StringComparison.Ordinal) ? Environment.NewLine : string.Empty;
        string insertedCode = $"{separatorBefore}{snippet.Code}{separatorAfter}";
        string updatedSource = $"{before}{insertedCode}{after}";
        int newCaretIndex = insertionIndex + insertedCode.Length;

        _document.Source = updatedSource;
        _document.IsDirty = !string.Equals(updatedSource, _savedSource, StringComparison.Ordinal);
        SetEditorSource(updatedSource);
        EditorTextBox.CaretIndex = Math.Min(newCaretIndex, updatedSource.Length);
        DocumentNameText.Text = _document.Name + (_document.IsDirty ? " *" : string.Empty);
        EditorStatusText.Text = _document.IsDirty ? $"Inserted {snippet.Core} snippet · {snippet.Name}" : "All changes saved";
        SelectTab(1);
    }

    private static string AddRequiredIncludes(string source, IReadOnlyList<string> includes)
    {
        List<string> missingIncludes = includes
            .Select(include => $"//cs_include {include}")
            .Where(directive => !source.Split(['\r', '\n'], StringSplitOptions.RemoveEmptyEntries)
                .Any(line => string.Equals(line.Trim(), directive, StringComparison.OrdinalIgnoreCase)))
            .ToList();

        if (missingIncludes.Count == 0)
            return source;

        int insertionIndex = 0;
        string trimmedSource = source.TrimStart();
        if (trimmedSource.StartsWith("/*", StringComparison.Ordinal))
        {
            int metadataEnd = source.IndexOf("*/", StringComparison.Ordinal);
            if (metadataEnd >= 0 && metadataEnd < 1000)
                insertionIndex = metadataEnd + 2;
        }

        string prefix = insertionIndex > 0 ? Environment.NewLine : string.Empty;
        string directives = string.Join(Environment.NewLine, missingIncludes);
        return source.Insert(insertionIndex, $"{prefix}{directives}{Environment.NewLine}");
    }

    private void EditorTextBox_OnTextChanged(object? sender, Avalonia.Controls.TextChangedEventArgs e)
    {
        if (_isUpdatingEditor)
            return;

        _document.Source = EditorTextBox.Text ?? string.Empty;
        _document.IsDirty = !string.Equals(_document.Source, _savedSource, StringComparison.Ordinal);
        _documentEditVersion++;
        InvalidateDiagnostics();
        DocumentNameText.Text = _document.Name + (_document.IsDirty ? " *" : string.Empty);
        EditorStatusText.Text = _document.IsDirty ? "Unsaved changes" : "All changes saved";
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
