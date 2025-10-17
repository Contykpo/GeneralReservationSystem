Use AppDb;

BEGIN TRANSACTION;

-- Migration identifier (unique per script)
DECLARE @MigrationNameIdentifier NVARCHAR(256) = '1_reservation_schemas';

-- Check if this migration was already applied
IF NOT EXISTS (SELECT 1 FROM __migrations WHERE MigrationName = @MigrationNameIdentifier)
BEGIN
    SET QUOTED_IDENTIFIER ON;
    SET ANSI_NULLS ON;

    ---- Create ApplicationUser table
    IF OBJECT_ID(N'ApplicationUser', 'U') IS NULL
    BEGIN
        CREATE TABLE ApplicationUser (
            UserId INT IDENTITY(1,1) NOT NULL,
            UserName NVARCHAR(256) NOT NULL,
            --NormalizedUserName NVARCHAR(256) NOT NULL,
            NormalizedUserName AS UPPER(LTRIM(RTRIM(UserName))) PERSISTED,
            Email NVARCHAR(256) NULL,
            --NormalizedEmail NVARCHAR(256) NULL,
            NormalizedEmail AS UPPER(LTRIM(RTRIM(Email))) PERSISTED,
            -- EmailConfirmed BIT NOT NULL DEFAULT(0),
            PasswordHash VARBINARY(256) NOT NULL,
            PasswordSalt VARBINARY(32) NOT NULL,
            IsAdmin BIT NOT NULL DEFAULT(0),
            CONSTRAINT PK_ApplicationUser PRIMARY KEY (UserId),
            CONSTRAINT UQ_ApplicationUser_NormalizedUserName UNIQUE (NormalizedUserName),
            CONSTRAINT UQ_ApplicationUser_NormalizedEmail UNIQUE (NormalizedEmail)
        );
    END

    ---- Create Station table
    IF OBJECT_ID(N'Station', 'U') IS NULL
    BEGIN
        CREATE TABLE Station (
            StationId INT IDENTITY(1,1) NOT NULL,
            StationName NVARCHAR(100) NOT NULL,
            -- NormalizedStationName NVARCHAR(100) NOT NULL,
            NormalizedStationName AS UPPER(LTRIM(RTRIM(StationName))) PERSISTED,
            City NVARCHAR(50) NOT NULL,
            -- NormalizedCity NVARCHAR(50) NOT NULL,
            NormalizedCity AS UPPER(LTRIM(RTRIM(City))) PERSISTED,
            Region NVARCHAR(50) NOT NULL,
            -- NormalizedRegion NVARCHAR(50) NOT NULL,
            NormalizedRegion AS UPPER(LTRIM(RTRIM(Region))) PERSISTED,
            Country NVARCHAR(50) NOT NULL,
            -- NormalizedCountry NVARCHAR(50) NOT NULL,
            NormalizedCountry AS UPPER(LTRIM(RTRIM(Country))) PERSISTED,
            CONSTRAINT PK_Station PRIMARY KEY (StationId),
            CONSTRAINT UQ_Station UNIQUE (NormalizedStationName, NormalizedCity, NormalizedRegion, NormalizedCountry)
        );
    END

    ---- Create Trip table
    IF OBJECT_ID(N'Trip', 'U') IS NULL
    BEGIN
        CREATE TABLE Trip (
            TripId INT IDENTITY(1,1) NOT NULL,
            DepartureStationId INT NOT NULL,
            DepartureTime DATETIME NOT NULL,
            ArrivalStationId INT NOT NULL,
            ArrivalTime DATETIME NOT NULL,
            AvailableSeats INT NOT NULL,
            CONSTRAINT PK_Trip PRIMARY KEY (TripId),
            CONSTRAINT FK_Trip_Departure FOREIGN KEY(DepartureStationId) REFERENCES Station(StationId),
            CONSTRAINT FK_Trip_Arrival FOREIGN KEY(ArrivalStationId) REFERENCES Station(StationId),
            CONSTRAINT CK_Trip_Departure_Arrival CHECK (DepartureStationId <> ArrivalStationId), -- Ensure departure and arrival are different
            CONSTRAINT CK_Trip_Times CHECK (ArrivalTime > DepartureTime), -- Ensure arrival time is after departure time
            CONSTRAINT CK_Trip_AvailableSeats CHECK (AvailableSeats > 0) -- Ensure available seats is positive
        );
    END

    ---- Add trigger for manual cascading deletes on Trip when Station is deleted
    ---- NOTE: A trigger is used instead of ON DELETE CASCADE because SQL Server does not support cascading deletes
    ---- for multiple foreign key references to the same table (Trip.DepartureId and Trip.StationId both reference Station).
    IF OBJECT_ID(N'tr_DeleteTripsOnStationDelete', 'TR') IS NULL
    BEGIN
        EXEC('CREATE TRIGGER tr_DeleteTripsOnStationDelete
        ON Station
        INSTEAD OF DELETE
        AS
        BEGIN
            -- Delete trips referencing the deleted stations
            DELETE Trip
            FROM Trip
            INNER JOIN deleted d
                ON Trip.DepartureStationId = d.StationId
                OR Trip.ArrivalStationId = d.StationId;

            -- Now delete the stations themselves
            DELETE s
            FROM Station s
            INNER JOIN deleted d ON s.StationId = d.StationId;
        END;');
    END

    -- Create Reservation table
    IF OBJECT_ID(N'Reservation', 'U') IS NULL
    BEGIN
        CREATE TABLE Reservation (
            TripId INT NOT NULL,
            UserId INT NOT NULL,
            Seat INT NOT NULL,
            CONSTRAINT PK_Reservation PRIMARY KEY (TripId, UserId, Seat),
            CONSTRAINT FK_Reservation_User FOREIGN KEY(UserId) REFERENCES ApplicationUser(UserId) ON DELETE CASCADE,
            CONSTRAINT FK_Reservation_Trip FOREIGN KEY(TripId) REFERENCES Trip(TripId) ON DELETE CASCADE,
            CONSTRAINT UQ_Reservation_Trip_Seat UNIQUE (TripId, Seat) -- Ensure a seat can only be booked once per trip
        );
    END

    -- Record migration
    INSERT INTO __migrations (MigrationName) VALUES (@MigrationNameIdentifier);
END

COMMIT TRANSACTION;

-- Ensure AvailableSeats column and its constraint exist even if the migration was already applied previously
IF OBJECT_ID(N'Trip', 'U') IS NOT NULL
BEGIN
    IF COL_LENGTH('Trip', 'AvailableSeats') IS NULL
    BEGIN
        ALTER TABLE Trip ADD AvailableSeats INT NOT NULL CONSTRAINT DF_Trip_AvailableSeats DEFAULT(1);
        UPDATE Trip SET AvailableSeats = 1 WHERE AvailableSeats IS NULL;
    END
    IF NOT EXISTS (
        SELECT 1 FROM sys.check_constraints WHERE name = N'CK_Trip_AvailableSeats' AND parent_object_id = OBJECT_ID(N'Trip')
    )
    BEGIN
        ALTER TABLE Trip WITH CHECK ADD CONSTRAINT CK_Trip_AvailableSeats CHECK (AvailableSeats > 0);
    END
END