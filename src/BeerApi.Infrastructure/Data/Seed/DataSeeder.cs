using BeerApi.Infrastructure.Identity;
using Microsoft.AspNetCore.Identity;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;

namespace BeerApi.Infrastructure.Data.Seed;

public static class DataSeeder
{
    public static async Task SeedRolesAndAdminAsync(IServiceProvider services)
    {
        var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();
        var config = services.GetRequiredService<IConfiguration>();

        string[] roles = ["Admin", "Brewer", "Wholesaler"];
        foreach (var role in roles)
        {
            if (!await roleManager.RoleExistsAsync(role))
            {
                var result = await roleManager.CreateAsync(new IdentityRole(role));
                if (!result.Succeeded)
                    throw new InvalidOperationException($"Could not create role '{role}': {string.Join("; ", result.Errors.Select(error => error.Description))}");
            }
        }

        var adminEmail = config["AdminUser:Email"] ?? "admin@beerapi.com";
        var adminPassword = config["AdminUser:Password"];
        if (string.IsNullOrWhiteSpace(adminPassword))
        {
            if (services.GetRequiredService<IHostEnvironment>().IsProduction())
                throw new InvalidOperationException("AdminUser:Password must be configured in Production.");
            adminPassword = "Admin@123!";
        }

        if (await userManager.FindByEmailAsync(adminEmail) is null)
        {
            var admin = new ApplicationUser
            {
                UserName = adminEmail,
                Email = adminEmail,
                FirstName = "Admin",
                LastName = "User",
                EmailConfirmed = true
            };

            var result = await userManager.CreateAsync(admin, adminPassword);
            if (!result.Succeeded)
                throw new InvalidOperationException($"Could not create the configured admin user: {string.Join("; ", result.Errors.Select(error => error.Description))}");

            var roleResult = await userManager.AddToRoleAsync(admin, "Admin");
            if (!roleResult.Succeeded)
                throw new InvalidOperationException($"Could not assign the Admin role: {string.Join("; ", roleResult.Errors.Select(error => error.Description))}");
        }
    }
}
