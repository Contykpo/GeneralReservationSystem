using GeneralReservationSystem.Application.Helpers;
using Microsoft.Data.SqlClient;
using System.Reflection;

namespace GeneralReservationSystem.Infrastructure.Database
{
    public static class MigrationsRunner
    {
        public static void RunMigrations(string connectionString)
        {
            ArgumentNullException.ThrowIfNull(connectionString);

            Console.WriteLine("Starting database migrations...");

            using SqlConnection connection = new(connectionString);
            connection.Open();

            Assembly assembly = Assembly.GetExecutingAssembly();
            string[] resourceNames = assembly.GetManifestResourceNames();

            foreach (string resourceName in resourceNames)
            {
                if (resourceName.Contains("Migrations") && resourceName.EndsWith(".sql"))
                {
                    Console.WriteLine($"Running migration: {resourceName}");
                    using Stream? stream = assembly.GetManifestResourceStream(resourceName);
                    if (stream == null)
                    {
                        Console.WriteLine($"Could not find resource: {resourceName}");
                        continue;
                    }
                    using StreamReader reader = new(stream);
                    string sql = reader.ReadToEnd();
                    try
                    {
                        using SqlCommand command = new(sql, connection);
                        _ = command.ExecuteNonQuery();
                        Console.WriteLine($"Migration {resourceName} applied successfully.");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Error applying migration {resourceName}: {ex.Message}");
                        throw;
                    }
                }
            }

            Console.WriteLine("All migrations processed.");
        }

        public static void SeedData(string connectionString)
        {
            ArgumentNullException.ThrowIfNull(connectionString);
            Console.WriteLine("Starting data seeding...");
            using SqlConnection connection = new(connectionString);
            connection.Open();

            // TODO: Find a better way to do this.

            // Create a default admin user if it doesn't exists.
            string checkAdminSql = "SELECT COUNT(*) FROM ApplicationUser WHERE UserName = 'admin'";
            using (SqlCommand checkCommand = new(checkAdminSql, connection))
            {
                int adminCount = (int)checkCommand.ExecuteScalar()!;
                if (adminCount == 0)
                {
                    (byte[] hash, byte[] salt) = PasswordHelper.HashPassword("admin123");

                    string insertAdminSql = @"
                        INSERT INTO ApplicationUser (UserName, Email, PasswordHash, PasswordSalt, IsAdmin)
                        VALUES (@UserName, @Email, @PasswordHash, @PasswordSalt, @IsAdmin)";

                    using (SqlCommand insertCommand = new(insertAdminSql, connection))
                    {
                        _ = insertCommand.Parameters.AddWithValue("@UserName", "admin");
                        _ = insertCommand.Parameters.AddWithValue("@Email", "admin@example.com");
                        _ = insertCommand.Parameters.AddWithValue("@PasswordHash", hash);
                        _ = insertCommand.Parameters.AddWithValue("@PasswordSalt", salt);
                        _ = insertCommand.Parameters.AddWithValue("@IsAdmin", true);
                        _ = insertCommand.ExecuteNonQuery();
                    }

                    Console.WriteLine("Default admin user created.");
                }
                else
                {
                    Console.WriteLine("Admin user already exists. Skipping creation.");
                }
            }

            Console.WriteLine("Data seeding completed.");
        }
    }
}
