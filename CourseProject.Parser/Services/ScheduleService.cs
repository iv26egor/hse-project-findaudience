using Microsoft.EntityFrameworkCore;
using CourseProject.Parser.Data;
using CourseProject.Parser.Models;
using CourseProject.Parser;

namespace CourseProject.Parser.Services
{
    public class ScheduleService
    {
        private readonly AppDbContext _context;

        public ScheduleService(AppDbContext context)
        {
            _context = context;
        }

        public async Task SaveScheduleToDatabaseAsync(List<ExcelExtractor.LessonInfo> lessons)
        {
            int addedCount = 0;
            int updatedCount = 0;
            int skippedCount = 0;

            foreach (var lesson in lessons)
            {
                try
                {
                    // проверяем, существует ли уже такая запись
                    var existing = await _context.ScheduleRecords
                        .FirstOrDefaultAsync(r =>
                            r.Date == lesson.Date &&
                            r.PairNumber == lesson.PairNumber &&
                            r.Cabinet == lesson.Cabinet &&
                            r.Building == lesson.Building);

                    if (existing == null)
                    {
                        _context.ScheduleRecords.Add(new ScheduleRecord
                        {
                            Date = lesson.Date,
                            PairNumber = lesson.PairNumber,
                            Cabinet = lesson.Cabinet,
                            Building = lesson.Building
                        });
                        addedCount++;
                    }
                    else
                    {
                        existing.Date = lesson.Date;
                        existing.PairNumber = lesson.PairNumber;
                        existing.Cabinet = lesson.Cabinet;
                        existing.Building = lesson.Building;

                        _context.ScheduleRecords.Update(existing);
                        updatedCount++;
                    }
                }
                catch (Exception ex)
                {
                    Console.WriteLine($"ошибка при обработке записи: дата {lesson.Date:dd.MM.yyyy}, пара {lesson.PairNumber}, каб. {lesson.Cabinet}, корп. {lesson.Building}");
                    Console.WriteLine($"  {ex.Message}");
                    skippedCount++;
                }
            }

            try
            {
                await _context.SaveChangesAsync();
                Console.WriteLine($"сохранено: добавлено {addedCount}, обновлено {updatedCount}, пропущено {skippedCount}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ошибка при сохранении: {ex.Message}");

                if (ex.InnerException != null)
                {
                    Console.WriteLine($"внутренняя ошибка: {ex.InnerException.Message}");

                    if (ex.InnerException is Npgsql.PostgresException pgEx)
                    {
                        Console.WriteLine($"код ошибки: {pgEx.SqlState}");
                        Console.WriteLine($"таблица: {pgEx.TableName}");
                        Console.WriteLine($"ограничение: {pgEx.ConstraintName}");
                    }
                }

                Console.WriteLine("попытка сохранить записи по одной...");
                await SaveOneByOne(lessons);
            }
        }

        private async Task SaveOneByOne(List<ExcelExtractor.LessonInfo> lessons)
        {
            int successCount = 0;
            int errorCount = 0;

            foreach (var lesson in lessons)
            {
                try
                {
                    var existing = await _context.ScheduleRecords
                        .FirstOrDefaultAsync(r =>
                            r.Date == lesson.Date &&
                            r.PairNumber == lesson.PairNumber &&
                            r.Cabinet == lesson.Cabinet &&
                            r.Building == lesson.Building);

                    if (existing == null)
                    {
                        _context.ScheduleRecords.Add(new ScheduleRecord
                        {
                            Date = lesson.Date,
                            PairNumber = lesson.PairNumber,
                            Cabinet = lesson.Cabinet,
                            Building = lesson.Building
                        });

                        await _context.SaveChangesAsync();
                        successCount++;
                    }
                }
                catch (Exception ex)
                {
                    errorCount++;
                    if (errorCount <= 5)
                    {
                        Console.WriteLine($"ошибка в записи: дата {lesson.Date:dd.MM.yyyy}, пара {lesson.PairNumber}, каб. {lesson.Cabinet}, корп. {lesson.Building}");
                        Console.WriteLine($"  {ex.InnerException?.Message ?? ex.Message}");
                    }
                }
            }

            Console.WriteLine($"результат: успешно сохранено {successCount} из {lessons.Count}, ошибок: {errorCount}");
        }

        public async Task<List<ScheduleRecord>> GetScheduleByDate(DateTime date)
        {
            return await _context.ScheduleRecords
                .Where(r => r.Date == date)
                .OrderBy(r => r.PairNumber)
                .ToListAsync();
        }

        public async Task PrintAllSchedule()
        {
            var records = await _context.ScheduleRecords
                .OrderBy(r => r.Date)
                .ThenBy(r => r.PairNumber)
                .ToListAsync();

            if (records.Count == 0)
            {
                Console.WriteLine("в базе данных нет записей");
                return;
            }

            var grouped = records.GroupBy(r => r.Date);

            foreach (var group in grouped)
            {
                Console.WriteLine(group.Key.ToString("dd.MM.yyyy"));
                foreach (var record in group)
                {
                    Console.WriteLine($"  пара {record.PairNumber}: каб. {record.Cabinet}, корп. {record.Building}");
                }
                Console.WriteLine();
            }
        }
    }
}