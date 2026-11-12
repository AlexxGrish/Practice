using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using Pracrice.Models;
using Pracrice.Services;
using Pracrice.Windows;

namespace Pracrice;

public partial class MainWindow
{
    private UserRow? _selectedUser;

    private async Task LoadUsersAsync()
    {
        if (_isLoadingUsers || CurrentUser?.IsAdministrator != true)
        {
            return;
        }

        _isLoadingUsers = true;
        try
        {
            IReadOnlyList<UserRow> users = await UserService.GetUsersAsync(CurrentUser.Id);
            UsersGrid.ItemsSource = users;
            UsersCountText.Text = users.Count == 1
                ? "1 учётная запись"
                : $"{users.Count} учётных записей";

            if (users.Count > 0)
            {
                UsersGrid.SelectedIndex = 0;
                _selectedUser = users[0];
            }
            else
            {
                _selectedUser = null;
            }

            UpdateUserButtons();
        }
        catch (Exception exception)
        {
            App.LogException(exception);
            DialogService.ShowError("Не удалось загрузить список пользователей.\n\n" + exception.Message);
        }
        finally
        {
            _isLoadingUsers = false;
        }
    }

    private void UsersGrid_SelectionChanged(object sender, SelectionChangedEventArgs e)
    {
        _selectedUser = UsersGrid.SelectedItem as UserRow;
        UpdateUserButtons();
    }

    private void UsersGrid_MouseDoubleClick(object sender, MouseButtonEventArgs e)
    {
        if (UsersGrid.SelectedItem is UserRow)
        {
            OpenUserEditor();
        }
    }

    private void UpdateUserButtons()
    {
        bool hasSelection = _selectedUser is not null;
        BtnEditUser.IsEnabled = hasSelection;
        BtnDeleteUser.IsEnabled = hasSelection && _selectedUser!.Id != CurrentUser?.Id;
    }

    private async void BtnAddUser_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentUser?.IsAdministrator != true)
        {
            DialogService.ShowWarning("Добавлять пользователей может только администратор.");
            return;
        }

        try
        {
            var window = new UserEditWindow(null, CurrentUser.Id) { Owner = this };
            if (window.ShowDialog() == true)
            {
                await LoadUsersAsync();
            }
        }
        catch (Exception exception)
        {
            App.LogException(exception);
            DialogService.ShowError("Не удалось открыть форму добавления пользователя.\n\n" + exception.Message);
        }
    }

    private async void BtnEditUser_Click(object sender, RoutedEventArgs e)
    {
        await OpenUserEditorAsync();
    }

    private async Task OpenUserEditorAsync()
    {
        if (CurrentUser?.IsAdministrator != true)
        {
            DialogService.ShowWarning("Редактировать пользователей может только администратор.");
            return;
        }

        if (_selectedUser is null)
        {
            DialogService.ShowWarning("Выберите пользователя в списке.");
            return;
        }

        try
        {
            User? user = await UserService.GetAsync(_selectedUser.Id);
            if (user is null)
            {
                DialogService.ShowWarning("Пользователь не найден. Обновите список.");
                await LoadUsersAsync();
                return;
            }

            var window = new UserEditWindow(user, CurrentUser.Id) { Owner = this };
            if (window.ShowDialog() == true)
            {
                await LoadUsersAsync();
                await SyncCurrentUserAsync();
            }
        }
        catch (Exception exception)
        {
            App.LogException(exception);
            DialogService.ShowError("Не удалось открыть учётную запись.\n\n" + exception.Message);
        }
    }

    private void OpenUserEditor()
    {
        _ = OpenUserEditorAsync();
    }

    private async void BtnDeleteUser_Click(object sender, RoutedEventArgs e)
    {
        if (CurrentUser?.IsAdministrator != true)
        {
            DialogService.ShowWarning("Удалять пользователей может только администратор.");
            return;
        }

        if (_selectedUser is null)
        {
            DialogService.ShowWarning("Выберите пользователя в списке.");
            return;
        }

        if (_selectedUser.Id == CurrentUser.Id)
        {
            DialogService.ShowWarning("Нельзя удалить собственную учётную запись.");
            return;
        }

        UserRow user = _selectedUser;
        if (!DialogService.Confirm(
                $"Удалить учётную запись «{user.FullName}» ({user.Login})?\n\n" +
                "Пользователь потеряет доступ к системе. Действие нельзя отменить.",
                "Удаление пользователя"))
        {
            return;
        }

        try
        {
            await UserService.DeleteAsync(user.Id, CurrentUser.Id);
            DialogService.ShowInfo("Учётная запись удалена.");
            await LoadUsersAsync();
        }
        catch (ValidationException exception)
        {
            DialogService.ShowWarning(exception.Message, "Удаление невозможно");
        }
        catch (Exception exception)
        {
            App.LogException(exception);
            DialogService.ShowError("Не удалось удалить пользователя.\n\n" + exception.Message);
        }
    }

    private async void BtnRefreshUsers_Click(object sender, RoutedEventArgs e)
    {
        await LoadUsersAsync();
    }

    private async Task SyncCurrentUserAsync()
    {
        UserSession? current = CurrentUser;
        if (current is null)
        {
            return;
        }

        try
        {
            User? user = await UserService.GetAsync(current.Id);
            if (user is null)
            {
                return;
            }

            var session = new UserSession(user.Id, user.Login, user.FullName, user.Role);
            Session.SignIn(session);
            FIO.Text = session.FullName;
            humRole.Text = session.RoleDisplayName;
            StatusUser.Text = session.FullName;
            StatusRole.Text = "· " + session.RoleDisplayName;
        }
        catch (Exception exception)
        {
            App.LogException(exception);
        }
    }
}
