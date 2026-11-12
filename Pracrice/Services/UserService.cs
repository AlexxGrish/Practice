using Microsoft.EntityFrameworkCore;
using Pracrice.Data;
using Pracrice.Models;

namespace Pracrice.Services;

public static class UserService
{
    public static async Task<UserSession?> AuthenticateAsync(string login, string password)
    {
        await using var context = AppDatabase.CreateContext();
        var user = await context.Users.FirstOrDefaultAsync(item => item.Login == login);

        if (user is null || !PasswordService.Verify(password, user.PasswordHash))
        {
            return null;
        }

        if (PasswordService.NeedsRehash(user.PasswordHash))
        {
            user.PasswordHash = PasswordService.Hash(password);
            await context.SaveChangesAsync();
        }

        return new UserSession(user.Id, user.Login, user.FullName, user.Role);
    }

    public static async Task<IReadOnlyList<UserRow>> GetUsersAsync(int? currentUserId = null)
    {
        await using var context = AppDatabase.CreateContext();
        var users = await context.Users.AsNoTracking()
            .OrderBy(user => user.FullName)
            .ToListAsync();

        return users.Select(user => UserRow.From(user, currentUserId)).ToList();
    }

    public static async Task<User?> GetAsync(int userId)
    {
        await using var context = AppDatabase.CreateContext();
        return await context.Users.AsNoTracking().FirstOrDefaultAsync(user => user.Id == userId);
    }

    public static async Task<User> CreateAsync(
        string login,
        string fullName,
        UserRole role,
        string password)
    {
        login = NormalizeLogin(login);
        fullName = NormalizeFullName(fullName);
        ValidatePassword(password);

        await using var context = AppDatabase.CreateContext();

        if (await context.Users.AnyAsync(user => user.Login == login))
        {
            throw new ValidationException($"Пользователь с логином «{login}» уже существует.");
        }

        var user = new User
        {
            Login = login,
            FullName = fullName,
            Role = role,
            PasswordHash = PasswordService.Hash(password)
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    public static async Task<User> UpdateAsync(
        int userId,
        string login,
        string fullName,
        UserRole role,
        string? newPassword,
        int currentUserId)
    {
        login = NormalizeLogin(login);
        fullName = NormalizeFullName(fullName);

        if (!string.IsNullOrWhiteSpace(newPassword))
        {
            ValidatePassword(newPassword);
        }

        await using var context = AppDatabase.CreateContext();
        var user = await context.Users.FirstOrDefaultAsync(item => item.Id == userId)
                   ?? throw new ValidationException("Пользователь не найден. Возможно, запись уже удалена.");

        if (await context.Users.AnyAsync(item => item.Login == login && item.Id != userId))
        {
            throw new ValidationException($"Пользователь с логином «{login}» уже существует.");
        }

        if (user.Role == UserRole.Administrator && role != UserRole.Administrator)
        {
            if (user.Id == currentUserId)
            {
                throw new ValidationException("Нельзя снять с себя права администратора.");
            }

            int administrators = await context.Users.CountAsync(item => item.Role == UserRole.Administrator);
            if (administrators <= 1)
            {
                throw new ValidationException(
                    "В системе должен остаться хотя бы один администратор. Сначала назначьте другого администратора.");
            }
        }

        user.Login = login;
        user.FullName = fullName;
        user.Role = role;

        if (!string.IsNullOrWhiteSpace(newPassword))
        {
            user.PasswordHash = PasswordService.Hash(newPassword);
        }

        await context.SaveChangesAsync();
        return user;
    }

    public static async Task DeleteAsync(int userId, int currentUserId)
    {
        if (userId == currentUserId)
        {
            throw new ValidationException("Нельзя удалить собственную учётную запись.");
        }

        await using var context = AppDatabase.CreateContext();
        var user = await context.Users.FirstOrDefaultAsync(item => item.Id == userId)
                   ?? throw new ValidationException("Пользователь не найден. Возможно, запись уже удалена.");

        if (user.Role == UserRole.Administrator)
        {
            int administrators = await context.Users.CountAsync(item => item.Role == UserRole.Administrator);
            if (administrators <= 1)
            {
                throw new ValidationException("Нельзя удалить единственного администратора системы.");
            }
        }

        context.Users.Remove(user);
        await context.SaveChangesAsync();
    }

    public static async Task<int> CountAdministratorsAsync()
    {
        await using var context = AppDatabase.CreateContext();
        return await context.Users.CountAsync(user => user.Role == UserRole.Administrator);
    }

    private static string NormalizeLogin(string login)
    {
        login = (login ?? string.Empty).Trim();
        if (login.Length == 0)
        {
            throw new ValidationException("Укажите логин пользователя.");
        }

        if (login.Length > 50)
        {
            throw new ValidationException("Логин не должен превышать 50 символов.");
        }

        if (login.Any(char.IsWhiteSpace))
        {
            throw new ValidationException("Логин не должен содержать пробелы.");
        }

        return login;
    }

    private static string NormalizeFullName(string fullName)
    {
        fullName = (fullName ?? string.Empty).Trim();
        if (fullName.Length == 0)
        {
            throw new ValidationException("Укажите фамилию и имя пользователя.");
        }

        if (fullName.Length > 150)
        {
            throw new ValidationException("Фамилия и имя не должны превышать 150 символов.");
        }

        return fullName;
    }

    private static void ValidatePassword(string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new ValidationException("Укажите пароль.");
        }

        if (password.Length < 6)
        {
            throw new ValidationException("Пароль должен содержать не менее 6 символов.");
        }

        if (password.Length > 72)
        {
            throw new ValidationException("Пароль не должен превышать 72 символа.");
        }
    }
}
