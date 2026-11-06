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
using IniParser;
using IniParser.Model;
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
            OptimalEq.IsEnabled = false;
            OptPlan.IsEnabled = false;
            OptimalRoute.IsEnabled = false;
            OptTick.IsEnabled = false;
            Invest.IsEnabled = false;

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
                        OptPlan.IsEnabled = On;
                        OptimalEq.IsEnabled = On;
                        OptimalRoute.IsEnabled = On;
                        OptPlan.IsChecked = On;
                        OptTick.IsEnabled = On;
                        Invest.IsEnabled = On;
                        MenuLogin.IsEnabled = false;
                        OptPlanv.Visibility = OptPlan.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;
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
                BtnLogin.IsEnabled = On;
            }
      
        }

        private void OptTick_Checked(object sender, RoutedEventArgs e)
        {

        }

        private void OptPlan_Checked(object sender, RoutedEventArgs e)
        {
            OptPlanv.Visibility = OptPlan.IsChecked == true ? Visibility.Visible : Visibility.Collapsed;

        }
    }
    }
