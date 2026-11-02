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

namespace Pracrice
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public MainWindow()
        {
            InitializeComponent();
            OptimalEq.IsEnabled = false;

        }


        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {
            if (LoginText.Text == "admin" && PassText.Password == "12345")
            {
                // 1. Разблокируем меню
                OptimalEq.IsEnabled = true;

                // 2. Программно переключаем на нужную вкладку (например, Оптимальный план)
                OptimalRoute.IsChecked = true;

                // 3. (Опционально) Блокируем саму вкладку Вход, чтобы нельзя было вернуться назад без Logout
                
            }
            else
            {
                MessageBox.Show("Неверный логин или пароль!");
            }


        }


        }
    }
}