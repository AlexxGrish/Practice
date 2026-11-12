using System;
using System.Collections.Generic;
using System.Globalization;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using Microsoft.Win32;

namespace Pracrice
{
    /// <summary>
    /// Логика экрана «План выпуска продукции».
    /// Отдельный partial-файл: добавьте его в проект рядом с MainWindow.xaml.cs
    /// (в Visual Studio: правый клик по проекту → Add → Existing Item).
    /// </summary>
    public partial class MainWindow : Window
    {
        // ================================================================
        //  Модели строк таблиц экрана «План выпуска продукции»
        // ================================================================

        /// <summary>Строка таблицы норм: продукт, нормы расхода и прибыль.</summary>
        public class ProductNorm
        {
            public string Product { get; set; }   // наименование продукции
            public double Raw { get; set; }       // сырьё, кг/шт.
            public double Fill { get; set; }      // розлив, мин/шт.
            public double Pack { get; set; }      // упаковка, мин/шт.
            public double Profit { get; set; }    // прибыль, ₽/шт.
        }

        /// <summary>Строка таблицы результатов: оптимальный выпуск по продукту.</summary>
        public class ProductResult
        {
            public string Product { get; set; }   // наименование продукции
            public string Volume { get; set; }    // выпуск, шт.
            public string Profit { get; set; }    // прибыль, ₽
            public string Share { get; set; }     // доля в общей прибыли, %
        }

        private static readonly CultureInfo RuCulture = CultureInfo.GetCultureInfo("ru-RU");

        // ================================================================
        //  Обработчики событий
        // ================================================================

        /// <summary>Первое открытие раздела — загружаем данные и считаем план.</summary>
        private void OptPlan_Checked(object sender, RoutedEventArgs e)
        {
            if (ProductsGrid.ItemsSource == null)
            {
                LoadProductionData();
            }
        }

        /// <summary>Кнопка «Рассчитать план».</summary>
        private void BtnPlanCalc_Click(object sender, RoutedEventArgs e)
        {
            RecalculatePlan();
        }

        /// <summary>Кнопка «Экспорт» — сохранение оптимального плана в CSV.</summary>
        private void BtnPlanExport_Click(object sender, RoutedEventArgs e)
        {
            var results = PlanResultGrid.ItemsSource as List<ProductResult>;
            if (results == null || results.Count == 0)
            {
                MessageBox.Show("Сначала рассчитайте план выпуска.",
                    "Экспорт", MessageBoxButton.OK, MessageBoxImage.Information);
                return;
            }

            var dialog = new SaveFileDialog
            {
                Title = "Экспорт плана выпуска продукции",
                Filter = "CSV-файлы (*.csv)|*.csv|Все файлы (*.*)|*.*",
                FileName = "План выпуска продукции.csv"
            };

            if (dialog.ShowDialog(this) != true) return;

            var csv = new StringBuilder();
            csv.AppendLine("Оптимальный план выпуска продукции");
            csv.AppendLine("Продукция;Выпуск, шт.;Прибыль, руб;Доля прибыли");
            foreach (var row in results)
            {
                csv.AppendLine(string.Format("{0};{1};{2};{3}",
                    row.Product, row.Volume, row.Profit, row.Share));
            }
            csv.AppendLine();
            csv.AppendLine("Максимальная прибыль;" + PlanProfitText.Text);
            csv.AppendLine("Использование ресурсов;" + PlanUsageText.Text);

            File.WriteAllText(dialog.FileName, csv.ToString(), Encoding.UTF8);

            MessageBox.Show("План выпуска сохранён:\n" + dialog.FileName,
                "Экспорт", MessageBoxButton.OK, MessageBoxImage.Information);
        }

        // ================================================================
        //  Данные и расчёт оптимального плана
        // ================================================================

        /// <summary>Демонстрационные нормы расхода (продукция «Роспив»).</summary>
        private List<ProductNorm> BuildDefaultNorms()
        {
            return new List<ProductNorm>
            {
                new ProductNorm { Product = "Вода 0,5 л",             Raw = 0.42, Fill = 0.40, Pack = 0.30, Profit = 12 },
                new ProductNorm { Product = "Лимонад «Дюшес» 0,5 л",  Raw = 0.86, Fill = 0.60, Pack = 0.40, Profit = 25 },
                new ProductNorm { Product = "Сок «Яблоко» 1 л",       Raw = 1.64, Fill = 0.90, Pack = 0.60, Profit = 38 },
                new ProductNorm { Product = "Квас 0,5 л",             Raw = 0.55, Fill = 0.50, Pack = 0.35, Profit = 20 }
            };
        }

        /// <summary>Загрузка данных раздела и первичный расчёт.</summary>
        private void LoadProductionData()
        {
            ProductsGrid.ItemsSource = BuildDefaultNorms();
            RecalculatePlan();
        }

        /// <summary>Разбор лимита из поля ввода («6 400» → 6400). Возвращает −1 при ошибке.</summary>
        private double ParseLimit(string text)
        {
            if (string.IsNullOrWhiteSpace(text)) return -1;

            string cleaned = text
                .Replace(" ", "")
                .Replace("\u00A0", "")
                .Replace("\u202F", "")
                .Trim();

            double value;
            if (!double.TryParse(cleaned, NumberStyles.Float, RuCulture, out value) &&
                !double.TryParse(cleaned, NumberStyles.Float, CultureInfo.InvariantCulture, out value))
            {
                return -1;
            }
            return value;
        }

        /// <summary>
        /// Расчёт оптимального плана выпуска: симплекс-метод максимизирует прибыль
        /// при ограничениях по сырью, розливу и упаковке.
        /// </summary>
        private void RecalculatePlan()
        {
            // Нормы берём из таблицы (их можно править прямо на экране)
            var norms = ProductsGrid.ItemsSource as List<ProductNorm>;
            if (norms == null || norms.Count == 0)
            {
                norms = BuildDefaultNorms();
                ProductsGrid.ItemsSource = norms;
            }

            if (norms.Any(p => p.Raw < 0 || p.Fill < 0 || p.Pack < 0 || p.Profit < 0))
            {
                MessageBox.Show("Нормы расхода и прибыль должны быть неотрицательными.",
                    "План выпуска продукции", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            // Лимиты ресурсов из полей ввода
            double rawLimit = ParseLimit(RawLimitText.Text);
            double fillLimit = ParseLimit(FillLimitText.Text);
            double packLimit = ParseLimit(PackLimitText.Text);

            if (rawLimit < 0 || fillLimit < 0 || packLimit < 0)
            {
                MessageBox.Show("Лимиты ресурсов должны быть неотрицательными числами.",
                    "План выпуска продукции", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            int n = norms.Count;
            double[] objective = new double[n];
            for (int j = 0; j < n; j++) objective[j] = norms[j].Profit;

            double[][] constraints = new double[3][];
            for (int i = 0; i < 3; i++) constraints[i] = new double[n];
            for (int j = 0; j < n; j++)
            {
                constraints[0][j] = norms[j].Raw;    // сырьё
                constraints[1][j] = norms[j].Fill;   // розлив
                constraints[2][j] = norms[j].Pack;   // упаковка
            }
            double[] limits = { rawLimit, fillLimit, packLimit };

            double[] plan = SimplexMaximize(objective, constraints, limits);

            // Выпуск округляем до целых штук, показатели пересчитываем от округлённого плана
            double[] volumes = plan.Select(v => Math.Max(0, Math.Round(v, 0))).ToArray();

            double[] used =
            {
                volumes.Select((v, j) => v * norms[j].Raw).Sum(),
                volumes.Select((v, j) => v * norms[j].Fill).Sum(),
                volumes.Select((v, j) => v * norms[j].Pack).Sum()
            };

            double totalProfit = volumes.Select((v, j) => v * norms[j].Profit).Sum();

            var rows = new List<ProductResult>();
            for (int j = 0; j < n; j++)
            {
                double rowProfit = volumes[j] * norms[j].Profit;
                rows.Add(new ProductResult
                {
                    Product = norms[j].Product,
                    Volume = Num(volumes[j]),
                    Profit = Num(rowProfit),
                    Share = totalProfit > 0 ? Pct(rowProfit / totalProfit * 100.0) : "0 %"
                });
            }

            PlanResultGrid.ItemsSource = rows;
            PlanProfitText.Text = Num(totalProfit) + " ₽";
            PlanUsageText.Text = string.Format(
                "Сырьё — {0} · Розлив — {1} · Упаковка — {2}",
                PctShare(used[0], rawLimit),
                PctShare(used[1], fillLimit),
                PctShare(used[2], packLimit));
        }

        // ================================================================
        //  Симплекс-метод (максимизация c·x при A·x ≤ b, x ≥ 0, b ≥ 0)
        // ================================================================

        /// <summary>
        /// Классический симплекс-метод с естественным базисом (слабые переменные).
        /// Возвращает вектор оптимального решения x.
        /// </summary>
        private static double[] SimplexMaximize(double[] objective, double[][] constraints, double[] limits)
        {
            int m = limits.Length;
            int n = objective.Length;
            int total = n + m;

            // Симплекс-таблица: [A | E | b]
            double[][] table = new double[m][];
            for (int i = 0; i < m; i++)
            {
                table[i] = new double[total + 1];
                for (int j = 0; j < n; j++) table[i][j] = constraints[i][j];
                table[i][n + i] = 1.0;
                table[i][total] = Math.Max(0.0, limits[i]);
            }

            int[] basis = new int[m];
            for (int i = 0; i < m; i++) basis[i] = n + i;

            for (int iteration = 0; iteration < 500; iteration++)
            {
                // Оценки z_j − c_j: выбираем столбец с самой отрицательной оценкой
                int enter = -1;
                double worst = -1e-9;
                for (int j = 0; j < total; j++)
                {
                    if (Array.IndexOf(basis, j) >= 0) continue;

                    double z = 0.0;
                    for (int i = 0; i < m; i++) z += Coef(basis[i], objective, n) * table[i][j];
                    if (j < n) z -= objective[j];

                    if (z < worst)
                    {
                        worst = z;
                        enter = j;
                    }
                }

                if (enter < 0) break; // оптимальный план найден

                // Отношение b_i / a_i,enter: выбираем строку с минимальным
                int leave = -1;
                double bestRatio = double.PositiveInfinity;
                for (int i = 0; i < m; i++)
                {
                    if (table[i][enter] > 1e-12)
                    {
                        double ratio = table[i][total] / table[i][enter];
                        if (ratio < bestRatio)
                        {
                            bestRatio = ratio;
                            leave = i;
                        }
                    }
                }

                if (leave < 0) break; // целевая функция не ограничена

                // Пересчёт таблицы вокруг разрешающего элемента
                double pivot = table[leave][enter];
                for (int j = 0; j <= total; j++) table[leave][j] /= pivot;

                for (int i = 0; i < m; i++)
                {
                    if (i == leave || Math.Abs(table[i][enter]) < 1e-15) continue;
                    double factor = table[i][enter];
                    for (int j = 0; j <= total; j++)
                    {
                        table[i][j] -= factor * table[leave][j];
                    }
                }

                basis[leave] = enter;
            }

            // Восстанавливаем решение по базисным столбцам
            double[] x = new double[n];
            for (int i = 0; i < m; i++)
            {
                if (basis[i] < n) x[basis[i]] = table[i][total];
            }
            return x;
        }

        /// <summary>Целевой коэффициент столбца (у слабых переменных равен нулю).</summary>
        private static double Coef(int basisColumn, double[] objective, int n)
        {
            return basisColumn < n ? objective[basisColumn] : 0.0;
        }

        // ================================================================
        //  Форматирование
        // ================================================================

        /// <summary>Число с разрядами по русской локали: 5600 → «5 600».</summary>
        private static string Num(double value)
        {
            return value.ToString("N0", RuCulture);
        }

        /// <summary>Процент: 90.22 → «90 %».</summary>
        private static string Pct(double percent)
        {
            return Math.Round(percent).ToString("N0", RuCulture) + " %";
        }

        /// <summary>Загрузка ресурса в процентах от лимита.</summary>
        private static string PctShare(double used, double limit)
        {
            if (limit <= 0) return used <= 0 ? "100 %" : "—";
            return Pct(used / limit * 100.0);
        }
    }
}
