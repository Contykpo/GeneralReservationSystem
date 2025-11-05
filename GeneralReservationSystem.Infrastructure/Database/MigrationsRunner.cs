using GeneralReservationSystem.Application.Helpers;
using Npgsql;
using System.Reflection;

namespace GeneralReservationSystem.Infrastructure.Database
{
    public static class MigrationsRunner
    {
        private static void EnsureMigrationsTableExists(NpgsqlConnection connection)
        {
            string createTableSql = "" +
                "CREATE TABLE IF NOT EXISTS grsdb.\"__migrations\" (" +
                    "\"MigrationName\" VARCHAR(256) PRIMARY KEY," +
                    "\"AppliedAt\" TIMESTAMPTZ NOT NULL DEFAULT NOW()" +
                ");";
            using NpgsqlCommand createTableCommand = new(createTableSql, connection);
            _ = createTableCommand.ExecuteNonQuery();
        }

        public static void RunMigrations(string connectionString)
        {
            ArgumentNullException.ThrowIfNull(connectionString);
            Console.WriteLine("Starting database migrations...");
            using NpgsqlConnection connection = new(connectionString);
            connection.Open();
            EnsureMigrationsTableExists(connection);
            Assembly assembly = Assembly.GetExecutingAssembly();
            string[] resourceNames = assembly.GetManifestResourceNames();
            foreach (string resourceName in resourceNames)
            {
                if (resourceName.Contains("Migrations") && resourceName.EndsWith(".pgsql"))
                {
                    string migrationName = Path.GetFileNameWithoutExtension(resourceName).Split('.').Last();
                    using NpgsqlTransaction transaction = connection.BeginTransaction();
                    try
                    {
                        string checkSql = "SELECT COUNT(*) FROM grsdb.\"__migrations\" WHERE \"MigrationName\" = @MigrationName";
                        using NpgsqlCommand checkCommand = new(checkSql, connection, transaction);
                        _ = checkCommand.Parameters.AddWithValue("MigrationName", migrationName);
                        long count = (long)(checkCommand.ExecuteScalar() ?? 0);
                        if (count > 0)
                        {
                            Console.WriteLine($"Migration {migrationName} already applied. Skipping.");
                            transaction.Commit();
                            continue;
                        }
                        Console.WriteLine($"Running migration: {resourceName}");
                        using Stream? stream = assembly.GetManifestResourceStream(resourceName);
                        if (stream == null)
                        {
                            Console.WriteLine($"Could not find resource: {resourceName}");
                            transaction.Commit();
                            continue;
                        }
                        using StreamReader reader = new(stream);
                        string sql = reader.ReadToEnd();
                        using NpgsqlCommand command = new(sql, connection, transaction);
                        _ = command.ExecuteNonQuery();
                        using NpgsqlCommand insertCommand = new("INSERT INTO grsdb.\"__migrations\" (\"MigrationName\") VALUES (@MigrationName)", connection, transaction);
                        _ = insertCommand.Parameters.AddWithValue("MigrationName", migrationName);
                        _ = insertCommand.ExecuteNonQuery();
                        transaction.Commit();
                        Console.WriteLine($"Migration {resourceName} applied successfully.");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Console.WriteLine($"Error applying migration {resourceName}: {ex.Message}");
                        throw;
                    }
                }
            }
            Console.WriteLine("All migrations processed.");
        }

        public static void RunMigration(string connectionString, string migrationName)
        {
            ArgumentNullException.ThrowIfNull(connectionString);
            ArgumentNullException.ThrowIfNull(migrationName);
            using NpgsqlConnection connection = new(connectionString);
            connection.Open();
            EnsureMigrationsTableExists(connection);
            Assembly assembly = Assembly.GetExecutingAssembly();
            string? resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(r => r.Contains("Migrations") && r.EndsWith($"{migrationName}.pgsql"));
            if (resourceName == null)
            {
                Console.WriteLine($"Migration resource not found for: {migrationName}");
                return;
            }
            using NpgsqlTransaction transaction = connection.BeginTransaction();
            try
            {
                string checkSql = "SELECT COUNT(*) FROM grsdb.\"__migrations\" WHERE \"MigrationName\" = @MigrationName";
                using NpgsqlCommand checkCommand = new(checkSql, connection, transaction);
                _ = checkCommand.Parameters.AddWithValue("MigrationName", migrationName);
                long count = (long)(checkCommand.ExecuteScalar() ?? 0);
                if (count > 0)
                {
                    Console.WriteLine($"Migration {migrationName} already applied. Skipping.");
                    transaction.Commit();
                    return;
                }
                Console.WriteLine($"Running migration: {resourceName}");
                using Stream? stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    Console.WriteLine($"Could not find resource stream: {resourceName}");
                    transaction.Commit();
                    return;
                }
                using StreamReader reader = new(stream);
                string sql = reader.ReadToEnd();
                using NpgsqlCommand command = new(sql, connection, transaction);
                _ = command.ExecuteNonQuery();
                using NpgsqlCommand insertCommand = new("INSERT INTO grsdb.\"__migrations\" (\"MigrationName\") VALUES (@MigrationName)", connection, transaction);
                _ = insertCommand.Parameters.AddWithValue("MigrationName", migrationName);
                _ = insertCommand.ExecuteNonQuery();
                transaction.Commit();
                Console.WriteLine($"Migration {resourceName} applied successfully.");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"Error applying migration {resourceName}: {ex.Message}");
                throw;
            }
        }

        public static void RunRevert(string connectionString, string migrationName)
        {
            ArgumentNullException.ThrowIfNull(connectionString);
            ArgumentNullException.ThrowIfNull(migrationName);
            using NpgsqlConnection connection = new(connectionString);
            connection.Open();
            EnsureMigrationsTableExists(connection);
            Assembly assembly = Assembly.GetExecutingAssembly();
            string? resourceName = assembly.GetManifestResourceNames()
                .FirstOrDefault(r => r.Contains("Reverts") && r.EndsWith($"{migrationName}.pgsql"));
            if (resourceName == null)
            {
                Console.WriteLine($"Revert resource not found for: {migrationName}");
                return;
            }
            using NpgsqlTransaction transaction = connection.BeginTransaction();
            try
            {
                Console.WriteLine($"Running revert migration: {resourceName}");
                using Stream? stream = assembly.GetManifestResourceStream(resourceName);
                if (stream == null)
                {
                    Console.WriteLine($"Could not find resource stream: {resourceName}");
                    transaction.Commit();
                    return;
                }
                using StreamReader reader = new(stream);
                string sql = reader.ReadToEnd();
                using NpgsqlCommand command = new(sql, connection, transaction);
                _ = command.ExecuteNonQuery();
                using NpgsqlCommand deleteCommand = new("DELETE FROM grsdb.\"__migrations\" WHERE \"MigrationName\" = @MigrationName", connection, transaction);
                _ = deleteCommand.Parameters.AddWithValue("MigrationName", migrationName);
                _ = deleteCommand.ExecuteNonQuery();
                transaction.Commit();
                Console.WriteLine($"Revert migration {resourceName} applied successfully.");
            }
            catch (Exception ex)
            {
                transaction.Rollback();
                Console.WriteLine($"Error applying revert migration {resourceName}: {ex.Message}");
                throw;
            }
        }

        public static void RunReverts(string connectionString)
        {
            ArgumentNullException.ThrowIfNull(connectionString);
            Console.WriteLine("Starting revert migrations...");
            using NpgsqlConnection connection = new(connectionString);
            connection.Open();
            EnsureMigrationsTableExists(connection);
            Assembly assembly = Assembly.GetExecutingAssembly();
            string[] resourceNames = assembly.GetManifestResourceNames();
            foreach (string resourceName in resourceNames)
            {
                if (resourceName.Contains("Reverts") && resourceName.EndsWith(".pgsql"))
                {
                    string migrationName = Path.GetFileNameWithoutExtension(resourceName).Split('.').Last();
                    using NpgsqlTransaction transaction = connection.BeginTransaction();
                    try
                    {
                        Console.WriteLine($"Running revert migration: {resourceName}");
                        using Stream? stream = assembly.GetManifestResourceStream(resourceName);
                        if (stream == null)
                        {
                            Console.WriteLine($"Could not find resource: {resourceName}");
                            transaction.Commit();
                            continue;
                        }
                        using StreamReader reader = new(stream);
                        string sql = reader.ReadToEnd();
                        using NpgsqlCommand command = new(sql, connection, transaction);
                        _ = command.ExecuteNonQuery();
                        using NpgsqlCommand deleteCommand = new("DELETE FROM grsdb.\"__migrations\" WHERE \"MigrationName\" = @MigrationName", connection, transaction);
                        _ = deleteCommand.Parameters.AddWithValue("MigrationName", migrationName);
                        _ = deleteCommand.ExecuteNonQuery();
                        transaction.Commit();
                        Console.WriteLine($"Revert migration {resourceName} applied successfully.");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        Console.WriteLine($"Error applying revert migration {resourceName}: {ex.Message}");
                        throw;
                    }
                }
            }
            Console.WriteLine("All revert migrations processed.");
        }

        public static void SeedData(string connectionString)
        {
            ArgumentNullException.ThrowIfNull(connectionString);
            Console.WriteLine("Starting data seeding...");
            using NpgsqlConnection connection = new(connectionString);
            connection.Open();
            string checkAdminSql = "SELECT COUNT(*) FROM grsdb.\"ApplicationUser\" WHERE \"UserName\" = 'admin'";
            using NpgsqlCommand checkCommand = new(checkAdminSql, connection);
            long adminCount = (long)(checkCommand.ExecuteScalar() ?? 0);
            if (adminCount == 0)
            {
                (byte[] hash, byte[] salt) = PasswordHelper.HashPassword("admin123");
                string insertAdminSql = "" +
                    "INSERT INTO grsdb.\"ApplicationUser\" (\"UserName\", \"Email\", \"PasswordHash\", \"PasswordSalt\", \"IsAdmin\")" +
                    "VALUES (@UserName, @Email, @PasswordHash, @PasswordSalt, @IsAdmin)";
                using NpgsqlCommand insertCommand = new(insertAdminSql, connection);
                _ = insertCommand.Parameters.AddWithValue("UserName", "admin");
                _ = insertCommand.Parameters.AddWithValue("Email", "admin@example.com");
                _ = insertCommand.Parameters.AddWithValue("PasswordHash", hash);
                _ = insertCommand.Parameters.AddWithValue("PasswordSalt", salt);
                _ = insertCommand.Parameters.AddWithValue("IsAdmin", true);
                _ = insertCommand.ExecuteNonQuery();
                Console.WriteLine("Default admin user created.");
            }
            else
            {
                Console.WriteLine("Admin user already exists. Skipping creation.");
            }
            Console.WriteLine("Data seeding completed.");
        }
    }
}
