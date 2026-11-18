using Microsoft.EntityFrameworkCore;
using Pracrice.Data;
using Pracrice.Models;
using System.Globalization;

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
                    Raw = decimal.Round(raw, 4),
                    Fill = decimal.Round(fill, 4),
                    Pack = decimal.Round(pack, 4),
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
                              product.Profit > 0 &&
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

        int n = usable.Count;
        int m = 3;
        int totalCols = n + m + 1;
        int rhsCol = n + m;
        double[,] tableau = new double[m + 1, totalCols];

        for (int j = 0; j < n; j++)
        {
            tableau[0, j] = (double)usable[j].Raw;
            tableau[1, j] = (double)usable[j].Fill;
            tableau[2, j] = (double)usable[j].Pack;
            tableau[m, j] = -(double)usable[j].Profit;
        }

        tableau[0, n + 0] = 1.0;
        tableau[0, rhsCol] = (double)rawLimit;

        tableau[1, n + 1] = 1.0;
        tableau[1, rhsCol] = (double)fillLimit;

        tableau[2, n + 2] = 1.0;
        tableau[2, rhsCol] = (double)packLimit;

        int[] basis = { n, n + 1, n + 2 };
        const double eps = 1e-9;

        // Прямой симплекс-метод (правило Бланда)
        for (int iter = 0; iter < 1000; iter++)
        {
            int pivotCol = -1;
            for (int j = 0; j < rhsCol; j++)
            {
                if (tableau[m, j] < -eps)
                {
                    pivotCol = j;
                    break;
                }
            }

            if (pivotCol == -1)
            {
                break;
            }

            int pivotRow = -1;
            double minRatio = double.PositiveInfinity;

            for (int i = 0; i < m; i++)
            {
                double aij = tableau[i, pivotCol];
                if (aij > eps)
                {
                    double ratio = tableau[i, rhsCol] / aij;
                    if (ratio < minRatio - eps || (Math.Abs(ratio - minRatio) <= eps && pivotRow != -1 && basis[i] < basis[pivotRow]))
                    {
                        minRatio = ratio;
                        pivotRow = i;
                    }
                }
            }

            if (pivotRow == -1)
            {
                break;
            }

            double pivotVal = tableau[pivotRow, pivotCol];
            for (int j = 0; j <= rhsCol; j++)
            {
                tableau[pivotRow, j] /= pivotVal;
            }

            for (int i = 0; i <= m; i++)
            {
                if (i == pivotRow)
                {
                    continue;
                }

                double factor = tableau[i, pivotCol];
                if (Math.Abs(factor) > eps)
                {
                    for (int j = 0; j <= rhsCol; j++)
                    {
                        tableau[i, j] -= factor * tableau[pivotRow, j];
                    }
                }
            }

            basis[pivotRow] = pivotCol;
        }

        double[] continuous = new double[n];
        for (int i = 0; i < m; i++)
        {
            if (basis[i] < n)
            {
                continuous[basis[i]] = Math.Max(0.0, tableau[i, rhsCol]);
            }
        }

        // Округляем непрерывное решение вниз до целых единиц продукции
        int[] units = continuous
            .Select(value => Math.Max(0, (int)Math.Floor(value + 1e-6)))
            .ToArray();

        decimal remRaw = rawLimit - usable.Select((p, i) => p.Raw * units[i]).Sum();
        decimal remFill = fillLimit - usable.Select((p, i) => p.Fill * units[i]).Sum();
        decimal remPack = packLimit - usable.Select((p, i) => p.Pack * units[i]).Sum();

        // Адекватный целочисленный добор остатков ресурсов с учётом нормированной эффективности
        while (true)
        {
            int best = -1;
            decimal bestScore = -1m;

            for (int index = 0; index < usable.Count; index++)
            {
                var p = usable[index];
                if (p.Raw > remRaw || p.Fill > remFill || p.Pack > remPack)
                {
                    continue;
                }

                // Вычисляем долю потребляемых остатков ресурсов для взвешенной оценки эффективности
                decimal resourceLoad =
                    (remRaw > 0 ? p.Raw / remRaw : 0m) +
                    (remFill > 0 ? p.Fill / remFill : 0m) +
                    (remPack > 0 ? p.Pack / remPack : 0m);

                decimal score = resourceLoad > 0 ? p.Profit / resourceLoad : p.Profit;

                if (score > bestScore)
                {
                    bestScore = score;
                    best = index;
                }
            }

            if (best < 0)
            {
                break;
            }

            units[best]++;
            remRaw -= usable[best].Raw;
            remFill -= usable[best].Fill;
            remPack -= usable[best].Pack;
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
            RawUsagePercent = rawLimit == 0 ? 0 : Math.Min(100m, rawUsed * 100m / rawLimit),
            FillUsagePercent = fillLimit == 0 ? 0 : Math.Min(100m, fillUsed * 100m / fillLimit),
            PackUsagePercent = packLimit == 0 ? 0 : Math.Min(100m, packUsed * 100m / packLimit)
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
            var shopFlows = new int[storeCount];
            for (int storeIndex = 0; storeIndex < storeCount; storeIndex++)
            {
                shopFlows[storeIndex] = flows[warehouseIndex, storeIndex];
                totalCost += flows[warehouseIndex, storeIndex] * (decimal)costs[warehouseIndex, storeIndex];
            }

            rows.Add(new TransportResultRow
            {
                Warehouse = data.Warehouses[warehouseIndex].Name,
                Shops = shopFlows,
                Reserve = hasDummyStore ? flows[warehouseIndex, demandCount - 1] : 0
            });
        }

        if (hasDummyWarehouse)
        {
            var deficitFlows = new int[storeCount];
            for (int storeIndex = 0; storeIndex < storeCount; storeIndex++)
            {
                deficitFlows[storeIndex] = flows[supplyCount - 1, storeIndex];
            }

            rows.Add(new TransportResultRow
            {
                Warehouse = "Дефицит",
                Shops = deficitFlows,
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
                Decision = replace ? "Заменить" : "Эксплуатировать",
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
      .Where(row => row.Decision == "Заменить")
      .Select(row => row.Year)
      .ToList();

        string replacementPlan = replacementYears.Count == 0
            ? "Замена не требуется"
            : "Замена: " + string.Join(", ", replacementYears.Select(year => $"год {year}"));


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

        var rates = await context.InvestmentRates.AsNoTracking()
            .ToListAsync();

        var maxAmountByCompany = rates
            .GroupBy(rate => rate.CompanyId)
            .ToDictionary(group => group.Key, group => group.Max(rate => rate.MaxAmount));

        return projects.Select(project =>
        {
            decimal profitability = project.Investment == 0
                ? 0
                : project.AnnualIncome * 100m / project.Investment;

            int maxCopies = 1;
            if (project.Investment > 0m &&
                maxAmountByCompany.TryGetValue(project.CompanyId, out decimal companyMaxAmount) &&
                companyMaxAmount > 0m)
            {
                maxCopies = Math.Max(1, (int)Math.Floor(companyMaxAmount / project.Investment));
            }

            return new InvestmentProjectRow
            {
                ProjectId = project.Id,
                ProjectName = project.Name,
                Company = project.Company.Name,
                Invests = project.Investment / 1_000_000m,
                Income = project.AnnualIncome / 1_000_000m,
                ProfitPercent = profitability.ToString("N1", CultureInfo.CurrentCulture) + " %",
                InvestmentValue = project.Investment,
                MaxCopies = maxCopies
            };
        }).ToList();
    }


    public static InvestmentCalculationResult CalculateInvestments(
     IReadOnlyList<InvestmentProjectRow> projects,
     decimal budget)
    {
        var selected = projects.Where(project => project.IsIncluded && project.MaxCopies > 0).ToList();
        int count = selected.Count;
        decimal bestIncome = 0m;
        decimal bestInvestment = 0m;
        decimal[] allocatedAmounts = new decimal[count]; // точная сумма вложений в каждый проект
        int[] integerCounts = new int[count];          // количество целых вложений в каждый проект

        if (count > 0 && budget > 0m)
        {
            // 1. Вычисляем шаг сетки для таблицы доходности Беллмана
            decimal step = budget;
            for (int i = 0; i < count; i++)
            {
                if (selected[i].InvestmentValue > 0m)
                {
                    step = GcdDecimal(step, selected[i].InvestmentValue);
                }
            }

            const int maxSteps = 100_000;
            if (budget / step > maxSteps)
            {
                step = budget / maxSteps;
            }

            int maxBudget = (int)Math.Floor(budget / step);

            // Таблица доходности Беллмана:
            // dpIncome[i, x] — максимальный доход для первых i проектов при затратах x шагов
            // takeCount[i, x] — сколько раз взят i-й проект на уровне x
            decimal[,] dpIncome = new decimal[count + 1, maxBudget + 1];
            int[,] takeCount = new int[count + 1, maxBudget + 1];
            bool[,] reachable = new bool[count + 1, maxBudget + 1];
            reachable[0, 0] = true;

            // 2. Прямой ход: заполняем таблицу доходности с учётом ограничения MaxCopies из БД
            for (int i = 1; i <= count; i++)
            {
                var proj = selected[i - 1];
                decimal projectCost = proj.InvestmentValue;
                decimal incomePerUnit = proj.Income * 1_000_000m;
                int weight = projectCost > 0m ? (int)Math.Round(projectCost / step) : 0;

                int maxAllowed = (projectCost > 0m && weight > 0)
                    ? Math.Min(proj.MaxCopies, (int)Math.Floor(budget / projectCost))
                    : 0;

                for (int x = 0; x <= maxBudget; x++)
                {
                    if (reachable[i - 1, x])
                    {
                        dpIncome[i, x] = dpIncome[i - 1, x];
                        reachable[i, x] = true;
                        takeCount[i, x] = 0;
                    }

                    for (int k = 1; k <= maxAllowed; k++)
                    {
                        int totalWeight = k * weight;
                        if (totalWeight > x)
                        {
                            break;
                        }

                        if (!reachable[i - 1, x - totalWeight])
                        {
                            continue;
                        }

                        decimal candidateIncome = dpIncome[i - 1, x - totalWeight] + k * incomePerUnit;
                        if (!reachable[i, x] || candidateIncome > dpIncome[i, x])
                        {
                            dpIncome[i, x] = candidateIncome;
                            reachable[i, x] = true;
                            takeCount[i, x] = k;
                        }
                    }
                }
            }

            // 3. Находим лучший достижимый уровень в таблице доходности,
            // учитывая доход от добора остатка бюджета в проекты, где ещё остался лимит MaxCopies
            int bestX = 0;
            decimal maxTotalProjectedIncome = -1m;

            for (int x = 0; x <= maxBudget; x++)
            {
                if (!reachable[count, x])
                {
                    continue;
                }

                int[] tempCounts = new int[count];
                decimal exactCostX = 0m;
                decimal exactIncomeX = 0m;
                int cursor = x;

                for (int i = count; i >= 1; i--)
                {
                    int k = takeCount[i, cursor];
                    if (k > 0)
                    {
                        tempCounts[i - 1] = k;
                        exactCostX += selected[i - 1].InvestmentValue * k;
                        exactIncomeX += selected[i - 1].Income * 1_000_000m * k;
                        cursor -= k * (int)Math.Round(selected[i - 1].InvestmentValue / step);
                    }
                }

                if (exactCostX > budget)
                {
                    continue;
                }

                // Остаток бюджета распределяем по рентабельным проектам в пределах их свободного лимита MaxCopies
                decimal remainder = budget - exactCostX;
                decimal remainderIncome = 0m;

                var availableProjects = selected
                    .Select((p, idx) => new
                    {
                        RemainingCapacity = Math.Max(0m, (p.MaxCopies - tempCounts[idx]) * p.InvestmentValue),
                        Efficiency = p.InvestmentValue > 0m ? (p.Income * 1_000_000m) / p.InvestmentValue : 0m
                    })
                    .Where(item => item.RemainingCapacity > 0m)
                    .OrderByDescending(item => item.Efficiency);

                foreach (var item in availableProjects)
                {
                    if (remainder <= 0m)
                    {
                        break;
                    }

                    decimal addAmount = Math.Min(remainder, item.RemainingCapacity);
                    remainderIncome += addAmount * item.Efficiency;
                    remainder -= addAmount;
                }

                decimal totalIncomeWithRemainder = exactIncomeX + remainderIncome;
                if (totalIncomeWithRemainder > maxTotalProjectedIncome)
                {
                    maxTotalProjectedIncome = totalIncomeWithRemainder;
                    bestX = x;
                }
            }

            // 4. Фиксируем выбранные целые вложения из оптимальной ячейки таблицы доходности
            int currentX = bestX;
            for (int i = count; i >= 1; i--)
            {
                int k = takeCount[i, currentX];
                if (k > 0)
                {
                    integerCounts[i - 1] = k;
                    allocatedAmounts[i - 1] = selected[i - 1].InvestmentValue * k;
                    bestInvestment += allocatedAmounts[i - 1];
                    bestIncome += selected[i - 1].Income * 1_000_000m * k;

                    currentX -= k * (int)Math.Round(selected[i - 1].InvestmentValue / step);
                }
            }

            // 5. Если остался неразделённый нацело остаток — динамически докладываем его в доступные проекты
            decimal unallocated = budget - bestInvestment;
            if (unallocated > 0m)
            {
                var availableProjects = selected
                    .Select((p, idx) => new
                    {
                        Index = idx,
                        RemainingCapacity = Math.Max(0m, (p.MaxCopies * p.InvestmentValue) - allocatedAmounts[idx]),
                        Efficiency = p.InvestmentValue > 0m ? (p.Income * 1_000_000m) / p.InvestmentValue : 0m
                    })
                    .Where(item => item.RemainingCapacity > 0m)
                    .OrderByDescending(item => item.Efficiency);

                foreach (var item in availableProjects)
                {
                    if (unallocated <= 0m)
                    {
                        break;
                    }

                    decimal addAmount = Math.Min(unallocated, item.RemainingCapacity);
                    allocatedAmounts[item.Index] += addAmount;
                    bestInvestment += addAmount;
                    bestIncome += addAmount * item.Efficiency;
                    unallocated -= addAmount;
                }
            }
        }

        // 6. Формируем итоговый портфель
        var portfolio = new List<InvestmentPortfolioRow>();
        for (int index = 0; index < count; index++)
        {
            if (allocatedAmounts[index] <= 0m)
            {
                continue;
            }

            var project = selected[index];
            // Переводим вложенную сумму обратно в масштаб свойства Invests (если Invests в млн, а InvestmentValue в рублях)
            decimal scaleFactor = project.InvestmentValue > 0m ? project.Invests / project.InvestmentValue : 1m;
            decimal displayAllocated = allocatedAmounts[index] * scaleFactor;

            string title = integerCounts[index] > 1
                ? $"{project.ProjectName} (×{integerCounts[index]})"
                : project.ProjectName;

            // Если в проект пошёл дробный остаток, помечаем это в названии
           

            portfolio.Add(new InvestmentPortfolioRow
            {
                SelectedProject = title,
                Company = project.Company,
                Allocated = displayAllocated.ToString("N2", CultureInfo.CurrentCulture) + " млн ₽",
                Share = bestInvestment == 0m
                    ? "0 %"
                    : (allocatedAmounts[index] * 100m / bestInvestment)
                        .ToString("N1", CultureInfo.CurrentCulture) + " %"
            });
        }

        return new InvestmentCalculationResult
        {
            Portfolio = portfolio,
            TotalIncome = decimal.Round(bestIncome, 2),
            TotalAllocated = decimal.Round(bestInvestment, 2)
        };
    }

    private static decimal GcdDecimal(decimal a, decimal b)
    {
        a = Math.Abs(a);
        b = Math.Abs(b);
        while (b > 0.000001m)
        {
            decimal rem = a % b;
            a = b;
            b = rem;
        }
        return a;
    }


    




}
