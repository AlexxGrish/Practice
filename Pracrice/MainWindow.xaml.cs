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
        public MainWindow()
        {
            InitializeComponent();
            OptimalEq.IsEnabled = false;
            OptPlan.IsEnabled = false;
            OptimalRoute.IsEnabled = false;
            OptTick.IsEnabled = false;
            Invest.IsEnabled = false;

        }



        void parce()
        {
            string filePath = "config.ini";
            var parser = new FileIniDataParser();
            IniData data;

            if (!System.IO.File.Exists(filePath))
            {
                data = new IniData();
                data["Database"]["Host"] = "localhost";
                data["Database"]["Port"] = "3306"; 
                data["Database"]["User"] = "root"; 
                data["Database"]["Password"] = ""; 
                data["Database"]["DB"] = "RosPiv"; 


                parser.WriteFile(filePath, data);
            }
            else
            {
                try
                {
                    data = parser.ReadFile(filePath);
                }
                catch (IniParser.Exceptions.ParsingException)
                {
                    data = new IniData();
                }
            }
            string host = data["Database"]["Host"] ?? "localhost";

            if (!int.TryParse(data["Database"]["Port"], out int port))
            {
                port = 3306;
            }

            data["Database"]["Host"] = "127.0.0.1";

            parser.WriteFile(filePath, data);
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
            parce();
        }

        private void OptTick_Checked(object sender, RoutedEventArgs e)
        {

        }
    }
    }
