using System.Windows;

namespace Pracrice.Windows;

public enum DialogKind
{
    Info,
    Warning,
    Error,
    Question
}

public partial class MessageDialogWindow : Window
{
    public MessageDialogWindow(string title, string message, DialogKind kind, bool yesNo)
    {
        InitializeComponent();

        Title = title;
        HeaderTitle.Text = title;
        MessageText.Text = message;
        BtnNo.Visibility = yesNo ? Visibility.Visible : Visibility.Collapsed;
        BtnYes.Content = yesNo ? "Да" : "Понятно";
        BtnYes.IsDefault = true;
        BtnYes.IsCancel = !yesNo;
        BtnNo.IsCancel = yesNo;
        BtnNo.ToolTip = yesNo ? "Отменить действие" : "Закрыть окно";

        switch (kind)
        {
            case DialogKind.Warning:
                IconCircle.Background = (System.Windows.Media.Brush)FindResource("GoldDark");
                IconGlyph.Text = "!";
                break;
            case DialogKind.Error:
                IconCircle.Background = (System.Windows.Media.Brush)FindResource("Danger");
                IconGlyph.Text = "!";
                break;
            case DialogKind.Question:
                IconCircle.Background = (System.Windows.Media.Brush)FindResource("AccentGreenDark");
                IconGlyph.Text = "?";
                break;
            default:
                IconCircle.Background = (System.Windows.Media.Brush)FindResource("CardBg");
                IconGlyph.Text = "i";
                break;
        }

        Loaded += (_, _) => BtnYes.Focus();
    }

    private void BtnYes_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = true;
    }

    private void BtnNo_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
