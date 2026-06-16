using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Components.Authorization;
using CourseProject.Parser.Data;
using CourseProject.Parser.Services;
using MySite.Components;

namespace MySite
{
    public class Program
    {
        public static void Main(string[] args)
        {
            var builder = WebApplication.CreateBuilder(args);

            AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
            AppContext.SetSwitch("Npgsql.DisableDateTimeInfinityConversions", true);

            builder.Services.AddRazorComponents()
                .AddInteractiveServerComponents();

            var connectionString = builder.Configuration.GetConnectionString("DefaultConnection");
            builder.Services.AddDbContext<AppDbContext>(options =>
                options.UseNpgsql(connectionString));

            // регистрация сервисов
            builder.Services.AddScoped<AuditoriumService>();
            builder.Services.AddScoped<BookingService>();
            builder.Services.AddScoped<ScheduleService>();
            builder.Services.AddScoped<UserService>();
            builder.Services.AddHostedService<ScheduleMonitorService>();

            // аутентификация
            builder.Services.AddScoped<AuthenticationStateProvider, CustomAuthenticationStateProvider>();
            builder.Services.AddScoped<AuthService>();
            builder.Services.AddCascadingAuthenticationState();
            
            var app = builder.Build();

            if (!app.Environment.IsDevelopment())
            {
                app.UseExceptionHandler("/Error");
                app.UseHsts();
            }

            app.UseStatusCodePagesWithReExecute("/not-found", createScopeForStatusCodePages: true);
            app.UseHttpsRedirection();
            app.UseStaticFiles();
            app.UseAntiforgery();

            app.MapRazorComponents<App>()
                .AddInteractiveServerRenderMode();

            app.Run();
        }
    }
}
