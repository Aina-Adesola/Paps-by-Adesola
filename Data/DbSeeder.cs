using Microsoft.AspNetCore.Identity;
using TemsGroupProject.Models;

namespace TemsGroupProject.Data
{
    public static class DbSeeder
    {
        // Call this once at startup. It creates the 4 roles if missing, and creates
        // one bootstrap SGC user so someone can actually log in and start creating
        // other users (nothing in the API lets you create an SGC account otherwise).
        public static async Task SeedAsync(IServiceProvider services)
        {
            var roleManager = services.GetRequiredService<RoleManager<IdentityRole<int>>>();
            var userManager = services.GetRequiredService<UserManager<ApplicationUser>>();

            string[] allRoles = { Roles.Audit, Roles.Requester, Roles.Approver, Roles.SGC };

            foreach (var roleName in allRoles)
            {
                if (!await roleManager.RoleExistsAsync(roleName))
                {
                    await roleManager.CreateAsync(new IdentityRole<int>(roleName));
                }
            }

            const string bootstrapEmail = "ainadeshola@gmail.com";
            if (await userManager.FindByEmailAsync(bootstrapEmail) is null)
            {
                var sgcUser = new ApplicationUser
                {
                    FirstName = "System",
                    LastName = "Admin",
                    Email = bootstrapEmail,
                    UserName = bootstrapEmail,
                    StaffId = "SGC-000",
                    DateCreated = DateTime.UtcNow,
                    EmailConfirmed = true
                };

                // CHANGE THIS PASSWORD before deploying anywhere real.
                var result = await userManager.CreateAsync(sgcUser, "Trial+123!");
                if (result.Succeeded)
                {
                    await userManager.AddToRoleAsync(sgcUser, Roles.SGC);
                }
            }
        }
    }
}

//"firstName": "Boluwatife",
//"lastName": "Aina",
//  "email": "boluwatife.aina@gmail.com",
//  "staffId": "ST1039874",
//  "password": "Aina+Adesola12",
//  "role": "SGC"

//"email": "oluwanisunayomiii@gmail.com",
//    "password": "Admin+Nisun1234"


//"email": "favourchukwudi302@gmail.com",
//    "password": "Admin+Nisun1234"

//"email": "ayomide.olayode@gmail.com",
//    "password": "Ayo+Shola123"