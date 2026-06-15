using Microsoft.EntityFrameworkCore;
using CourseProject.Parser.Data;
using CourseProject.Parser.Models;

namespace CourseProject.Parser.Services
{
    public class AuditoriumService
    {
        private readonly AppDbContext _context;

        // соответствие времени номерам пар
        private readonly List<(TimeSpan Start, TimeSpan End, int PairNumber)> _pairSchedule = new()
        {
            (TimeSpan.FromHours(8).Add(TimeSpan.FromMinutes(10)), TimeSpan.FromHours(9).Add(TimeSpan.FromMinutes(30)), 1),
            (TimeSpan.FromHours(9).Add(TimeSpan.FromMinutes(40)), TimeSpan.FromHours(11).Add(TimeSpan.FromMinutes(0)), 2),
            (TimeSpan.FromHours(11).Add(TimeSpan.FromMinutes(30)), TimeSpan.FromHours(12).Add(TimeSpan.FromMinutes(50)), 3),
            (TimeSpan.FromHours(13).Add(TimeSpan.FromMinutes(10)), TimeSpan.FromHours(14).Add(TimeSpan.FromMinutes(30)), 4),
            (TimeSpan.FromHours(15).Add(TimeSpan.FromMinutes(0)), TimeSpan.FromHours(16).Add(TimeSpan.FromMinutes(20)), 5),
            (TimeSpan.FromHours(16).Add(TimeSpan.FromMinutes(40)), TimeSpan.FromHours(18).Add(TimeSpan.FromMinutes(0)), 6),
            (TimeSpan.FromHours(18).Add(TimeSpan.FromMinutes(20)), TimeSpan.FromHours(19).Add(TimeSpan.FromMinutes(40)), 7),
            (TimeSpan.FromHours(20).Add(TimeSpan.FromMinutes(10)), TimeSpan.FromHours(21).Add(TimeSpan.FromMinutes(30)), 8)
        };

        public AuditoriumService(AppDbContext context)
        {
            _context = context;
        }

        // получение всех аудиторий
        public async Task<List<Auditorium>> GetAllAuditoriumsAsync()
        {
            return await _context.Set<Auditorium>()
                .OrderBy(a => a.Building)
                .ThenBy(a => a.RoomNumber)
                .ToListAsync();
        }

        // получение списка уникальных корпусов
        public async Task<List<string>> GetUniqueBuildingsAsync()
        {
            return await _context.Set<Auditorium>()
                .Select(a => a.Building)
                .Distinct()
                .OrderBy(b => b)
                .ToListAsync();
        }

        // преобразование временного интервала в номера пар
        private List<int> GetAffectedPairNumbers(TimeSpan startTime, TimeSpan endTime)
        {
            var affectedPairs = new List<int>();

            foreach (var pair in _pairSchedule)
            {
                // если временной интервал пересекается с парой
                if (startTime < pair.End && endTime > pair.Start)
                {
                    affectedPairs.Add(pair.PairNumber);
                }
            }

            return affectedPairs;
        }

        // получение свободных аудиторий
        public async Task<List<Auditorium>> GetFreeAuditoriumsAsync(
            string? building,
            int? minCapacity,
            int? maxCapacity,
            bool? hasComputers,
            bool? hasProjector,
            DateTime selectedDate,
            TimeSpan startTime,
            TimeSpan endTime)
        {
            // получаем номера пар, попадающие в выбранный интервал
            var affectedPairNumbers = GetAffectedPairNumbers(startTime, endTime);

            if (affectedPairNumbers.Count == 0)
            {
                return new List<Auditorium>();
            }

            // все аудитории с учётом фильтров
            var auditoriumsQuery = _context.Set<Auditorium>().AsQueryable();

            if (!string.IsNullOrEmpty(building))
            {
                auditoriumsQuery = auditoriumsQuery.Where(a => a.Building == building);
            }

            if (minCapacity.HasValue)
            {
                auditoriumsQuery = auditoriumsQuery.Where(a => a.Capacity >= minCapacity.Value);
            }

            if (maxCapacity.HasValue)
            {
                auditoriumsQuery = auditoriumsQuery.Where(a => a.Capacity <= maxCapacity.Value);
            }

            if (hasComputers.HasValue)
            {
                auditoriumsQuery = auditoriumsQuery.Where(a => a.HasComputers == hasComputers.Value);
            }

            if (hasProjector.HasValue)
            {
                auditoriumsQuery = auditoriumsQuery.Where(a => a.HasProjector == hasProjector.Value);
            }

            var allAuditoriums = await auditoriumsQuery.ToListAsync();

            // получение занятых аудиторий из расписания
            var occupiedFromSchedule = await _context.Set<ScheduleRecord>()
                .Where(r => r.Date == selectedDate && affectedPairNumbers.Contains(r.PairNumber))
                .Select(r => new { r.Building, r.Cabinet })
                .Distinct()
                .ToListAsync();

            var occupiedKeys = occupiedFromSchedule
                .Select(o => $"{o.Building}_{o.Cabinet}")
                .ToHashSet();

            // получение занятых аудиторий из бронирований
            var occupiedFromBookings = await _context.Set<Booking>()
                .Where(b => b.BookingDate == selectedDate &&
                       b.Status == "active" &&
                       ((b.StartTime < endTime && b.EndTime > startTime)))
                .Select(b => b.AuditoriumId)
                .ToListAsync();

            var occupiedBookingIds = occupiedFromBookings.ToHashSet();

            // фильтрация свободных аудиторий
            return allAuditoriums.Where(a =>
                !occupiedBookingIds.Contains(a.Id) &&
                !occupiedKeys.Contains($"{a.Building}_{a.RoomNumber}")).ToList();
        }

        // проверка, свободна ли конкретная аудитория
        public async Task<bool> IsAuditoriumFreeAsync(
            int auditoriumId,
            string building,
            string roomNumber,
            DateTime date,
            TimeSpan startTime,
            TimeSpan endTime)
        {
            var affectedPairNumbers = GetAffectedPairNumbers(startTime, endTime);

            if (affectedPairNumbers.Count == 0)
            {
                return false;
            }

            // проверка расписания
            var hasSchedule = await _context.Set<ScheduleRecord>()
                .AnyAsync(r => r.Date == date &&
                          r.Building == building &&
                          r.Cabinet == roomNumber &&
                          affectedPairNumbers.Contains(r.PairNumber));

            if (hasSchedule) return false;

            // проверка бронирований
            var hasBooking = await _context.Set<Booking>()
                .AnyAsync(b => b.AuditoriumId == auditoriumId &&
                          b.BookingDate == date &&
                          b.Status == "active" &&
                          ((b.StartTime < endTime && b.EndTime > startTime)));

            return !hasBooking;
        }

        // получение аудитории по id
        public async Task<Auditorium?> GetAuditoriumByIdAsync(int id)
        {
            return await _context.Set<Auditorium>().FindAsync(id);
        }
    }
}