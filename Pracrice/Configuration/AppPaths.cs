using System.IO;

namespace Pracrice.Configuration;

public static class AppPaths
{
    public static string BaseDirectory => AppContext.BaseDirectory;

    public static string ImagesDirectory => Path.Combine(BaseDirectory, "images");

    public static string ReportsDirectory => Path.Combine(BaseDirectory, "Reports");

    public static string LogsDirectory => Path.Combine(BaseDirectory, "Logs");

    public static string LogFile => Path.Combine(LogsDirectory, "errors.log");

    public static string ConfigFile => Path.Combine(BaseDirectory, ConfigService.FileName);

    public static void EnsureDirectories()
    {
        Directory.CreateDirectory(ImagesDirectory);
        Directory.CreateDirectory(ReportsDirectory);
        Directory.CreateDirectory(LogsDirectory);
    }
}
