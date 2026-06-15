using Microsoft.EntityFrameworkCore;
using CourseProject.Parser.Data;
using CourseProject.Parser.Models;

namespace CourseProject.Parser.Services
{
    public class BookingService
    {
        private readonly AppDbContext _context;

        public BookingService(AppDbContext context)
        {
            _context = context;
        }

        public async Task<(bool success, string message)> CreateBookingAsync(
            int auditoriumId,
            int userId,
            DateTime bookingDate,
            TimeSpan startTime,
            TimeSpan endTime)
        {
            if (bookingDate.Date < DateTime.Today)
            {
                return (false, "Нельзя бронировать аудиторию в прошлом");
            }

            if (startTime >= endTime)
            {
                return (false, "Время начала должно быть меньше времени окончания");
            }

            if ((endTime - startTime).TotalHours > 4)
            {
                return (false, "Максимальная длительность бронирования - 4 часа");
            }

            var hasConflict = await _context.Set<Booking>()
                .AnyAsync(b => b.AuditoriumId == auditoriumId &&
                          b.BookingDate == bookingDate &&
                          b.Status == "active" &&
                          ((b.StartTime < endTime && b.EndTime > startTime)));

            if (hasConflict)
            {
                return (false, "Аудитория уже забронирована на это время");
            }

            var booking = new Booking
            {
                AuditoriumId = auditoriumId,
                UserId = userId,
                BookingDate = bookingDate,
                StartTime = startTime,
                EndTime = endTime,
                Status = "active",
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            try
            {
                _context.Set<Booking>().Add(booking);
                await _context.SaveChangesAsync();
                return (true, "Аудитория успешно забронирована");
            }
            catch (Exception ex)
            {
                return (false, $"Ошибка при бронировании: {ex.Message}");
            }
        }

        public async Task<(bool success, string message)> CancelBookingAsync(int bookingId, int userId)
        {
            var booking = await _context.Set<Booking>()
                .FirstOrDefaultAsync(b => b.Id == bookingId && b.UserId == userId);

            if (booking == null)
            {
                return (false, "Бронирование не найдено");
            }

            if (booking.Status != "active")
            {
                return (false, "Бронирование уже отменено или истекло");
            }

            booking.Status = "cancelled";
            booking.UpdatedAt = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return (true, "Бронирование отменено");
        }

        public async Task<List<Booking>> GetUserBookingsAsync(int userId)
        {
            return await _context.Set<Booking>()
                .Include(b => b.Auditorium)
                .Where(b => b.UserId == userId)
                .OrderByDescending(b => b.BookingDate)
                .ThenBy(b => b.StartTime)
                .ToListAsync();
        }
    }
}