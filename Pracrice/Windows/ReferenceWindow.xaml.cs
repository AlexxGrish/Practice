using System.Data;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Pracrice.Services;

namespace Pracrice.Windows;

public partial class ReferenceWindow : Window
{
    private readonly UserSession _user;
    private ReferenceSection _section;
    private DataTable? _table;
    private string? _selectedKey;
    private bool _loading;

    public ReferenceWindow(ReferenceSection initialSection, UserSession user)
    {
        InitializeComponent();

        _user = user;
        _section = initialSection;
        SectionsList.ItemsSource = ReferenceService.GetSections();

        ReferenceSectionInfo? selected = ReferenceService.GetSections()
            .FirstOrDefault(item => item.Section == initialSection);

        _loading = true;
        SectionsList.SelectedItem = selected;
        _loading = false;

        UpdateSectionHeader();
        Loaded += async (_, _) => await LoadSectionAsync();
    }

    private ReferenceSectionInfo CurrentSectionInfo => ReferenceService.GetInfo(_section);

    private async void SectionsList_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        if (SectionsList.SelectedItem is not ReferenceSectionInfo info || _loading)
        {
            return;
        }

        if (info.Section == _section && _table is not null)
        {
            return;
        }

        _section = info.Section;
        UpdateSectionHeader();
        await LoadSectionAsync();
    }

    private void UpdateSectionHeader()
    {
        SectionTitle.Text = CurrentSectionInfo.Title;
        SectionDescription.Text = CurrentSectionInfo.Description;
    }

    private async Task LoadSectionAsync()
    {
        if (_loading)
        {
            return;
        }

        _loading = true;
        try
        {
            _table = await ReferenceService.LoadTableAsync(_section);
            ReferencesGrid.ItemsSource = _table.DefaultView;
            CountText.Text = _table.Rows.Count == 1
                ? "1 запись"
                : $"{_table.Rows.Count} записей";

            if (_table.Rows.Count > 0)
            {
                ReferencesGrid.SelectedIndex = 0;
                _selectedKey = Convert.ToString(_table.Rows[0]["Key"]);
            }
            else
            {
                _selectedKey = null;
            }

            UpdateButtons();
        }
        catch (Exception exception)
        {
            App.LogException(exception);
            DialogService.ShowError("Не удалось загрузить справочник.\n\n" + exception.Message);
        }
        finally
        {
            _loading = false;
        }
    }

    private void ReferencesGrid_AutoGeneratingColumn(object? sender, DataGridAutoGeneratingColumnEventArgs e)
    {
        if (string.Equals(e.PropertyName, "Key", StringComparison.Ordinal))
        {
            e.Cancel = true;
            return;
        }

        ReferenceColumn? column = ReferenceService.GetColumns(_section)
            .FirstOrDefault(item => item.Name == e.PropertyName);

        if (column is not null)
        {
            e.Column.Header = column.Header;
        }

        e.Column.IsReadOnly = true;
    }

    private void ReferencesGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedKey = ReferencesGrid.SelectedItem is DataRowView row
            ? Convert.ToString(row["Key"])
            : null;
        UpdateButtons();
    }

    private void ReferencesGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (ReferencesGrid.SelectedItem is DataRowView)
        {
            OpenEditor();
        }
    }

    private void UpdateButtons()
    {
        bool hasSelection = !string.IsNullOrWhiteSpace(_selectedKey);
        BtnEdit.IsEnabled = hasSelection;
        BtnDelete.IsEnabled = hasSelection;
    }

    private async void BtnAdd_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            ReferenceForm form = await ReferenceService.BuildFormAsync(_section, null);
            var window = new ReferenceEditWindow(form) { Owner = this };
            if (window.ShowDialog() == true)
            {
                await LoadSectionAsync();
            }
        }
        catch (ValidationException exception)
        {
            DialogService.ShowWarning(exception.Message, "Добавление записи");
        }
        catch (Exception exception)
        {
            App.LogException(exception);
            DialogService.ShowError("Не удалось открыть форму добавления.\n\n" + exception.Message);
        }
    }

    private async void BtnEdit_Click(object sender, RoutedEventArgs e)
    {
        await OpenEditorAsync();
    }

    private async Task OpenEditorAsync()
    {
        if (string.IsNullOrWhiteSpace(_selectedKey))
        {
            DialogService.ShowWarning("Выберите запись в справочнике.");
            return;
        }

        try
        {
            ReferenceForm form = await ReferenceService.BuildFormAsync(_section, _selectedKey);
            var window = new ReferenceEditWindow(form) { Owner = this };
            if (window.ShowDialog() == true)
            {
                await LoadSectionAsync();
            }
        }
        catch (ValidationException exception)
        {
            DialogService.ShowWarning(exception.Message, "Редактирование записи");
        }
        catch (Exception exception)
        {
            App.LogException(exception);
            DialogService.ShowError("Не удалось открыть форму редактирования.\n\n" + exception.Message);
        }
    }

    private void OpenEditor()
    {
        _ = OpenEditorAsync();
    }

    private async void BtnDelete_Click(object sender, RoutedEventArgs e)
    {
        if (string.IsNullOrWhiteSpace(_selectedKey))
        {
            DialogService.ShowWarning("Выберите запись в справочнике.");
            return;
        }

        if (!DialogService.Confirm(
                $"Удалить выбранную запись из справочника «{CurrentSectionInfo.Title}»?\n\n" +
                "Действие нельзя отменить.",
                "Удаление записи"))
        {
            return;
        }

        try
        {
            await ReferenceService.DeleteAsync(_section, _selectedKey);
            DialogService.ShowInfo("Запись удалена.");
            await LoadSectionAsync();
        }
        catch (ValidationException exception)
        {
            DialogService.ShowWarning(exception.Message, "Удаление невозможно");
        }
        catch (Exception exception)
        {
            App.LogException(exception);
            DialogService.ShowError("Не удалось удалить запись.\n\n" + exception.Message);
        }
    }

    private async void BtnRefresh_Click(object sender, RoutedEventArgs e)
    {
        await LoadSectionAsync();
    }

    private async void BtnReport_Click(object sender, RoutedEventArgs e)
    {
        if (_table is null)
        {
            return;
        }

        try
        {
            IReadOnlyList<ReferenceColumn> columns = ReferenceService.GetColumns(_section);
            var headers = columns.Select(column => column.Header).ToList();
            var rows = new List<IReadOnlyList<string>>();

            foreach (DataRow row in _table.Rows)
            {
                rows.Add(columns
                    .Select(column => Convert.ToString(row[column.Name]) ?? string.Empty)
                    .ToList());
            }

            string path = await ReportService.GenerateReferenceReportAsync(
                CurrentSectionInfo.Title,
                headers,
                rows,
                DateTime.Today,
                _user);

            try
            {
                ReportService.OpenReport(path);
            }
            catch (Exception openException)
            {
                App.LogException(openException);
                DialogService.ShowWarning(
                    $"Отчёт сохранён, но не удалось открыть его в браузере.\n\nФайл:\n{path}\n\n{openException.Message}",
                    "Открытие отчёта");
            }
        }
        catch (Exception exception)
        {
            App.LogException(exception);
            DialogService.ShowError("Не удалось сформировать отчёт.\n\n" + exception.Message);
        }
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
}
