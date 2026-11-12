using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using Pracrice.Models;
using Pracrice.Services;

namespace Pracrice;

public partial class MainWindow
{
    private IReadOnlyList<PlanProductRow> _planProducts = Array.Empty<PlanProductRow>();
    private PlanCalculationResult? _planResult;
    private TransportData? _transportData;
    private List<TransportTariffRow> _transportTariffRows = new();
    private List<TransportStoreRow> _transportStoreRows = new();
    private TransportCalculationResult? _transportResult;
    private EquipmentCalculationResult? _equipmentResult;
    private IReadOnlyList<CashFlowRow> _cashFlows = Array.Empty<CashFlowRow>();
    private CashCalculationResult? _cashResult;
    private IReadOnlyList<InvestmentProjectRow> _investmentProjects = Array.Empty<InvestmentProjectRow>();
    private InvestmentCalculationResult? _investmentResult;
    private bool _isLoadingPlan;
    private bool _isLoadingTransport;
    private bool _isLoadingCash;

    private async Task LoadPlanAsync()
    {
        if (_isLoadingPlan)
        {
            return;
        }

        _isLoadingPlan = true;
        try
        {
            _planProducts = await CalculationService.LoadPlanProductsAsync();
            ProductsPlanGrid.ItemsSource = _planProducts;

            if (_planProducts.Count == 0)
            {
                PlanProfitText.Text = "—";
                PlanUsageText.Text = "Нет товаров с рецептурами. Добавьте товары и рецептуры в справочниках.";
            }
        }
        catch (Exception exception)
        {
            App.LogException(exception);
            DialogService.ShowError("Не удалось загрузить данные для плана выпуска.\n\n" + exception.Message);
        }
        finally
        {
            _isLoadingPlan = false;
        }
    }

    private async void BtnPlanCalc_Click(object sender, RoutedEventArgs e)
    {
        if (_planProducts.Count == 0)
        {
            await LoadPlanAsync();
        }

        if (_planProducts.Count == 0)
        {
            DialogService.ShowWarning("Нет товаров с рецептурами для расчёта плана.");
            return;
        }

        if (!TryParseDecimal(RawLimitText.Text, out decimal rawLimit) ||
            !TryParseDecimal(FillLimitText.Text, out decimal fillLimit) ||
            !TryParseDecimal(PackLimitText.Text, out decimal packLimit) ||
            rawLimit <= 0 || fillLimit <= 0 || packLimit <= 0)
        {
            DialogService.ShowWarning("Укажите положительные лимиты сырья, воды и упаковки.");
            return;
        }

        try
        {
            var includedProducts = _planProducts.Where(product => product.IsIncluded).ToList();
            if (includedProducts.Count == 0)
            {
                DialogService.ShowWarning("Выберите хотя бы один товар для расчёта.");
                return;
            }

            PlanCalculationResult result = CalculationService.CalculatePlan(
                includedProducts, rawLimit, fillLimit, packLimit);
            _planResult = result;

            PlanResultGrid.ItemsSource = result.Rows;
            PlanProfitText.Text = result.TotalProfit.ToString("N0", CultureInfo.CurrentCulture) + " ₽";
            PlanUsageText.Text =
                $"Сырьё — {result.RawUsagePercent.ToString("N0", CultureInfo.CurrentCulture)} % · " +
                $"Вода — {result.FillUsagePercent.ToString("N0", CultureInfo.CurrentCulture)} % · " +
                $"Упаковка — {result.PackUsagePercent.ToString("N0", CultureInfo.CurrentCulture)} %";

            if (result.Rows.Count == 0)
            {
                PlanUsageText.Text = "При заданных ограничениях выпуск невозможен.";
            }
        }
        catch (Exception exception)
        {
            App.LogException(exception);
            DialogService.ShowError("Не удалось рассчитать план выпуска.\n\n" + exception.Message);
        }
    }

    private async Task LoadTransportAsync()
    {
        if (_isLoadingTransport)
        {
            return;
        }

        _isLoadingTransport = true;
        try
        {
            _transportData = await CalculationService.LoadTransportAsync();
            _transportResult = null;
            _transportTariffRows = CalculationService.BuildTransportTariffs(_transportData).ToList();
            _transportStoreRows = CalculationService.BuildTransportStores(_transportData).ToList();
            TariffsDataGrid.ItemsSource = _transportTariffRows;
            TransportStoresGrid.ItemsSource = _transportStoreRows;
            ApplyTransportHeaders();

            int supply = _transportData.Supplies.Sum();
            int demand = _transportData.Stores.Sum(store => store.Demand);
            RouteDemandText.Text = $"Запас: {supply} ед. · Потребность магазинов: {demand} ед.";
        }
        catch (ValidationException exception)
        {
            DialogService.ShowWarning(exception.Message, "План перевозок");
        }
        catch (Exception exception)
        {
            App.LogException(exception);
            DialogService.ShowError("Не удалось загрузить данные для плана перевозок.\n\n" + exception.Message);
        }
        finally
        {
            _isLoadingTransport = false;
        }
    }

    private void ApplyTransportHeaders()
    {
        if (_transportData is null)
        {
            return;
        }

        for (int index = 0; index < 3; index++)
        {
            string fullName = index < _transportData.Stores.Count
                ? _transportData.Stores[index].Name
                : "—";
            string shortName = ShortStoreName(fullName);
            var header = new TextBlock
            {
                Text = shortName,
                ToolTip = fullName
            };

            TariffsDataGrid.Columns[index + 2].Header = header;
            ResultRouteGrid.Columns[index + 1].Header = new TextBlock
            {
                Text = shortName,
                ToolTip = fullName
            };
        }
    }

    private static string ShortStoreName(string name) =>
        name.Replace("Магазин ", string.Empty, StringComparison.OrdinalIgnoreCase).Trim();

    private async void BtnRouteCalc_Click(object sender, RoutedEventArgs e)
    {
        if (_transportData is null)
        {
            await LoadTransportAsync();
        }

        if (_transportData is null)
        {
            return;
        }

        if (_transportTariffRows.All(row => !row.IsIncluded))
        {
            DialogService.ShowWarning("Выберите хотя бы один склад для расчёта.");
            return;
        }

        if (_transportStoreRows.All(row => !row.IsIncluded))
        {
            DialogService.ShowWarning("Выберите хотя бы один магазин для расчёта.");
            return;
        }

        try
        {
            var filteredSupplies = _transportData.Supplies
                .Select((supply, index) => _transportTariffRows[index].IsIncluded ? supply : 0)
                .ToArray();
            var filteredStores = _transportData.Stores
                .Select((store, index) => new Store
                {
                    Id = store.Id,
                    Name = store.Name,
                    Address = store.Address,
                    Phone = store.Phone,
                    Demand = _transportStoreRows[index].IsIncluded ? store.Demand : 0
                })
                .ToList();
            var filteredData = new TransportData(
                _transportData.Warehouses, filteredStores, filteredSupplies, _transportData.Costs);

            bool isOpen = TransportTaskTypeCombo.SelectedIndex == 1;
            TransportCalculationResult result = CalculationService.CalculateTransport(filteredData, isOpen);
            _transportResult = result;
            ResultRouteGrid.ItemsSource = result.Rows;

            RouteCostText.Text = result.TotalCost.ToString("N0", CultureInfo.CurrentCulture) + " ₽";
            RouteStatusText.Text = isOpen
                ? $"Открытая задача · запас {result.TotalSupply} ед. · потребность {result.TotalDemand} ед."
                : $"Закрытая задача · запас {result.TotalSupply} ед. · потребность {result.TotalDemand} ед.";

            var routes = new List<string>();
            for (int warehouseIndex = 0; warehouseIndex < result.Rows.Count; warehouseIndex++)
            {
                TransportResultRow row = result.Rows[warehouseIndex];
                if (row.Warehouse == "Дефицит")
                {
                    AddRoute(routes, "Дефицит", result.Stores, row);
                    continue;
                }

                AddRoute(routes, row.Warehouse, result.Stores, row);
                if (row.Reserve > 0)
                {
                    routes.Add($"{row.Warehouse} → резерв: {row.Reserve} ед.");
                }
            }

            RouteSummaryText.Text = routes.Count == 0
                ? "Маршруты не назначены."
                : string.Join("\n", routes);

            RouteDemandText.Text = isOpen && result.TotalSupply != result.TotalDemand
                ? $"Запас: {result.TotalSupply} ед. · потребность: {result.TotalDemand} ед. · " +
                  (result.TotalSupply > result.TotalDemand ? "есть резерв" : "есть дефицит")
                : $"Запас: {result.TotalSupply} ед. · потребность: {result.TotalDemand} ед.";
        }
        catch (ValidationException exception)
        {
            DialogService.ShowWarning(exception.Message, "План перевозок");
        }
        catch (Exception exception)
        {
            App.LogException(exception);
            DialogService.ShowError("Не удалось рассчитать план перевозок.\n\n" + exception.Message);
        }
    }

    private void BtnEquipmentCalc_Click(object sender, RoutedEventArgs e)
    {
        int age = ExtractFirstInteger(EquipmentAgeText.Text, 6);
        int horizon = ExtractFirstInteger(EquipmentHorizonText.Text, 3);
        CalculateEquipmentView(age, horizon);
    }

    private void CalculateEquipmentView(int age, int horizon)
    {
        bool includeCurrent = _equipmentResult?.Costs
            .FirstOrDefault(row => row.Equipment.StartsWith("Текущее", StringComparison.Ordinal))?.IsIncluded ?? true;
        bool includeNew = _equipmentResult?.Costs
            .FirstOrDefault(row => row.Equipment.StartsWith("Новое", StringComparison.Ordinal))?.IsIncluded ?? true;

        if (!includeCurrent && !includeNew)
        {
            DialogService.ShowWarning("Выберите хотя бы один вариант оборудования для расчёта.");
            return;
        }

        try
        {
            EquipmentCalculationResult result = CalculationService.CalculateEquipment(
                age, horizon, includeCurrent, includeNew);
            _equipmentResult = result;
            EqCostsGrid.ItemsSource = result.Costs;
            StrategyGrid.ItemsSource = result.Strategies;
            EquipmentSavingLabel.Text = $"Экономия за {horizon} г.";
            EquipmentSavingText.Text = result.Saving.ToString("N1", CultureInfo.CurrentCulture) + " млн ₽";
            EquipmentDecisionText.Text = result.Decision;
        }
        catch (Exception exception)
        {
            App.LogException(exception);
            DialogService.ShowError("Не удалось рассчитать стратегию замены.\n\n" + exception.Message);
        }
    }

    private async Task LoadCashAsync()
    {
        if (_isLoadingCash)
        {
            return;
        }

        _isLoadingCash = true;
        try
        {
            _cashFlows = await CalculationService.LoadCashFlowAsync();
            CashflowGrid.ItemsSource = _cashFlows;

            if (_cashFlows.Count == 0)
            {
                DialogService.ShowWarning("Для расчёта касс добавьте магазины в справочнике.");
            }
        }
        catch (Exception exception)
        {
            App.LogException(exception);
            DialogService.ShowError("Не удалось загрузить данные для расчёта касс.\n\n" + exception.Message);
        }
        finally
        {
            _isLoadingCash = false;
        }
    }

    private async void BtnCashCalc_Click(object sender, RoutedEventArgs e)
    {
        if (_cashFlows.Count == 0)
        {
            await LoadCashAsync();
        }

        if (_cashFlows.Count == 0)
        {
            DialogService.ShowWarning("Нет магазинов для расчёта касс.");
            return;
        }

        if (!TryParseDecimal(CashWaitLimitText.Text, out decimal waitLimit) ||
            !TryParseDecimal(CashLoadLimitText.Text, out decimal loadLimit) ||
            waitLimit <= 0 || loadLimit is <= 0 or >= 100)
        {
            DialogService.ShowWarning("Укажите корректные ограничения ожидания и загрузки.");
            return;
        }

        try
        {
            var includedFlows = _cashFlows.Where(flow => flow.IsIncluded).ToList();
            if (includedFlows.Count == 0)
            {
                DialogService.ShowWarning("Выберите хотя бы один магазин для расчёта.");
                return;
            }

            CashCalculationResult result = CalculationService.CalculateCash(
                includedFlows, waitLimit, loadLimit);
            _cashResult = result;

            CashResultGrid.ItemsSource = result.Rows;
            CashTotalText.Text = $"{result.TotalCashiers} касс";
            CashSummaryText.Text = $"Из {result.TotalAvailable} доступных в магазинах";
        }
        catch (Exception exception)
        {
            App.LogException(exception);
            DialogService.ShowError("Не удалось рассчитать кассы.\n\n" + exception.Message);
        }
    }

    private void LoadInvestments()
    {
        _investmentProjects = CalculationService.GetInvestmentProjects();
        InvestProjectsGrid.ItemsSource = _investmentProjects;
    }

    private void BtnInvestCalc_Click(object sender, RoutedEventArgs e)
    {
        if (_investmentProjects.Count == 0)
        {
            LoadInvestments();
        }

        if (!TryParseDecimal(InvestBudgetText.Text, out decimal budget) || budget <= 0)
        {
            DialogService.ShowWarning("Укажите положительный инвестиционный бюджет.");
            return;
        }

        try
        {
            var includedProjects = _investmentProjects.Where(project => project.IsIncluded).ToList();
            if (includedProjects.Count == 0)
            {
                DialogService.ShowWarning("Выберите хотя бы один проект для расчёта.");
                return;
            }

            InvestmentCalculationResult result = CalculationService.CalculateInvestments(
                includedProjects, budget);
            _investmentResult = result;

            InvestPortfolioGrid.ItemsSource = result.Portfolio;
            InvestIncomeText.Text =
                (result.TotalIncome / 1_000_000m).ToString("N2", CultureInfo.CurrentCulture) + " млн ₽";
            InvestAllocatedText.Text =
                "Распределено: " +
                (result.TotalAllocated / 1_000_000m).ToString("N2", CultureInfo.CurrentCulture) + " млн ₽";
        }
        catch (Exception exception)
        {
            App.LogException(exception);
            DialogService.ShowError("Не удалось распределить инвестиции.\n\n" + exception.Message);
        }
    }

    private async void BtnPlanExport_Click(object sender, RoutedEventArgs e)
    {
        if (_planResult is null || _planResult.Rows.Count == 0)
        {
            DialogService.ShowWarning("Сначала выполните расчёт плана выпуска.");
            return;
        }

        var headers = new[] { "Продукция", "Выпуск, шт.", "Прибыль, ₽", "Доля" };
        var rows = _planResult.Rows
            .Select(row => (IReadOnlyList<string>)new[]
            {
                row.Product,
                row.Volume.ToString("N0", CultureInfo.CurrentCulture),
                row.Profit.ToString("N0", CultureInfo.CurrentCulture),
                row.Share
            })
            .ToList();

        string subtitle =
            $"Лимиты: сырьё {RawLimitText.Text} кг, вода {FillLimitText.Text} л, упаковка {PackLimitText.Text} шт. " +
            $"Итоговая прибыль: {_planResult.TotalProfit.ToString("N0", CultureInfo.CurrentCulture)} ₽.";

        await ExportAsync("План выпуска продукции", subtitle, headers, rows);
    }

    private async void BtnRouteExport_Click(object sender, RoutedEventArgs e)
    {
        if (_transportResult is null || _transportResult.Rows.Count == 0)
        {
            DialogService.ShowWarning("Сначала выполните расчёт плана перевозок.");
            return;
        }

        var headers = new List<string> { "Склад" };
        for (int index = 0; index < 3; index++)
        {
            headers.Add(index < _transportResult.Stores.Count
                ? ShortStoreName(_transportResult.Stores[index].Name)
                : "—");
        }

        headers.Add("Резерв");

        var rows = _transportResult.Rows
            .Select(row => (IReadOnlyList<string>)new[]
            {
                row.Warehouse,
                row.Shop1.ToString(CultureInfo.CurrentCulture),
                row.Shop2.ToString(CultureInfo.CurrentCulture),
                row.Shop3.ToString(CultureInfo.CurrentCulture),
                row.Reserve.ToString(CultureInfo.CurrentCulture)
            })
            .ToList();

        string subtitle =
            $"{(_transportResult.IsOpen ? "Открытая" : "Закрытая")} задача. " +
            $"Запас: {_transportResult.TotalSupply} ед., потребность: {_transportResult.TotalDemand} ед. " +
            $"Минимальные затраты: {_transportResult.TotalCost.ToString("N0", CultureInfo.CurrentCulture)} ₽.";

        await ExportAsync("План перевозок", subtitle, headers, rows);
    }

    private async void BtnEquipmentExport_Click(object sender, RoutedEventArgs e)
    {
        if (_equipmentResult is null || _equipmentResult.Strategies.Count == 0)
        {
            DialogService.ShowWarning("Сначала выполните расчёт стратегии замены.");
            return;
        }

        var headers = new[] { "Стратегия", "Общие затраты, млн ₽", "Решение" };
        var rows = _equipmentResult.Strategies
            .Select(row => (IReadOnlyList<string>)new[]
            {
                row.Strategy,
                row.TotalCosts.ToString("N2", CultureInfo.CurrentCulture),
                row.Decision
            })
            .ToList();

        string subtitle =
            $"Возраст оборудования: {EquipmentAgeText.Text}. Горизонт: {EquipmentHorizonText.Text}. " +
            $"Рекомендация: {_equipmentResult.Decision}. " +
            $"Экономия: {_equipmentResult.Saving.ToString("N1", CultureInfo.CurrentCulture)} млн ₽.";

        await ExportAsync("Обновление оборудования", subtitle, headers, rows);
    }

    private async void BtnCashExport_Click(object sender, RoutedEventArgs e)
    {
        if (_cashResult is null || _cashResult.Rows.Count == 0)
        {
            DialogService.ShowWarning("Сначала выполните расчёт касс.");
            return;
        }

        var headers = new[] { "Магазин", "Касс", "Загрузка", "Ожидание" };
        var rows = _cashResult.Rows
            .Select(row => (IReadOnlyList<string>)new[]
            {
                row.Market,
                row.Count.ToString(CultureInfo.CurrentCulture),
                row.LoadPercent,
                row.WaitTime
            })
            .ToList();

        string subtitle =
            $"Открыто касс: {_cashResult.TotalCashiers} из {_cashResult.TotalAvailable} доступных. " +
            $"Ожидание не более {CashWaitLimitText.Text} мин, загрузка не более {CashLoadLimitText.Text} %.";

        await ExportAsync("Планирование касс", subtitle, headers, rows);
    }

    private async void BtnInvestExport_Click(object sender, RoutedEventArgs e)
    {
        if (_investmentResult is null || _investmentResult.Portfolio.Count == 0)
        {
            DialogService.ShowWarning("Сначала выполните распределение инвестиций.");
            return;
        }

        var headers = new[] { "Проект", "Выделено", "Доля" };
        var rows = _investmentResult.Portfolio
            .Select(row => (IReadOnlyList<string>)new[]
            {
                row.SelectedProject,
                row.Allocated,
                row.Share
            })
            .ToList();

        string subtitle =
            $"Доход в год: {(_investmentResult.TotalIncome / 1_000_000m).ToString("N2", CultureInfo.CurrentCulture)} млн ₽. " +
            $"Распределено: {(_investmentResult.TotalAllocated / 1_000_000m).ToString("N2", CultureInfo.CurrentCulture)} млн ₽.";

        await ExportAsync("Распределение инвестиций", subtitle, headers, rows);
    }

    private async Task ExportAsync(
        string title,
        string subtitle,
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<string>> rows)
    {
        if (CurrentUser is null)
        {
            return;
        }

        try
        {
            string path = await ReportService.GenerateTableReportAsync(
                title,
                subtitle,
                headers,
                rows,
                ReportDatePicker.SelectedDate ?? DateTime.Today,
                CurrentUser);

            try
            {
                ReportService.OpenReport(path);
            }
            catch (Exception openException)
            {
                App.LogException(openException);
                DialogService.ShowWarning(
                    $"Отчёт сохранён, но не удалось открыть его в браузере.\n\nФайл:\n{path}\n\n{openException.Message}",
                    "Экспорт отчёта");
            }
        }
        catch (Exception exception)
        {
            App.LogException(exception);
            DialogService.ShowError("Не удалось сформировать отчёт.\n\n" + exception.Message, "Экспорт");
        }
    }

    private static void AddRoute(
        ICollection<string> routes,
        string warehouse,
        IReadOnlyList<Store> stores,
        TransportResultRow row)
    {
        int[] quantities = { row.Shop1, row.Shop2, row.Shop3 };
        for (int index = 0; index < stores.Count && index < quantities.Length; index++)
        {
            if (quantities[index] > 0)
            {
                routes.Add($"{warehouse} → {ShortStoreName(stores[index].Name)}: {quantities[index]} ед.");
            }
        }
    }

    private static bool TryParseDecimal(string text, out decimal value)
    {
        string normalized = text
            .Replace(" ", string.Empty)
            .Replace("\u00A0", string.Empty)
            .Replace("₽", string.Empty)
            .Replace("млн", string.Empty)
            .Replace("кг", string.Empty)
            .Replace("л", string.Empty)
            .Replace("шт.", string.Empty)
            .Replace("%", string.Empty)
            .Trim();

        return decimal.TryParse(normalized, NumberStyles.Any, CultureInfo.CurrentCulture, out value)
               || decimal.TryParse(normalized.Replace(',', '.'), NumberStyles.Any,
                   CultureInfo.InvariantCulture, out value);
    }

    private static int ExtractFirstInteger(string text, int defaultValue)
    {
        Match match = Regex.Match(text, @"\d+");
        return match.Success && int.TryParse(match.Value, out int value) ? value : defaultValue;
    }
}
