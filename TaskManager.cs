using System.Diagnostics;
using Serilog;

namespace TaskManagerApp;

public class TaskManager
{
    private readonly List<Task> tasks = new();
    private int nextId = 1;

    public void AddTask(string title)
    {
        var logger = Log.ForContext("Operation", "AddTask");
        Stopwatch stopwatch = Stopwatch.StartNew();
        string result = "Error";
        string details = "задача не добавлена";
        Tracer.Write(TraceEventType.Start, 0, "Начало операции AddTask");

        try
        {
            Tracer.Write(TraceEventType.Verbose, 4, "AddTask: проверка названия задачи");
            title = title.Trim();

            if (!IsValidTitle(title, "AddTask", logger))
            {
                result = "Warning";
                details = "название задачи отклонено";
                return;
            }

            Tracer.Write(TraceEventType.Verbose, 4, "AddTask: создание задачи и добавление в коллекцию");
            var task = new Task { Id = nextId++, Title = title };
            tasks.Add(task);
            result = "Success";
            details = $"добавлена задача '{title}'";
            logger.ForContext("Result", result)
                .Information("Создана задача {@TaskItem}. Всего задач: {TaskCount}", task, tasks.Count);
        }
        finally
        {
            FinishOperation("AddTask", stopwatch, result, details, logger);
        }
    }

    public void RemoveTask(string title)
    {
        var logger = Log.ForContext("Operation", "RemoveTask");
        Stopwatch stopwatch = Stopwatch.StartNew();
        string result = "Error";
        string details = "задача не удалена";
        Tracer.Write(TraceEventType.Start, 0, "Начало операции RemoveTask");

        try
        {
            Tracer.Write(TraceEventType.Verbose, 4, "RemoveTask: проверка названия задачи");
            title = title.Trim();

            if (!IsValidTitle(title, "RemoveTask", logger))
            {
                result = "Warning";
                details = "название задачи отклонено";
                return;
            }

            Tracer.Write(TraceEventType.Verbose, 4, $"RemoveTask: поиск задачи '{title}' в коллекции");
            int index = tasks.FindIndex(task =>
                string.Equals(task.Title, title, StringComparison.OrdinalIgnoreCase));

            if (index == -1)
            {
                details = $"задача '{title}' не найдена";
                Tracer.Write(TraceEventType.Error, 3, $"RemoveTask: {details}");
                logger.Error("Задача \"{TaskTitle:l}\" не найдена для удаления. Результат: {Result:l}. Всего задач: {TaskCount}",
                    title, "NotFound", tasks.Count);
                return;
            }

            Task removedTask = tasks[index];
            tasks.RemoveAt(index);
            result = "Success";
            details = $"удалена задача '{removedTask.Title}'";
            logger.ForContext("Result", result)
                .Information("Удалена задача {@TaskItem}. Всего задач: {TaskCount}", removedTask, tasks.Count);
        }
        finally
        {
            FinishOperation("RemoveTask", stopwatch, result, details, logger);
        }
    }

    public void ListTasks()
    {
        var logger = Log.ForContext("Operation", "ListTasks");
        Stopwatch stopwatch = Stopwatch.StartNew();
        string result = "Error";
        Tracer.Write(TraceEventType.Start, 0, "Начало операции ListTasks");

        try
        {
            Tracer.Write(TraceEventType.Verbose, 4, $"ListTasks: проверка коллекции, задач: {tasks.Count}");

            if (tasks.Count == 0)
            {
                logger.Information("Список задач пуст");
            }
            else
            {
                for (int i = 0; i < tasks.Count; i++)
                {
                    Console.WriteLine($"{i + 1}. {tasks[i].Title}");
                }
            }

            logger.ForContext("Result", "Success")
                .Information("Текущий список задач: {@Tasks}. Всего задач: {TaskCount}", tasks, tasks.Count);
            result = "Success";
        }
        finally
        {
            string details = tasks.Count == 0 ? "список пуст" : "список выведен";
            FinishOperation("ListTasks", stopwatch, result, details, logger);
        }
    }

    private bool IsValidTitle(string title, string operation, ILogger logger)
    {
        if (string.IsNullOrWhiteSpace(title))
        {
            Tracer.Write(TraceEventType.Warning, 2, $"{operation}: попытка использовать пустое название задачи");
            logger.Warning("Пустое название задачи. Результат: {Result}. Название: {TaskTitle}. Всего задач: {TaskCount}",
                "InvalidTitle", title, tasks.Count);
            return false;
        }

        if (title.Length > 100 || title.Any(char.IsControl))
        {
            Tracer.Write(TraceEventType.Warning, 2, $"{operation}: невалидное название задачи");
            logger.Warning("Невалидное название: допустимо до 100 символов без управляющих символов. Результат: {Result}. Название: {TaskTitle}. Всего задач: {TaskCount}",
                "InvalidTitle", title, tasks.Count);
            return false;
        }

        return true;
    }

    private void FinishOperation(string operation, Stopwatch stopwatch, string result, string details, ILogger logger)
    {
        stopwatch.Stop();
        double elapsedMs = stopwatch.Elapsed.TotalMilliseconds;
        string resultText = result switch
        {
            "Success" => "успех",
            "Warning" => "предупреждение",
            _ => "ошибка"
        };

        Tracer.Write(TraceEventType.Stop, 1,
            $"Завершение {operation}. Результат: {resultText}. {details}. Всего задач: {tasks.Count}. Время: {elapsedMs:F3} мс");
        logger.Debug("Завершение операции {Operation}. Результат: {Result}. Время: {ElapsedMs} мс. Всего задач: {TaskCount}",
            operation, result, elapsedMs, tasks.Count);
    }
}
