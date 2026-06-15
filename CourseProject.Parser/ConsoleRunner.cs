using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using CourseProject.Parser.Data;
using CourseProject.Parser;
using CourseProject.Parser.Services;

namespace CourseProject.Parser
{
    public static class ConsoleRunner
    {
        public static async Task RunAsConsoleAsync(string[] args)
        {
            try
            {
                // настройка подключения к БД
                var connectionString = "Host=localhost;Database=schedule_db;Username=postgres;Password=psDATA0908";

                var services = new ServiceCollection();
                services.AddDbContext<AppDbContext>(options =>
                    options.UseNpgsql(connectionString));
                services.AddScoped<ScheduleService>();

                var serviceProvider = services.BuildServiceProvider();

                // применяем миграции
                using (var scope = serviceProvider.CreateScope())
                {
                    var context = scope.ServiceProvider.GetRequiredService<AppDbContext>();
                    await context.Database.EnsureCreatedAsync();
                    Console.WriteLine("✓ Подключение к базе данных успешно установлено");
                }

                Console.WriteLine("=== Парсер расписания НИУ ВШЭ Пермь ===\n");
                Console.WriteLine("Выберите режим работы:");
                Console.WriteLine("1 - Однократная загрузка и парсинг");
                Console.WriteLine("2 - Мониторинг изменений (каждые 5 минут)");
                Console.Write("Ваш выбор: ");

                string choice = Console.ReadLine();
                var cts = new CancellationTokenSource();

                switch (choice)
                {
                    case "1":
                        await ProcessOnce(serviceProvider, cts.Token);
                        break;

                    case "2":
                        Console.WriteLine("Запущен мониторинг. Нажмите Ctrl+C для остановки.");
                        Console.CancelKeyPress += (s, e) =>
                        {
                            Console.WriteLine("\nОстанавливаю мониторинг...");
                            cts.Cancel();
                            e.Cancel = true;
                        };

                        await MonitorAndParse(serviceProvider, cts.Token);
                        break;

                    default:
                        Console.WriteLine("Неверный выбор");
                        break;
                }

                Console.WriteLine("\nНажмите любую клавишу для выхода...");
                Console.ReadKey();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Критическая ошибка: {ex.Message}");
                Console.WriteLine($"Стек вызовов: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Внутренняя ошибка: {ex.InnerException.Message}");
                }
                Console.ReadKey();
            }
        }

        static async Task ProcessOnce(IServiceProvider serviceProvider, CancellationToken token)
        {
            Console.WriteLine("\n=== Начинаю обработку ===\n");

            // проверяем текущую директорию
            Console.WriteLine($"Текущая директория: {Directory.GetCurrentDirectory()}");

            // проверяем существование папки Schedule
            string scheduleDir = Path.Combine(Directory.GetCurrentDirectory(), "Schedule");
            Console.WriteLine($"Поиск папки Schedule в: {scheduleDir}");

            if (!Directory.Exists(scheduleDir))
            {
                Console.WriteLine($"⚠ Папка Schedule не найдена!");
                Console.WriteLine("Создайте папку 'Schedule' и поместите в нее .xls файлы");

                // пробуем создать папку
                try
                {
                    Directory.CreateDirectory("Schedule");
                    Console.WriteLine("✓ Папка Schedule создана");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Не удалось создать папку: {ex.Message}");
                }
                return;
            }
            else
            {
                Console.WriteLine("✓ Папка Schedule найдена");
            }

            // ищем файлы
            Console.WriteLine("\nПоиск .xls файлов...");
            var xlsFiles = Directory.GetFiles(scheduleDir, "*.xls");
            var xlsxFiles = Directory.GetFiles(scheduleDir, "*.xlsx");
            var allFiles = xlsFiles.Concat(xlsxFiles).ToList();

            Console.WriteLine($"Найдено файлов: {allFiles.Count}");
            foreach (var file in allFiles)
            {
                var fileInfo = new FileInfo(file);
                Console.WriteLine($"  - {fileInfo.Name} (размер: {fileInfo.Length} байт)");
            }

            if (allFiles.Count == 0)
            {
                Console.WriteLine("⚠ Нет файлов для обработки!");
                return;
            }

            // обрабатываем каждый файл
            int totalSaved = 0;
            foreach (string file in allFiles)
            {
                try
                {
                    Console.WriteLine($"\n--- Обработка файла: {Path.GetFileName(file)} ---");

                    // проверяем, что файл не пустой
                    var fileInfo = new FileInfo(file);
                    if (fileInfo.Length == 0)
                    {
                        Console.WriteLine("⚠ Файл пустой, пропускаем");
                        continue;
                    }

                    // извлекаем данные из Excel
                    Console.WriteLine("Извлечение данных из Excel...");
                    var lessons = ExcelExtractor.ExtractLessons(file);
                    Console.WriteLine($"Извлечено {lessons.Count} записей");

                    if (lessons.Count > 0)
                    {
                        // показываем первые несколько записей для проверки
                        Console.WriteLine("Первые 3 записи:");
                        foreach (var lesson in lessons.Take(3))
                        {
                            Console.WriteLine($"  {lesson.Date}, пара {lesson.PairNumber}, каб. {lesson.Cabinet}, корп. {lesson.Building}");
                        }

                        // сохраняем в БД
                        Console.WriteLine("Сохранение в базу данных...");
                        using (var scope = serviceProvider.CreateScope())
                        {
                            var scheduleService = scope.ServiceProvider.GetRequiredService<ScheduleService>();
                            await scheduleService.SaveScheduleToDatabaseAsync(lessons);
                        }

                        Console.WriteLine($"✓ Сохранено {lessons.Count} записей");
                        totalSaved += lessons.Count;
                    }
                    else
                    {
                        Console.WriteLine("⚠ Из файла не извлечено ни одной записи");
                        Console.WriteLine("Возможные причины:");
                        Console.WriteLine("  - Файл имеет неправильный формат");
                        Console.WriteLine("  - Данные в файле не соответствуют ожидаемой структуре");
                        Console.WriteLine("  - Регулярные выражения не находят совпадений");
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"✗ Ошибка при обработке файла {file}:");
                    Console.WriteLine($"  {ex.Message}");
                    Console.WriteLine($"  Стек вызовов: {ex.StackTrace}");
                }
            }

            Console.WriteLine($"\n=== Всего сохранено записей: {totalSaved} ===");

            // выводим результат из БД
            if (totalSaved > 0)
            {
                try
                {
                    using (var scope = serviceProvider.CreateScope())
                    {
                        var scheduleService = scope.ServiceProvider.GetRequiredService<ScheduleService>();
                        Console.WriteLine("\n=== Текущее расписание из БД ===");
                        await scheduleService.PrintAllSchedule();
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка при выводе расписания: {ex.Message}");
                }
            }

            Console.WriteLine("\nГотово!");
        }

        static async Task MonitorAndParse(IServiceProvider serviceProvider, CancellationToken token)
        {
            while (!token.IsCancellationRequested)
            {
                try
                {
                    Console.WriteLine($"\n[{DateTime.Now:HH:mm:ss}] Проверка обновлений...");

                    var scheduleDir = Path.Combine(Directory.GetCurrentDirectory(), "Schedule");
                    if (Directory.Exists(scheduleDir))
                    {
                        var allFiles = Directory.GetFiles(scheduleDir, "*.xls")
                            .Concat(Directory.GetFiles(scheduleDir, "*.xlsx"))
                            .ToList();

                        if (allFiles.Count > 0)
                        {
                            Console.WriteLine($"Обнаружено {allFiles.Count} файлов!");

                            foreach (string file in allFiles)
                            {
                                Console.WriteLine($"\nПарсинг: {Path.GetFileName(file)}");

                                var lessons = ExcelExtractor.ExtractLessons(file);
                                Console.WriteLine($"Извлечено {lessons.Count} записей");

                                if (lessons.Count > 0)
                                {
                                    using (var scope = serviceProvider.CreateScope())
                                    {
                                        var scheduleService = scope.ServiceProvider.GetRequiredService<ScheduleService>();
                                        await scheduleService.SaveScheduleToDatabaseAsync(lessons);
                                    }
                                    Console.WriteLine($"Сохранено {lessons.Count} записей");
                                }
                            }
                        }
                        else
                        {
                            Console.WriteLine("Файлы не найдены в папке Schedule");
                        }
                    }
                    else
                    {
                        Console.WriteLine("Папка Schedule не найдена");
                    }

                    await Task.Delay(TimeSpan.FromMinutes(5), token);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"Ошибка: {ex.Message}");
                    await Task.Delay(TimeSpan.FromMinutes(1), token);
                }
            }
        }
    }
}