using backend_api.Api.Data;
using backend_api.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace backend_api.Api.Data;


public static class DbSeeder
{
    public static async Task SeedAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<QuantIQContext>();
        var config  = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger  = scope.ServiceProvider.GetRequiredService<ILogger<WebApplication>>();

        var adminUsername = config["AdminSeed:Username"] ?? "admin";
        var adminEmail    = config["AdminSeed:Email"]    ?? "admin@quantiq.vn";
        var adminPassword = config["AdminSeed:Password"];

        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            logger.LogWarning(
                "[DbSeeder] AdminSeed:Password chưa được cấu hình. Bỏ qua việc tạo tài khoản Admin.");
            return;
        }
        
        var existingAdmin = await context.Users
            .AsNoTracking()
            .FirstOrDefaultAsync(u => u.Role == "Admin");

        if (existingAdmin != null)
        {
            logger.LogInformation(
                "[DbSeeder] Tài khoản Admin đã tồn tại (UserId={Id}). Bỏ qua seed.",
                existingAdmin.UserId);
            return;
        }

        var admin = new User
        {
            UserId        = Guid.NewGuid().ToString(),
            Username      = adminUsername,
            Email         = adminEmail,
            PasswordHash  = BCrypt.Net.BCrypt.HashPassword(adminPassword),
            Role          = "Admin",
            AccountStatus = "ACTIVE",   
            KycStatus     = "APPROVED",
            CreatedAt     = DateTime.UtcNow
        };

        await context.Users.AddAsync(admin);
        await context.SaveChangesAsync();

        logger.LogInformation(
            "[DbSeeder] Tài khoản Admin được tạo thành công: Username={Username}, Email={Email}",
            admin.Username, admin.Email);
    }
}
