using System.IO;
using System.Text;
using System.Text.RegularExpressions;

namespace Pracrice.Configuration;

public static partial class ConfigService
{
    public const string FileName = "config.ini";

    private const string DefaultFileContent = """
        [Database]
        Host = 127.0.0.1
        Port = 3306
        User = root
        Password =
        DB = RosPiv
        """;

    public static string ConfigPath => Path.Combine(AppContext.BaseDirectory, FileName);

    public static ConfigLoadResult Load()
    {
        bool created = false;

        if (!File.Exists(ConfigPath))
        {
            TryCreateDefaultFile();
            created = true;
        }

        string text;
        try
        {
            text = File.ReadAllText(ConfigPath, Encoding.UTF8);
        }
        catch (Exception exception)
        {
            throw new ConfigException(
                $"Файл настроек не найден или недоступен для чтения:\n{ConfigPath}\n\n{exception.Message}",
                exception);
        }

        var values = ParseDatabaseSection(text);

        string host = GetRequired(values, "Host");
        string user = GetRequired(values, "User");
        string database = GetRequired(values, "DB", "Database");

        string portText = GetRequired(values, "Port");
        if (!int.TryParse(portText, out int port) || port is < 1 or > 65535)
        {
            throw new ConfigException($"В config.ini указан некорректный порт: «{portText}».");
        }

        if (!DatabaseNameRegex().IsMatch(database))
        {
            throw new ConfigException(
                "Имя базы данных в config.ini может содержать только латинские буквы, цифры и знак подчёркивания.");
        }

        values.TryGetValue("Password", out string? password);

        return new ConfigLoadResult(new AppConfig
        {
            Host = host,
            Port = port,
            User = user,
            Password = Unquote(password ?? string.Empty),
            Database = database
        }, created);
    }

    private static void TryCreateDefaultFile()
    {
        try
        {
            File.WriteAllText(ConfigPath, DefaultFileContent, new UTF8Encoding(false));
        }
        catch (Exception exception)
        {
            throw new ConfigException(
                $"Не удалось создать файл настроек:\n{ConfigPath}\n\n{exception.Message}",
                exception);
        }
    }

    private static Dictionary<string, string> ParseDatabaseSection(string text)
    {
        var values = new Dictionary<string, string>(StringComparer.OrdinalIgnoreCase);
        string? section = null;
        int lineNumber = 0;

        foreach (string rawLine in text.Split('\n'))
        {
            lineNumber++;
            string line = rawLine.Trim().TrimEnd('\r');

            if (line.Length == 0 || line.StartsWith(';') || line.StartsWith('#'))
            {
                continue;
            }

            if (line.StartsWith('[') && line.EndsWith(']'))
            {
                section = line[1..^1].Trim();
                continue;
            }

            int separator = line.IndexOf('=');
            if (separator <= 0)
            {
                throw new ConfigException($"Ошибка в config.ini, строка {lineNumber}: ожидается формат «Ключ = Значение».");
            }

            if (!string.Equals(section, "Database", StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            string key = line[..separator].Trim();
            string value = line[(separator + 1)..].Trim();

            if (key.Length > 0)
            {
                values[key] = value;
            }
        }

        return values;
    }

    private static string GetRequired(Dictionary<string, string> values, params string[] keys)
    {
        foreach (string key in keys)
        {
            if (values.TryGetValue(key, out string? value) && !string.IsNullOrWhiteSpace(value))
            {
                return Unquote(value.Trim());
            }
        }

        throw new ConfigException(
            $"В файле config.ini не заполнен обязательный параметр [{string.Join(" / ", keys)}].");
    }

    private static string Unquote(string value)
    {
        if (value.Length >= 2 &&
            ((value[0] == '"' && value[^1] == '"') || (value[0] == '\'' && value[^1] == '\'')))
        {
            return value[1..^1];
        }

        return value;
    }

    [GeneratedRegex(@"^[A-Za-z0-9_]{1,64}$", RegexOptions.CultureInvariant)]
    private static partial Regex DatabaseNameRegex();
}
