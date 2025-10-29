using GeneralReservationSystem.Infrastructure.Database;

ArgumentNullException.ThrowIfNull(Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection"));

string connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")!;

MigrationsRunner.RunMigrations(connectionString);

MigrationsRunner.SeedData(connectionString);