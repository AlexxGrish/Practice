using System.Windows;
using Pracrice.Windows;

namespace Pracrice.Services;

public static class DialogService
{
    public static void ShowInfo(string message, string title = "Информация") =>
        ShowMessage(title, message, DialogKind.Info, yesNo: false);

    public static void ShowWarning(string message, string title = "Предупреждение") =>
        ShowMessage(title, message, DialogKind.Warning, yesNo: false);

    public static void ShowError(string message, string title = "Ошибка") =>
        ShowMessage(title, message, DialogKind.Error, yesNo: false);

    public static bool Confirm(string message, string title = "Подтверждение") =>
        ShowMessage(title, message, DialogKind.Question, yesNo: true);

    private static bool ShowMessage(string title, string message, DialogKind kind, bool yesNo)
    {
        var window = new MessageDialogWindow(title, message, kind, yesNo);
        Window? owner = OwnerWindow;
        if (owner is not null && owner.IsVisible)
        {
            window.Owner = owner;
        }

        return window.ShowDialog() == true;
    }

    private static Window? OwnerWindow =>
        Application.Current.Windows.OfType<Window>().FirstOrDefault(window => window.IsActive)
        ?? Application.Current.MainWindow;
}
