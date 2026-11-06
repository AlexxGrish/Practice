using IniParser;
using IniParser.Model;
using Microsoft.EntityFrameworkCore;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Pracrice.Models;

namespace Pracrice
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        private const bool On = true;

        public MainWindow()
        {
            InitializeComponent();
            MenuLogin.IsChecked = true;
            OptimalEq.IsEnabled = false;
            OptPlan.IsEnabled = false;
            OptimalRoute.IsEnabled = false;
            OptTick.IsEnabled = false;
            Invest.IsEnabled = false;
            AdminPanel.IsEnabled = false;
            AdminPanel.Visibility = Visibility.Collapsed;
        }










        private async void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            string username = LoginText.Text.Trim();
            string password = PassText.Password;

            if (string.IsNullOrEmpty(username) || string.IsNullOrEmpty(password))
            {
                MessageBox.Show("Заполните все поля!", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            BtnLogin.IsEnabled = false;

            try
            {
                using (var db = new AppDbContext())
                {
                    var user = await Microsoft.EntityFrameworkCore.EntityFrameworkQueryableExtensions
                        .FirstOrDefaultAsync(db.Users, u => u.Login == username);

                    if (user != null && BCrypt.Net.BCrypt.Verify(password, user.Pass))
                    {
                        OptPlan.IsEnabled = true;
                        OptimalEq.IsEnabled = true;
                        OptimalRoute.IsEnabled = true;
                        OptTick.IsEnabled = true;
                        Invest.IsEnabled = true;
                        OptPlan.IsChecked = true;


                        OptPlanv.InvalidateProperty(UIElement.VisibilityProperty);
                        MenuLogin.Visibility = Visibility.Collapsed;


                        if (user.Role == 0)
                        {
                            AdminPanel.IsEnabled = true;
                            AdminPanel.Visibility = Visibility.Visible;
                            InitializeAdminPanelAsync();
                            humRole.Text = "Администратор";

                        }
                        FIO.Text = user.FullName;
                    }
                    else
                    {
                        MessageBox.Show("Неверный логин или пароль.", "Ошибка авторизации", MessageBoxButton.OK, MessageBoxImage.Error);
                    }
                }
            }
            catch (System.Exception ex)
            {
                MessageBox.Show($"Ошибка подключения к базе данных!\nПроверьте config.ini или сеть.\n\nДетали: {ex.Message}",
                                "Критическая ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
            }
            finally
            {
                // В WPF используется true, а не On
                BtnLogin.IsEnabled = true;
            }
        }


        private void OptTick_Checked(object sender, RoutedEventArgs e)
        {

        }

       

        private void OptPlan_Checked(object sender, RoutedEventArgs e)
        {
          //  OptPlanv.Visibility = Visibility.Visible;
        }

        private void OptPlan_Unchecked(object sender, RoutedEventArgs e)
        {
          //  OptPlanv.Visibility = Visibility.Collapsed;
        }
        private void BtnPlanExport_Click(object sender, RoutedEventArgs e)
        {
            //TODO

        }
        private void BtnPlanCalc_Click(object sender, RoutedEventArgs e)
        {
            //TODO


        }



        private void NavigationItem_PreviewMouseDown(object sender, System.Windows.Input.MouseButtonEventArgs e)
        {
            if (sender is RadioButton currentRb)
            {
                string targetPanelName = currentRb.Name + "v";

                if (currentRb.IsChecked == true)
                {
                    currentRb.IsChecked = false;

                    if (this.FindName(targetPanelName) is FrameworkElement panel)
                    {
                        panel.Visibility = Visibility.Collapsed;
                    }

                    e.Handled = true; 
                    return;
                }

                if (NavigationPanel != null)
                {
                    foreach (var child in NavigationPanel.Children)
                    {
                        if (child is RadioButton rb && rb != currentRb)
                        {
                            string otherPanelName = rb.Name + "v";
                            if (this.FindName(otherPanelName) is FrameworkElement otherPanel)
                            {
                                otherPanel.Visibility = Visibility.Collapsed;
                            }
                        }
                    }
                }

                if (this.FindName(targetPanelName) is FrameworkElement targetPanel)
                {
                    targetPanel.Visibility = Visibility.Visible;
                }
            }
        }


        public async Task InitializeAdminPanelAsync()
            {
                await LoadUsersDataAsync();
                LoadRightsGridData();
            }

            /// <summary>
            /// Загрузка пользователей напрямую из вашей модели БД
            /// </summary>
            private async Task LoadUsersDataAsync()
            {
                try
                {
                    using (var db = new AppDbContext())
                    {
                        // Загружаем список пользователей асинхронно
                        var usersFromDb = await db.Users.ToListAsync();

                        // Трансформируем во вспомогательную модель для DataGrid
                        var displayList = usersFromDb.Select(u => new UserGridDisplay
                        {
                            UserId = u.UserId,
                            UserLogin = u.Login,
                            FullName = u.FullName,
                            // Преобразуем числовой int Role в текстовое название
                            UserRole = GetRoleName(u.Role),
                            Status = "Активен",
                            LastChangesText = u.LastChanges?.ToString("dd.MM.yyyy HH:mm") ?? "Нет изменений"
                        }).ToList();

                        // Устанавливаем источник данных для таблицы
                        UsersGrid.ItemsSource = displayList;
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка при загрузке пользователей:\n{ex.Message}",
                                    "Ошибка БД", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }

            /// <summary>
            /// Хелпер для маппинга числовой роли в строку
            /// </summary>
            private string GetRoleName(int roleId)
            {
                return roleId switch
                {
                    0 => "Администратор",
                    1 => "Аналитик",
                    2 => "Менеджер",
                    _ => $"Роль #{roleId}"
                };
            }

  
            private void LoadRightsGridData()
            {
                var rightsList = new List<RightGridModel>
            {
                new RightGridModel { SectionName = "Оптимальный план", AdminRight = "Полный доступ", AnalystRight = "Только чтение" },
                new RightGridModel { SectionName = "Оптимальное оборудование", AdminRight = "Полный доступ", AnalystRight = "Доступ ограничен" },
                new RightGridModel { SectionName = "Оптимальный маршрут", AdminRight = "Полный доступ", AnalystRight = "Полный доступ" },
                new RightGridModel { SectionName = "Инвестиции", AdminRight = "Полный доступ", AnalystRight = "Нет доступа" }
            };

                RightsGrid.ItemsSource = rightsList;
            }

 
            private void BtnAddUser_Click(object sender, RoutedEventArgs e)
            {
                // Здесь открываем форму добавления/редактирования
                MessageBox.Show("Открытие формы добавления...", "Инфо", MessageBoxButton.OK, MessageBoxImage.Information);

                // Пример интеграции с вашей страницей AddEditPage:
                // NavigationService.Navigate(new AddEditPage()); 
                // Или если это диалоговое окно:
                // var addWindow = new AddEditPage();
                // if (addWindow.ShowDialog() == true) { await LoadUsersDataAsync(); }
            }

            private async void BtnSaveChanges_Click(object sender, RoutedEventArgs e)
            {
                if (sender is System.Windows.Controls.Button btn) btn.IsEnabled = false;

                try
                {


                    await Task.Delay(300); // Имитация сохранения
                    MessageBox.Show("Изменения успешно сохранены!", "Успех", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Ошибка сохранения: {ex.Message}", "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
                finally
                {
                    if (sender is System.Windows.Controls.Button button) button.IsEnabled = true;
                }
            }
        }
    }








