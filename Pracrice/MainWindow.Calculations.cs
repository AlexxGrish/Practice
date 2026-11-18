using Pracrice.Models;
using Pracrice.Services;
using Pracrice.Windows;
using System.Globalization;
using System.Text.RegularExpressions;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;

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
    private int _investmentHorizon = 1;
    private bool _isLoadingPlan;
    private bool _isLoadingTransport;
    private bool _isLoadingCash;
    private bool _isLoadingInvestments;

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

        // Перестраиваем колонки таблицы результатов ResultRouteGrid
        ResultRouteGrid.Columns.Clear();
        ResultRouteGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "Склад",
            Binding = new Binding(nameof(TransportResultRow.Warehouse)),
            Width = new DataGridLength(1, DataGridLengthUnitType.Star)
        });

        for (int i = 0; i < _transportData.Stores.Count; i++)
        {
            ResultRouteGrid.Columns.Add(new DataGridTextColumn
            {
                Header = _transportData.Stores[i].Name,
                Binding = new Binding($"Shops[{i}]"),
                Width = new DataGridLength(95)
            });
        }

        ResultRouteGrid.Columns.Add(new DataGridTextColumn
        {
            Header = "Резерв",
            Binding = new Binding(nameof(TransportResultRow.Reserve)),
            Width = new DataGridLength(80)
        });
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
        CalculateEquipmentView();
    }

    private void CalculateEquipmentView()
    {
        int age = ExtractFirstInteger(EquipmentAgeText.Text, 6);
        int horizon = ExtractFirstInteger(EquipmentHorizonText.Text, 3);
        int serviceLife = ExtractFirstInteger(EquipmentLifeText.Text, 8);

        if (age < 0 || age > 30 || horizon < 1 || horizon > 10 || serviceLife < 1 || serviceLife > 30)
        {
            DialogService.ShowWarning("Проверьте возраст, горизонт и срок службы оборудования.");
            return;
        }

        if (!TryParseDecimal(EquipmentReplacementPriceText.Text, out decimal replacementPrice) ||
            !TryParseDecimal(EquipmentRevenueText.Text, out decimal revenue) ||
            !TryParseDecimal(EquipmentCurrentCostText.Text, out decimal currentCost) ||
            !TryParseDecimal(EquipmentNewCostText.Text, out decimal newCost) ||
            !TryParseDecimal(EquipmentGrowthText.Text, out decimal growth) ||
            replacementPrice < 0 || revenue < 0 || currentCost < 0 || newCost < 0 || growth < 0)
        {
            DialogService.ShowWarning("Укажите корректные параметры модели замены оборудования.");
            return;
        }

        try
        {
            EquipmentCalculationResult result = CalculationService.CalculateEquipment(
                age, horizon, replacementPrice, revenue, currentCost, newCost, growth, serviceLife);
            _equipmentResult = result;
            BellmanGrid.ItemsSource = result.BellmanRows;
            StrategyGrid.ItemsSource = result.Schedule;
            EquipmentSavingLabel.Text = "Максимальная прибыль";
            EquipmentSavingText.Text = result.TotalProfit.ToString("N2", CultureInfo.CurrentCulture) + " млн ₽";
            EquipmentDecisionText.Text =
                $"{result.ReplacementPlan}. Затраты: {result.TotalCosts.ToString("N2", CultureInfo.CurrentCulture)} млн ₽. " +
                $"Остаток на конец: {result.FinalResidual.ToString("N2", CultureInfo.CurrentCulture)} млн ₽.";
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

    private async Task LoadInvestmentsAsync()
    {
        if (_isLoadingInvestments)
        {
            return;
        }

        _isLoadingInvestments = true;
        try
        {
            _investmentProjects = await CalculationService.LoadInvestmentProjectsAsync();
            _investmentResult = null;
            InvestProjectsGrid.ItemsSource = _investmentProjects;

            if (_investmentProjects.Count == 0)
            {
                DialogService.ShowWarning("Добавьте инвестиционные проекты в справочнике.");
            }
        }
        catch (Exception exception)
        {
            App.LogException(exception);
            DialogService.ShowError("Не удалось загрузить инвестиционные проекты.\n\n" + exception.Message);
        }
        finally
        {
            _isLoadingInvestments = false;
        }
    }

    private async void BtnInvestCalc_Click(object sender, RoutedEventArgs e)
    {
        if (_investmentProjects.Count == 0)
        {
            await LoadInvestmentsAsync();
        }

        if (!TryParseDecimal(InvestBudgetText.Text, out decimal budget) || budget <= 0)
        {
            DialogService.ShowWarning("Укажите положительный инвестиционный бюджет.");
            return;
        }

        int horizon = ExtractFirstInteger(InvestHorizonText.Text, 1);
        if (horizon < 1)
        {
            DialogService.ShowWarning("Горизонт инвестирования должен быть не менее одного года.");
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
                includedProjects, budget * 1000000);
            _investmentResult = result;
            _investmentHorizon = horizon;

            InvestPortfolioGrid.ItemsSource = result.Portfolio;
            InvestIncomeLabel.Text = $"Ожидаемый доход за {horizon} г.";
            InvestIncomeText.Text =
                (result.TotalIncome * horizon / 1_000_000m).ToString("N2", CultureInfo.CurrentCulture) + " млн ₽";
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

    private async void BtnAddInvestmentProject_Click(object sender, RoutedEventArgs e)
    {
        await OpenInvestmentProjectEditorAsync(null);
    }

    private async void BtnEditInvestmentProject_Click(object sender, RoutedEventArgs e)
    {
        if (InvestProjectsGrid.SelectedItem is not InvestmentProjectRow project)
        {
            DialogService.ShowWarning("Выберите проект в таблице.");
            return;
        }

        await OpenInvestmentProjectEditorAsync(project.ProjectId);
    }

    private async Task OpenInvestmentProjectEditorAsync(int? projectId)
    {
        try
        {
            ReferenceForm form = await ReferenceService.BuildFormAsync(
                ReferenceSection.InvestmentProjects,
                projectId?.ToString(CultureInfo.InvariantCulture));
            var window = new ReferenceEditWindow(form) { Owner = this };
            if (window.ShowDialog() == true)
            {
                await LoadInvestmentsAsync();
            }
        }
        catch (ValidationException exception)
        {
            DialogService.ShowWarning(exception.Message, "Инвестиционные проекты");
        }
        catch (Exception exception)
        {
            App.LogException(exception);
            DialogService.ShowError("Не удалось открыть проект.\n\n" + exception.Message);
        }
    }

    private async void BtnInvestCompanies_Click(object sender, RoutedEventArgs e)
    {
        await OpenReferenceAndReloadAsync(ReferenceSection.InvestmentCompanies, LoadInvestmentsAsync);
    }

    private async void BtnCashStores_Click(object sender, RoutedEventArgs e)
    {
        await OpenReferenceAndReloadAsync(ReferenceSection.Stores, LoadCashAsync);
    }

    private async void BtnPlanRecipes_Click(object sender, RoutedEventArgs e)
    {
        await OpenReferenceAndReloadAsync(ReferenceSection.ProductRecipes, LoadPlanAsync);
    }

    private async void BtnRouteReferences_Click(object sender, RoutedEventArgs e)
    {
        await OpenReferenceAndReloadAsync(ReferenceSection.Stores, LoadTransportAsync);
    }

    private async Task OpenReferenceAndReloadAsync(ReferenceSection section, Func<Task> reload)
    {
        if (CurrentUser is null)
        {
            return;
        }

        try
        {
            var window = new ReferenceWindow(section, CurrentUser) { Owner = this };
            window.ShowDialog();
            await reload();
        }
        catch (Exception exception)
        {
            App.LogException(exception);
            DialogService.ShowError("Не удалось открыть справочник.\n\n" + exception.Message);
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
        headers.AddRange(_transportResult.Stores.Select(store => ShortStoreName(store.Name)));
        headers.Add("Резерв");

        var rows = _transportResult.Rows
            .Select(row =>
            {
                var cells = new List<string>(row.Shops.Length + 2)
                {
                System.Net.WebUtility.HtmlEncode(row.Warehouse)
                };

                foreach (int flow in row.Shops)
                {
                    cells.Add(flow.ToString(CultureInfo.CurrentCulture));
                }

                cells.Add(row.Reserve.ToString(CultureInfo.CurrentCulture));
                return (IReadOnlyList<string>)cells;
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
        if (_equipmentResult is null || _equipmentResult.Schedule.Count == 0)
        {
            DialogService.ShowWarning("Сначала выполните расчёт стратегии замены.");
            return;
        }

        var headers = new[]
        {
            "Год",
            "Возраст",
            "Решение",
            "Эксплуатация, млн ₽",
            "Замена, млн ₽",
            "Доход, млн ₽",
            "Прибыль, млн ₽",
            "Остаточная стоимость, млн ₽"
        };
        var rows = _equipmentResult.Schedule
            .Select(row => (IReadOnlyList<string>)new[]
            {
                row.Year.ToString(CultureInfo.CurrentCulture),
                row.Age.ToString(CultureInfo.CurrentCulture),
                row.Decision,
                row.OperatingCost.ToString("N2", CultureInfo.CurrentCulture),
                row.ReplacementCost.ToString("N2", CultureInfo.CurrentCulture),
                row.Revenue.ToString("N2", CultureInfo.CurrentCulture),
                row.Profit.ToString("N2", CultureInfo.CurrentCulture),
                row.Residual.ToString("N2", CultureInfo.CurrentCulture)
            })
            .ToList();

        string subtitle =
            $"Оборудование: {EquipmentNameText.Text}. Возраст: {EquipmentAgeText.Text}. Горизонт: {EquipmentHorizonText.Text}. " +
            $"График: {_equipmentResult.ReplacementPlan}. " +
            $"Максимальная прибыль: {_equipmentResult.TotalProfit.ToString("N2", CultureInfo.CurrentCulture)} млн ₽. " +
            $"Затраты: {_equipmentResult.TotalCosts.ToString("N2", CultureInfo.CurrentCulture)} млн ₽. " +
            $"Остаток на конец: {_equipmentResult.FinalResidual.ToString("N2", CultureInfo.CurrentCulture)} млн ₽.";

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

        var headers = new[] { "Проект", "Компания", "Выделено", "Доля" };
        var rows = _investmentResult.Portfolio
            .Select(row => (IReadOnlyList<string>)new[]
            {
                row.SelectedProject,
                row.Company,
                row.Allocated,
                row.Share
            })
            .ToList();

        string subtitle =
            $"Доход в год: {(_investmentResult.TotalIncome / 1_000_000m).ToString("N2", CultureInfo.CurrentCulture)} млн ₽. " +
            $"Доход за {_investmentHorizon} г.: {(_investmentResult.TotalIncome * _investmentHorizon / 1_000_000m).ToString("N2", CultureInfo.CurrentCulture)} млн ₽. " +
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

    private static void AddRoute(List<string> routes, string warehouseName, IReadOnlyList<Store> stores, TransportResultRow row)
    {
        for (int i = 0; i < stores.Count && i < row.Shops.Length; i++)
        {
            if (row.Shops[i] > 0)
            {
                routes.Add($"{warehouseName} → {stores[i].Name}: {row.Shops[i]} ед.");
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
