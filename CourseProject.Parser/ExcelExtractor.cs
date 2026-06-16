using NPOI.HSSF.UserModel;
using NPOI.SS.UserModel;
using NPOI.SS.Util;
using System.Text.RegularExpressions;
using CourseProject.Parser.Models;
using CourseProject.Parser.Data;
using Microsoft.EntityFrameworkCore;

namespace CourseProject.Parser
{
    public class ExcelExtractor
    {
        public class LessonInfo
        {
            public DateTime Date { get; set; }
            public int PairNumber { get; set; }
            public string Cabinet { get; set; }
            public string Building { get; set; }
            public string SheetName { get; set; }
        }

        public static List<LessonInfo> ExtractLessons(string filePath)
        {
            var lessons = new List<LessonInfo>();

            // извлекаем дату начала недели из имени файла
            var fileName = Path.GetFileName(filePath);
            var weekStartDate = FileNameDateExtractor.ExtractStartDateFromFileName(fileName);

            if (weekStartDate == null)
            {
                Console.WriteLine($"не удалось извлечь дату из имени файла: {fileName}");
                return lessons;
            }

            Console.WriteLine($"дата начала недели: {weekStartDate.Value:dd.MM.yyyy}");

            using var fs = new FileStream(filePath, FileMode.Open, FileAccess.Read);
            var workbook = new HSSFWorkbook(fs);

            for (int sheetIdx = 0; sheetIdx < workbook.NumberOfSheets; sheetIdx++)
            {
                var sheet = workbook.GetSheetAt(sheetIdx);
                var sheetLessons = ParseSheet(sheet, weekStartDate.Value);
                lessons.AddRange(sheetLessons);
            }

            return lessons;
        }

        static List<LessonInfo> ParseSheet(ISheet sheet, DateTime weekStartDate)
        {
            var lessons = new List<LessonInfo>();

            string currentDay = "";
            int currentPair = 0;

            for (int rowIdx = 0; rowIdx <= sheet.LastRowNum; rowIdx++)
            {
                var row = sheet.GetRow(rowIdx);
                if (row == null) continue;

                for (int colIdx = 0; colIdx < row.LastCellNum; colIdx++)
                {
                    var cell = row.GetCell(colIdx);
                    if (cell == null) continue;

                    string cellValue = GetCellValue(cell).Trim();

                    // определяем день недели (колонка a)
                    if (colIdx == 0 && !string.IsNullOrEmpty(cellValue))
                    {
                        var dayMatch = Regex.Match(cellValue, @"(Понедельник|Вторник|Среда|Четверг|Пятница|Суббота)");
                        if (dayMatch.Success)
                            currentDay = dayMatch.Value;
                    }

                    // определяем номер пары (колонка b)
                    if (colIdx == 1 && !string.IsNullOrEmpty(cellValue))
                    {
                        var pairMatch = Regex.Match(cellValue, @"^(\d+)\s*$", RegexOptions.Multiline);
                        if (pairMatch.Success)
                            currentPair = int.Parse(pairMatch.Groups[1].Value);
                    }

                    // если в ячейке есть занятие
                    if (!string.IsNullOrEmpty(cellValue) && !string.IsNullOrEmpty(currentDay) && currentPair > 0)
                    {
                        // извлекаем кабинет и корпус
                        var cabinetMatch = Regex.Match(cellValue, @"\((\d+)\[(\d+)\]");
                        if (cabinetMatch.Success)
                        {
                            string cabinet = cabinetMatch.Groups[1].Value;
                            string building = cabinetMatch.Groups[2].Value;

                            // преобразуем название дня недели в конкретную дату
                            DateTime lessonDate = FileNameDateExtractor.GetDateFromWeekStart(weekStartDate, currentDay);

                            lessons.Add(new LessonInfo
                            {
                                Date = lessonDate,
                                PairNumber = currentPair,
                                Cabinet = cabinet,
                                Building = building,
                                SheetName = sheet.SheetName
                            });
                        }
                    }
                }
            }

            return lessons;
        }

        static string GetCellValue(ICell cell)
        {
            if (cell == null) return "";

            switch (cell.CellType)
            {
                case CellType.String:
                    return cell.StringCellValue;
                case CellType.Numeric:
                    return cell.NumericCellValue.ToString();
                case CellType.Boolean:
                    return cell.BooleanCellValue.ToString();
                case CellType.Formula:
                    return cell.CellFormula;
                default:
                    return "";
            }
        }
    }
}
