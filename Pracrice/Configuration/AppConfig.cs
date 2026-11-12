using MySqlConnector;

namespace Pracrice.Configuration;

public sealed class AppConfig
{
    public required string Host { get; init; }

    public required int Port { get; init; }

    public required string User { get; init; }

    public required string Password { get; init; }

    public required string Database { get; init; }

    public string ServerConnectionString => BuildConnectionString(database: null);

    public string DatabaseConnectionString => BuildConnectionString(Database);

    public static AppConfig Default => new()
    {
        Host = "127.0.0.1",
        Port = 3306,
        User = "root",
        Password = string.Empty,
        Database = "RosPiv"
    };

    private string BuildConnectionString(string? database)
    {
        var builder = new MySqlConnectionStringBuilder
        {
            Server = Host,
            Port = (uint)Port,
            UserID = User,
            Password = Password,
            CharacterSet = "utf8mb4",
            SslMode = MySqlSslMode.None,
            AllowPublicKeyRetrieval = true,
            ConnectionTimeout = 6,
            DefaultCommandTimeout = 30
        };

        if (!string.IsNullOrWhiteSpace(database))
        {
            builder.Database = database;
        }

        return builder.ConnectionString;
    }
}

public sealed class ConfigException : Exception
{
    public ConfigException(string message) : base(message)
    {
    }

    public ConfigException(string message, Exception innerException) : base(message, innerException)
    {
    }
}

public sealed record ConfigLoadResult(AppConfig Config, bool CreatedDefaultFile);
