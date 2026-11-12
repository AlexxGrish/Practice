using Microsoft.EntityFrameworkCore;
using Pracrice.Configuration;

namespace Pracrice.Data;

public static class AppDatabase
{
    private static string? _connectionString;
    private static ServerVersion? _serverVersion;

    public static string ConnectionString =>
        _connectionString ?? throw new InvalidOperationException("Подключение к базе данных не инициализировано.");

    public static ServerVersion ServerVersion =>
        _serverVersion ?? throw new InvalidOperationException("Версия сервера базы данных не определена.");

    public static bool IsConfigured => _connectionString is not null && _serverVersion is not null;

    public static void Configure(string connectionString, ServerVersion serverVersion)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(connectionString);
        ArgumentNullException.ThrowIfNull(serverVersion);

        _connectionString = connectionString;
        _serverVersion = serverVersion;
    }

    public static AppDbContext CreateContext()
    {
        if (!IsConfigured)
        {
            throw new InvalidOperationException(
                $"Подключение к базе данных не настроено. Проверьте файл {AppPaths.ConfigFile}.");
        }

        return new AppDbContext(ConnectionString, ServerVersion);
    }
}
