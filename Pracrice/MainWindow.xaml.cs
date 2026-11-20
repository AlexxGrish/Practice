using System.Diagnostics;
using System.IO;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Threading;
using Pracrice.Models;
using Pracrice.Services;
using Pracrice.Windows;

namespace Pracrice;

public partial class MainWindow : Window
{
    private readonly DispatcherTimer _clockTimer;
    private bool _initialized;
    private bool _isLoggingIn;
    private bool _isLoadingProducts;
    private bool _isLoadingUsers;

    private bool _isFullScreen = true;

    public MainWindow()
    {
        InitializeComponent();

        ApplyFullScreen(true);

        _clockTimer = new DispatcherTimer(DispatcherPriority.Background)
        {
            Interval = TimeSpan.FromSeconds(1)
        };
        _clockTimer.Tick += (_, _) => UpdateClock();
        _clockTimer.Start();
        UpdateClock();

        ReportDatePicker.SelectedDate = DateTime.Today;
        LoadRights();
        SetLoggedOutState();

        _initialized = true;
    }

    protected override void OnPreviewKeyDown(System.Windows.Input.KeyEventArgs e)
    {
        var key = e.Key == System.Windows.Input.Key.System ? e.SystemKey : e.Key;
        var modifiers = System.Windows.Input.Keyboard.Modifiers;

        // 1. Комбинация Ctrl + Shift + O — выключение ПК
        if (key == System.Windows.Input.Key.S &&
            modifiers.HasFlag(System.Windows.Input.ModifierKeys.Control) &&
            modifiers.HasFlag(System.Windows.Input.ModifierKeys.Shift))
        {
            ShutdownComputer();
            e.Handled = true;
            return;
        }

        if (key == System.Windows.Input.Key.F11 && modifiers == System.Windows.Input.ModifierKeys.None)
        {
            ApplyFullScreen(!_isFullScreen);
            e.Handled = true;
            return;
        }

        base.OnPreviewKeyDown(e);
    }

    private void ApplyFullScreen(bool enable)
    {
        _isFullScreen = enable;

        if (enable)
        {
            WindowStyle = WindowStyle.None;
            WindowState = WindowState.Normal;
            WindowState = WindowState.Maximized;
        }
        else
        {
            WindowStyle = WindowStyle.SingleBorderWindow;
            WindowState = WindowState.Normal;
            Width = 1280;
            Height = 760;
        }
    }

    private void ShutdownComputer()
    {
        var result = System.Windows.MessageBox.Show(
            "Вы действительно хотите выключить компьютер?",
            "Выключение ПК",
            System.Windows.MessageBoxButton.YesNo,
            System.Windows.MessageBoxImage.Warning);

        if (result != System.Windows.MessageBoxResult.Yes)
        {
            return;
        }

        System.Diagnostics.Process.Start(new System.Diagnostics.ProcessStartInfo
        {
            FileName = "shutdown",
            Arguments = "/s /t 0",
            CreateNoWindow = true,
            UseShellExecute = false
        });
    }

    private UserSession? CurrentUser => Session.Current;

    private async void BtnLogin_Click(object sender, RoutedEventArgs e)
    {
        if (_isLoggingIn)
        {
            return;
        }

        string login = LoginText.Text.Trim();
        string password = PassText.Password;

        if (login.Length == 0 || password.Length == 0)
        {
            DialogService.ShowWarning("Заполните логин и пароль.");
            if (login.Length == 0)
            {
                LoginText.Focus();
            }
            else
            {
                PassText.Focus();
            }

            return;
        }

        _isLoggingIn = true;
        BtnLogin.IsEnabled = false;

        try
        {
            UserSession? session = await UserService.AuthenticateAsync(login, password);
            if (session is null)
            {
                DialogService.ShowWarning("Неверный логин или пароль.", "Ошибка авторизации");
                PassText.Clear();
                PassText.Focus();
                return;
            }

            Session.SignIn(session);
            ApplySession(session);
            PassText.Clear();

            await LoadProductsAsync();
            if (session.IsAdministrator)
            {
                await LoadUsersAsync();
            }
        }
        catch (Exception exception)
        {
            App.LogException(exception);
            DialogService.ShowError(
                "Не удалось выполнить вход.\n\n" +
                "Проверьте подключение к базе данных и настройки в config.ini.\n\n" +
                exception.Message,
                "Ошибка авторизации");
        }
        finally
        {
            _isLoggingIn = false;
            BtnLogin.IsEnabled = true;
        }
    }

    private void ApplySession(UserSession session)
    {
        FIO.Text = session.FullName;
        humRole.Text = session.RoleDisplayName;
        StatusUser.Text = session.FullName;
        StatusRole.Text = "· " + session.RoleDisplayName;
        LogoutButton.Visibility = Visibility.Visible;

        ModulesMenu.IsEnabled = true;
        ReferencesMenu.IsEnabled = true;
        ReportsMenu.IsEnabled = true;

        OptPlan.IsEnabled = true;
        OptimalRoute.IsEnabled = true;
        OptimalEq.IsEnabled = true;
        OptTick.IsEnabled = true;
        Invest.IsEnabled = true;
        ProductsNav.IsEnabled = true;
        Reports.IsEnabled = true;
        ReferencesNav.IsEnabled = true;

        AdminPanel.IsEnabled = session.IsAdministrator;
        AdminPanel.Visibility = session.IsAdministrator ? Visibility.Visible : Visibility.Collapsed;
        AdminMenu.Visibility = session.IsAdministrator ? Visibility.Visible : Visibility.Collapsed;
        UsersReportMenuItem.Visibility = session.IsAdministrator ? Visibility.Visible : Visibility.Collapsed;

        ReportsItems.ItemsSource = ReportService.AvailableReports
            .Where(report => session.IsAdministrator || report.Kind != ReportKind.Users)
            .ToList();

        MenuLogin.IsChecked = false;
        SelectModule(nameof(OptPlan));
        MenuLogin.Visibility = Visibility.Collapsed;
    }

    private void SetLoggedOutState()
    {
        MenuLogin.Visibility = Visibility.Visible;

        FIO.Text = "Вход не выполнен";
        humRole.Text = "Вход не выполнен";
        StatusUser.Text = "Вход не выполнен";
        StatusRole.Text = string.Empty;
        LogoutButton.Visibility = Visibility.Collapsed;

        ModulesMenu.IsEnabled = false;
        ReferencesMenu.IsEnabled = false;
        ReportsMenu.IsEnabled = false;
        AdminMenu.Visibility = Visibility.Collapsed;
        AdminPanel.Visibility = Visibility.Collapsed;
        UsersReportMenuItem.Visibility = Visibility.Collapsed;

        foreach (RadioButton item in new[]
                 {
                     OptPlan, OptimalRoute, OptimalEq, OptTick, Invest, ProductsNav, Reports, AdminPanel
                 })
        {
            item.IsEnabled = false;
            item.IsChecked = false;
        }

        ReferencesNav.IsEnabled = false;
        AdminPanel.IsEnabled = false;
        ProductsGrid.ItemsSource = null;
        UsersGrid.ItemsSource = null;
        ProductsCountText.Text = "0 позиций";
        UsersCountText.Text = "0 учётных записей";
        UpdateProductDetails(null);
        UpdateProductButtons();

        MenuLogin.IsEnabled = true;
        MenuLogin.IsChecked = true;
        LoginText.Clear();
        PassText.Clear();
        LoginText.Focus();
    }

    private void LogoutButton_Click(object sender, RoutedEventArgs e) => Logout();

    private void LogoutMenuItem_Click(object sender, RoutedEventArgs e) => Logout();

    private void Logout()
    {
        if (!Session.IsAuthenticated)
        {
            DialogService.ShowInfo("Вход в систему ещё не выполнен.");
            return;
        }

        if (!DialogService.Confirm("Завершить текущую сессию и выйти из учётной записи?", "Выход"))
        {
            return;
        }

        Session.SignOut();
        SetLoggedOutState();
    }

    private void LoginField_KeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key == Key.Enter)
        {
            BtnLogin_Click(sender, e);
            e.Handled = true;
        }
    }

    private async void NavigationItem_Checked(object sender, RoutedEventArgs e)
    {
        if (!_initialized || sender is not RadioButton radioButton || radioButton.IsChecked != true)
        {
            return;
        }

        try
        {
            if (radioButton == OptPlan && Session.IsAuthenticated)
            {
                await LoadPlanAsync();
            }
            else if (radioButton == OptimalRoute && Session.IsAuthenticated)
            {
                await LoadTransportAsync();
            }
            else if (radioButton == OptimalEq && Session.IsAuthenticated)
            {
                CalculateEquipmentView();
            }
            else if (radioButton == OptTick && Session.IsAuthenticated)
            {
                await LoadCashAsync();
            }
            else if (radioButton == Invest && Session.IsAuthenticated)
            {
                await LoadInvestmentsAsync();
            }
            else if (radioButton == ProductsNav && Session.IsAuthenticated)
            {
                await LoadProductsAsync();
            }
            else if (radioButton == AdminPanel)
            {
                if (CurrentUser?.IsAdministrator != true)
                {
                    radioButton.IsChecked = false;
                    SelectModule(nameof(OptPlan));
                    DialogService.ShowWarning("Раздел администрирования доступен только администратору.");
                    return;
                }

                await LoadUsersAsync();
            }
            else if (radioButton == Reports && Session.IsAuthenticated)
            {
                ReportDatePicker.SelectedDate ??= DateTime.Today;
            }
        }
        catch (Exception exception)
        {
            App.LogException(exception);
            DialogService.ShowError("Не удалось открыть раздел.\n\n" + exception.Message);
        }
    }

    private void ModuleMenu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: string moduleName })
        {
            SelectModule(moduleName);
        }
    }

    private void SelectModule(string moduleName)
    {
        if (FindName(moduleName) is not RadioButton radioButton)
        {
            return;
        }

        if (!radioButton.IsEnabled)
        {
            DialogService.ShowWarning("Раздел недоступен для текущей учётной записи.");
            return;
        }

        radioButton.IsChecked = true;
    }

    private void OpenReferences_Click(object sender, RoutedEventArgs e) => OpenReferences(ReferenceSection.Resources);

    private void OpenReferenceSection_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: string sectionName } &&
            Enum.TryParse(sectionName, out ReferenceSection section))
        {
            OpenReferences(section);
        }
    }

    private void OpenReferences(ReferenceSection section)
    {
        if (CurrentUser is null)
        {
            DialogService.ShowWarning("Сначала войдите в систему.");
            return;
        }

        try
        {
            var window = new ReferenceWindow(section, CurrentUser) { Owner = this };
            window.ShowDialog();
        }
        catch (Exception exception)
        {
            App.LogException(exception);
            DialogService.ShowError("Не удалось открыть справочник.\n\n" + exception.Message);
        }
    }

    private void AboutMenuItem_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            var window = new AboutWindow { Owner = this };
            window.ShowDialog();
        }
        catch (Exception exception)
        {
            App.LogException(exception);
            DialogService.ShowError("Не удалось открыть окно «О программе».\n\n" + exception.Message);
        }
    }

    private void HelpButton_Click(object sender, RoutedEventArgs e) => OpenHelp();

    private void Window_PreviewKeyDown(object sender, KeyEventArgs e)
    {
        if (e.Key != Key.F1)
        {
            return;
        }

        OpenHelp();
        e.Handled = true;
    }

    private void OpenHelp()
    {
        string helpDirectory = Path.Combine(AppContext.BaseDirectory, "Help");
        string chmPath = Path.Combine(helpDirectory, "PracriceHelp.chm");
        string htmlPath = Path.Combine(helpDirectory, "PracriceHelp.html");

        try
        {
            if (File.Exists(chmPath))
            {
                // HTML Help does not render pages reliably from long paths.
                // Copy the CHM to a short temporary path before opening it.
                string tempPath = Path.Combine(
                    Path.GetTempPath(),
                    $"rphelp_{Guid.NewGuid():N}.chm");
                File.Copy(chmPath, tempPath, overwrite: true);

                string viewer = Path.Combine(
                    Environment.GetFolderPath(Environment.SpecialFolder.Windows),
                    "hh.exe");

                Process.Start(new ProcessStartInfo
                {
                    FileName = File.Exists(viewer) ? viewer : "hh.exe",
                    Arguments = $"\"{tempPath}\"",
                    UseShellExecute = true
                });
                return;
            }

            if (File.Exists(htmlPath))
            {
                Process.Start(new ProcessStartInfo
                {
                    FileName = htmlPath,
                    UseShellExecute = true
                });
                return;
            }

            DialogService.ShowError(
                "Файл справки не найден.\n\n" +
                "Ожидаемый путь:\n" + chmPath);
        }
        catch (Exception exception)
        {
            App.LogException(exception);
            DialogService.ShowError("Не удалось открыть справку.\n\n" + exception.Message);
        }
    }

    private void ExitMenuItem_Click(object sender, RoutedEventArgs e) => Close();

    private async void MenuItemChangeOwnPassword_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentUser is null)
        {
            return;
        }

        try
        {
            User? user = await UserService.GetAsync(CurrentUser.Id);
            if (user is null)
            {
                DialogService.ShowError("Учётная запись не найдена.");
                return;
            }

            var window = new UserEditWindow(user, CurrentUser.Id) { Owner = this };
            if (window.ShowDialog() == true)
            {
                DialogService.ShowInfo("Данные учётной записи сохранены.");
                await SyncCurrentUserAsync();
                if (CurrentUser?.IsAdministrator == true)
                {
                    await LoadUsersAsync();
                }
            }
        }
        catch (Exception exception)
        {
            App.LogException(exception);
            DialogService.ShowError("Не удалось открыть учётную запись.\n\n" + exception.Message);
        }
    }

    private async void BtnGenerateReport_Click(object sender, RoutedEventArgs e)
    {
        if (sender is Button { Tag: ReportKind kind })
        {
            await GenerateReportAsync(kind);
        }
    }

    private async void GenerateReportMenu_Click(object sender, RoutedEventArgs e)
    {
        if (sender is MenuItem { Tag: string kindName } &&
            Enum.TryParse(kindName, out ReportKind kind))
        {
            await GenerateReportAsync(kind);
        }
    }

    private async void BtnProductReport_Click(object sender, RoutedEventArgs e) =>
        await GenerateReportAsync(ReportKind.Products);

    private async Task GenerateReportAsync(ReportKind kind)
    {
        if (CurrentUser is null)
        {
            DialogService.ShowWarning("Сначала войдите в систему.");
            return;
        }

        if (kind == ReportKind.Users && !CurrentUser.IsAdministrator)
        {
            DialogService.ShowWarning("Отчёт по пользователям доступен только администратору.");
            return;
        }

        DateTime reportDate = ReportDatePicker.SelectedDate ?? DateTime.Today;
        bool onlyWithImages = kind == ReportKind.Products && ReportOnlyWithImages.IsChecked == true;

        Mouse.OverrideCursor = Cursors.Wait;
        try
        {
            string path = await ReportService.GenerateAsync(kind, reportDate, CurrentUser, onlyWithImages);

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
            DialogService.ShowError(
                "Не удалось сформировать отчёт.\n\n" + exception.Message,
                "Ошибка отчёта");
        }
        finally
        {
            Mouse.OverrideCursor = null;
        }
    }

    private void BtnOpenReportsFolder_Click(object sender, RoutedEventArgs e)
    {
        try
        {
            Process.Start(new ProcessStartInfo(ReportService.ReportsDirectory) { UseShellExecute = true });
        }
        catch (Exception exception)
        {
            App.LogException(exception);
            DialogService.ShowError("Не удалось открыть папку отчётов.\n\n" + exception.Message);
        }
    }

    private void UpdateClock()
    {
        DateTime now = DateTime.Now;
        StatusDate.Text = now.ToString("dd.MM.yyyy");
        StatusTime.Text = now.ToString("HH:mm:ss");
    }

    private void LoadRights()
    {
        RightsGrid.ItemsSource = new List<RightsRow>
        {
            new()
            {
                SectionName = "План выпуска продукции", AdminRight = "Полный доступ",
                DirectorRight = "Полный доступ", ManagerRight = "Просмотр"
            },
            new()
            {
                SectionName = "План перевозок", AdminRight = "Полный доступ",
                DirectorRight = "Полный доступ", ManagerRight = "Просмотр"
            },
            new()
            {
                SectionName = "Обновление оборудования", AdminRight = "Полный доступ",
                DirectorRight = "Полный доступ", ManagerRight = "Нет доступа"
            },
            new()
            {
                SectionName = "Планирование касс", AdminRight = "Полный доступ",
                DirectorRight = "Полный доступ", ManagerRight = "Просмотр"
            },
            new()
            {
                SectionName = "Распределение инвестиций", AdminRight = "Полный доступ",
                DirectorRight = "Полный доступ", ManagerRight = "Нет доступа"
            },
            new()
            {
                SectionName = "Товары и изображения", AdminRight = "Полный доступ",
                DirectorRight = "Полный доступ", ManagerRight = "Полный доступ"
            },
            new()
            {
                SectionName = "Справочники", AdminRight = "Полный доступ",
                DirectorRight = "Полный доступ", ManagerRight = "Полный доступ"
            },
            new()
            {
                SectionName = "Отчёты", AdminRight = "Все отчёты",
                DirectorRight = "Все, кроме пользователей", ManagerRight = "Все, кроме пользователей"
            },
            new()
            {
                SectionName = "Администрирование", AdminRight = "Полный доступ",
                DirectorRight = "Нет доступа", ManagerRight = "Нет доступа"
            }
        };
    }
}
