using System;
using System.Collections.Generic;
using System.IO;
using System.Net.Http;
using System.Threading.Tasks;
using System.Threading;
using HtmlAgilityPack;

namespace CourseProject.Parser
{
    public class Parser
    {
        private static string Url = "https://perm.hse.ru/students/timetable/";
        private static string folder = "Schedule";

        public async Task<List<string>> DownloadFilesAsync(CancellationToken cancellationToken = default)
        {
            List<string> downloadedFiles = new List<string>();
            Directory.CreateDirectory(folder);

            using (HttpClient client = new HttpClient())
            {
                try
                {
                    string htmlContent = await client.GetStringAsync(Url, cancellationToken);
                    Console.WriteLine("страница загружена");

                    var htmlDoc = new HtmlDocument();
                    htmlDoc.LoadHtml(htmlContent);

                    var allLinks = htmlDoc.DocumentNode.SelectNodes("//a[@href]");

                    if (allLinks == null)
                    {
                        Console.WriteLine("не найдено ссылок");
                        return downloadedFiles;
                    }

                    List<string> urls = new List<string>();
                    HashSet<string> currentFiles = new HashSet<string>();

                    foreach (var link in allLinks)
                    {
                        string href = link.GetAttributeValue("href", "");

                        if (href.EndsWith(".xls", StringComparison.OrdinalIgnoreCase))
                        {
                            Uri baseUri = new Uri(Url);
                            Uri absoluteUri = new Uri(baseUri, href);
                            string fileName = Path.GetFileName(absoluteUri.ToString());

                            // проверяем, является ли файл файлом расписания
                            if (FileNameDateExtractor.IsScheduleFile(fileName))
                            {
                                urls.Add(absoluteUri.ToString());
                                currentFiles.Add(fileName);
                                Console.WriteLine($"найден файл расписания: {fileName}");
                            }
                        }
                    }

                    Console.WriteLine($"найдено файлов расписания: {urls.Count}");

                    // удаление файлов, которых нет на сайте
                    if (Directory.Exists(folder))
                    {
                        string[] existingFiles = Directory.GetFiles(folder, "*.xls*");
                        foreach (string existingFile in existingFiles)
                        {
                            string fileName = Path.GetFileName(existingFile);
                            if (!currentFiles.Contains(fileName))
                            {
                                File.Delete(existingFile);
                                Console.WriteLine($"удален устаревший файл: {fileName}");
                            }
                        }
                    }

                    // скачивание файлов
                    if (urls.Count > 0)
                    {
                        int downloadedFilesCount = 0;
                        for (int i = 0; i < urls.Count; i++)
                        {
                            string fileUrl = urls[i];
                            try
                            {
                                string fileName = Path.GetFileName(fileUrl);
                                string filePath = Path.Combine(folder, fileName);

                                if (File.Exists(filePath))
                                {
                                    continue;
                                }

                                byte[] fileBytes = await client.GetByteArrayAsync(fileUrl, cancellationToken);
                                await File.WriteAllBytesAsync(filePath, fileBytes, cancellationToken);

                                downloadedFiles.Add(filePath);
                                Console.WriteLine($"файл скачан: {fileName}");
                                downloadedFilesCount += 1;
                            }
                            catch (Exception ex)
                            {
                                Console.WriteLine($"ошибка при скачивании: {ex.Message}");
                            }
                        }

                        if (downloadedFilesCount > 0)
                        {
                            Console.WriteLine($"файлы сохранены в папку {Path.GetFullPath(folder)}");
                        }
                    }
                }
                catch (HttpRequestException ex)
                {
                    Console.WriteLine($"ошибка загрузки страницы: {ex.Message}");
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ошибка: {ex.Message}");
                }
            }

            return downloadedFiles;
        }

        public async Task CheckingAsync(CancellationToken cancellationToken = default)
        {
            Console.WriteLine("запущен мониторинг изменений на сайте");

            while (!cancellationToken.IsCancellationRequested)
            {
                try
                {
                    await DownloadFilesAsync(cancellationToken);
                    Console.WriteLine($"файлы были успешно обновлены. время: {DateTime.Now}");
                }
                catch (OperationCanceledException)
                {
                    Console.WriteLine("мониторинг остановлен");
                    break;
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ошибка: {ex.Message}");
                }

                await Task.Delay(TimeSpan.FromSeconds(300), cancellationToken);
            }
        }
    }
}