using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Pracrice.Data;
using Pracrice.Models;

namespace Pracrice.Services;

public sealed record TransportData(
    IReadOnlyList<Warehouse> Warehouses,
    IReadOnlyList<Store> Stores,
    int[] Supplies,
    int[,] Costs);

public static class CalculationService
{
    private const int TransportCostPerKm = 50;

    public static async Task<IReadOnlyList<PlanProductRow>> LoadPlanProductsAsync()
    {
        await using var context = AppDatabase.CreateContext();
        var products = await context.Products.AsNoTracking()
            .Include(product => product.ProductRecipes)
            .ThenInclude(recipe => recipe.Resource)
            .OrderBy(product => product.Name)
            .ToListAsync();

        return products
            .Where(product => product.ProductRecipes.Count > 0)
            .Select(product =>
            {
                decimal raw = 0;
                decimal fill = 0;
                decimal pack = 0;

                foreach (var recipe in product.ProductRecipes)
                {
                    string unit = recipe.Resource.Unit.Trim().ToLowerInvariant();
                    if (unit == "кг")
                    {
                        raw += recipe.Consumption;
                    }
                    else if (unit == "л")
                    {
                        fill += recipe.Consumption;
                    }
                    else if (unit is "шт." or "шт" or "штука")
                    {
                        pack += recipe.Consumption;
                    }
                    else
                    {
                        raw += recipe.Consumption;
                    }
                }

                return new PlanProductRow
                {
                    Product = product.Name,
                    Raw = raw,
                    Fill = fill,
                    Pack = pack,
                    Profit = product.Price
                };
            })
            .ToList();
    }

    public static PlanCalculationResult CalculatePlan(
        IReadOnlyList<PlanProductRow> products,
        decimal rawLimit,
        decimal fillLimit,
        decimal packLimit)
    {
        var usable = products
            .Where(product => product.IsIncluded &&
                              (product.Raw > 0 || product.Fill > 0 || product.Pack > 0))
            .ToList();

        if (usable.Count == 0 || rawLimit <= 0 || fillLimit <= 0 || packLimit <= 0)
        {
            return new PlanCalculationResult
            {
                Rows = Array.Empty<PlanResultRow>(),
                TotalProfit = 0,
                RawUsagePercent = 0,
                FillUsagePercent = 0,
                PackUsagePercent = 0
            };
        }

        double[][] a =
        {
            usable.Select(product => (double)product.Raw).ToArray(),
            usable.Select(product => (double)product.Fill).ToArray(),
            usable.Select(product => (double)product.Pack).ToArray()
        };
        double[] b = { (double)rawLimit, (double)fillLimit, (double)packLimit };
        double[] c = usable.Select(product => (double)product.Profit).ToArray();

        double[] continuous = SolveLinearProgram(a, b, c);
        int[] units = continuous.Select(value => Math.Max(0, (int)Math.Floor(value + 1e-6))).ToArray();

        double[] remaining =
        {
            (double)rawLimit - usable.Select((product, index) => (double)product.Raw * units[index]).Sum(),
            (double)fillLimit - usable.Select((product, index) => (double)product.Fill * units[index]).Sum(),
            (double)packLimit - usable.Select((product, index) => (double)product.Pack * units[index]).Sum()
        };

        while (true)
        {
            int best = -1;
            for (int index = 0; index < usable.Count; index++)
            {
                bool fits = usable[index].Raw * units[index] >= 0
                            && (double)usable[index].Raw <= remaining[0] + 1e-6
                            && (double)usable[index].Fill <= remaining[1] + 1e-6
                            && (double)usable[index].Pack <= remaining[2] + 1e-6;

                if (!fits)
                {
                    continue;
                }

                if (best < 0 || usable[index].Profit > usable[best].Profit)
                {
                    best = index;
                }
            }

            if (best < 0)
            {
                break;
            }

            units[best]++;
            remaining[0] -= (double)usable[best].Raw;
            remaining[1] -= (double)usable[best].Fill;
            remaining[2] -= (double)usable[best].Pack;
        }

        int totalUnits = units.Sum();
        var resultRows = new List<PlanResultRow>();
        decimal totalProfit = 0;

        for (int index = 0; index < usable.Count; index++)
        {
            if (units[index] <= 0)
            {
                continue;
            }

            decimal profit = units[index] * usable[index].Profit;
            totalProfit += profit;
            resultRows.Add(new PlanResultRow
            {
                Product = usable[index].Product,
                Volume = units[index],
                Profit = profit,
                Share = totalUnits == 0
                    ? "0 %"
                    : (units[index] * 100m / totalUnits).ToString("N1", CultureInfo.CurrentCulture) + " %"
            });
        }

        decimal rawUsed = usable.Select((product, index) => product.Raw * units[index]).Sum();
        decimal fillUsed = usable.Select((product, index) => product.Fill * units[index]).Sum();
        decimal packUsed = usable.Select((product, index) => product.Pack * units[index]).Sum();

        return new PlanCalculationResult
        {
            Rows = resultRows,
            TotalProfit = totalProfit,
            RawUsagePercent = rawLimit == 0 ? 0 : rawUsed * 100m / rawLimit,
            FillUsagePercent = fillLimit == 0 ? 0 : fillUsed * 100m / fillLimit,
            PackUsagePercent = packLimit == 0 ? 0 : packUsed * 100m / packLimit
        };
    }

    private static double[] SolveLinearProgram(double[][] a, double[] b, double[] c)
    {
        int constraintCount = b.Length;
        int variableCount = c.Length;
        int totalColumns = variableCount + constraintCount;
        var table = new double[constraintCount + 1, totalColumns + 1];
        int[] basis = new int[constraintCount];

        for (int row = 0; row < constraintCount; row++)
        {
            for (int column = 0; column < variableCount; column++)
            {
                table[row, column] = a[row][column];
            }

            table[row, variableCount + row] = 1;
            table[row, totalColumns] = b[row];
            basis[row] = variableCount + row;
        }

        for (int column = 0; column < variableCount; column++)
        {
            table[constraintCount, column] = -c[column];
        }

        for (int iteration = 0; iteration < 1000; iteration++)
        {
            int pivotColumn = -1;
            double best = -1e-9;
            for (int column = 0; column < totalColumns; column++)
            {
                if (table[constraintCount, column] < best)
                {
                    best = table[constraintCount, column];
                    pivotColumn = column;
                }
            }

            if (pivotColumn < 0)
            {
                break;
            }

            int pivotRow = -1;
            double bestRatio = double.MaxValue;
            for (int row = 0; row < constraintCount; row++)
            {
                if (table[row, pivotColumn] <= 1e-9)
                {
                    continue;
                }

                double ratio = table[row, totalColumns] / table[row, pivotColumn];
                if (ratio < bestRatio)
                {
                    bestRatio = ratio;
                    pivotRow = row;
                }
            }

            if (pivotRow < 0)
            {
                break;
            }

            double pivot = table[pivotRow, pivotColumn];
            for (int column = 0; column <= totalColumns; column++)
            {
                table[pivotRow, column] /= pivot;
            }

            for (int row = 0; row <= constraintCount; row++)
            {
                if (row == pivotRow)
                {
                    continue;
                }

                double factor = table[row, pivotColumn];
                if (Math.Abs(factor) < 1e-12)
                {
                    continue;
                }

                for (int column = 0; column <= totalColumns; column++)
                {
                    table[row, column] -= factor * table[pivotRow, column];
                }
            }

            basis[pivotRow] = pivotColumn;
        }

        var solution = new double[variableCount];
        for (int row = 0; row < constraintCount; row++)
        {
            if (basis[row] < variableCount)
            {
                solution[basis[row]] = table[row, totalColumns];
            }
        }

        return solution;
    }

    public static async Task<TransportData> LoadTransportAsync()
    {
        await using var context = AppDatabase.CreateContext();
        var warehouses = await context.Warehouses.AsNoTracking()
            .Include(warehouse => warehouse.WarehouseStocks)
            .OrderBy(warehouse => warehouse.Name)
            .ToListAsync();
        var stores = await context.Stores.AsNoTracking()
            .OrderBy(store => store.Id)
            .ToListAsync();
        var distances = await context.StoreWarehouseDistances.AsNoTracking()
            .ToListAsync();

        if (stores.Count > 3)
        {
            throw new ValidationException("В расчётном модуле поддерживается не более трёх магазинов.");
        }

        var supplies = warehouses
            .Select(warehouse => warehouse.WarehouseStocks.Sum(stock => stock.Quantity))
            .ToArray();
        var costs = new int[warehouses.Count, stores.Count];

        for (int warehouseIndex = 0; warehouseIndex < warehouses.Count; warehouseIndex++)
        {
            for (int storeIndex = 0; storeIndex < stores.Count; storeIndex++)
            {
                var distance = distances.FirstOrDefault(item =>
                    item.WarehouseId == warehouses[warehouseIndex].Id &&
                    item.StoreId == stores[storeIndex].Id);
                costs[warehouseIndex, storeIndex] = distance is null
                    ? 1_000_000
                    : (int)Math.Round(distance.DistanceKm * TransportCostPerKm);
            }
        }

        return new TransportData(warehouses, stores, supplies, costs);
    }

    public static IReadOnlyList<TransportTariffRow> BuildTransportTariffs(TransportData data)
    {
        var rows = new List<TransportTariffRow>();
        for (int warehouseIndex = 0; warehouseIndex < data.Warehouses.Count; warehouseIndex++)
        {
            rows.Add(new TransportTariffRow
            {
                Warehouse = data.Warehouses[warehouseIndex].Name,
                Shop1 = data.Stores.Count > 0 ? data.Costs[warehouseIndex, 0] : 0,
                Shop2 = data.Stores.Count > 1 ? data.Costs[warehouseIndex, 1] : 0,
                Shop3 = data.Stores.Count > 2 ? data.Costs[warehouseIndex, 2] : 0,
                Supply = data.Supplies[warehouseIndex]
            });
        }

        return rows;
    }

    public static IReadOnlyList<TransportStoreRow> BuildTransportStores(TransportData data) =>
        data.Stores
            .Select(store => new TransportStoreRow
            {
                Id = store.Id,
                Name = store.Name,
                Demand = store.Demand
            })
            .ToList();

    public static TransportCalculationResult CalculateTransport(TransportData data, bool isOpen)
    {
        int warehouseCount = data.Warehouses.Count;
        int storeCount = data.Stores.Count;
        int totalSupply = data.Supplies.Sum();
        int totalDemand = data.Stores.Sum(store => store.Demand);

        if (warehouseCount == 0 || storeCount == 0)
        {
            throw new ValidationException("Для расчёта нужны склады и магазины.");
        }

        if (!isOpen && totalSupply != totalDemand)
        {
            throw new ValidationException(
                $"Закрытая задача невозможна: запас {totalSupply} ед., потребность {totalDemand} ед.\n" +
                "Выберите открытую задачу или измените потребность магазинов.");
        }

        int supplyCount = warehouseCount;
        int demandCount = storeCount;
        bool hasDummyWarehouse = false;
        bool hasDummyStore = false;

        if (isOpen && totalSupply > totalDemand)
        {
            demandCount++;
            hasDummyStore = true;
        }
        else if (isOpen && totalDemand > totalSupply)
        {
            supplyCount++;
            hasDummyWarehouse = true;
        }

        var supplies = new int[supplyCount];
        var demands = new int[demandCount];
        var costs = new int[supplyCount, demandCount];

        for (int index = 0; index < warehouseCount; index++)
        {
            supplies[index] = data.Supplies[index];
        }

        for (int index = 0; index < storeCount; index++)
        {
            demands[index] = data.Stores[index].Demand;
        }

        for (int warehouseIndex = 0; warehouseIndex < supplyCount; warehouseIndex++)
        {
            for (int storeIndex = 0; storeIndex < demandCount; storeIndex++)
            {
                costs[warehouseIndex, storeIndex] =
                    warehouseIndex < warehouseCount && storeIndex < storeCount
                        ? data.Costs[warehouseIndex, storeIndex]
                        : 0;
            }
        }

        if (hasDummyStore)
        {
            demands[^1] = totalSupply - totalDemand;
        }

        if (hasDummyWarehouse)
        {
            supplies[^1] = totalDemand - totalSupply;
        }

        int[,] flows = SolveTransport(supplies, demands, costs);

        var rows = new List<TransportResultRow>();
        decimal totalCost = 0;

        for (int warehouseIndex = 0; warehouseIndex < warehouseCount; warehouseIndex++)
        {
            rows.Add(new TransportResultRow
            {
                Warehouse = data.Warehouses[warehouseIndex].Name,
                Shop1 = storeCount > 0 ? flows[warehouseIndex, 0] : 0,
                Shop2 = storeCount > 1 ? flows[warehouseIndex, 1] : 0,
                Shop3 = storeCount > 2 ? flows[warehouseIndex, 2] : 0,
                Reserve = hasDummyStore ? flows[warehouseIndex, demandCount - 1] : 0
            });

            for (int storeIndex = 0; storeIndex < storeCount; storeIndex++)
            {
                totalCost += flows[warehouseIndex, storeIndex] * (decimal)costs[warehouseIndex, storeIndex];
            }
        }

        if (hasDummyWarehouse)
        {
            rows.Add(new TransportResultRow
            {
                Warehouse = "Дефицит",
                Shop1 = storeCount > 0 ? flows[supplyCount - 1, 0] : 0,
                Shop2 = storeCount > 1 ? flows[supplyCount - 1, 1] : 0,
                Shop3 = storeCount > 2 ? flows[supplyCount - 1, 2] : 0,
                Reserve = 0
            });
        }

        return new TransportCalculationResult
        {
            Rows = rows,
            Stores = data.Stores,
            TotalCost = totalCost,
            TotalSupply = totalSupply,
            TotalDemand = totalDemand,
            IsOpen = isOpen
        };
    }

    private static int[,] SolveTransport(int[] supplies, int[] demands, int[,] costs)
    {
        int source = 0;
        int warehouseOffset = 1;
        int storeOffset = warehouseOffset + supplies.Length;
        int sink = storeOffset + demands.Length;
        int nodeCount = sink + 1;
        var graph = new List<Edge>[nodeCount];
        for (int index = 0; index < nodeCount; index++)
        {
            graph[index] = new List<Edge>();
        }

        for (int warehouseIndex = 0; warehouseIndex < supplies.Length; warehouseIndex++)
        {
            AddEdge(graph, source, warehouseOffset + warehouseIndex, supplies[warehouseIndex], 0);
        }

        var transportEdges = new Edge[supplies.Length, demands.Length];
        for (int warehouseIndex = 0; warehouseIndex < supplies.Length; warehouseIndex++)
        {
            for (int storeIndex = 0; storeIndex < demands.Length; storeIndex++)
            {
                transportEdges[warehouseIndex, storeIndex] =
                    AddEdge(graph, warehouseOffset + warehouseIndex, storeOffset + storeIndex,
                        Math.Min(supplies[warehouseIndex], demands[storeIndex]), costs[warehouseIndex, storeIndex]);
            }
        }

        for (int storeIndex = 0; storeIndex < demands.Length; storeIndex++)
        {
            AddEdge(graph, storeOffset + storeIndex, sink, demands[storeIndex], 0);
        }

        int targetFlow = Math.Min(supplies.Sum(), demands.Sum());
        int flow = 0;

        while (flow < targetFlow)
        {
            var distance = new int[nodeCount];
            var parentNode = new int[nodeCount];
            var parentEdge = new int[nodeCount];
            Array.Fill(distance, int.MaxValue);
            Array.Fill(parentNode, -1);
            Array.Fill(parentEdge, -1);
            distance[source] = 0;

            for (int iteration = 0; iteration < nodeCount - 1; iteration++)
            {
                bool changed = false;
                for (int node = 0; node < nodeCount; node++)
                {
                    if (distance[node] == int.MaxValue)
                    {
                        continue;
                    }

                    for (int edgeIndex = 0; edgeIndex < graph[node].Count; edgeIndex++)
                    {
                        Edge edge = graph[node][edgeIndex];
                        if (edge.Capacity <= 0 || distance[edge.To] <= distance[node] + edge.Cost)
                        {
                            continue;
                        }

                        distance[edge.To] = distance[node] + edge.Cost;
                        parentNode[edge.To] = node;
                        parentEdge[edge.To] = edgeIndex;
                        changed = true;
                    }
                }

                if (!changed)
                {
                    break;
                }
            }

            if (distance[sink] == int.MaxValue)
            {
                break;
            }

            int augmentation = targetFlow - flow;
            for (int node = sink; node != source; node = parentNode[node])
            {
                Edge edge = graph[parentNode[node]][parentEdge[node]];
                augmentation = Math.Min(augmentation, edge.Capacity);
            }

            for (int node = sink; node != source; node = parentNode[node])
            {
                Edge edge = graph[parentNode[node]][parentEdge[node]];
                edge.Capacity -= augmentation;
                graph[edge.To][edge.Reverse].Capacity += augmentation;
            }

            flow += augmentation;
        }

        var result = new int[supplies.Length, demands.Length];
        for (int warehouseIndex = 0; warehouseIndex < supplies.Length; warehouseIndex++)
        {
            for (int storeIndex = 0; storeIndex < demands.Length; storeIndex++)
            {
                Edge edge = transportEdges[warehouseIndex, storeIndex];
                result[warehouseIndex, storeIndex] = edge.InitialCapacity - edge.Capacity;
            }
        }

        return result;
    }

    private static Edge AddEdge(List<Edge>[] graph, int from, int to, int capacity, int cost)
    {
        var forward = new Edge(to, graph[to].Count, capacity, cost);
        var reverse = new Edge(from, graph[from].Count, 0, -cost);
        graph[from].Add(forward);
        graph[to].Add(reverse);
        return forward;
    }

    private sealed class Edge
    {
        public Edge(int to, int reverse, int capacity, int cost)
        {
            To = to;
            Reverse = reverse;
            Capacity = capacity;
            InitialCapacity = capacity;
            Cost = cost;
        }

        public int To { get; }

        public int Reverse { get; }

        public int Capacity { get; set; }

        public int InitialCapacity { get; }

        public int Cost { get; }
    }

    public static EquipmentCalculationResult CalculateEquipment(
        int age,
        int horizon,
        decimal replacementPrice,
        decimal annualRevenue,
        decimal currentOperatingCost,
        decimal newOperatingCost,
        decimal costGrowthPercent,
        int serviceLife)
    {
        age = Math.Clamp(age, 0, 30);
        horizon = Math.Clamp(horizon, 1, 10);
        serviceLife = Math.Clamp(serviceLife, 1, 30);
        replacementPrice = Math.Max(0m, replacementPrice);
        annualRevenue = Math.Max(0m, annualRevenue);
        currentOperatingCost = Math.Max(0m, currentOperatingCost);
        newOperatingCost = Math.Max(0m, newOperatingCost);

        decimal factor = 1m + Math.Max(0m, costGrowthPercent) / 100m;

        decimal Compound(decimal value, int years)
        {
            decimal result = value;
            for (int index = 0; index < years; index++)
            {
                result *= factor;
            }

            return result;
        }

        decimal currentCostAtZero = currentOperatingCost / Compound(1m, age);

        decimal OldOperatingCost(int equipmentAge) =>
            Compound(currentCostAtZero, equipmentAge);

        decimal NewEquipmentCost(int equipmentAge) =>
            Compound(newOperatingCost, equipmentAge);

        decimal Salvage(int equipmentAge) =>
            Math.Max(0m, replacementPrice * (1m - (decimal)equipmentAge / serviceLife));

        int maxAge = age + horizon + 2;
        var oldDp = new decimal[horizon + 1, maxAge + 2];
        var newDp = new decimal[horizon + 1, maxAge + 2];
        var replaceOld = new bool[horizon + 1, maxAge + 2];
        var replaceNew = new bool[horizon + 1, maxAge + 2];

        for (int equipmentAge = 0; equipmentAge <= maxAge; equipmentAge++)
        {
            oldDp[horizon, equipmentAge] = Salvage(equipmentAge);
            newDp[horizon, equipmentAge] = Salvage(equipmentAge);
        }

        for (int year = horizon - 1; year >= 0; year--)
        {
            for (int equipmentAge = 0; equipmentAge <= maxAge; equipmentAge++)
            {
                decimal keepOld =
                    annualRevenue - OldOperatingCost(equipmentAge) +
                    oldDp[year + 1, equipmentAge + 1];
                decimal replaceOldCost =
                    annualRevenue - NewEquipmentCost(0) - replacementPrice +
                    Salvage(equipmentAge) + newDp[year + 1, 1];
                replaceOld[year, equipmentAge] = replaceOldCost >= keepOld;
                oldDp[year, equipmentAge] = Math.Max(keepOld, replaceOldCost);

                decimal keepNew =
                    annualRevenue - NewEquipmentCost(equipmentAge) +
                    newDp[year + 1, equipmentAge + 1];
                decimal replaceNewCost =
                    annualRevenue - NewEquipmentCost(0) - replacementPrice +
                    Salvage(equipmentAge) + newDp[year + 1, 1];
                bool canReplaceNew = equipmentAge >= 2;
                replaceNew[year, equipmentAge] = canReplaceNew && replaceNewCost >= keepNew;
                newDp[year, equipmentAge] = canReplaceNew
                    ? Math.Max(keepNew, replaceNewCost)
                    : keepNew;
            }
        }

        int stateAge = age;
        bool isNew = false;
        var schedule = new List<EquipmentScheduleRow>(horizon);

        for (int year = 0; year < horizon; year++)
        {
            bool replace = isNew
                ? stateAge >= 2 && replaceNew[year, stateAge]
                : replaceOld[year, stateAge];

            int ageAtStart = stateAge;
            decimal operating = replace
                ? NewEquipmentCost(0)
                : isNew
                    ? NewEquipmentCost(stateAge)
                    : OldOperatingCost(stateAge);
            decimal replacement = replace ? replacementPrice : 0m;
            decimal salvage = replace ? Salvage(stateAge) : 0m;
            decimal profit = annualRevenue - operating - replacement + salvage;

            stateAge = replace ? 1 : stateAge + 1;
            isNew |= replace;

            schedule.Add(new EquipmentScheduleRow
            {
                Year = year + 1,
                Age = ageAtStart,
                Decision = replace ? "��������" : "���������������",
                OperatingCost = decimal.Round(operating, 2),
                ReplacementCost = decimal.Round(replacement, 2),
                Revenue = decimal.Round(annualRevenue, 2),
                Profit = decimal.Round(profit, 2),
                Residual = decimal.Round(Salvage(stateAge), 2)
            });
        }

        var ageCosts = new List<EquipmentAgeCostRow>(serviceLife + 1);
        for (int equipmentAge = 0; equipmentAge <= serviceLife; equipmentAge++)
        {
            ageCosts.Add(new EquipmentAgeCostRow
            {
                Age = equipmentAge,
                OperatingCost = decimal.Round(OldOperatingCost(equipmentAge), 2),
                Residual = decimal.Round(Salvage(equipmentAge), 2)
            });
        }

        var replacementYears = schedule
            .Where(row => row.Decision == "��������")
            .Select(row => row.Year)
            .ToList();
        string replacementPlan = replacementYears.Count == 0
            ? "������ �� ���������"
            : "������: " + string.Join(", ", replacementYears.Select(year => $"��� {year}"));

        decimal totalProfit = oldDp[0, age];
        decimal totalCosts = annualRevenue * horizon - totalProfit;

        return new EquipmentCalculationResult
        {
            AgeCosts = ageCosts,
            Schedule = schedule,
            TotalProfit = decimal.Round(totalProfit, 2),
            TotalCosts = decimal.Round(totalCosts, 2),
            FinalResidual = decimal.Round(Salvage(stateAge), 2),
            ReplacementPlan = replacementPlan
        };
    }
    public static async Task<IReadOnlyList<CashFlowRow>> LoadCashFlowAsync()
    {
        await using var context = AppDatabase.CreateContext();
        var stores = await context.Stores.AsNoTracking()
            .OrderBy(store => store.Id)
            .ToListAsync();

        var rows = new List<CashFlowRow>();
        for (int index = 0; index < stores.Count; index++)
        {
            rows.Add(new CashFlowRow
            {
                Market = stores[index].Name,
                Flow = stores[index].CustomerFlow > 0 ? stores[index].CustomerFlow : 100 + index * 20,
                ServiceRate = stores[index].ServiceRate > 0 ? stores[index].ServiceRate : 25,
                Limit = stores[index].RegisterLimit > 0 ? stores[index].RegisterLimit : 6
            });
        }

        return rows;
    }

    public static CashCalculationResult CalculateCash(
        IReadOnlyList<CashFlowRow> flows,
        decimal targetWaitMinutes,
        decimal targetLoadPercent)
    {
        decimal targetLoad = Math.Clamp(targetLoadPercent / 100m, 0.1m, 0.99m);
        var rows = new List<CashResultRow>();
        int totalCashiers = 0;
        int totalAvailable = 0;

        foreach (var flow in flows.Where(flow => flow.IsIncluded))
        {
            int cashiers = Math.Max(1, (int)Math.Ceiling(flow.Flow / (flow.ServiceRate * (double)targetLoad)));
            double wait = double.MaxValue;

            while (cashiers <= flow.Limit)
            {
                double load = flow.Flow / (cashiers * (double)flow.ServiceRate);
                wait = load >= 0.999
                    ? 999
                    : 60 * load / (cashiers * flow.ServiceRate - flow.Flow);

                if (load <= (double)targetLoad + 1e-6 && wait <= (double)targetWaitMinutes + 1e-6)
                {
                    break;
                }

                cashiers++;
            }

            cashiers = Math.Min(cashiers, flow.Limit);
            double finalLoad = flow.Flow / (cashiers * (double)flow.ServiceRate);
            double finalWait = finalLoad >= 0.999
                ? 999
                : 60 * finalLoad / (cashiers * flow.ServiceRate - flow.Flow);

            rows.Add(new CashResultRow
            {
                Market = flow.Market,
                Count = cashiers,
                LoadPercent = (finalLoad * 100).ToString("N1", CultureInfo.CurrentCulture) + " %",
                WaitTime = finalWait >= 999
                    ? "перегрузка"
                    : finalWait.ToString("N1", CultureInfo.CurrentCulture) + " мин"
            });

            totalCashiers += cashiers;
            totalAvailable += flow.Limit;
        }

        return new CashCalculationResult
        {
            Rows = rows,
            TotalCashiers = totalCashiers,
            TotalAvailable = totalAvailable
        };
    }

    public static async Task<IReadOnlyList<InvestmentProjectRow>> LoadInvestmentProjectsAsync()
    {
        await using var context = AppDatabase.CreateContext();
        var projects = await context.InvestmentProjects.AsNoTracking()
            .Include(project => project.Company)
            .OrderBy(project => project.Company.Name)
            .ThenBy(project => project.Name)
            .ToListAsync();

        return projects.Select(project =>
        {
            decimal profitability = project.Investment == 0
                ? 0
                : project.AnnualIncome * 100m / project.Investment;

            return new InvestmentProjectRow
            {
                ProjectId = project.Id,
                ProjectName = project.Name,
                Company = project.Company.Name,
                Invests = project.Investment / 1_000_000m,
                Income = project.AnnualIncome / 1_000_000m,
                ProfitPercent = profitability.ToString("N1", CultureInfo.CurrentCulture) + " %",
                InvestmentValue = project.Investment
            };
        }).ToList();
    }

    public static InvestmentCalculationResult CalculateInvestments(
        IReadOnlyList<InvestmentProjectRow> projects,
        decimal budget)
    {
        var selected = projects.Where(project => project.IsIncluded).ToList();
        int count = selected.Count;
        int bestMask = 0;
        decimal bestIncome = 0;
        decimal bestInvestment = 0;

        for (int mask = 0; mask < 1 << count; mask++)
        {
            decimal investment = 0;
            decimal income = 0;
            for (int index = 0; index < count; index++)
            {
                if ((mask & (1 << index)) == 0)
                {
                    continue;
                }

                investment += selected[index].InvestmentValue;
                income += selected[index].Income * 1_000_000m;
            }

            if (investment <= budget && income > bestIncome)
            {
                bestIncome = income;
                bestInvestment = investment;
                bestMask = mask;
            }
        }

        var portfolio = new List<InvestmentPortfolioRow>();
        for (int index = 0; index < count; index++)
        {
            if ((bestMask & (1 << index)) == 0)
            {
                continue;
            }

            var project = selected[index];
            portfolio.Add(new InvestmentPortfolioRow
            {
                SelectedProject = project.ProjectName,
                Company = project.Company,
                Allocated = project.Invests.ToString("N2", CultureInfo.CurrentCulture) + " млн ₽",
                Share = bestInvestment == 0
                    ? "0 %"
                    : (project.InvestmentValue * 100m / bestInvestment)
                        .ToString("N1", CultureInfo.CurrentCulture) + " %"
            });
        }

        return new InvestmentCalculationResult
        {
            Portfolio = portfolio,
            TotalIncome = bestIncome,
            TotalAllocated = bestInvestment
        };
    }
}
