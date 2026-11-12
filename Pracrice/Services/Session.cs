using Pracrice.Models;

namespace Pracrice.Services;

public sealed record UserSession(int Id, string Login, string FullName, UserRole Role)
{
    public bool IsAdministrator => Role == UserRole.Administrator;

    public bool IsDirector => Role == UserRole.Director;

    public bool IsManager => Role == UserRole.Manager;

    public string RoleDisplayName => RoleInfo.GetDisplayName(Role);
}

public static class Session
{
    public static UserSession? Current { get; private set; }

    public static bool IsAuthenticated => Current is not null;

    public static void SignIn(UserSession session) => Current = session;

    public static void SignOut() => Current = null;
}

public sealed record RoleOption(UserRole Value, string Title, string Description)
{
    public override string ToString() => Title;
}

public static class RoleInfo
{
    public static IReadOnlyList<RoleOption> Options { get; } = new[]
    {
        new RoleOption(UserRole.Administrator, "Администратор",
            "Полный доступ, управление пользователями и справочниками"),
        new RoleOption(UserRole.Director, "Руководитель",
            "Доступ к планам, отчётам и справочникам"),
        new RoleOption(UserRole.Manager, "Менеджер",
            "Работа с товарами, складами, магазинами и отчётами")
    };

    public static string GetDisplayName(UserRole role) => role switch
    {
        UserRole.Administrator => "Администратор",
        UserRole.Director => "Руководитель",
        UserRole.Manager => "Менеджер",
        _ => "Неизвестная роль"
    };

    public static string GetShortName(UserRole role) => role switch
    {
        UserRole.Administrator => "Админ",
        UserRole.Director => "Руководитель",
        UserRole.Manager => "Менеджер",
        _ => "—"
    };

    public static bool CanManageUsers(UserRole role) => role == UserRole.Administrator;

    public static bool CanEditReferences(UserRole role) => true;

    public static bool CanViewReports(UserRole role) => true;
}
