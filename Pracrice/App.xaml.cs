using System.IO;
using System.Text;
using System.Windows;
using System.Windows.Threading;
using Pracrice.Configuration;
using Pracrice.Data;

namespace Pracrice;

public partial class App : Application
{
    protected override void OnStartup(StartupEventArgs e)
    {
        base.OnStartup(e);

        RegisterExceptionHandlers();

        try
        {
            AppPaths.EnsureDirectories();

            ConfigLoadResult configResult = ConfigService.Load();
            if (configResult.CreatedDefaultFile)
            {
                MessageBox.Show(
                    $"Файл настроек не был найден, поэтому создан файл по умолчанию:\n{ConfigService.ConfigPath}\n\n" +
                    "При необходимости измените в нём параметры подключения и перезапустите приложение.",
                    "Настройки подключения",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }

            DatabaseInitResult result = DatabaseInitializer.Initialize(configResult.Config);

            if (result.AdministratorCreated)
            {
                MessageBox.Show(
                    "База данных подготовлена к работе.\n\n" +
                    "Создана учётная запись администратора:\n" +
                    "Логин: admin\nПароль: admin123\n\n" +
                    "После входа смените пароль в разделе «Администрирование».",
                    "Первый запуск",
                    MessageBoxButton.OK,
                    MessageBoxImage.Information);
            }
        }
        catch (ConfigException exception)
        {
            LogException(exception);
            MessageBox.Show(exception.Message, "Ошибка настроек", MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
        catch (DatabaseException exception)
        {
            LogException(exception);
            MessageBox.Show(exception.Message, "Ошибка подключения к базе данных",
                MessageBoxButton.OK, MessageBoxImage.Error);
            Shutdown();
        }
        catch (Exception exception)
        {
            LogException(exception);
            MessageBox.Show(
                "Не удалось запустить приложение.\n\n" + exception.Message,
                "Критическая ошибка",
                MessageBoxButton.OK,
                MessageBoxImage.Error);
            Shutdown();
        }
    }

    private void RegisterExceptionHandlers()
    {
        DispatcherUnhandledException += OnDispatcherUnhandledException;
        AppDomain.CurrentDomain.UnhandledException += (_, args) =>
            LogException(args.ExceptionObject as Exception ?? new Exception("Неизвестная ошибка."));
        TaskScheduler.UnobservedTaskException += (_, args) =>
        {
            LogException(args.Exception);
            args.SetObserved();
        };
    }

    private void OnDispatcherUnhandledException(object sender, DispatcherUnhandledExceptionEventArgs e)
    {
        LogException(e.Exception);
        MessageBox.Show(
            "Во время работы приложения произошла непредвиденная ошибка.\n\n" +
            e.Exception.Message + "\n\nПодробности сохранены в журнале ошибок.",
            "Ошибка",
            MessageBoxButton.OK,
            MessageBoxImage.Error);
        e.Handled = true;
    }

    public static void LogException(Exception exception)
    {
        try
        {
            Directory.CreateDirectory(AppPaths.LogsDirectory);
            string text =
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] {exception.GetType().FullName}: {exception.Message}\n" +
                exception + "\n\n";
            File.AppendAllText(AppPaths.LogFile, text, new UTF8Encoding(false));
        }
        catch (Exception)
        {
        }
    }
}
