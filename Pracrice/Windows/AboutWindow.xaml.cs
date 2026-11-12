using System.Reflection;
using System.Windows;

namespace Pracrice.Windows;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();

        Version? version = Assembly.GetExecutingAssembly().GetName().Version;
        VersionText.Text = version is null
            ? "Версия 1.0.0"
            : $"Версия {version.Major}.{version.Minor}.{version.Build}";
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
}
