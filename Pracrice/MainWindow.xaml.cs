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
            OptPlan.IsEnabled = false;
            OptimalRoute.IsEnabled = false;
            OptTick.IsEnabled = false;
            Invest.IsEnabled = false;

        }
        private DispatcherTimer _typingTimer;
        private string _fullText = "Гришков Александр Эдуардович";
        private int _currentLetterIndex = 0;

        private void TypingTimer_Tick(object sender, EventArgs e)
        {
            if (_currentLetterIndex < _fullText.Length)
            {
                FIO.Text += _fullText[_currentLetterIndex];
                _currentLetterIndex++;
            }
            else
            {
                // Когда весь текст набран, останавливаем таймер
                _typingTimer.Stop();
            }
        }

        private void StartTypingEffect()
        {
            // Очищаем текст перед началом
            FIO.Text = "";
            _currentLetterIndex = 0;

            // Инициализируем таймер
            _typingTimer = new DispatcherTimer();

            // Интервал между буквами — 50 миллисекунд (можно уменьшить для ускорения)
            _typingTimer.Interval = TimeSpan.FromMilliseconds(50);

            // Привязываем событие, которое срабатывает каждый тик
            _typingTimer.Tick += TypingTimer_Tick;

            // Запускаем
            _typingTimer.Start();
        }

        private void BtnLogin_Click(object sender, RoutedEventArgs e)
        {

            if (LoginText.Text == "admin" && PassText.Password == "12345")
            {
              


                Products.ItemsSource = new[]
                   {
                    new { Name = "Светлое пиво «Роспив»", Price = 150.00, MaxDemand = 500, IsActive = true },
                    new { Name = "Темное нефильтрованное", Price = 180.50, MaxDemand = 300, IsActive = true },
                    new { Name = "Крафтовый Эль", Price = 220.00, MaxDemand = 150, IsActive = false }
                };
 
                humRole.Text = "Administrator";
                StartTypingEffect();
                OptimalEq.IsEnabled = true;
                OptPlan.IsEnabled = true;
                OptimalRoute.IsEnabled = true;
                OptTick.IsEnabled = true;
                Invest.IsEnabled = true;
                OptPlan.IsChecked= true;
                MenuLogin.IsEnabled = false;
               


            }


            else
            {
                MessageBox.Show("Неверный логин или пароль!");
            }


        }

        private void OptTick_Checked(object sender, RoutedEventArgs e)
        {

        }
    }
    }
