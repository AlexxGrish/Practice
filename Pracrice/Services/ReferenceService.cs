using System.Data;
using System.Globalization;
using Microsoft.EntityFrameworkCore;
using Pracrice.Data;
using Pracrice.Models;

namespace Pracrice.Services;

public enum ReferenceSection
{
    Resources,
    Warehouses,
    Stores,
    ProductRecipes,
    Distances,
    Stocks,
    InvestmentCompanies,
    InvestmentRates
}

public enum FieldKind
{
    Text,
    Integer,
    Decimal,
    Lookup
}

public sealed record ReferenceSectionInfo(ReferenceSection Section, string Title, string Description)
{
    public override string ToString() => Title;
}

public sealed record ReferenceColumn(string Name, string Header);

public sealed record LookupItem(int Id, string Name)
{
    public override string ToString() => Name;
}

public sealed class FormField
{
    public required string Key { get; init; }

    public required string Label { get; init; }

    public required FieldKind Kind { get; init; }

    public bool Required { get; init; }

    public string ToolTip { get; init; } = string.Empty;

    public bool IsEnabled { get; init; } = true;

    public IReadOnlyList<LookupItem> Options { get; init; } = Array.Empty<LookupItem>();

    public string? Hint { get; init; }
}

public sealed class ReferenceForm
{
    public required ReferenceSection Section { get; init; }

    public string? Key { get; init; }

    public required string Title { get; init; }

    public required string Subtitle { get; init; }

    public required IReadOnlyList<FormField> Fields { get; init; }

    public Dictionary<string, object?> Values { get; init; } = new(StringComparer.OrdinalIgnoreCase);

    public bool IsEdit => Key is not null;
}

public static class ReferenceService
{
    private static readonly IReadOnlyList<ReferenceSectionInfo> Sections = new[]
    {
        new ReferenceSectionInfo(ReferenceSection.Resources, "Ресурсы",
            "Сырьё и материалы, используемые в производстве"),
        new ReferenceSectionInfo(ReferenceSection.Warehouses, "Склады",
            "Места хранения готовой продукции"),
        new ReferenceSectionInfo(ReferenceSection.Stores, "Магазины",
            "Торговые точки компании"),
        new ReferenceSectionInfo(ReferenceSection.ProductRecipes, "Рецептуры",
            "Нормы расхода ресурсов на единицу товара"),
        new ReferenceSectionInfo(ReferenceSection.Distances, "Расстояния",
            "Расстояния между складами и магазинами"),
        new ReferenceSectionInfo(ReferenceSection.Stocks, "Складские остатки",
            "Количество товара на складах"),
        new ReferenceSectionInfo(ReferenceSection.InvestmentCompanies, "Инвестиционные компании",
            "Партнёры и их условия финансирования"),
        new ReferenceSectionInfo(ReferenceSection.InvestmentRates, "Инвестиционные ставки",
            "Диапазоны сумм финансирования")
    };

    public static IReadOnlyList<ReferenceSectionInfo> GetSections() => Sections;

    public static ReferenceSectionInfo GetInfo(ReferenceSection section) =>
        Sections.First(item => item.Section == section);

    public static IReadOnlyList<ReferenceColumn> GetColumns(ReferenceSection section) => section switch
    {
        ReferenceSection.Resources => new[]
        {
            new ReferenceColumn("Name", "Наименование"),
            new ReferenceColumn("Unit", "Единица измерения")
        },
        ReferenceSection.Warehouses => new[]
        {
            new ReferenceColumn("Name", "Наименование"),
            new ReferenceColumn("Address", "Адрес")
        },
        ReferenceSection.Stores => new[]
        {
            new ReferenceColumn("Name", "Наименование"),
            new ReferenceColumn("Address", "Адрес"),
            new ReferenceColumn("Phone", "Телефон"),
            new ReferenceColumn("Demand", "Потребность, ед.")
        },
        ReferenceSection.ProductRecipes => new[]
        {
            new ReferenceColumn("Product", "Товар"),
            new ReferenceColumn("Resource", "Ресурс"),
            new ReferenceColumn("Consumption", "Расход на единицу")
        },
        ReferenceSection.Distances => new[]
        {
            new ReferenceColumn("Store", "Магазин"),
            new ReferenceColumn("Warehouse", "Склад"),
            new ReferenceColumn("Distance", "Расстояние, км")
        },
        ReferenceSection.Stocks => new[]
        {
            new ReferenceColumn("Warehouse", "Склад"),
            new ReferenceColumn("Product", "Товар"),
            new ReferenceColumn("Quantity", "Количество, ед.")
        },
        ReferenceSection.InvestmentCompanies => new[]
        {
            new ReferenceColumn("Name", "Наименование")
        },
        ReferenceSection.InvestmentRates => new[]
        {
            new ReferenceColumn("Company", "Инвестиционная компания"),
            new ReferenceColumn("MinAmount", "Мин. сумма, ₽"),
            new ReferenceColumn("MaxAmount", "Макс. сумма, ₽")
        },
        _ => Array.Empty<ReferenceColumn>()
    };

    public static async Task<DataTable> LoadTableAsync(ReferenceSection section)
    {
        await using var context = AppDatabase.CreateContext();
        var table = new DataTable();
        table.Columns.Add("Key", typeof(string));

        foreach (ReferenceColumn column in GetColumns(section))
        {
            table.Columns.Add(column.Name, typeof(string));
        }

        switch (section)
        {
            case ReferenceSection.Resources:
            {
                var items = await context.Resources.AsNoTracking().OrderBy(item => item.Name).ToListAsync();
                foreach (var item in items)
                {
                    table.Rows.Add(item.Id.ToString(), item.Name, item.Unit);
                }

                break;
            }
            case ReferenceSection.Warehouses:
            {
                var items = await context.Warehouses.AsNoTracking().OrderBy(item => item.Name).ToListAsync();
                foreach (var item in items)
                {
                    table.Rows.Add(item.Id.ToString(), item.Name, item.Address ?? "—");
                }

                break;
            }
            case ReferenceSection.Stores:
            {
                var items = await context.Stores.AsNoTracking().OrderBy(item => item.Name).ToListAsync();
                foreach (var item in items)
                {
                    table.Rows.Add(item.Id.ToString(), item.Name, item.Address ?? "—", item.Phone ?? "—",
                        item.Demand.ToString(CultureInfo.CurrentCulture));
                }

                break;
            }
            case ReferenceSection.ProductRecipes:
            {
                var items = await context.ProductRecipes.AsNoTracking()
                    .Include(item => item.Product)
                    .Include(item => item.Resource)
                    .OrderBy(item => item.Product.Name)
                    .ThenBy(item => item.Resource.Name)
                    .ToListAsync();

                foreach (var item in items)
                {
                    table.Rows.Add(
                        $"{item.ProductId}|{item.ResourceId}",
                        item.Product.Name,
                        item.Resource.Name,
                        item.Consumption.ToString("0.####", CultureInfo.CurrentCulture));
                }

                break;
            }
            case ReferenceSection.Distances:
            {
                var items = await context.StoreWarehouseDistances.AsNoTracking()
                    .Include(item => item.Store)
                    .Include(item => item.Warehouse)
                    .OrderBy(item => item.Store.Name)
                    .ThenBy(item => item.Warehouse.Name)
                    .ToListAsync();

                foreach (var item in items)
                {
                    table.Rows.Add(
                        $"{item.StoreId}|{item.WarehouseId}",
                        item.Store.Name,
                        item.Warehouse.Name,
                        item.DistanceKm.ToString("N2", CultureInfo.CurrentCulture));
                }

                break;
            }
            case ReferenceSection.Stocks:
            {
                var items = await context.WarehouseStocks.AsNoTracking()
                    .Include(item => item.Warehouse)
                    .Include(item => item.Product)
                    .OrderBy(item => item.Warehouse.Name)
                    .ThenBy(item => item.Product.Name)
                    .ToListAsync();

                foreach (var item in items)
                {
                    table.Rows.Add(
                        $"{item.WarehouseId}|{item.ProductId}",
                        item.Warehouse.Name,
                        item.Product.Name,
                        item.Quantity.ToString(CultureInfo.CurrentCulture));
                }

                break;
            }
            case ReferenceSection.InvestmentCompanies:
            {
                var items = await context.InvestmentCompanies.AsNoTracking().OrderBy(item => item.Name).ToListAsync();
                foreach (var item in items)
                {
                    table.Rows.Add(item.Id.ToString(), item.Name);
                }

                break;
            }
            case ReferenceSection.InvestmentRates:
            {
                var items = await context.InvestmentRates.AsNoTracking()
                    .Include(item => item.Company)
                    .OrderBy(item => item.Company.Name)
                    .ThenBy(item => item.MinAmount)
                    .ToListAsync();

                foreach (var item in items)
                {
                    table.Rows.Add(
                        item.Id.ToString(),
                        item.Company.Name,
                        item.MinAmount.ToString("N2", CultureInfo.CurrentCulture),
                        item.MaxAmount.ToString("N2", CultureInfo.CurrentCulture));
                }

                break;
            }
        }

        return table;
    }

    public static async Task<ReferenceForm> BuildFormAsync(ReferenceSection section, string? key)
    {
        ReferenceSectionInfo info = GetInfo(section);
        var fields = new List<FormField>();
        var values = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);

        await using var context = AppDatabase.CreateContext();

        var products = await LoadProductLookupsAsync(context);
        var resources = await LoadResourceLookupsAsync(context);
        var warehouses = await LoadWarehouseLookupsAsync(context);
        var stores = await LoadStoreLookupsAsync(context);
        var companies = await LoadCompanyLookupsAsync(context);

        switch (section)
        {
            case ReferenceSection.Resources:
                AddTextField(fields, values, "Name", "Наименование *", "Название ресурса, например «Солод»", true);
                AddTextField(fields, values, "Unit", "Единица измерения *", "Единица учёта: кг, л, шт.", true);

                if (key is not null)
                {
                    var entity = await context.Resources.AsNoTracking()
                        .FirstOrDefaultAsync(item => item.Id == int.Parse(key))
                        ?? throw new ValidationException("Запись ресурса не найдена.");
                    values["Name"] = entity.Name;
                    values["Unit"] = entity.Unit;
                }

                break;

            case ReferenceSection.Warehouses:
                AddTextField(fields, values, "Name", "Наименование *", "Название склада", true);
                AddTextField(fields, values, "Address", "Адрес", "Фактический адрес склада", false);

                if (key is not null)
                {
                    var entity = await context.Warehouses.AsNoTracking()
                        .FirstOrDefaultAsync(item => item.Id == int.Parse(key))
                        ?? throw new ValidationException("Запись склада не найдена.");
                    values["Name"] = entity.Name;
                    values["Address"] = entity.Address;
                }

                break;

            case ReferenceSection.Stores:
                AddTextField(fields, values, "Name", "Наименование *", "Название магазина или торговой точки", true);
                AddTextField(fields, values, "Address", "Адрес", "Адрес магазина", false);
                AddTextField(fields, values, "Phone", "Телефон", "Контактный телефон магазина", false);
                AddIntegerField(fields, values, "Demand", "Потребность, ед. *",
                    "Плановый спрос магазина на продукцию за период", true);

                if (key is not null)
                {
                    var entity = await context.Stores.AsNoTracking()
                        .FirstOrDefaultAsync(item => item.Id == int.Parse(key))
                        ?? throw new ValidationException("Запись магазина не найдена.");
                    values["Name"] = entity.Name;
                    values["Address"] = entity.Address;
                    values["Phone"] = entity.Phone;
                    values["Demand"] = entity.Demand;
                }

                break;

            case ReferenceSection.ProductRecipes:
                AddLookupField(fields, values, "ProductId", "Товар *",
                    "Товар, для которого задаётся норма расхода", true, products, key is null);
                AddLookupField(fields, values, "ResourceId", "Ресурс *",
                    "Ресурс, расходуемый при производстве", true, resources, key is null);
                AddDecimalField(fields, values, "Consumption", "Расход на единицу *",
                    "Количество ресурса на одну единицу товара", true);

                if (key is not null)
                {
                    var parts = ParseCompositeKey(key);
                    var entity = await context.ProductRecipes.AsNoTracking()
                        .FirstOrDefaultAsync(item => item.ProductId == parts[0] && item.ResourceId == parts[1])
                        ?? throw new ValidationException("Запись рецептуры не найдена.");
                    values["ProductId"] = entity.ProductId;
                    values["ResourceId"] = entity.ResourceId;
                    values["Consumption"] = entity.Consumption;
                }

                break;

            case ReferenceSection.Distances:
                AddLookupField(fields, values, "StoreId", "Магазин *",
                    "Магазин, до которого рассчитывается расстояние", true, stores, key is null);
                AddLookupField(fields, values, "WarehouseId", "Склад *",
                    "Склад отправки продукции", true, warehouses, key is null);
                AddDecimalField(fields, values, "DistanceKm", "Расстояние, км *",
                    "Расстояние между складом и магазином в километрах", true);

                if (key is not null)
                {
                    var parts = ParseCompositeKey(key);
                    var entity = await context.StoreWarehouseDistances.AsNoTracking()
                        .FirstOrDefaultAsync(item => item.StoreId == parts[0] && item.WarehouseId == parts[1])
                        ?? throw new ValidationException("Запись расстояния не найдена.");
                    values["StoreId"] = entity.StoreId;
                    values["WarehouseId"] = entity.WarehouseId;
                    values["DistanceKm"] = entity.DistanceKm;
                }

                break;

            case ReferenceSection.Stocks:
                AddLookupField(fields, values, "WarehouseId", "Склад *",
                    "Склад, на котором учитывается остаток", true, warehouses, key is null);
                AddLookupField(fields, values, "ProductId", "Товар *",
                    "Товар, остаток которого учитывается", true, products, key is null);
                AddIntegerField(fields, values, "Quantity", "Количество, ед. *",
                    "Количество единиц товара на складе", true);

                if (key is not null)
                {
                    var parts = ParseCompositeKey(key);
                    var entity = await context.WarehouseStocks.AsNoTracking()
                        .FirstOrDefaultAsync(item => item.WarehouseId == parts[0] && item.ProductId == parts[1])
                        ?? throw new ValidationException("Запись складского остатка не найдена.");
                    values["WarehouseId"] = entity.WarehouseId;
                    values["ProductId"] = entity.ProductId;
                    values["Quantity"] = entity.Quantity;
                }

                break;

            case ReferenceSection.InvestmentCompanies:
                AddTextField(fields, values, "Name", "Наименование *",
                    "Название инвестиционной компании или партнёра", true);

                if (key is not null)
                {
                    var entity = await context.InvestmentCompanies.AsNoTracking()
                        .FirstOrDefaultAsync(item => item.Id == int.Parse(key))
                        ?? throw new ValidationException("Запись инвестиционной компании не найдена.");
                    values["Name"] = entity.Name;
                }

                break;

            case ReferenceSection.InvestmentRates:
                AddLookupField(fields, values, "CompanyId", "Инвестиционная компания *",
                    "Компания, которой принадлежит ставка", true, companies, true);
                AddDecimalField(fields, values, "MinAmount", "Минимальная сумма, ₽ *",
                    "Нижняя граница суммы финансирования", true);
                AddDecimalField(fields, values, "MaxAmount", "Максимальная сумма, ₽ *",
                    "Верхняя граница суммы финансирования", true);

                if (key is not null)
                {
                    var entity = await context.InvestmentRates.AsNoTracking()
                        .FirstOrDefaultAsync(item => item.Id == int.Parse(key))
                        ?? throw new ValidationException("Запись инвестиционной ставки не найдена.");
                    values["CompanyId"] = entity.CompanyId;
                    values["MinAmount"] = entity.MinAmount;
                    values["MaxAmount"] = entity.MaxAmount;
                }

                break;
        }

        return new ReferenceForm
        {
            Section = section,
            Key = key,
            Title = key is null ? $"Добавление: {info.Title}" : $"Редактирование: {info.Title}",
            Subtitle = info.Description,
            Fields = fields,
            Values = values
        };
    }

    public static async Task SaveAsync(ReferenceForm form, IReadOnlyDictionary<string, object?> values)
    {
        await using var context = AppDatabase.CreateContext();

        switch (form.Section)
        {
            case ReferenceSection.Resources:
            {
                Resource entity;
                if (form.Key is null)
                {
                    entity = new Resource();
                    context.Resources.Add(entity);
                }
                else
                {
                    int id = int.Parse(form.Key);
                    entity = await context.Resources.FirstOrDefaultAsync(item => item.Id == id)
                             ?? throw new ValidationException("Запись ресурса не найдена.");
                }

                entity.Name = RequireText(values, "Name", "наименование ресурса", 100);
                entity.Unit = RequireText(values, "Unit", "единицу измерения", 20);
                break;
            }
            case ReferenceSection.Warehouses:
            {
                Warehouse entity;
                if (form.Key is null)
                {
                    entity = new Warehouse();
                    context.Warehouses.Add(entity);
                }
                else
                {
                    int id = int.Parse(form.Key);
                    entity = await context.Warehouses.FirstOrDefaultAsync(item => item.Id == id)
                             ?? throw new ValidationException("Запись склада не найдена.");
                }

                entity.Name = RequireText(values, "Name", "наименование склада", 100);
                entity.Address = OptionalText(values, "Address", 255);
                break;
            }
            case ReferenceSection.Stores:
            {
                Store entity;
                if (form.Key is null)
                {
                    entity = new Store();
                    context.Stores.Add(entity);
                }
                else
                {
                    int id = int.Parse(form.Key);
                    entity = await context.Stores.FirstOrDefaultAsync(item => item.Id == id)
                             ?? throw new ValidationException("Запись магазина не найдена.");
                }

                entity.Name = RequireText(values, "Name", "наименование магазина", 100);
                entity.Address = OptionalText(values, "Address", 255);
                entity.Phone = OptionalText(values, "Phone", 20);
                entity.Demand = RequireInteger(values, "Demand", "потребность магазина", 0);
                break;
            }
            case ReferenceSection.ProductRecipes:
            {
                int productId = RequireLookup(values, "ProductId", "товар");
                int resourceId = RequireLookup(values, "ResourceId", "ресурс");
                decimal consumption = RequireDecimal(values, "Consumption", "расход на единицу", 0.0001m);

                if (form.Key is null)
                {
                    if (await context.ProductRecipes.AnyAsync(item =>
                            item.ProductId == productId && item.ResourceId == resourceId))
                    {
                        throw new ValidationException("Для выбранного товара уже задана норма расхода этого ресурса.");
                    }

                    context.ProductRecipes.Add(new ProductRecipe
                    {
                        ProductId = productId,
                        ResourceId = resourceId,
                        Consumption = consumption
                    });
                }
                else
                {
                    var parts = ParseCompositeKey(form.Key);
                    var entity = await context.ProductRecipes
                        .FirstOrDefaultAsync(item => item.ProductId == parts[0] && item.ResourceId == parts[1])
                        ?? throw new ValidationException("Запись рецептуры не найдена.");
                    entity.Consumption = consumption;
                }

                break;
            }
            case ReferenceSection.Distances:
            {
                int storeId = RequireLookup(values, "StoreId", "магазин");
                int warehouseId = RequireLookup(values, "WarehouseId", "склад");
                decimal distance = RequireDecimal(values, "DistanceKm", "расстояние", 0m);

                if (form.Key is null)
                {
                    if (await context.StoreWarehouseDistances.AnyAsync(item =>
                            item.StoreId == storeId && item.WarehouseId == warehouseId))
                    {
                        throw new ValidationException("Расстояние для выбранной пары «магазин — склад» уже задано.");
                    }

                    context.StoreWarehouseDistances.Add(new StoreWarehouseDistance
                    {
                        StoreId = storeId,
                        WarehouseId = warehouseId,
                        DistanceKm = distance
                    });
                }
                else
                {
                    var parts = ParseCompositeKey(form.Key);
                    var entity = await context.StoreWarehouseDistances
                        .FirstOrDefaultAsync(item => item.StoreId == parts[0] && item.WarehouseId == parts[1])
                        ?? throw new ValidationException("Запись расстояния не найдена.");
                    entity.DistanceKm = distance;
                }

                break;
            }
            case ReferenceSection.Stocks:
            {
                int warehouseId = RequireLookup(values, "WarehouseId", "склад");
                int productId = RequireLookup(values, "ProductId", "товар");
                int quantity = RequireInteger(values, "Quantity", "количество", 0);

                if (form.Key is null)
                {
                    if (await context.WarehouseStocks.AnyAsync(item =>
                            item.WarehouseId == warehouseId && item.ProductId == productId))
                    {
                        throw new ValidationException("Остаток выбранного товара на складе уже учитывается.");
                    }

                    context.WarehouseStocks.Add(new WarehouseStock
                    {
                        WarehouseId = warehouseId,
                        ProductId = productId,
                        Quantity = quantity
                    });
                }
                else
                {
                    var parts = ParseCompositeKey(form.Key);
                    var entity = await context.WarehouseStocks
                        .FirstOrDefaultAsync(item => item.WarehouseId == parts[0] && item.ProductId == parts[1])
                        ?? throw new ValidationException("Запись складского остатка не найдена.");
                    entity.Quantity = quantity;
                }

                break;
            }
            case ReferenceSection.InvestmentCompanies:
            {
                InvestmentCompany entity;
                if (form.Key is null)
                {
                    entity = new InvestmentCompany();
                    context.InvestmentCompanies.Add(entity);
                }
                else
                {
                    int id = int.Parse(form.Key);
                    entity = await context.InvestmentCompanies.FirstOrDefaultAsync(item => item.Id == id)
                             ?? throw new ValidationException("Запись инвестиционной компании не найдена.");
                }

                entity.Name = RequireText(values, "Name", "наименование компании", 100);
                break;
            }
            case ReferenceSection.InvestmentRates:
            {
                int companyId = RequireLookup(values, "CompanyId", "инвестиционную компанию");
                decimal min = RequireDecimal(values, "MinAmount", "минимальную сумму", 0m);
                decimal max = RequireDecimal(values, "MaxAmount", "максимальную сумму", 0m);

                if (max < min)
                {
                    throw new ValidationException("Максимальная сумма не может быть меньше минимальной.");
                }

                InvestmentRate entity;
                if (form.Key is null)
                {
                    entity = new InvestmentRate();
                    context.InvestmentRates.Add(entity);
                }
                else
                {
                    int id = int.Parse(form.Key);
                    entity = await context.InvestmentRates.FirstOrDefaultAsync(item => item.Id == id)
                             ?? throw new ValidationException("Запись инвестиционной ставки не найдена.");
                }

                entity.CompanyId = companyId;
                entity.MinAmount = decimal.Round(min, 2);
                entity.MaxAmount = decimal.Round(max, 2);
                break;
            }
        }

        await context.SaveChangesAsync();
    }

    public static async Task DeleteAsync(ReferenceSection section, string key)
    {
        await using var context = AppDatabase.CreateContext();

        switch (section)
        {
            case ReferenceSection.Resources:
            {
                int id = int.Parse(key);
                var entity = await context.Resources.FirstOrDefaultAsync(item => item.Id == id)
                             ?? throw new ValidationException("Запись ресурса не найдена.");

                if (await context.ProductRecipes.AnyAsync(item => item.ResourceId == id))
                {
                    throw new ValidationException(
                        "Нельзя удалить ресурс: он используется в рецептурах товаров.");
                }

                context.Resources.Remove(entity);
                break;
            }
            case ReferenceSection.Warehouses:
            {
                int id = int.Parse(key);
                var entity = await context.Warehouses.FirstOrDefaultAsync(item => item.Id == id)
                             ?? throw new ValidationException("Запись склада не найдена.");

                if (await context.WarehouseStocks.AnyAsync(item => item.WarehouseId == id) ||
                    await context.StoreWarehouseDistances.AnyAsync(item => item.WarehouseId == id))
                {
                    throw new ValidationException(
                        "Нельзя удалить склад: с ним связаны остатки или расстояния до магазинов.");
                }

                context.Warehouses.Remove(entity);
                break;
            }
            case ReferenceSection.Stores:
            {
                int id = int.Parse(key);
                var entity = await context.Stores.FirstOrDefaultAsync(item => item.Id == id)
                             ?? throw new ValidationException("Запись магазина не найдена.");

                if (await context.StoreWarehouseDistances.AnyAsync(item => item.StoreId == id))
                {
                    throw new ValidationException(
                        "Нельзя удалить магазин: для него заданы расстояния до складов.");
                }

                context.Stores.Remove(entity);
                break;
            }
            case ReferenceSection.ProductRecipes:
            {
                var parts = ParseCompositeKey(key);
                var entity = await context.ProductRecipes
                    .FirstOrDefaultAsync(item => item.ProductId == parts[0] && item.ResourceId == parts[1])
                    ?? throw new ValidationException("Запись рецептуры не найдена.");
                context.ProductRecipes.Remove(entity);
                break;
            }
            case ReferenceSection.Distances:
            {
                var parts = ParseCompositeKey(key);
                var entity = await context.StoreWarehouseDistances
                    .FirstOrDefaultAsync(item => item.StoreId == parts[0] && item.WarehouseId == parts[1])
                    ?? throw new ValidationException("Запись расстояния не найдена.");
                context.StoreWarehouseDistances.Remove(entity);
                break;
            }
            case ReferenceSection.Stocks:
            {
                var parts = ParseCompositeKey(key);
                var entity = await context.WarehouseStocks
                    .FirstOrDefaultAsync(item => item.WarehouseId == parts[0] && item.ProductId == parts[1])
                    ?? throw new ValidationException("Запись складского остатка не найдена.");
                context.WarehouseStocks.Remove(entity);
                break;
            }
            case ReferenceSection.InvestmentCompanies:
            {
                int id = int.Parse(key);
                var entity = await context.InvestmentCompanies.FirstOrDefaultAsync(item => item.Id == id)
                             ?? throw new ValidationException("Запись инвестиционной компании не найдена.");

                if (await context.InvestmentRates.AnyAsync(item => item.CompanyId == id))
                {
                    throw new ValidationException(
                        "Нельзя удалить компанию: для неё заданы инвестиционные ставки.");
                }

                context.InvestmentCompanies.Remove(entity);
                break;
            }
            case ReferenceSection.InvestmentRates:
            {
                int id = int.Parse(key);
                var entity = await context.InvestmentRates.FirstOrDefaultAsync(item => item.Id == id)
                             ?? throw new ValidationException("Запись инвестиционной ставки не найдена.");
                context.InvestmentRates.Remove(entity);
                break;
            }
        }

        await context.SaveChangesAsync();
    }

    private static async Task<List<LookupItem>> LoadProductLookupsAsync(AppDbContext context)
    {
        var items = await context.Products.AsNoTracking().OrderBy(item => item.Name).ToListAsync();
        return items.Select(item => new LookupItem(item.Id, $"{item.Name} ({item.Sku})")).ToList();
    }

    private static async Task<List<LookupItem>> LoadResourceLookupsAsync(AppDbContext context)
    {
        var items = await context.Resources.AsNoTracking().OrderBy(item => item.Name).ToListAsync();
        return items.Select(item => new LookupItem(item.Id, $"{item.Name} ({item.Unit})")).ToList();
    }

    private static async Task<List<LookupItem>> LoadWarehouseLookupsAsync(AppDbContext context)
    {
        var items = await context.Warehouses.AsNoTracking().OrderBy(item => item.Name).ToListAsync();
        return items.Select(item => new LookupItem(item.Id, item.Name)).ToList();
    }

    private static async Task<List<LookupItem>> LoadStoreLookupsAsync(AppDbContext context)
    {
        var items = await context.Stores.AsNoTracking().OrderBy(item => item.Name).ToListAsync();
        return items.Select(item => new LookupItem(item.Id, item.Name)).ToList();
    }

    private static async Task<List<LookupItem>> LoadCompanyLookupsAsync(AppDbContext context)
    {
        var items = await context.InvestmentCompanies.AsNoTracking().OrderBy(item => item.Name).ToListAsync();
        return items.Select(item => new LookupItem(item.Id, item.Name)).ToList();
    }

    private static void AddTextField(
        ICollection<FormField> fields,
        IDictionary<string, object?> values,
        string key,
        string label,
        string toolTip,
        bool required)
    {
        fields.Add(new FormField { Key = key, Label = label, Kind = FieldKind.Text, Required = required, ToolTip = toolTip });
        values.TryAdd(key, string.Empty);
    }

    private static void AddDecimalField(
        ICollection<FormField> fields,
        IDictionary<string, object?> values,
        string key,
        string label,
        string toolTip,
        bool required)
    {
        fields.Add(new FormField { Key = key, Label = label, Kind = FieldKind.Decimal, Required = required, ToolTip = toolTip });
        values.TryAdd(key, 0m);
    }

    private static void AddIntegerField(
        ICollection<FormField> fields,
        IDictionary<string, object?> values,
        string key,
        string label,
        string toolTip,
        bool required)
    {
        fields.Add(new FormField { Key = key, Label = label, Kind = FieldKind.Integer, Required = required, ToolTip = toolTip });
        values.TryAdd(key, 0);
    }

    private static void AddLookupField(
        ICollection<FormField> fields,
        IDictionary<string, object?> values,
        string key,
        string label,
        string toolTip,
        bool required,
        IReadOnlyList<LookupItem> options,
        bool enabled)
    {
        fields.Add(new FormField
        {
            Key = key,
            Label = label,
            Kind = FieldKind.Lookup,
            Required = required,
            ToolTip = toolTip,
            Options = options,
            IsEnabled = enabled
        });

        values.TryAdd(key, options.FirstOrDefault()?.Id);
    }

    private static int ParseIntKey(string key) =>
        int.TryParse(key, out int id) ? id : throw new ValidationException("Некорректный идентификатор записи.");

    private static int[] ParseCompositeKey(string key)
    {
        string[] parts = key.Split('|');
        if (parts.Length != 2 || !int.TryParse(parts[0], out int first) || !int.TryParse(parts[1], out int second))
        {
            throw new ValidationException("Некорректный ключ связанной записи.");
        }

        return new[] { first, second };
    }

    private static string RequireText(IReadOnlyDictionary<string, object?> values, string key, string fieldName, int maxLength)
    {
        string text = Convert.ToString(values.GetValueOrDefault(key))?.Trim() ?? string.Empty;
        if (text.Length == 0)
        {
            throw new ValidationException($"Укажите {fieldName}.");
        }

        if (text.Length > maxLength)
        {
            throw new ValidationException($"Поле «{fieldName}» не должно превышать {maxLength} символов.");
        }

        return text;
    }

    private static string? OptionalText(IReadOnlyDictionary<string, object?> values, string key, int maxLength)
    {
        string? text = Convert.ToString(values.GetValueOrDefault(key))?.Trim();
        if (string.IsNullOrWhiteSpace(text))
        {
            return null;
        }

        if (text.Length > maxLength)
        {
            throw new ValidationException($"Значение не должно превышать {maxLength} символов.");
        }

        return text;
    }

    private static decimal RequireDecimal(
        IReadOnlyDictionary<string, object?> values,
        string key,
        string fieldName,
        decimal minimum)
    {
        var raw = values.GetValueOrDefault(key);
        string text = Convert.ToString(raw)?.Trim() ?? string.Empty;

        if (!decimal.TryParse(text, NumberStyles.Any, CultureInfo.CurrentCulture, out decimal value) &&
            !decimal.TryParse(text.Replace(',', '.'), NumberStyles.Any, CultureInfo.InvariantCulture, out value))
        {
            throw new ValidationException($"Укажите корректное значение: {fieldName}.");
        }

        if (value < minimum)
        {
            throw new ValidationException($"Значение «{fieldName}» не может быть меньше {minimum:0.####}.");
        }

        return value;
    }

    private static int RequireInteger(
        IReadOnlyDictionary<string, object?> values,
        string key,
        string fieldName,
        int minimum)
    {
        var raw = values.GetValueOrDefault(key);
        if (raw is null || !int.TryParse(Convert.ToString(raw), NumberStyles.Integer,
                CultureInfo.CurrentCulture, out int value))
        {
            throw new ValidationException($"Укажите корректное целое значение: {fieldName}.");
        }

        if (value < minimum)
        {
            throw new ValidationException($"Значение «{fieldName}» не может быть меньше {minimum}.");
        }

        return value;
    }

    private static int RequireLookup(IReadOnlyDictionary<string, object?> values, string key, string fieldName)
    {
        var raw = values.GetValueOrDefault(key);
        if (raw is null || !int.TryParse(Convert.ToString(raw), out int value) || value <= 0)
        {
            throw new ValidationException($"Выберите {fieldName}.");
        }

        return value;
    }
}
