using System.Windows;
using System.Windows.Controls;
using Pracrice.Models;
using Pracrice.Services;

namespace Pracrice.Windows;

public partial class UserEditWindow : Window
{
    private readonly User? _user;
    private readonly int _currentUserId;

    public UserEditWindow(User? user, int currentUserId)
    {
        InitializeComponent();

        _user = user;
        _currentUserId = currentUserId;

        RoleCombo.ItemsSource = RoleInfo.Options;

        if (user is null)
        {
            Title = "Добавление пользователя";
            HeaderTitle.Text = "Новая учётная запись";
            HeaderSubtitle.Text = "Заполните логин, фамилию и имя, назначьте роль";
            RoleCombo.SelectedValue = UserRole.Manager;
            PasswordLabel.Text = "Пароль *";
            PasswordHint.Text = "Пароль должен содержать не менее 6 символов.";
        }
        else
        {
            Title = "Редактирование пользователя";
            HeaderTitle.Text = "Редактирование учётной записи";
            HeaderSubtitle.Text = $"Пользователь: {user.FullName}";
            LoginBox.Text = user.Login;
            FullNameBox.Text = user.FullName;
            RoleCombo.SelectedValue = user.Role;
            PasswordLabel.Text = "Новый пароль";
            PasswordConfirmLabel.Text = "Повторите новый пароль";
            PasswordHint.Text = "Оставьте поля пароля пустыми, чтобы сохранить текущий пароль.";
            RoleCombo.IsEnabled = user.Id != currentUserId;
            RoleCombo.ToolTip = user.Id == currentUserId
                ? "Свою роль администратора изменить нельзя"
                : "Роль определяет доступные разделы и права пользователя";
        }

        RoleCombo_SelectionChanged(RoleCombo, null);
        Loaded += (_, _) =>
        {
            LoginBox.Focus();
            LoginBox.SelectAll();
        };
    }

    private void RoleCombo_SelectionChanged(object sender, SelectionChangedEventArgs? e)
    {
        if (RoleCombo.SelectedItem is RoleOption option)
        {
            RoleDescription.Text = option.Description;
        }
    }

    private async void BtnSave_Click(object sender, RoutedEventArgs e)
    {
        string login = LoginBox.Text.Trim();
        string fullName = FullNameBox.Text.Trim();
        string password = PasswordBox.Password;
        string confirm = PasswordConfirmBox.Password;

        if (RoleCombo.SelectedValue is not UserRole role)
        {
            DialogService.ShowWarning("Выберите роль пользователя.");
            return;
        }

        if (password != confirm)
        {
            DialogService.ShowWarning("Пароли не совпадают.");
            PasswordConfirmBox.Focus();
            return;
        }

        if (_user is null && password.Length == 0)
        {
            DialogService.ShowWarning("Укажите пароль для новой учётной записи.");
            return;
        }

        if (_user is not null && _user.Role != role &&
            !DialogService.Confirm(
                $"Изменить роль пользователя «{_user.FullName}» " +
                $"с «{RoleInfo.GetDisplayName(_user.Role)}» на «{RoleInfo.GetDisplayName(role)}»?",
                "Изменение роли"))
        {
            return;
        }

        BtnSave.IsEnabled = false;
        try
        {
            if (_user is null)
            {
                await UserService.CreateAsync(login, fullName, role, password);
            }
            else
            {
                await UserService.UpdateAsync(_user.Id, login, fullName, role,
                    string.IsNullOrWhiteSpace(password) ? null : password, _currentUserId);
            }

            DialogResult = true;
        }
        catch (ValidationException exception)
        {
            DialogService.ShowWarning(exception.Message, "Проверьте данные");
        }
        catch (Exception exception)
        {
            App.LogException(exception);
            DialogService.ShowError("Не удалось сохранить учётную запись.\n\n" + exception.Message);
        }
        finally
        {
            BtnSave.IsEnabled = true;
        }
    }

    private void BtnCancel_Click(object sender, RoutedEventArgs e)
    {
        DialogResult = false;
    }
}
