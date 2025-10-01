BEGIN TRANSACTION;

-- Migration identifier (unique per script)
DECLARE @MigrationNameIdentifier NVARCHAR(256) = '1_reservation_schemas';

-- Check if this migration was already applied
IF NOT EXISTS (SELECT 1 FROM __migrations WHERE MigrationName = @MigrationNameIdentifier)
BEGIN
    -- Create Driver table
    IF OBJECT_ID(N'Driver', 'U') IS NULL
    BEGIN
        CREATE TABLE Driver (
            DriverId INT IDENTITY(1,1) NOT NULL,
            IdentificationNumber INT NOT NULL,
            FirstName NVARCHAR(50) NOT NULL,
            LastName NVARCHAR(50) NOT NULL,
            LicenseNumber NVARCHAR(20) NOT NULL,
            LicenseExpiryDate DATE NOT NULL,
            CONSTRAINT PK_Driver PRIMARY KEY (DriverId),
            CONSTRAINT UQ_Driver_IdentificationNumber UNIQUE (IdentificationNumber),
            CONSTRAINT UQ_Driver_LicenseNumber UNIQUE (LicenseNumber)
        );
    END

    -- Create VehicleModel table
    IF OBJECT_ID(N'VehicleModel', 'U') IS NULL
    BEGIN
        CREATE TABLE VehicleModel (
            VehicleModelId INT IDENTITY(1,1) NOT NULL,
            Name NVARCHAR(50) NOT NULL,
            Manufacturer NVARCHAR(50) NOT NULL,
            CONSTRAINT PK_VehicleModel PRIMARY KEY (VehicleModelId)
        );
    END

    -- Create Vehicle table
    IF OBJECT_ID(N'Vehicle', 'U') IS NULL
    BEGIN
        CREATE TABLE Vehicle (
            VehicleId INT IDENTITY(1,1) NOT NULL,
            VehicleModelId INT NOT NULL,
            LicensePlate NVARCHAR(10) NOT NULL,
            Status NVARCHAR(20) NOT NULL,
            CONSTRAINT PK_Vehicle PRIMARY KEY (VehicleId),
            CONSTRAINT FK_Vehicle_Model FOREIGN KEY(VehicleModelId) REFERENCES VehicleModel(VehicleModelId) ON DELETE CASCADE,
            CONSTRAINT UQ_Vehicle_LicensePlate UNIQUE (LicensePlate)
        );
    END

    -- Create Destination table
    IF OBJECT_ID(N'Destination', 'U') IS NULL
    BEGIN
        CREATE TABLE Destination (
            DestinationId INT IDENTITY(1,1) NOT NULL,
            Name NVARCHAR(100) NOT NULL,
            NormalizedName NVARCHAR(100) NOT NULL,
            Code NVARCHAR(10) NOT NULL,
            NormalizedCode NVARCHAR(10) NOT NULL,
            City NVARCHAR(50) NOT NULL,
            NormalizedCity NVARCHAR(50) NOT NULL,
            Region NVARCHAR(50) NOT NULL,
            NormalizedRegion NVARCHAR(50) NOT NULL,
            Country NVARCHAR(50) NOT NULL,
            NormalizedCountry NVARCHAR(50) NOT NULL,
            TimeZone NVARCHAR(100) NOT NULL,
            CONSTRAINT PK_Destination PRIMARY KEY (DestinationId),
            CONSTRAINT UQ_Destination_Code UNIQUE (Code)
        );
    END

    -- Create Trip table
    IF OBJECT_ID(N'Trip', 'U') IS NULL
    BEGIN
        CREATE TABLE Trip (
            TripId INT IDENTITY(1,1) NOT NULL,
            VehicleId INT NOT NULL,
            DepartureId INT NOT NULL,
            DestinationId INT NOT NULL,
            DriverId INT NOT NULL,
            DepartureTime DATETIME NOT NULL,
            ArrivalTime DATETIME NOT NULL,
            CONSTRAINT PK_Trip PRIMARY KEY (TripId),
            CONSTRAINT FK_Trip_Vehicle FOREIGN KEY(VehicleId) REFERENCES Vehicle(VehicleId) ON DELETE CASCADE,
            CONSTRAINT FK_Trip_Departure FOREIGN KEY(DepartureId) REFERENCES Destination(DestinationId),
            CONSTRAINT FK_Trip_Destination FOREIGN KEY(DestinationId) REFERENCES Destination(DestinationId),
            CONSTRAINT FK_Trip_Driver FOREIGN KEY(DriverId) REFERENCES Driver(DriverId) ON DELETE CASCADE,
            CONSTRAINT CK_Trip_Departure_Destination CHECK (DepartureId <> DestinationId)
        );
    END

    -- Add trigger for manual cascading deletes on Trip when Destination is deleted
    -- NOTE: A trigger is used instead of ON DELETE CASCADE because SQL Server does not support cascading deletes
    -- for multiple foreign key references to the same table (Trip.DepartureId and Trip.DestinationId both reference Destination).
    IF OBJECT_ID(N'tr_DeleteTripsOnDestinationDelete', 'TR') IS NULL
    BEGIN
        EXEC('CREATE TRIGGER tr_DeleteTripsOnDestinationDelete ON Destination
        AFTER DELETE
        AS
        BEGIN
            DELETE Trip
            FROM Trip
            INNER JOIN (SELECT DestinationId FROM deleted) AS d
                ON Trip.DepartureId = d.DestinationId
                OR Trip.DestinationId = d.DestinationId;
        END');
    END

    -- Create Seat table
    IF OBJECT_ID(N'Seat', 'U') IS NULL
    BEGIN
        CREATE TABLE Seat (
            SeatId INT IDENTITY(1,1) NOT NULL,
            VehicleModelId INT NOT NULL,
            SeatRow INT NOT NULL,
            SeatColumn INT NOT NULL,
            IsAtWindow BIT NOT NULL DEFAULT (0),
            IsAtAisle BIT NOT NULL DEFAULT (0),
            IsInFront BIT NOT NULL DEFAULT (0),
            IsInBack BIT NOT NULL DEFAULT (0),
            IsAccessible BIT NOT NULL DEFAULT (0),
            CONSTRAINT PK_Seat PRIMARY KEY (SeatId),
            CONSTRAINT FK_Seat_VehicleModel FOREIGN KEY(VehicleModelId) REFERENCES VehicleModel(VehicleModelId) ON DELETE CASCADE,
            CONSTRAINT UQ_Seat_Position UNIQUE (VehicleModelId, SeatRow, SeatColumn)
        );
    END

    -- Create Reservation table
    IF OBJECT_ID(N'Reservation', 'U') IS NULL
    BEGIN
        CREATE TABLE Reservation (
            ReservationId INT IDENTITY(1,1) NOT NULL,
            TripId INT NOT NULL,
            SeatId INT NOT NULL,
            UserId UNIQUEIDENTIFIER NOT NULL,
            ReservedAt DATETIME NOT NULL DEFAULT (GETDATE()),
            CONSTRAINT PK_Reservation PRIMARY KEY (ReservationId),
            CONSTRAINT FK_Reservation_User FOREIGN KEY(UserId) REFERENCES ApplicationUser(UserId),
            CONSTRAINT FK_Reservation_Trip FOREIGN KEY(TripId) REFERENCES Trip(TripId) ON DELETE CASCADE,
            CONSTRAINT FK_Reservation_Seat FOREIGN KEY(SeatId) REFERENCES Seat(SeatId),
            CONSTRAINT UQ_Reservation_Trip_Seat UNIQUE (TripId, SeatId)
        );
    END

    -- Add trigger for manual cascading deletes on Reservation when Seat is deleted
    IF OBJECT_ID(N'tr_DeleteReservationsOnSeatDelete', 'TR') IS NULL
    BEGIN
        EXEC('CREATE TRIGGER tr_DeleteReservationsOnSeatDelete ON Seat
        AFTER DELETE
        AS
        BEGIN
            DELETE FROM Reservation WHERE SeatId IN (SELECT SeatId FROM deleted);
        END');
    END

    -- Create view for available seats
    IF OBJECT_ID(N'TripAvailableSeats', 'V') IS NULL
    BEGIN
        EXEC('CREATE VIEW TripAvailableSeats AS
        SELECT s.SeatId, t.TripId, s.SeatRow, s.SeatColumn, s.IsAtWindow, s.IsAtAisle, s.IsInFront, s.IsInBack, s.IsAccessible
        FROM Seat s
        JOIN VehicleModel vm ON s.VehicleModelId = vm.VehicleModelId
        JOIN Vehicle v ON vm.VehicleModelId = v.VehicleModelId
        JOIN Trip t ON v.VehicleId = t.VehicleId
        WHERE s.SeatId NOT IN (SELECT SeatId FROM Reservation WHERE TripId = t.TripId)');
    END

    -- Create TripDetailsView for trip search and details
    IF OBJECT_ID(N'TripDetailsView', 'V') IS NULL
    BEGIN
        EXEC('CREATE VIEW TripDetailsView AS
        WITH SeatCounts AS (
            SELECT
                t.TripId,
                COUNT(s.SeatId) AS TotalSeats,
                COUNT(s.SeatId) - COUNT(r.SeatId) AS AvailableSeats
            FROM Trip t
            JOIN Vehicle v ON t.VehicleId = v.VehicleId
            JOIN Seat s ON v.VehicleModelId = s.VehicleModelId
            LEFT JOIN Reservation r ON r.SeatId = s.SeatId AND r.TripId = t.TripId
            GROUP BY t.TripId
        )
        SELECT
            t.TripId,
            t.VehicleId,
            t.DepartureId,
            t.DestinationId,
            t.DriverId,
            t.DepartureTime,
            t.ArrivalTime,
            dep.Name AS DepartureName,
            dep.City AS DepartureCity,
            dep.Region AS DepartureRegion,
            dep.Country AS DepartureCountry,
            dep.TimeZone AS DepartureTimeZone,
            dest.Name AS DestinationName,
            dest.City AS DestinationCity,
            dest.Region AS DestinationRegion,
            dest.Country AS DestinationCountry,
            dest.TimeZone AS DestinationTimeZone,
            sc.TotalSeats,
            sc.AvailableSeats
        FROM Trip t
        JOIN Vehicle v ON t.VehicleId = v.VehicleId
        JOIN Destination dep ON t.DepartureId = dep.DestinationId
        JOIN Destination dest ON t.DestinationId = dest.DestinationId
        LEFT JOIN SeatCounts sc ON t.TripId = sc.TripId');
    END

    -- Record migration
    INSERT INTO __migrations (MigrationName) VALUES (@MigrationNameIdentifier);
END

COMMIT TRANSACTION;
