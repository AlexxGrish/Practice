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
        bool includeCurrent,
        bool includeNew)
    {
        horizon = Math.Clamp(horizon, 1, 5);
        age = Math.Clamp(age, 0, 30);

        decimal oldYear1 = 1.20m + age * 0.05m;
        decimal oldYear2 = 1.60m + age * 0.07m;
        decimal oldYear3 = 2.10m + age * 0.09m;
        decimal oldResidual = 1.00m;
        decimal newPrice = 2.80m;
        decimal newYear = 0.70m;
        decimal newResidual = 2.00m;

        var costs = new List<EquipmentCostRow>
        {
            new()
            {
                Equipment = "Текущее оборудование",
                Year1 = oldYear1,
                Year2 = oldYear2,
                Year3 = oldYear3,
                Residual = oldResidual,
                IsIncluded = includeCurrent
            },
            new()
            {
                Equipment = "Новое оборудование",
                Year1 = newYear,
                Year2 = newYear,
                Year3 = newYear,
                Residual = newResidual,
                IsIncluded = includeNew
            }
        };

        var strategies = new List<EquipmentStrategyRow>();
        decimal bestCost = decimal.MaxValue;
        string bestDecision = includeCurrent ? "Не заменять" : "Нет данных";

        if (includeNew)
        {
            for (int replacementYear = 0; replacementYear < horizon; replacementYear++)
            {
                decimal cost = 0;
                decimal[] oldYears = { oldYear1, oldYear2, oldYear3 };
                for (int year = 0; year < replacementYear; year++)
                {
                    cost += oldYears[Math.Min(year, 2)];
                }

                int newYears = horizon - replacementYear;
                cost += newPrice + newYear * newYears;
                cost -= newResidual;

                string strategy = replacementYear == 0
                    ? "Замена сейчас"
                    : $"Замена через {replacementYear} г.";
                strategies.Add(new EquipmentStrategyRow
                {
                    Strategy = strategy,
                    TotalCosts = decimal.Round(cost, 2),
                    Decision = string.Empty
                });

                if (cost < bestCost)
                {
                    bestCost = cost;
                    bestDecision = strategy;
                }
            }
        }

        decimal oldTotal = 0;
        decimal[] allOldYears = { oldYear1, oldYear2, oldYear3 };
        for (int year = 0; year < horizon; year++)
        {
            oldTotal += allOldYears[Math.Min(year, 2)];
        }

        decimal noReplaceCost = oldTotal - oldResidual;

        if (includeCurrent)
        {
            strategies.Add(new EquipmentStrategyRow
            {
                Strategy = "Не заменять",
                TotalCosts = decimal.Round(noReplaceCost, 2),
                Decision = string.Empty
            });

            if (noReplaceCost < bestCost)
            {
                bestCost = noReplaceCost;
                bestDecision = "Не заменять";
            }
        }

        var finalStrategies = strategies
            .Select(row => new EquipmentStrategyRow
            {
                Strategy = row.Strategy,
                TotalCosts = row.TotalCosts,
                Decision = row.Strategy == bestDecision ? "Рекомендуется" : "—"
            })
            .ToList();

        return new EquipmentCalculationResult
        {
            Costs = costs,
            Strategies = finalStrategies,
            Saving = includeCurrent && bestCost < decimal.MaxValue
                ? Math.Max(0, noReplaceCost - bestCost)
                : 0,
            Decision = bestDecision
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
                Flow = 100 + index * 20,
                ServiceRate = 25,
                Limit = 6
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

    public static IReadOnlyList<InvestmentProjectRow> GetInvestmentProjects() => new[]
    {
        new InvestmentProjectRow
        {
            ProjectName = "Новая линия розлива",
            Invests = 4.00m,
            Income = 1.10m,
            ProfitPercent = "27,5 %",
            InvestmentValue = 4_000_000m
        },
        new InvestmentProjectRow
        {
            ProjectName = "Холодильное оборудование",
            Invests = 2.50m,
            Income = 0.60m,
            ProfitPercent = "24,0 %",
            InvestmentValue = 2_500_000m
        },
        new InvestmentProjectRow
        {
            ProjectName = "Автоматизация склада",
            Invests = 3.00m,
            Income = 0.80m,
            ProfitPercent = "26,7 %",
            InvestmentValue = 3_000_000m
        },
        new InvestmentProjectRow
        {
            ProjectName = "Расширение ассортимента",
            Invests = 1.50m,
            Income = 0.45m,
            ProfitPercent = "30,0 %",
            InvestmentValue = 1_500_000m
        },
        new InvestmentProjectRow
        {
            ProjectName = "Рекламная кампания",
            Invests = 1.00m,
            Income = 0.30m,
            ProfitPercent = "30,0 %",
            InvestmentValue = 1_000_000m
        },
        new InvestmentProjectRow
        {
            ProjectName = "Модернизация лаборатории",
            Invests = 2.00m,
            Income = 0.50m,
            ProfitPercent = "25,0 %",
            InvestmentValue = 2_000_000m
        }
    };

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
