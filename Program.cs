using System.Text;
using Serilog;
using Serilog.Debugging;
using Serilog.Formatting.Json;

namespace TaskManagerApp;

public class Program
{
    public static void Main()
    {
        Console.InputEncoding = new UTF8Encoding(false);
        Console.OutputEncoding = new UTF8Encoding(false);

        SelfLog.Enable(message => Console.Error.WriteLine($"[ERROR] Сбой логирования: {message.TrimEnd()}"));

        string logPath = Path.Combine("logs", "taskmanager-.jsonl");
        string outputTemplate = "[{Timestamp:yyyy-MM-dd HH:mm:ss} {Level:u3}] {Message:lj}{NewLine}{Exception}";

        Log.Logger = new LoggerConfiguration()
            .MinimumLevel.Debug()
            .Enrich.WithProperty("Application", "TaskManager")
            .Enrich.WithProperty("SessionId", Guid.NewGuid())
            .WriteTo.Console(outputTemplate: outputTemplate)
            .WriteTo.File(new JsonFormatter(renderMessage: true), logPath,
                rollingInterval: RollingInterval.Day,
                retainedFileCountLimit: 7,
                encoding: new UTF8Encoding(false))
            .CreateLogger();

        Tracer.Configure();

        try
        {
            Log.Information("Программа TaskManager с трассировкой операций запущена");
            Log.Debug("Логер настроен: структурированные логи и трассировка TraceSource");
            Console.WriteLine($"Папка логов: {Path.GetFullPath("logs")}");
            Console.WriteLine($"Файл трассировки: {Path.GetFullPath(Path.Combine("logs", "taskmanagerTrace.log"))}");
            ShowHelp();

            var taskManager = new TaskManager();
            bool isRunning = true;

            while (isRunning)
            {
                Console.Write("\nКоманда > ");
                string? command = Console.ReadLine();

                if (command == null)
                {
                    Log.Information("Поток ввода завершён");
                    break;
                }

                command = command.Trim().ToLowerInvariant();
                Log.Debug("Получена команда {Command}", command);

                switch (command)
                {
                    case "add":
                    case "remove":
                        Console.Write(command == "add"
                            ? "Название новой задачи: "
                            : "Название задачи для удаления: ");
                        string? title = Console.ReadLine();

                        if (title == null)
                        {
                            Log.Warning("Операция {Command} отменена: поток ввода завершён", command);
                            isRunning = false;
                            break;
                        }

                        if (command == "add")
                        {
                            taskManager.AddTask(title);
                        }
                        else
                        {
                            taskManager.RemoveTask(title);
                        }

                        break;
                    case "list":
                        taskManager.ListTasks();
                        break;
                    case "help":
                        ShowHelp();
                        break;
                    case "exit":
                        isRunning = false;
                        break;
                    default:
                        Log.Warning("Неизвестная команда \"{Command:l}\". Введите help для справки", command);
                        break;
                }
            }
        }
        finally
        {
            Log.Information("Завершение работы программы");
            Tracer.Close();
            Log.CloseAndFlush();
            SelfLog.Disable();
        }
    }

    private static void ShowHelp()
    {
        Console.WriteLine("add    - добавить задачу");
        Console.WriteLine("remove - удалить задачу по названию");
        Console.WriteLine("list   - вывести список задач");
        Console.WriteLine("help   - показать команды");
        Console.WriteLine("exit   - завершить работу");
        Console.WriteLine("Название: от 1 до 100 символов без управляющих символов");
    }
}
