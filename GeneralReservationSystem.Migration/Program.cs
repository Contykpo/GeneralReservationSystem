using GeneralReservationSystem.Infrastructure.Database;

ArgumentNullException.ThrowIfNull(Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection"));

var connectionString = Environment.GetEnvironmentVariable("ConnectionStrings__DefaultConnection")!;

MigrationsRunner.RunMigrations(connectionString);

MigrationsRunner.SeedData(connectionString);