using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace CourseProject.Parser.Services
{
    public class ScheduleMonitorService : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<ScheduleMonitorService> _logger;
        private readonly Parser _parser;
        private readonly string _scheduleDirectory;

        public ScheduleMonitorService(
            IServiceProvider serviceProvider,
            ILogger<ScheduleMonitorService> logger)
        {
            _serviceProvider = serviceProvider;
            _logger = logger;
            _parser = new Parser();
            _scheduleDirectory = Path.Combine(Directory.GetCurrentDirectory(), "Schedule");
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            _logger.LogInformation("Служба мониторинга расписания запущена");

            // создаем папку schedule, если её нет
            if (!Directory.Exists(_scheduleDirectory))
            {
                Directory.CreateDirectory(_scheduleDirectory);
                _logger.LogInformation("Создана папка Schedule по пути: {Path}", _scheduleDirectory);
            }

            // выполняем первоначальную загрузку при старте
            await DownloadAndProcessScheduleAsync(stoppingToken);

            // запускаем цикл мониторинга
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    _logger.LogInformation("Проверка обновлений расписания в: {Time}", DateTime.Now);
                    await DownloadAndProcessScheduleAsync(stoppingToken);

                    // ожидание 5 минут до следующей проверки
                    await Task.Delay(TimeSpan.FromMinutes(5), stoppingToken);
                }
                catch (OperationCanceledException)
                {
                    break;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при выполнении мониторинга расписания");
                    await Task.Delay(TimeSpan.FromMinutes(1), stoppingToken);
                }
            }

            _logger.LogInformation("Служба мониторинга расписания остановлена");
        }

        private async Task DownloadAndProcessScheduleAsync(CancellationToken token)
        {
            // шаг 1: скачиваем свежие файлы с сайта
            _logger.LogInformation("Начало скачивания файлов расписания с сайта");

            try
            {
                var downloadedFiles = await _parser.DownloadFilesAsync(token);

                if (downloadedFiles.Count > 0)
                {
                    _logger.LogInformation("Скачано новых файлов: {Count}", downloadedFiles.Count);
                    foreach (var file in downloadedFiles)
                    {
                        _logger.LogInformation("Скачан файл: {FileName}", Path.GetFileName(file));
                    }
                }
                else
                {
                    _logger.LogInformation("Новых файлов для скачивания не обнаружено");
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Ошибка при скачивании файлов с сайта");
                return;
            }

            // шаг 2: обрабатываем все файлы в папке schedule
            await ProcessScheduleFilesAsync(token);
        }

        private async Task ProcessScheduleFilesAsync(CancellationToken token)
        {
            if (!Directory.Exists(_scheduleDirectory))
            {
                _logger.LogWarning("Папка Schedule не найдена по пути: {Path}", _scheduleDirectory);
                return;
            }

            var allFiles = Directory.GetFiles(_scheduleDirectory, "*.xls")
                .Concat(Directory.GetFiles(_scheduleDirectory, "*.xlsx"))
                .ToList();

            if (allFiles.Count == 0)
            {
                _logger.LogInformation("Нет файлов для обработки в папке Schedule");
                return;
            }

            _logger.LogInformation("Найдено файлов для обработки: {Count}", allFiles.Count);

            int totalSaved = 0;

            foreach (string filePath in allFiles)
            {
                if (token.IsCancellationRequested)
                    break;

                try
                {
                    var fileName = Path.GetFileName(filePath);
                    _logger.LogInformation("Обработка файла: {FileName}", fileName);

                    // проверяем, не пустой ли файл
                    var fileInfo = new FileInfo(filePath);
                    if (fileInfo.Length == 0)
                    {
                        _logger.LogWarning("Файл {FileName} пустой, пропускается", fileName);
                        continue;
                    }

                    // извлекаем данные из excel
                    var lessons = ExcelExtractor.ExtractLessons(filePath);
                    _logger.LogInformation("Извлечено {Count} записей из файла {FileName}", lessons.Count, fileName);

                    if (lessons.Count > 0)
                    {
                        // сохраняем в базу данных
                        using var scope = _serviceProvider.CreateScope();
                        var scheduleService = scope.ServiceProvider.GetRequiredService<ScheduleService>();
                        await scheduleService.SaveScheduleToDatabaseAsync(lessons);

                        totalSaved += lessons.Count;
                        _logger.LogInformation("Сохранено {Count} записей из файла {FileName}", lessons.Count, fileName);
                    }
                    else
                    {
                        _logger.LogWarning("Из файла {FileName} не извлечено ни одной записи", fileName);
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Ошибка при обработке файла: {FilePath}", filePath);
                }
            }

            if (totalSaved > 0)
            {
                _logger.LogInformation("Всего сохранено записей: {Total}", totalSaved);
            }
        }
    }
}