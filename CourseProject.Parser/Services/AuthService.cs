using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;

namespace CourseProject.Parser.Services
{
    public class AuthService
    {
        private readonly UserService _userService;
        private readonly CustomAuthenticationStateProvider _authProvider;

        public AuthService(UserService userService, AuthenticationStateProvider authProvider)
        {
            _userService = userService;
            _authProvider = authProvider as CustomAuthenticationStateProvider;
        }

        public async Task<bool> LoginAsync(string email, string password)
        {
            var user = await _userService.GetUserByEmailAsync(email);

            if (user == null)
                return false;

            bool isValid = BCrypt.Net.BCrypt.Verify(password, user.PasswordHash);

            if (isValid && _authProvider != null)
            {
                await _authProvider.LoginAsync(user.Id);
                return true;
            }

            return false;
        }

        public async Task LogoutAsync()
        {
            if (_authProvider != null)
            {
                await _authProvider.LogoutAsync();
            }
        }

        public async Task<int> GetCurrentUserIdAsync()
        {
            if (_authProvider == null)
                return 0;

            return await _authProvider.GetCurrentUserIdAsync();
        }
    }
}