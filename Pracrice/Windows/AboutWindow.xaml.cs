using System.Reflection;
using System.Windows;

namespace Pracrice.Windows;

public partial class AboutWindow : Window
{
    public AboutWindow()
    {
        InitializeComponent();

        Version? version = Assembly.GetExecutingAssembly().GetName().Version;
     
    }

    private void BtnClose_Click(object sender, RoutedEventArgs e) => Close();
}
