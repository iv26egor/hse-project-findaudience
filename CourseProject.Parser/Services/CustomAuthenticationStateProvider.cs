using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Components.Authorization;
using Microsoft.AspNetCore.Components.Server.ProtectedBrowserStorage;

namespace CourseProject.Parser.Services
{
    public class CustomAuthenticationStateProvider : AuthenticationStateProvider
    {
        private readonly ProtectedLocalStorage _protectedLocalStorage;
        private readonly UserService _userService;
        private ClaimsPrincipal _currentUser = new ClaimsPrincipal(new ClaimsIdentity());

        public CustomAuthenticationStateProvider(
            ProtectedLocalStorage protectedLocalStorage,
            UserService userService)
        {
            _protectedLocalStorage = protectedLocalStorage;
            _userService = userService;
        }

        public override async Task<AuthenticationState> GetAuthenticationStateAsync()
        {
            Console.WriteLine("GetAuthenticationStateAsync: начато");

            try
            {
                var userIdResult = await _protectedLocalStorage.GetAsync<int>("userId");

                Console.WriteLine($"GetAuthenticationStateAsync: userId = {userIdResult.Value}, Success = {userIdResult.Success}");

                if (!userIdResult.Success || userIdResult.Value == 0)
                {
                    Console.WriteLine("GetAuthenticationStateAsync: пользователь не авторизован");
                    return new AuthenticationState(_currentUser);
                }

                var emailResult = await _protectedLocalStorage.GetAsync<string>("userEmail");
                var firstNameResult = await _protectedLocalStorage.GetAsync<string>("userFirstName");
                var lastNameResult = await _protectedLocalStorage.GetAsync<string>("userLastName");
                var groupResult = await _protectedLocalStorage.GetAsync<string>("userGroup");

                Console.WriteLine($"GetAuthenticationStateAsync: email = {emailResult.Value}");

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, userIdResult.Value.ToString()),
                    new Claim(ClaimTypes.Name, emailResult.Success ? emailResult.Value : ""),
                    new Claim(ClaimTypes.Email, emailResult.Success ? emailResult.Value : ""),
                    new Claim("FirstName", firstNameResult.Success ? firstNameResult.Value : ""),
                    new Claim("LastName", lastNameResult.Success ? lastNameResult.Value : ""),
                    new Claim("Group", groupResult.Success ? groupResult.Value : "")
                };

                var identity = new ClaimsIdentity(claims, "CustomAuth");
                _currentUser = new ClaimsPrincipal(identity);

                Console.WriteLine("GetAuthenticationStateAsync: пользователь авторизован");

                return new AuthenticationState(_currentUser);
            }
            catch (Exception ex)
            {
                Console.WriteLine($"GetAuthenticationStateAsync ошибка: {ex.Message}");
                return new AuthenticationState(_currentUser);
            }
        }

        public async Task LoginAsync(int userId)
        {
            Console.WriteLine($"LoginAsync: начато с userId = {userId}");

            var user = await _userService.GetUserByIdAsync(userId);

            if (user == null)
            {
                Console.WriteLine("LoginAsync: пользователь не найден");
                _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
                await _protectedLocalStorage.DeleteAsync("userId");
                await _protectedLocalStorage.DeleteAsync("userEmail");
                await _protectedLocalStorage.DeleteAsync("userFirstName");
                await _protectedLocalStorage.DeleteAsync("userLastName");
                await _protectedLocalStorage.DeleteAsync("userGroup");
            }
            else
            {
                Console.WriteLine($"LoginAsync: пользователь найден: {user.Email}");

                await _protectedLocalStorage.SetAsync("userId", user.Id);
                Console.WriteLine("LoginAsync: userId сохранён");

                await _protectedLocalStorage.SetAsync("userEmail", user.Email);
                Console.WriteLine("LoginAsync: userEmail сохранён");

                await _protectedLocalStorage.SetAsync("userFirstName", user.FirstName);
                await _protectedLocalStorage.SetAsync("userLastName", user.LastName);
                await _protectedLocalStorage.SetAsync("userGroup", user.Group);

                // проверяем, что данные действительно сохранились
                var testRead = await _protectedLocalStorage.GetAsync<int>("userId");
                Console.WriteLine($"LoginAsync: проверка чтения userId = {testRead.Value}, Success = {testRead.Success}");

                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, user.Email),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim("FirstName", user.FirstName),
                    new Claim("LastName", user.LastName),
                    new Claim("Group", user.Group)
                };

                var identity = new ClaimsIdentity(claims, "CustomAuth");
                _currentUser = new ClaimsPrincipal(identity);
            }

            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        public async Task LogoutAsync()
        {
            await _protectedLocalStorage.DeleteAsync("userId");
            _currentUser = new ClaimsPrincipal(new ClaimsIdentity());
            NotifyAuthenticationStateChanged(GetAuthenticationStateAsync());
        }

        public async Task<int> GetCurrentUserIdAsync()
        {
            try
            {
                var result = await _protectedLocalStorage.GetAsync<int>("userId");
                return result.Success ? result.Value : 0;
            }
            catch
            {
                return 0;
            }
        }

        public async Task<bool> IsAuthenticatedAsync()
        {
            return await GetCurrentUserIdAsync() > 0;
        }

        public async Task<string> GetCurrentUserEmailAsync()
        {
            try
            {
                var result = await _protectedLocalStorage.GetAsync<string>("userEmail");
                if (result.Success && !string.IsNullOrEmpty(result.Value))
                {
                    return result.Value;
                }

                var userId = await GetCurrentUserIdAsync();
                if (userId > 0)
                {
                    var user = await _userService.GetUserByIdAsync(userId);
                    if (user != null)
                    {
                        await _protectedLocalStorage.SetAsync("userEmail", user.Email);
                        return user.Email;
                    }
                }
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        public async Task<string> GetCurrentUserFirstNameAsync()
        {
            try
            {
                var result = await _protectedLocalStorage.GetAsync<string>("userFirstName");
                if (result.Success && !string.IsNullOrEmpty(result.Value))
                {
                    return result.Value;
                }

                var userId = await GetCurrentUserIdAsync();
                if (userId > 0)
                {
                    var user = await _userService.GetUserByIdAsync(userId);
                    if (user != null)
                    {
                        await _protectedLocalStorage.SetAsync("userFirstName", user.FirstName);
                        return user.FirstName;
                    }
                }
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        public async Task<string> GetCurrentUserLastNameAsync()
        {
            try
            {
                var result = await _protectedLocalStorage.GetAsync<string>("userLastName");
                if (result.Success && !string.IsNullOrEmpty(result.Value))
                {
                    return result.Value;
                }

                var userId = await GetCurrentUserIdAsync();
                if (userId > 0)
                {
                    var user = await _userService.GetUserByIdAsync(userId);
                    if (user != null)
                    {
                        await _protectedLocalStorage.SetAsync("userLastName", user.LastName);
                        return user.LastName;
                    }
                }
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }

        public async Task<string> GetCurrentUserGroupAsync()
        {
            try
            {
                var result = await _protectedLocalStorage.GetAsync<string>("userGroup");
                if (result.Success && !string.IsNullOrEmpty(result.Value))
                {
                    return result.Value;
                }

                var userId = await GetCurrentUserIdAsync();
                if (userId > 0)
                {
                    var user = await _userService.GetUserByIdAsync(userId);
                    if (user != null)
                    {
                        await _protectedLocalStorage.SetAsync("userGroup", user.Group);
                        return user.Group;
                    }
                }
                return string.Empty;
            }
            catch
            {
                return string.Empty;
            }
        }
    }
}