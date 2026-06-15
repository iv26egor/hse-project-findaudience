using System;
using System.Text.RegularExpressions;

namespace CourseProject.Parser
{
    public static class FileNameDateExtractor
    {
        // метод для извлечения даты начала недели из имени файла
        public static DateTime? ExtractStartDateFromFileName(string fileName)
        {
            // регулярное выражение для поиска даты в формате дд.мм.гггг
            var datePattern = @"(\d{2})\.(\d{2})\.(\d{4})";
            var match = Regex.Match(fileName, datePattern);

            if (match.Success)
            {
                try
                {
                    int day = int.Parse(match.Groups[1].Value);
                    int month = int.Parse(match.Groups[2].Value);
                    int year = int.Parse(match.Groups[3].Value);
                    return new DateTime(year, month, day);
                }
                catch (Exception)
                {
                    return null;
                }
            }

            return null;
        }

        // метод для преобразования названия дня недели в дату
        public static DateTime GetDateFromWeekStart(DateTime weekStartDate, string dayName)
        {
            int offset = dayName switch
            {
                "Понедельник" => 0,
                "Вторник" => 1,
                "Среда" => 2,
                "Четверг" => 3,
                "Пятница" => 4,
                "Суббота" => 5,
                "Воскресенье" => 6,
                _ => 0
            };

            return weekStartDate.AddDays(offset);
        }

        // метод для проверки, что файл является файлом расписания
        public static bool IsScheduleFile(string fileName)
        {
            var pattern = @"Расписание занятий.*неделя.*\d{2}\.\d{2}\.\d{4}";
            return Regex.IsMatch(fileName, pattern, RegexOptions.IgnoreCase);
        }
    }
}