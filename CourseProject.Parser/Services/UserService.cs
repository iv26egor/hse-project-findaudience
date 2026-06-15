using Microsoft.EntityFrameworkCore;
using CourseProject.Parser.Data;
using CourseProject.Parser.Models;

namespace CourseProject.Parser.Services
{
    public class UserService
    {
        private readonly AppDbContext _context;

        public UserService(AppDbContext context)
        {
            _context = context;
        }

        // регистрация нового пользователя
        public async Task<(bool success, string message)> RegisterUserAsync(
            string firstName,
            string lastName,
            string? middleName,
            string group,
            string email,
            string password)
        {
            // проверка на существующего пользователя
            var existingUser = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (existingUser != null)
            {
                return (false, "Пользователь с таким email уже существует");
            }

            // хеширование пароля
            string passwordHash = BCrypt.Net.BCrypt.HashPassword(password);

            var user = new User
            {
                FirstName = firstName,
                LastName = lastName,
                MiddleName = middleName,
                Group = group,
                Email = email,
                PasswordHash = passwordHash,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };

            try
            {
                _context.Users.Add(user);
                await _context.SaveChangesAsync();
                return (true, "Регистрация прошла успешно");
            }
            catch (Exception ex)
            {
                return (false, $"Ошибка при сохранении: {ex.Message}");
            }
        }

        // аутентификация пользователя
        public async Task<(bool success, string message, User? user)> LoginAsync(string email, string password)
        {
            // поиск пользователя по email
            var user = await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);

            if (user == null)
            {
                return (false, "Неверный email или пароль", null);
            }

            // проверка пароля
            bool isValidPassword = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);

            if (!isValidPassword)
            {
                return (false, "Неверный email или пароль", null);
            }

            // обновление времени последнего входа
            user.UpdatedAt = DateTime.UtcNow;
            await _context.SaveChangesAsync();

            return (true, "Вход выполнен успешно", user);
        }

        // получение пользователя по id
        public async Task<User?> GetUserByIdAsync(int id)
        {
            return await _context.Users.FindAsync(id);
        }

        // получение пользователя по email
        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _context.Users
                .FirstOrDefaultAsync(u => u.Email == email);
        }
    }
}