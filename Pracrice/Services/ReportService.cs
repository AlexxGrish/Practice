using System.Globalization;
using System.IO;
using System.Net;
using System.Text;
using System.Windows;
using Microsoft.EntityFrameworkCore;
using Pracrice.Configuration;
using Pracrice.Data;
using Pracrice.Models;

namespace Pracrice.Services;

public enum ReportKind
{
    Products,
    Users,
    Stocks,
    Recipes,
    InvestmentRates
}

public sealed record ReportDefinition(ReportKind Kind, string Title, string Description);

public static class ReportService
{
    private static readonly CultureInfo RussianCulture = CultureInfo.GetCultureInfo("ru-RU");

    public static IReadOnlyList<ReportDefinition> AvailableReports { get; } = new[]
    {
        new ReportDefinition(ReportKind.Products, "Товары и изображения",
            "Каталог выпускаемой продукции с ценами и изображениями"),
        new ReportDefinition(ReportKind.Stocks, "Складские остатки",
            "Количество и стоимость продукции на складах"),
        new ReportDefinition(ReportKind.Recipes, "Рецептуры",
            "Нормы расхода ресурсов на единицу продукции"),
        new ReportDefinition(ReportKind.InvestmentRates, "Инвестиционные ставки",
            "Условия финансирования инвестиционных компаний"),
        new ReportDefinition(ReportKind.Users, "Пользователи системы",
            "Учётные записи, роли и права доступа")
    };

    public static async Task<string> GenerateAsync(
        ReportKind kind,
        DateTime reportDate,
        UserSession user,
        bool onlyWithImages = false)
    {
        var rows = new List<string[]>();
        string title;
        string subtitle;
        string[] headers;

        await using var context = AppDatabase.CreateContext();

        switch (kind)
        {
            case ReportKind.Products:
            {
                title = "Каталог выпускаемой продукции";
                subtitle = onlyWithImages
                    ? "Товары, для которых загружено изображение"
                    : "Все товары компании";

                var products = await context.Products.AsNoTracking()
                    .OrderBy(product => product.Name)
                    .ToListAsync();

                if (onlyWithImages)
                {
                    products = products.Where(product => ImageService.ResolveExistingPath(product.ImagePath) is not null).ToList();
                }

                headers = new[] { "Изображение", "Наименование", "Артикул", "Цена, ₽" };
                foreach (var product in products)
                {
                    string imageCell = "<span class=\"no-photo\">—</span>";
                    string? dataUri = ImageService.ReadAsDataUri(product.ImagePath);
                    if (dataUri is not null)
                    {
                        imageCell = $"<img class=\"photo\" src=\"{dataUri}\" alt=\"{Escape(product.Name)}\">";
                    }

                    rows.Add(new[]
                    {
                        imageCell,
                        Escape(product.Name),
                        Escape(product.Sku),
                        product.Price.ToString("N2", RussianCulture)
                    });
                }

                break;
            }
            case ReportKind.Users:
            {
                title = "Пользователи системы";
                subtitle = "Учётные записи и назначенные роли";

                var users = await context.Users.AsNoTracking()
                    .OrderBy(item => item.FullName)
                    .ToListAsync();

                headers = new[] { "Фамилия и имя", "Логин", "Роль" };
                rows.AddRange(users.Select(item => new[]
                {
                    Escape(item.FullName),
                    Escape(item.Login),
                    Escape(RoleInfo.GetDisplayName(item.Role))
                }));

                break;
            }
            case ReportKind.Stocks:
            {
                title = "Складские остатки";
                subtitle = "Наличие продукции на складах компании";

                var stocks = await context.WarehouseStocks.AsNoTracking()
                    .Include(item => item.Warehouse)
                    .Include(item => item.Product)
                    .OrderBy(item => item.Warehouse.Name)
                    .ThenBy(item => item.Product.Name)
                    .ToListAsync();

                headers = new[] { "Склад", "Товар", "Артикул", "Количество, ед.", "Цена, ₽", "Стоимость, ₽" };
                foreach (var stock in stocks)
                {
                    rows.Add(new[]
                    {
                        Escape(stock.Warehouse.Name),
                        Escape(stock.Product.Name),
                        Escape(stock.Product.Sku),
                        stock.Quantity.ToString("N0", RussianCulture),
                        stock.Product.Price.ToString("N2", RussianCulture),
                        (stock.Quantity * stock.Product.Price).ToString("N2", RussianCulture)
                    });
                }

                break;
            }
            case ReportKind.Recipes:
            {
                title = "Рецептуры продукции";
                subtitle = "Нормы расхода ресурсов на единицу товара";

                var recipes = await context.ProductRecipes.AsNoTracking()
                    .Include(item => item.Product)
                    .Include(item => item.Resource)
                    .OrderBy(item => item.Product.Name)
                    .ThenBy(item => item.Resource.Name)
                    .ToListAsync();

                headers = new[] { "Товар", "Артикул", "Ресурс", "Единица измерения", "Расход на единицу" };
                foreach (var recipe in recipes)
                {
                    rows.Add(new[]
                    {
                        Escape(recipe.Product.Name),
                        Escape(recipe.Product.Sku),
                        Escape(recipe.Resource.Name),
                        Escape(recipe.Resource.Unit),
                        recipe.Consumption.ToString("0.####", RussianCulture)
                    });
                }

                break;
            }
            case ReportKind.InvestmentRates:
            {
                title = "Инвестиционные ставки";
                subtitle = "Диапазоны сумм финансирования по компаниям";

                var rates = await context.InvestmentRates.AsNoTracking()
                    .Include(item => item.Company)
                    .OrderBy(item => item.Company.Name)
                    .ThenBy(item => item.MinAmount)
                    .ToListAsync();

                headers = new[] { "Инвестиционная компания", "Минимальная сумма, ₽", "Максимальная сумма, ₽" };
                foreach (var rate in rates)
                {
                    rows.Add(new[]
                    {
                        Escape(rate.Company.Name),
                        rate.MinAmount.ToString("N2", RussianCulture),
                        rate.MaxAmount.ToString("N2", RussianCulture)
                    });
                }

                break;
            }
            default:
                throw new ArgumentOutOfRangeException(nameof(kind), kind, "Неизвестный тип отчёта.");
        }

        string html = BuildHtml(title, subtitle, reportDate, user, headers, rows);
        return Save(html, $"{kind}_{reportDate:yyyyMMdd_HHmmss}");
    }

    public static Task<string> GenerateReferenceReportAsync(
        string title,
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<string>> rows,
        DateTime reportDate,
        UserSession user)
    {
        var escapedRows = rows
            .Select(row => row.Select(Escape).ToArray())
            .ToList();

        string html = BuildHtml(
            title,
            "Справочник информационной системы ООО «РОСПИВ»",
            reportDate,
            user,
            headers.ToArray(),
            escapedRows);

        return Task.FromResult(Save(html, $"Reference_{DateTime.Now:yyyyMMdd_HHmmss}"));
    }

    public static Task<string> GenerateTableReportAsync(
        string title,
        string subtitle,
        IReadOnlyList<string> headers,
        IReadOnlyList<IReadOnlyList<string>> rows,
        DateTime reportDate,
        UserSession user)
    {
        var escapedRows = rows
            .Select(row => row.Select(Escape).ToArray())
            .ToList();

        string html = BuildHtml(
            title,
            subtitle,
            reportDate,
            user,
            headers.ToArray(),
            escapedRows);

        return Task.FromResult(Save(html, $"Calculation_{DateTime.Now:yyyyMMdd_HHmmss}"));
    }

    public static void OpenReport(string path)
    {
        try
        {
            BrowserLauncher.Open(path);
        }
        catch (Exception exception)
        {
            throw new InvalidOperationException(
                $"Отчёт сохранён, но не удалось открыть его в браузере.\n\nФайл: {path}\n{exception.Message}",
                exception);
        }
    }

    public static string ReportsDirectory
    {
        get
        {
            Directory.CreateDirectory(AppPaths.ReportsDirectory);
            return AppPaths.ReportsDirectory;
        }
    }

    private static string Save(string html, string filePrefix)
    {
        string fileName = $"{filePrefix}_{Guid.NewGuid():N}.html";
        string path = Path.Combine(ReportsDirectory, fileName);

        File.WriteAllText(path, html, new UTF8Encoding(true));
        return path;
    }

    private static string BuildHtml(
        string title,
        string subtitle,
        DateTime reportDate,
        UserSession user,
        IReadOnlyList<string> headers,
        IReadOnlyList<string[]> rows)
    {
        string logo = GetLogoDataUri();
        var builder = new StringBuilder();

        builder.AppendLine("<!DOCTYPE html>");
        builder.AppendLine("<html lang=\"ru\">");
        builder.AppendLine("<head>");
        builder.AppendLine("<meta charset=\"utf-8\">");
        builder.AppendLine("<meta name=\"viewport\" content=\"width=device-width, initial-scale=1\">");
        builder.AppendLine($"<title>{Escape(title)} — ООО «РОСПИВ»</title>");
        builder.AppendLine("<style>");
        builder.AppendLine("""
            :root { --green:#2b5748; --green-dark:#18231d; --gold:#d8b56a; --gold-light:#f0d698; --ink:#22302b; --muted:#6c7a74; }
            * { box-sizing: border-box; }
            body { margin:0; padding:24px; background:#eef2ee; color:var(--ink); font-family:"Segoe UI", Arial, sans-serif; font-size:13px; }
            .sheet { max-width:1100px; margin:0 auto; background:#fff; border:1px solid #d8ded8; box-shadow:0 8px 30px rgba(24,35,29,.12); }
            .head { display:flex; align-items:center; gap:18px; padding:20px 26px; background:linear-gradient(120deg,#1d332a,#2b5748); color:#f4f1e6; }
            .head img { height:74px; width:auto; }
            .company { flex:1; }
            .company h2 { margin:0; font-family:Georgia, serif; font-size:22px; letter-spacing:2px; color:var(--gold-light); font-weight:normal; }
            .company p { margin:4px 0 0; color:#c9d6c7; font-size:11px; letter-spacing:1.4px; text-transform:uppercase; }
            .meta { text-align:right; font-size:11px; line-height:1.7; color:#d7e1d5; }
            .body { padding:26px; }
            h1 { margin:0; font-family:Georgia, serif; font-size:24px; color:var(--green-dark); font-weight:normal; }
            .subtitle { color:var(--muted); margin:6px 0 20px; font-size:12px; }
            .rule { height:3px; background:linear-gradient(90deg,var(--gold),var(--green)); margin:0 0 20px; }
            table { width:100%; border-collapse:collapse; }
            thead th { background:var(--green); color:#f2efe2; text-align:left; padding:9px 10px; font-size:11px; text-transform:uppercase; letter-spacing:.6px; border:1px solid #24483c; }
            tbody td { padding:8px 10px; border:1px solid #dfe5df; vertical-align:middle; }
            tbody tr:nth-child(even) { background:#f6f8f4; }
            tbody tr:hover { background:#eef3e9; }
            .photo { width:62px; height:48px; object-fit:cover; border-radius:4px; border:1px solid #cbd5cb; }
            .no-photo { color:#a6b0a8; }
            .footer { display:flex; justify-content:space-between; gap:16px; padding:14px 26px 22px; color:var(--muted); font-size:11px; border-top:1px solid #e4e9e4; }
            .footer b { color:var(--green); }
            .empty { padding:26px; text-align:center; color:var(--muted); border:1px dashed #c9d2c8; margin-top:14px; }
            @media print {
              body { background:#fff; padding:0; }
              .sheet { max-width:none; border:0; box-shadow:none; }
              thead { display:table-header-group; }
              tr { page-break-inside:avoid; }
              .head { -webkit-print-color-adjust:exact; print-color-adjust:exact; }
            }
            @page { size:A4; margin:10mm; }
            """);
        builder.AppendLine("</style>");
        builder.AppendLine("</head>");
        builder.AppendLine("<body>");
        builder.AppendLine("<div class=\"sheet\">");
        builder.AppendLine("<div class=\"head\">");
        builder.AppendLine($"<img src=\"{logo}\" alt=\"РОСПИВ\">");
        builder.AppendLine("<div class=\"company\"><h2>РОСПИВ</h2><p>Пивоваренная компания</p></div>");
        builder.AppendLine("<div class=\"meta\">");
        builder.AppendLine($"ООО «РОСПИВ»<br>Дата отчёта: <b>{reportDate:dd.MM.yyyy}</b><br>Сформировал: {Escape(user.FullName)}");
        builder.AppendLine("</div>");
        builder.AppendLine("</div>");
        builder.AppendLine("<div class=\"body\">");
        builder.AppendLine($"<h1>{Escape(title)}</h1>");
        builder.AppendLine($"<div class=\"subtitle\">{Escape(subtitle)}</div>");
        builder.AppendLine("<div class=\"rule\"></div>");

        if (rows.Count == 0)
        {
            builder.AppendLine("<div class=\"empty\">Данные для формирования отчёта отсутствуют.</div>");
        }
        else
        {
            builder.AppendLine("<table>");
            builder.AppendLine("<thead><tr>");
            foreach (string header in headers)
            {
                builder.AppendLine($"<th>{Escape(header)}</th>");
            }

            builder.AppendLine("</tr></thead>");
            builder.AppendLine("<tbody>");
            foreach (string[] row in rows)
            {
                builder.AppendLine("<tr>");
                foreach (string cell in row)
                {
                    builder.AppendLine($"<td>{cell}</td>");
                }

                builder.AppendLine("</tr>");
            }

            builder.AppendLine("</tbody>");
            builder.AppendLine("</table>");
        }

        builder.AppendLine("</div>");
        builder.AppendLine("<div class=\"footer\">");
        builder.AppendLine($"<div><b>ООО «РОСПИВ»</b> · {Escape(title)}</div>");
        builder.AppendLine($"<div>Сформировано {DateTime.Now.ToString("dd.MM.yyyy HH:mm", RussianCulture)} · {Escape(user.FullName)}</div>");
        builder.AppendLine("</div>");
        builder.AppendLine("</div>");

        builder.AppendLine("</body>");
        builder.AppendLine("</html>");
        return builder.ToString();
    }

    private static string GetLogoDataUri()
    {
        string filePath = Path.Combine(AppContext.BaseDirectory, "Assets", "logo.png");
        if (File.Exists(filePath))
        {
            try
            {
                return ToDataUri(File.ReadAllBytes(filePath));
            }
            catch (Exception)
            {
            }
        }

        try
        {
            var resource = Application.GetResourceStream(new Uri("pack://application:,,,/Assets/logo.png"));
            if (resource is not null)
            {
                using Stream stream = resource.Stream;
                using var memory = new MemoryStream();
                stream.CopyTo(memory);
                return ToDataUri(memory.ToArray());
            }
        }
        catch (Exception)
        {
        }

        return string.Empty;
    }

    private static string ToDataUri(byte[] bytes) =>
        $"data:image/png;base64,{Convert.ToBase64String(bytes)}";

    private static string Escape(string? value) => WebUtility.HtmlEncode(value ?? string.Empty);
}
