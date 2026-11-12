using System.Text.RegularExpressions;
using Microsoft.EntityFrameworkCore;
using MySqlConnector;
using Pracrice.Configuration;
using Pracrice.Models;
using Pracrice.Services;

namespace Pracrice.Data;

public sealed class DatabaseException : Exception
{
    public DatabaseException(string message, Exception? innerException = null) : base(message, innerException)
    {
    }
}

public sealed record DatabaseInitResult(bool AdministratorCreated, Version ServerVersion);

public static partial class DatabaseInitializer
{
    public static DatabaseInitResult Initialize(AppConfig config)
    {
        ArgumentNullException.ThrowIfNull(config);

        try
        {
            using var serverConnection = new MySqlConnection(config.ServerConnectionString);
            serverConnection.Open();

            string versionText;
            using (var versionCommand = serverConnection.CreateCommand())
            {
                versionCommand.CommandText = "SELECT VERSION();";
                versionText = Convert.ToString(versionCommand.ExecuteScalar()) ?? string.Empty;
            }

            using (var createCommand = serverConnection.CreateCommand())
            {
                createCommand.CommandText =
                    $"CREATE DATABASE IF NOT EXISTS `{config.Database}` " +
                    "CHARACTER SET utf8mb4 COLLATE utf8mb4_unicode_ci;";
                createCommand.ExecuteNonQuery();
            }

            Version version = ParseVersion(versionText) ?? new Version(5, 7, 39);
            var serverVersion = new MySqlServerVersion(version);

            AppDatabase.Configure(config.DatabaseConnectionString, serverVersion);

            using var context = AppDatabase.CreateContext();
            context.Database.EnsureCreated();

            EnsureColumn(context, "Products", "image_path", "VARCHAR(255) NULL");
            EnsureColumn(context, "Stores", "demand", "INT NOT NULL DEFAULT 0");

            bool administratorCreated = SeedAdministrator(context);

            return new DatabaseInitResult(administratorCreated, version);
        }
        catch (MySqlException exception)
        {
            throw new DatabaseException(
                "Не удалось подключиться к серверу MySQL.\n\n" +
                $"Сервер: {config.Host}:{config.Port}\nБаза данных: {config.Database}\n\n" +
                "Проверьте, что сервер запущен, а параметры в config.ini верны.\n" +
                $"Код ошибки MySQL: {exception.Number}.\n{exception.Message}",
                exception);
        }
        catch (DatabaseException)
        {
            throw;
        }
        catch (Exception exception)
        {
            throw new DatabaseException(
                "Ошибка при подготовке базы данных.\n\n" + exception.Message,
                exception);
        }
    }

    private static bool SeedAdministrator(AppDbContext context)
    {
        if (context.Users.Any())
        {
            return false;
        }

        context.Users.Add(new User
        {
            Login = "admin",
            PasswordHash = PasswordService.Hash("admin123"),
            Role = UserRole.Administrator,
            FullName = "Администратор системы"
        });

        context.SaveChanges();
        return true;
    }

    private static void EnsureColumn(AppDbContext context, string table, string column, string definition)
    {
        var connection = context.Database.GetDbConnection();

        try
        {
            if (connection.State != System.Data.ConnectionState.Open)
            {
                connection.Open();
            }

            using var checkCommand = connection.CreateCommand();
            checkCommand.CommandText =
                "SELECT COUNT(*) FROM information_schema.columns " +
                "WHERE table_schema = @database AND table_name = @table AND column_name = @column;";

            var databaseParameter = checkCommand.CreateParameter();
            databaseParameter.ParameterName = "@database";
            databaseParameter.Value = connection.Database;
            checkCommand.Parameters.Add(databaseParameter);

            var tableParameter = checkCommand.CreateParameter();
            tableParameter.ParameterName = "@table";
            tableParameter.Value = table;
            checkCommand.Parameters.Add(tableParameter);

            var columnParameter = checkCommand.CreateParameter();
            columnParameter.ParameterName = "@column";
            columnParameter.Value = column;
            checkCommand.Parameters.Add(columnParameter);

            int count = Convert.ToInt32(checkCommand.ExecuteScalar());
            if (count > 0)
            {
                return;
            }

            using var alterCommand = connection.CreateCommand();
            alterCommand.CommandText =
                $"ALTER TABLE `{table}` ADD COLUMN `{column}` {definition};";
            alterCommand.ExecuteNonQuery();
        }
        finally
        {
            if (connection.State == System.Data.ConnectionState.Open)
            {
                connection.Close();
            }
        }
    }

    private static Version? ParseVersion(string versionText)
    {
        Match match = VersionRegex().Match(versionText);
        if (!match.Success)
        {
            return null;
        }

        int major = int.Parse(match.Groups[1].Value);
        int minor = int.Parse(match.Groups[2].Value);
        int build = int.Parse(match.Groups[3].Value);
        return new Version(major, minor, build);
    }

    [GeneratedRegex(@"(\d+)\.(\d+)\.(\d+)", RegexOptions.CultureInvariant)]
    private static partial Regex VersionRegex();
}
