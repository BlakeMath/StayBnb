using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using StayBnb.Models;

namespace StayBnb.Data
{
    public static class RoleSeeder
    {
        public static async Task SeedAsync(IServiceProvider services, IConfiguration configuration)
        {
            // Get the required services from the service provider.
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole>>();
            var userManager = services.GetRequiredService<UserManager<IdentityUser>>();
            var db = services.GetRequiredService<ApplicationDbContext>();
            var email = configuration["SeedAdmin:Email"];

            // Ensure the email is provided in the configuration.
            if (string.IsNullOrWhiteSpace(email))
            {
                throw new InvalidOperationException("Set SeedAdmin:Email in User Secrets.");
            }

            // All seed changes succeed together or are rolled back.
            await using var transaction = await db.Database.BeginTransactionAsync();

            // Define the roles to be seeded.
            string[] roles =
            {
                "Guest",
                "Host",
                "Admin",
                "SuperAdmin"
            };

            // Create roles if they do not exist.
            foreach (var role in roles)
            {
                if (!await roleManager.RoleExistsAsync(role))
                {
                    CheckResult(await roleManager.CreateAsync(new IdentityRole(role)),$"Creating the {role} role");
                }
            }

            // Check if the SuperAdmin user already exists.
            var user = await userManager.FindByEmailAsync(email);
            bool createdUser = false;

            // If the user does not exist, create it and assign the SuperAdmin role.
            if (user == null)
            {
                var password = configuration["SeedAdmin:Password"];

                if (string.IsNullOrWhiteSpace(password))
                {
                    throw new InvalidOperationException("Set SeedAdmin:Password in User Secrets.");
                }

                user = new IdentityUser
                {
                    UserName = email,
                    Email = email,
                    EmailConfirmed = true
                };

                CheckResult(await userManager.CreateAsync(user, password), "Creating the SuperAdmin account");

                CheckResult(await userManager.AddToRoleAsync(user, "SuperAdmin"), "Assigning the SuperAdmin role");
                createdUser = true;
            }
            else if (!await userManager.IsInRoleAsync(user, "SuperAdmin"))
            {
                throw new InvalidOperationException("The configured email already belongs to an account " + "that is not a SuperAdmin. Use an unused email.");
            }

            // Create the assignment's profile record using the same ID.
            if (!await db.ApplicationUsers.AnyAsync(profile => profile.Id == user.Id))
            {
                db.ApplicationUsers.Add(new ApplicationUser
                {
                    Id = user.Id,
                    FirstName = "Super",
                    LastName = "Admin",
                    CreatedAt = DateTime.Now
                });

                await db.SaveChangesAsync();
            }

            // Log the activity if a new user was created.
            if (createdUser)
            {
                db.ActivityLogs.Add(new ActivityLog
                {
                    UserId = user.Id,
                    Action = "SuperAdmin role assigned during initial setup",
                    ActivityType = ActivityType.RoleChanged,
                    CreatedAt = DateTime.Now
                });

                await db.SaveChangesAsync();
            }

            await transaction.CommitAsync();
        }

        // Helper method to check the result of Identity operations and throw an exception if they fail.
        private static void CheckResult(
            IdentityResult result,
            string action)
        {
            if (!result.Succeeded)
            {
                var errors = string.Join(
                    "; ",
                    result.Errors.Select(error => error.Description));

                throw new InvalidOperationException(
                    $"{action} failed: {errors}");
            }
        }
    }
}