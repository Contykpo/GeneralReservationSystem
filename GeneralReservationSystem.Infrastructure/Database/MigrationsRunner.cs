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
            //Console.WriteLine($"Connecting to: {connectionString}");

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
    }
}
