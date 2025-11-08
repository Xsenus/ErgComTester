using ErgData;
using MicroluxErgConnect.Infrastructure;
using MicroluxErgConnect.Models;
using MicroluxErgConnect.Utils;
using MicroluxErgConnect.ViewModels;
using System.Collections.Generic;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq;
using System.Text;

namespace MicroluxErgConnect.Views
{
    public partial class MainForm : Form
    {
        private sealed class HeaderInputState
        {
            public HeaderInputState(TextBox textBox, string placeholder)
            {
                TextBox = textBox;
                Placeholder = placeholder;
            }

            public TextBox TextBox { get; }
            public string Placeholder { get; }
            public bool ShowingPlaceholder { get; set; }
        }

        private readonly EventHandler _checkUpdatesCanExecuteHandler;
        private readonly EventHandler _installUpdateCanExecuteHandler;
        private readonly List<HeaderInputState> _headerInputs;
        private readonly Color _headerPlaceholderColor = Color.FromArgb(160, 160, 160);
        private readonly Color _headerTextColor;
        private bool _isApplyingHeaderInputs;
        private bool _isExitRequested;
        private readonly BindingList<LogEntry> _logEntries = new();
        private readonly MainViewModel _viewModel;

        public MainForm()
        {
            _viewModel = AppServices.MainViewModel;
            InitializeComponent();
            DoubleBuffered = true;
            SetStyle(ControlStyles.OptimizedDoubleBuffer | ControlStyles.AllPaintingInWmPaint, true);
            UpdateStyles();
            EnsureLogGridConfigured();
            reportTemplateComboBox.DisplayMember = nameof(ReportTemplateOption.Description);
            reportTemplateComboBox.ValueMember = nameof(ReportTemplateOption.Value);
            reportTemplateComboBox.DataSource = new[]
            {
            new ReportTemplateOption(ReportTemplate.Classic, "Классический"),
            new ReportTemplateOption(ReportTemplate.Client, "Шаблон клиента")
        };
            renderingModeComboBox.DisplayMember = nameof(ReportRenderingModeOption.Description);
            renderingModeComboBox.ValueMember = nameof(ReportRenderingModeOption.Value);
            renderingModeComboBox.DataSource = new[]
            {
            new ReportRenderingModeOption(ReportRenderingMode.Automatic, "Автоматически (по версии Windows)"),
            new ReportRenderingModeOption(ReportRenderingMode.Modern, "Современный движок (QuestPDF)"),
            new ReportRenderingModeOption(ReportRenderingMode.Legacy, "Совместимый режим (Windows 7)")
        };
            logsBindingSource.DataSource = _logEntries;
            _viewModel.PropertyChanged += ViewModelOnPropertyChanged;
            _viewModel.Logs.CollectionChanged += LogsOnCollectionChanged;
            _checkUpdatesCanExecuteHandler = (_, _) => UpdateCommandState();
            _installUpdateCanExecuteHandler = (_, _) => UpdateCommandState();
            _viewModel.CheckUpdatesCommand.CanExecuteChanged += _checkUpdatesCanExecuteHandler;
            _viewModel.InstallUpdateCommand.CanExecuteChanged += _installUpdateCanExecuteHandler;
            AppServices.ExitRequested += OnExitRequested;
            UpdateAll();
            UpdateCommandState();

            var version = typeof(MainForm).Assembly.GetName().Version?.ToString() ?? "1.0.0.0";
            versionStatusLabel.Text = $"Версия: {version}";
            AppServices.Log.Info("Главное окно инициализировано.");

            _headerTextColor = textBoxCaption1.ForeColor;
            _headerInputs = new List<HeaderInputState>
            {
                new(textBoxCaption1, "Медицинская организация \"Microlux\""),
                new(textBoxCaption2, "Адрес: г. Москва, ул. Пример, д. 1"),
                new(textBoxCaption3, "Тел.: +7 (000) 000-00-00"),
                new(textBoxCaption4, "E-mail: clinic@example.com")
            };

            foreach (var input in _headerInputs)
            {
                input.TextBox.TextChanged += OnHeaderLineValidated;
                input.TextBox.Enter += OnHeaderInputEnter;
                input.TextBox.Leave += OnHeaderInputLeave;
            }

            RefreshHeaderPlaceholderState();
        }


        private void ApplyHeaderToInputs()
        {
            _isApplyingHeaderInputs = true;
            try
            {
                var lines = ReportHeaderFormatter.Split(_viewModel.ReportHeader);
                for (var i = 0; i < _headerInputs.Count; i++)
                {
                    var line = i < lines.Length ? lines[i] : string.Empty;
                    UpdateHeaderInput(_headerInputs[i], line, isPlaceholder: false);
                }
            }
            finally
            {
                _isApplyingHeaderInputs = false;
            }

            RefreshHeaderPlaceholderState();
        }

        private bool TryGetHeaderInput(TextBox textBox, out HeaderInputState? state)
        {
            state = _headerInputs.FirstOrDefault(input => input.TextBox == textBox);
            return state is not null;
        }

        private void UpdateHeaderInput(HeaderInputState state, string text, bool isPlaceholder)
        {
            var shouldGuard = !_isApplyingHeaderInputs;
            if (shouldGuard)
            {
                _isApplyingHeaderInputs = true;
            }

            try
            {
                state.ShowingPlaceholder = isPlaceholder;
                state.TextBox.ForeColor = isPlaceholder ? _headerPlaceholderColor : _headerTextColor;
                if (!string.Equals(state.TextBox.Text, text, StringComparison.Ordinal))
                {
                    state.TextBox.Text = text;
                }
            }
            finally
            {
                if (shouldGuard)
                {
                    _isApplyingHeaderInputs = false;
                }
            }
        }

        private void RefreshHeaderPlaceholderState()
        {
            if (_headerInputs.Count == 0)
            {
                return;
            }

            var hasUserInput = _headerInputs.Any(state =>
                !state.ShowingPlaceholder && !string.IsNullOrWhiteSpace(state.TextBox.Text));

            foreach (var state in _headerInputs)
            {
                if (hasUserInput)
                {
                    if (state.ShowingPlaceholder)
                    {
                        UpdateHeaderInput(state, string.Empty, isPlaceholder: false);
                    }
                }
                else
                {
                    var text = state.TextBox.Text ?? string.Empty;
                    if (state.ShowingPlaceholder && string.Equals(text, state.Placeholder, StringComparison.Ordinal))
                    {
                        continue;
                    }

                    if (string.IsNullOrWhiteSpace(text))
                    {
                        UpdateHeaderInput(state, state.Placeholder, isPlaceholder: true);
                    }
                }
            }
        }

        private void OnHeaderInputEnter(object? sender, EventArgs e)
        {
            if (sender is not TextBox textBox)
            {
                return;
            }

            if (TryGetHeaderInput(textBox, out var state) && state != null && state.ShowingPlaceholder)
            {
                UpdateHeaderInput(state, string.Empty, isPlaceholder: false);
            }
        }

        private void OnHeaderInputLeave(object? sender, EventArgs e)
        {
            if (_isApplyingHeaderInputs)
            {
                return;
            }

            RefreshHeaderPlaceholderState();
        }

        private async Task ApplySettingsAsync()
        {
            ToggleControls(false);
            try
            {
                var (success, error) = await _viewModel.TryUpdateReportsDirectoryAsync(labelPath.Text);
                if (!success)
                {
                    var message = string.IsNullOrWhiteSpace(error) ? "Не удалось обновить каталог отчетов." : error;
                    MessageBox.Show(this, message, "Ошибка", MessageBoxButtons.OK, MessageBoxIcon.Error);
                    return;
                }
            }
            finally
            {
                ToggleControls(true);
            }
        }

        private void ApplySettingsToInputs()
        {
            scanIntervalTextBox.Text = _viewModel.DeviceScanIntervalSeconds.ToString();
            reconnectDelayTextBox.Text = _viewModel.DeviceReconnectDelaySeconds.ToString();
            backgroundSyncTextBox.Text = _viewModel.BackgroundSyncIntervalMinutes.ToString();
            updateIntervalTextBox.Text = _viewModel.UpdateCheckIntervalMinutes.ToString();
            manifestUrlTextBox.Text = _viewModel.UpdateManifestUrl;
            reportTemplateComboBox.SelectedValue = _viewModel.ReportTemplate;
            renderingModeComboBox.SelectedValue = _viewModel.ReportRenderingMode;
            ApplyHeaderToInputs();
        }

        private void ApplyStatusVisualState(string? statusText, Label label, bool isBadge)
        {
            var color = ResolveStatusColor(statusText);
            if (isBadge)
            {
                label.BackColor = color;
                label.ForeColor = Color.White;
            }
            else
            {
                label.ForeColor = color;
            }
        }

        private async void btnGraphTuner_Click(object sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Выберите файл пациента (.bin)",
                Filter = "Файлы пациента (*.bin)|*.bin|Все файлы (*.*)|*.*",
                Multiselect = false
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
                return;

            var previousCursor = Cursor;
            Cursor = Cursors.WaitCursor;

            try
            {
                // Разбираем .bin с использованием вашей уже существующей логики
                var result = await _viewModel.ConvertRawFileAsync(dialog.FileName);

                if (!result.Success || result.Patient is null)
                {
                    MessageBox.Show(
                        this,
                        string.IsNullOrWhiteSpace(result.ErrorMessage)
                            ? "Не удалось прочитать файл пациента."
                            : result.ErrorMessage!,
                        "Microlux ERG-Connect",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                    return;
                }

                var test = result.Patient.Tests?.FirstOrDefault();
                if (test is null)
                {
                    MessageBox.Show(this, "В файле нет тестов с данными.", "Microlux ERG-Connect",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var eye = HasGraphs(test.RightEye) ? test.RightEye
                 : HasGraphs(test.LeftEye) ? test.LeftEye
                 : test.RightEye ?? test.LeftEye;

                if (eye is null || !HasGraphs(eye))
                {
                    MessageBox.Show(this, "В тесте нет валидных данных графиков.", "Microlux ERG-Connect",
                        MessageBoxButtons.OK, MessageBoxIcon.Warning);
                    return;
                }

                var tuner = new GraphTunerForm(test, eye)
                {
                    StartPosition = FormStartPosition.CenterParent
                };
                tuner.Show(this);
            }
            catch (OperationCanceledException)
            {
                AppServices.Log.Warn("Чтение .bin отменено пользователем.");
            }
            catch (Exception ex)
            {
                AppServices.Log.Error($"Ошибка при чтении .bin: {ex}");
                MessageBox.Show(this, $"Ошибка при обработке файла: {ex.Message}",
                    "Microlux ERG-Connect", MessageBoxButtons.OK, MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = previousCursor;
            }
        }

        private static string BuildManualConversionErrorBlock(ManualConversionResult result)
        {
            var builder = new StringBuilder();
            var title = !string.IsNullOrWhiteSpace(result.RawPath)
                ? Path.GetFileName(result.RawPath)
                : "Файл";
            builder.AppendLine(title + ":");

            var reason = string.IsNullOrWhiteSpace(result.ErrorMessage)
                ? "Неизвестная ошибка."
                : result.ErrorMessage!;
            builder.AppendLine($"  Причина: {reason}");

            if (!string.IsNullOrWhiteSpace(result.RawPath))
            {
                builder.AppendLine($"  Файл: {result.RawPath}");
            }

            if (!string.IsNullOrWhiteSpace(result.JsonPath))
            {
                builder.AppendLine($"  JSON: {result.JsonPath}");
            }

            return builder.ToString().TrimEnd();
        }

        private static string BuildManualConversionSuccessBlock(ManualConversionResult result)
        {
            var builder = new StringBuilder();
            var title = !string.IsNullOrWhiteSpace(result.RawPath)
                ? Path.GetFileName(result.RawPath)
                : "Файл";
            builder.AppendLine(title + ":");

            var detailsAdded = false;

            if (!string.IsNullOrWhiteSpace(result.RawPath))
            {
                builder.AppendLine($"  Исходный файл: {result.RawPath}");
                detailsAdded = true;
            }

            if (!string.IsNullOrWhiteSpace(result.JsonPath))
            {
                builder.AppendLine($"  JSON: {result.JsonPath}");
                detailsAdded = true;
            }

            if (!string.IsNullOrWhiteSpace(result.PdfPath))
            {
                builder.AppendLine($"  PDF: {result.PdfPath}");
                detailsAdded = true;
            }

            if (!string.IsNullOrWhiteSpace(result.DocxPath))
            {
                builder.AppendLine($"  Word: {result.DocxPath}");
                detailsAdded = true;
            }

            if (!detailsAdded)
            {
                builder.AppendLine("  Файлы отчета сохранены.");
            }

            if (result.Patient?.Warnings is { Count: > 0 } warnings)
            {
                builder.AppendLine("  Предупреждения:");
                foreach (var warning in warnings.Take(3))
                {
                    builder.AppendLine($"    • {warning}");
                }

                if (warnings.Count > 3)
                {
                    builder.AppendLine($"    … и ещё {warnings.Count - 3}");
                }
            }

            return builder.ToString().TrimEnd();
        }

        private async void buttonSetPathFolder_Click(object sender, EventArgs e)
        {
            using var dialog = new FolderBrowserDialog
            {
                SelectedPath = ResolveInitialFolder() ?? Application.StartupPath,
                Description = "Выберите папку для сохранения PDF-отчетов"
            };

            if (dialog.ShowDialog(this) == DialogResult.OK)
            {
                labelPath.Text = dialog.SelectedPath;
                await ApplySettingsAsync();
            }
        }

        private void EnsureLogGridConfigured()
        {
            if (logGridView.AutoGenerateColumns)
            {
                logGridView.AutoGenerateColumns = false;
            }

            timestampColumn = EnsureTextColumn(
                timestampColumn,
                nameof(timestampColumn),
                nameof(LogEntry.Timestamp),
                "Время",
                DataGridViewAutoSizeColumnMode.None,
                180,
                "yyyy-MM-dd HH:mm:ss");

            levelColumn = EnsureTextColumn(
                levelColumn,
                nameof(levelColumn),
                nameof(LogEntry.Level),
                "Уровень",
                DataGridViewAutoSizeColumnMode.None,
                80);

            messageColumn = EnsureTextColumn(
                messageColumn,
                nameof(messageColumn),
                nameof(LogEntry.Message),
                "Сообщение",
                DataGridViewAutoSizeColumnMode.Fill,
                100);

            if (logGridView.DataSource != logsBindingSource)
            {
                logGridView.DataSource = logsBindingSource;
            }
        }

        private DataGridViewTextBoxColumn EnsureTextColumn(
            DataGridViewTextBoxColumn? column,
            string name,
            string dataProperty,
            string headerText,
            DataGridViewAutoSizeColumnMode sizeMode,
            int minimumWidth,
            string? format = null)
        {
            column ??= new DataGridViewTextBoxColumn();

            var existing = logGridView.Columns[name] as DataGridViewTextBoxColumn;

            if (existing is not null)
            {
                column = existing;
            }
            else
            {
                if (column.DataGridView is not null && column.DataGridView != logGridView)
                {
                    column = new DataGridViewTextBoxColumn();
                }

                column.Name = name;
                logGridView.Columns.Add(column);
            }

            column.Name = name;
            column.DataPropertyName = dataProperty;
            column.HeaderText = headerText;
            column.AutoSizeMode = sizeMode;
            column.MinimumWidth = minimumWidth;
            column.ReadOnly = true;

            if (sizeMode != DataGridViewAutoSizeColumnMode.Fill)
            {
                column.Width = minimumWidth;
            }

            if (!string.IsNullOrWhiteSpace(format))
            {
                column.DefaultCellStyle ??= new DataGridViewCellStyle();
                column.DefaultCellStyle.Format = format;
            }

            return column;
        }

        private void ExecuteSafeAsync(RelayCommand command, string actionDescription)
        {
            if (!command.CanExecute(null))
            {
                AppServices.Log.Warn($"Команда на {actionDescription} в данный момент недоступна.");
                return;
            }

            try
            {
                command.Execute(null);
                AppServices.Log.Info($"Команда на {actionDescription} запущена пользователем.");
            }
            catch (Exception ex)
            {
                AppServices.Log.Error($"Ошибка при выполнении команды ({actionDescription}): {ex}");
                MessageBox.Show(
                    $"Ошибка при выполнении действия: {ex.Message}",
                    "Microlux ERG-Connect",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
        }

        private void ExitApplication()
        {
            _isExitRequested = true;
            AppServices.Log.Info("Пользователь запросил завершение приложения из трея.");
            trayIcon.Visible = false;
            Close();
        }

        private void ForceCommandStateLogging()
        {
            AppServices.Log.Debug($"Доступность команд: проверка обновлений={(checkUpdatesButton.Enabled ? "доступна" : "недоступна")}, установка={(installUpdateButton.Enabled ? "доступна" : "недоступна")}");
        }

        private static string FormatSingleLine(string? value, string placeholder = "-")
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return placeholder;
            }

            var parts = value
                .Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries)
                .Select(part => part.Trim())
                .Where(part => !string.IsNullOrWhiteSpace(part));

            var result = string.Join(" ", parts);
            return string.IsNullOrWhiteSpace(result) ? placeholder : result;
        }

        private static bool HasGraphs(EyeData? e)
        {
            return e?.GraphSamples is { Length: > 0 } arr && arr.Any(s => s is { Length: > 1 });
        }

        private void LogsOnCollectionChanged(object? sender, NotifyCollectionChangedEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new NotifyCollectionChangedEventHandler(LogsOnCollectionChanged), sender, e);
                return;
            }

            EnsureLogGridConfigured();

            if (e.Action == NotifyCollectionChangedAction.Reset)
            {
                _logEntries.Clear();
            }

            if (e.OldItems is { Count: > 0 })
            {
                foreach (var item in e.OldItems.Cast<LogEntry>())
                {
                    _logEntries.Remove(item);
                }
            }

            if (e.NewItems is { Count: > 0 })
            {
                foreach (var item in e.NewItems.Cast<LogEntry>())
                {
                    _logEntries.Add(item);
                }
                ScrollLogsToEnd();
            }
        }

        private void OnBackgroundSyncValidated(object? sender, EventArgs e)
        {
            if (TryParseTextBoxValue(backgroundSyncTextBox, out var value))
            {
                _viewModel.BackgroundSyncIntervalMinutes = value;
            }
        }

        private void OnCheckUpdatesClicked(object? sender, EventArgs e)
        {
            ExecuteSafeAsync(_viewModel.CheckUpdatesCommand, "проверку обновлений");
        }

        private async void OnConvertBinClicked(object? sender, EventArgs e)
        {
            using var dialog = new OpenFileDialog
            {
                Title = "Выберите файл пациента",
                Filter = "Файлы пациента (*.bin)|*.bin|Все файлы (*.*)|*.*",
                Multiselect = true
            };

            if (dialog.ShowDialog(this) != DialogResult.OK)
            {
                return;
            }

            var filePaths = dialog.FileNames
                .Where(path => !string.IsNullOrWhiteSpace(path))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .ToArray();

            if (filePaths.Length == 0)
            {
                return;
            }

            AppServices.Log.Info($"Пользователь выбрал {filePaths.Length} файл(ов) для ручного преобразования: {string.Join(", ", filePaths)}");

            var previousCursor = Cursor;
            Cursor = Cursors.WaitCursor;

            try
            {
                var successBlocks = new List<string>();
                var errorBlocks = new List<string>();

                foreach (var filePath in filePaths)
                {
                    var result = await _viewModel.ConvertRawFileAsync(filePath);
                    if (result.Success)
                    {
                        successBlocks.Add(BuildManualConversionSuccessBlock(result));
                    }
                    else
                    {
                        errorBlocks.Add(BuildManualConversionErrorBlock(result));
                    }
                }

                if (successBlocks.Count > 0)
                {
                    var header = successBlocks.Count == 1
                        ? "Файл успешно обработан."
                        : $"Успешно обработаны файлы ({successBlocks.Count} из {filePaths.Length}).";

                    var message = header + Environment.NewLine + Environment.NewLine + string.Join(Environment.NewLine + Environment.NewLine, successBlocks);
                    MessageBox.Show(
                        this,
                        message.TrimEnd(),
                        "Microlux ERG-Connect",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Information);
                }

                if (errorBlocks.Count > 0)
                {
                    var header = errorBlocks.Count == 1
                        ? "Не удалось обработать файл."
                        : $"Не удалось обработать файлы ({errorBlocks.Count} из {filePaths.Length}).";

                    var message = header + Environment.NewLine + Environment.NewLine + string.Join(Environment.NewLine + Environment.NewLine, errorBlocks);
                    MessageBox.Show(
                        this,
                        message.TrimEnd(),
                        "Microlux ERG-Connect",
                        MessageBoxButtons.OK,
                        MessageBoxIcon.Warning);
                }
            }
            catch (OperationCanceledException)
            {
                AppServices.Log.Warn("Ручное преобразование файла отменено пользователем.");
            }
            catch (Exception ex)
            {
                AppServices.Log.Error($"Ошибка при ручном преобразовании файла: {ex}");
                MessageBox.Show(
                    this,
                    $"Ошибка при обработке файла: {ex.Message}",
                    "Microlux ERG-Connect",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Error);
            }
            finally
            {
                Cursor = previousCursor;
            }
        }

        private void OnExitRequested(object? sender, AppServices.ExitRequestedEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => OnExitRequested(sender, e)));
                return;
            }

            if (_isExitRequested)
            {
                return;
            }

            _isExitRequested = true;
            AppServices.Log.Info($"Приложение будет закрыто автоматически: {e.Reason}");
            trayIcon.Visible = false;
            Close();
        }

        private void OnFormClosing(object? sender, FormClosingEventArgs e)
        {
            if (!_isExitRequested && AppServices.Settings.Current.MinimizeToTray)
            {
                e.Cancel = true;
                Hide();
                AppServices.Log.Info("Окно скрыто в трей вместо завершения работы.");
            }
        }

        private void OnFormLoaded(object? sender, EventArgs e)
        {
            PopulateLogs();
            ApplySettingsToInputs();

            var settings = AppServices.Settings.Current;
            if (settings.StartMinimized && !settings.MinimizeToTray)
            {
                AppServices.Log.Info("Старт в свернутом состоянии по настройкам пользователя.");
                WindowState = FormWindowState.Minimized;
                return;
            }

            if (WindowState != FormWindowState.Normal)
            {
                WindowState = FormWindowState.Normal;
            }

            Activate();
            AppServices.Log.Info("Главное окно отображено при запуске приложения.");

            labelPath.Text = _viewModel.ReportsDirectory;
        }

        private void OnFormResized(object? sender, EventArgs e)
        {
            if (WindowState == FormWindowState.Minimized && AppServices.Settings.Current.MinimizeToTray)
            {
                Hide();
                AppServices.Log.Info("Окно свернуто в трей.");
            }
        }

        private void OnHeaderLineValidated(object? sender, EventArgs e)
        {
            if (_isApplyingHeaderInputs)
            {
                return;
            }

            SaveHeaderFromInputs();
            RefreshHeaderPlaceholderState();
        }

        private void OnInstallUpdateClicked(object? sender, EventArgs e)
        {
            ExecuteSafeAsync(_viewModel.InstallUpdateCommand, "установку обновления");
        }

        private void OnManifestUrlValidated(object? sender, EventArgs e)
        {
            var url = manifestUrlTextBox.Text.Trim();
            if (!string.Equals(url, _viewModel.UpdateManifestUrl, StringComparison.Ordinal))
            {
                AppServices.Log.Info($"Пользователь изменил URL манифеста обновлений: '{url}'.");
                _viewModel.UpdateManifestUrl = url;
            }
            manifestUrlTextBox.Text = _viewModel.UpdateManifestUrl;
        }

        private void OnNumericTextBoxValidating(object? sender, CancelEventArgs e)
        {
            if (sender is not TextBox textBox)
            {
                return;
            }

            if (!int.TryParse(textBox.Text, out var value))
            {
                e.Cancel = true;
                AppServices.Log.Warn($"Введено недопустимое числовое значение '{textBox.Text}' в поле {textBox.Name}.");
                MessageBox.Show(
                    "Введите целое число.",
                    "Microlux ERG-Connect",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
            else if (value < 0)
            {
                e.Cancel = true;
                AppServices.Log.Warn($"Введено отрицательное значение {value} в поле {textBox.Name}. Значение должно быть неотрицательным.");
                MessageBox.Show(
                    "Значение должно быть неотрицательным.",
                    "Microlux ERG-Connect",
                    MessageBoxButtons.OK,
                    MessageBoxIcon.Warning);
            }
        }

        private void OnOpenLogsClicked(object? sender, EventArgs e)
        {
            ExecuteSafeAsync(_viewModel.OpenLogsCommand, "открытие каталога логов");
        }

        private void OnOpenReportsClicked(object? sender, EventArgs e)
        {
            ExecuteSafeAsync(_viewModel.OpenReportsCommand, "открытие каталога отчетов");
        }

        private void OnOpenSettingsClicked(object? sender, EventArgs e)
        {
            using var settingsForm = new SettingsForm();
            settingsForm.ShowDialog(this);
        }

        private void OnReconnectDelayValidated(object? sender, EventArgs e)
        {
            if (TryParseTextBoxValue(reconnectDelayTextBox, out var value))
            {
                _viewModel.DeviceReconnectDelaySeconds = value;
            }
        }

        private void OnReportRenderingModeChanged(object? sender, EventArgs e)
        {
            if (renderingModeComboBox.SelectedValue is ReportRenderingMode mode && mode != _viewModel.ReportRenderingMode)
            {
                AppServices.Log.Info($"Пользователь выбрал режим генерации отчетов: {mode}.");
                _viewModel.ReportRenderingMode = mode;
            }
        }

        private void OnReportTemplateChanged(object? sender, EventArgs e)
        {
            if (reportTemplateComboBox.SelectedValue is ReportTemplate template && template != _viewModel.ReportTemplate)
            {
                AppServices.Log.Info($"Пользователь выбрал шаблон отчетов: {template}.");
                _viewModel.ReportTemplate = template;
            }
        }

        private void OnResetPortClicked(object? sender, EventArgs e)
        {
            if (_viewModel.ForceRescanCommand.CanExecute(null))
            {
                AppServices.Log.Info("Пользователь инициировал сброс запомненного порта.");
                _viewModel.ForceRescanCommand.Execute(null);
            }
        }

        private void OnScanIntervalValidated(object? sender, EventArgs e)
        {
            if (TryParseTextBoxValue(scanIntervalTextBox, out var value))
            {
                _viewModel.DeviceScanIntervalSeconds = value;
            }
        }

        private void OnUpdateIntervalValidated(object? sender, EventArgs e)
        {
            if (TryParseTextBoxValue(updateIntervalTextBox, out var value))
            {
                _viewModel.UpdateCheckIntervalMinutes = value;
            }
        }

        private void PopulateLogs()
        {
            EnsureLogGridConfigured();

            var raiseEvents = logsBindingSource.RaiseListChangedEvents;
            try
            {
                logsBindingSource.RaiseListChangedEvents = false;
                _logEntries.Clear();
                foreach (var entry in _viewModel.Logs)
                {
                    _logEntries.Add(entry);
                }
            }
            finally
            {
                logsBindingSource.RaiseListChangedEvents = raiseEvents;
                logsBindingSource.ResetBindings(false);
            }

            ScrollLogsToEnd();
        }

        private static Color ResolveStatusColor(string? statusText)
        {
            const int defaultRed = 48;
            const int defaultGreen = 149;
            const int defaultBlue = 177;
            if (string.IsNullOrWhiteSpace(statusText))
            {
                return Color.FromArgb(defaultRed, defaultGreen, defaultBlue);
            }

            var normalized = statusText.ToLowerInvariant();
            if (normalized.Contains("ошиб") || normalized.Contains("error") || normalized.Contains("fail"))
            {
                return Color.FromArgb(227, 83, 64);
            }

            if (normalized.Contains("подключ") || normalized.Contains("готов") || normalized.Contains("успеш") || normalized.Contains("sync"))
            {
                return Color.FromArgb(59, 179, 115);
            }

            if (normalized.Contains("ожид") || normalized.Contains("поиск") || normalized.Contains("жд") || normalized.Contains("wait"))
            {
                return Color.FromArgb(242, 192, 64);
            }

            return Color.FromArgb(defaultRed, defaultGreen, defaultBlue);
        }

        private void RestoreFromTray()
        {
            Show();
            WindowState = FormWindowState.Normal;
            Activate();
            AppServices.Log.Info("Окно восстановлено из трея.");
        }

        private void SaveHeaderFromInputs()
        {
            if (_isApplyingHeaderInputs) return;

            var lines = _headerInputs
                .Select(state => state.ShowingPlaceholder ? string.Empty : state.TextBox.Text ?? string.Empty)
                .ToArray();

            var normalized = ReportHeaderFormatter.Normalize(string.Join('\n', ReportHeaderFormatter.EnsureLineCount(lines)));
            if (!string.Equals(normalized, _viewModel.ReportHeader, StringComparison.Ordinal))
            {
                AppServices.Log.Info("Шапка отчета обновлена через основное окно.");
                _viewModel.ReportHeader = normalized;
            }
        }

        private void ScrollLogsToEnd()
        {
            if (logGridView.Rows.Count > 0)
            {
                var lastIndex = logGridView.Rows.Count - 1;
                try
                {
                    logGridView.FirstDisplayedScrollingRowIndex = Math.Max(0, lastIndex);
                }
                catch (ArgumentOutOfRangeException)
                {
                    // Игнорируем, когда таблица еще не готова к прокрутке.
                }
                catch (InvalidOperationException)
                {
                    // Игнорируем временные ошибки прокрутки.
                }
            }
        }
        private void ToggleControls(bool enabled)
        {
            buttonSetPathFolder.Enabled = enabled;
        }

        private void TrayExitMenuItem_Click(object? sender, EventArgs e)
        {
            ExitApplication();
        }

        private void TrayIcon_DoubleClick(object? sender, EventArgs e)
        {
            RestoreFromTray();
        }

        private void TrayOpenMenuItem_Click(object? sender, EventArgs e)
        {
            RestoreFromTray();
        }

        private static bool TryParseTextBoxValue(TextBox textBox, out int value)
        {
            if (!int.TryParse(textBox.Text, out value))
            {
                AppServices.Log.Warn($"Не удалось преобразовать значение '{textBox.Text}' из поля {textBox.Name} к целому числу.");
                return false;
            }
            return true;
        }

        private void UpdateAll()
        {
            statusValueLabel.Text = FormatSingleLine(_viewModel.StatusText);
            portValueLabel.Text = FormatSingleLine(_viewModel.CurrentPort);
            deviceValueLabel.Text = FormatSingleLine(_viewModel.DeviceName);
            softwareValueLabel.Text = FormatSingleLine(_viewModel.SoftwareVersion);
            reportValueLabel.Text = FormatSingleLine(_viewModel.ReportName);
            syncStatusLabel.Text = FormatSingleLine(_viewModel.SyncStatus);
            updateStatusValueLabel.Text = FormatSingleLine(_viewModel.UpdateStatusText);
            UpdateHeaderSummary();
        }

        private void UpdateCommandState()
        {
            checkUpdatesButton.Enabled = _viewModel.CheckUpdatesCommand.CanExecute(null);
            installUpdateButton.Enabled = _viewModel.InstallUpdateCommand.CanExecute(null);
            ForceCommandStateLogging();
        }

        private void UpdateHeaderSummary()
        {
            headerStatusLabel.Text = $"Статус: {FormatSingleLine(_viewModel.StatusText)}";
            headerDeviceLabel.Text = $"Устройство: {FormatSingleLine(_viewModel.DeviceName, "Не обнаружено")}";
            headerPortLabel.Text = $"Порт: {FormatSingleLine(_viewModel.CurrentPort)}";
            headerSyncLabel.Text = $"Синхронизация: {FormatSingleLine(_viewModel.SyncStatus)}";

            ApplyStatusVisualState(_viewModel.StatusText, headerStatusLabel, isBadge: true);
            ApplyStatusVisualState(_viewModel.StatusText, statusValueLabel, isBadge: false);
            ApplyStatusVisualState(_viewModel.SyncStatus, headerSyncLabel, isBadge: true);
            ApplyStatusVisualState(_viewModel.SyncStatus, syncStatusLabel, isBadge: false);
        }

        private void ViewModelOnPropertyChanged(object? sender, PropertyChangedEventArgs e)
        {
            if (InvokeRequired)
            {
                BeginInvoke(new Action(() => ViewModelOnPropertyChanged(sender, e)));
                return;
            }

            switch (e.PropertyName)
            {
                case nameof(MainViewModel.StatusText):
                    statusValueLabel.Text = FormatSingleLine(_viewModel.StatusText);
                    break;
                case nameof(MainViewModel.CurrentPort):
                    portValueLabel.Text = FormatSingleLine(_viewModel.CurrentPort);
                    break;
                case nameof(MainViewModel.DeviceName):
                    deviceValueLabel.Text = FormatSingleLine(_viewModel.DeviceName);
                    break;
                case nameof(MainViewModel.SoftwareVersion):
                    softwareValueLabel.Text = FormatSingleLine(_viewModel.SoftwareVersion);
                    break;
                case nameof(MainViewModel.ReportName):
                    reportValueLabel.Text = FormatSingleLine(_viewModel.ReportName);
                    break;
                case nameof(MainViewModel.SyncStatus):
                    syncStatusLabel.Text = FormatSingleLine(_viewModel.SyncStatus);
                    break;
                case nameof(MainViewModel.UpdateStatusText):
                    updateStatusValueLabel.Text = FormatSingleLine(_viewModel.UpdateStatusText);
                    break;
                case nameof(MainViewModel.DeviceScanIntervalSeconds):
                case nameof(MainViewModel.DeviceReconnectDelaySeconds):
                case nameof(MainViewModel.BackgroundSyncIntervalMinutes):
                case nameof(MainViewModel.UpdateCheckIntervalMinutes):
                case nameof(MainViewModel.UpdateManifestUrl):
                case nameof(MainViewModel.ReportTemplate):
                case nameof(MainViewModel.ReportRenderingMode):
                case nameof(MainViewModel.ReportHeader):
                    ApplySettingsToInputs();
                    break;
            }

            if (string.IsNullOrEmpty(e.PropertyName) ||
                e.PropertyName is nameof(MainViewModel.StatusText)
                    or nameof(MainViewModel.CurrentPort)
                    or nameof(MainViewModel.DeviceName)
                    or nameof(MainViewModel.SyncStatus))
            {
                UpdateHeaderSummary();
            }
        }

        protected override void OnFormClosed(FormClosedEventArgs e)
        {
            _viewModel.PropertyChanged -= ViewModelOnPropertyChanged;
            _viewModel.Logs.CollectionChanged -= LogsOnCollectionChanged;
            _viewModel.CheckUpdatesCommand.CanExecuteChanged -= _checkUpdatesCanExecuteHandler;
            _viewModel.InstallUpdateCommand.CanExecuteChanged -= _installUpdateCanExecuteHandler;
            AppServices.ExitRequested -= OnExitRequested;
            trayIcon.Visible = false;
            base.OnFormClosed(e);
        }

        protected override void OnShown(EventArgs e)
        {
            base.OnShown(e);
            AppServices.Log.Info("Главное окно отображено пользователю.");
        }

        protected override bool ProcessCmdKey(ref Message msg, Keys keyData)
        {
            // Ctrl + Alt + Shift + D
            if ((keyData & (Keys.Control | Keys.Alt | Keys.Shift)) == (Keys.Control | Keys.Alt | Keys.Shift) &&
                (keyData & Keys.KeyCode) == Keys.D)
            {
                OnConvertBinClicked(this, EventArgs.Empty);
                return true;
            }

            return base.ProcessCmdKey(ref msg, keyData);
        }

        public string? ResolveInitialFolder()
        {
            var current = labelPath.Text?.Trim();
            if (!string.IsNullOrEmpty(current) && Directory.Exists(current))
            {
                return current;
            }

            if (!string.IsNullOrEmpty(_viewModel.ReportsDirectory) && Directory.Exists(_viewModel.ReportsDirectory))
            {
                return _viewModel.ReportsDirectory;
            }

            return AppContext.BaseDirectory;
        }

        private sealed record ReportTemplateOption(ReportTemplate Value, string Description);
        private sealed record ReportRenderingModeOption(ReportRenderingMode Value, string Description);
    }
}