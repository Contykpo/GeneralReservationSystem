using GeneralReservationSystem.Infrastructure.Database;

namespace GeneralReservationSystem.Migration
{
    internal class Program
    {
        static void Main(string[] args)
        {
            Console.WriteLine("General Reservation System - Migration Tool");

            if (args.Length < 2 || string.IsNullOrWhiteSpace(args[1]))
                throw new ArgumentException("Usage: <action> <connectionString> [migrationName]");

            string action = args[0].ToLowerInvariant();
            string connectionString = args[1];
            string? migrationName = args.Length > 2 ? args[2] : null;

            switch (action)
            {
                case "migrate":
                    MigrationsRunner.RunMigrations(connectionString);
                    MigrationsRunner.SeedData(connectionString);
                    break;
                case "revert":
                    MigrationsRunner.RunReverts(connectionString);
                    break;
                case "migrate-one":
                    if (string.IsNullOrWhiteSpace(migrationName))
                        throw new ArgumentException("Migration name required for migrate-one.");
                    MigrationsRunner.RunMigration(connectionString, migrationName);
                    break;
                case "revert-one":
                    if (string.IsNullOrWhiteSpace(migrationName))
                        throw new ArgumentException("Migration name required for revert-one.");
                    MigrationsRunner.RunRevert(connectionString, migrationName);
                    break;
                case "seed":
                    MigrationsRunner.SeedData(connectionString);
                    break;
                default:
                    throw new ArgumentException($"Unknown action: {action}");
            }
        }
    }
}