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
            CONSTRAINT PK_Station PRIMARY KEY (StationId)
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
            CONSTRAINT PK_Trip PRIMARY KEY (TripId),
            CONSTRAINT FK_Trip_Departure FOREIGN KEY(DepartureStationId) REFERENCES Station(StationId),
            CONSTRAINT FK_Trip_Arrival FOREIGN KEY(ArrivalStationId) REFERENCES Station(StationId),
            CONSTRAINT CK_Trip_Departure_Arrival CHECK (DepartureStationId <> ArrivalStationId), -- Ensure departure and arrival are different
            CONSTRAINT CK_Trip_Times CHECK (ArrivalTime > DepartureTime) -- Ensure arrival time is after departure time
        );
    END

    ---- Add trigger for manual cascading deletes on Trip when Station is deleted
    ---- NOTE: A trigger is used instead of ON DELETE CASCADE because SQL Server does not support cascading deletes
    ---- for multiple foreign key references to the same table (Trip.DepartureId and Trip.StationId both reference Station).
    IF OBJECT_ID(N'tr_DeleteTripsOnStationDelete', 'TR') IS NULL
    BEGIN
        EXEC('CREATE TRIGGER tr_DeleteTripsOnStationDelete ON Station
        AFTER DELETE
        AS
        BEGIN
            DELETE Trip
            FROM Trip
            INNER JOIN (SELECT StationId FROM deleted) AS d
                ON Trip.DepartureStationId = d.StationId
                OR Trip.ArrivalStationId = d.StationId;
        END');
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

---- Create ApplicationUser table
--IF OBJECT_ID(N'ApplicationUser', 'U') IS NULL
--BEGIN
--    CREATE TABLE ApplicationUser (
--        UserId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
--        UserName NVARCHAR(256) NOT NULL,
--        NormalizedUserName NVARCHAR(256) NOT NULL,
--        Email NVARCHAR(256) NULL,
--        NormalizedEmail NVARCHAR(256) NULL,
--        EmailConfirmed BIT NOT NULL DEFAULT(0),
--        PasswordHash VARBINARY(128) NOT NULL,             --Hash guardo en crudo
--        SecurityStamp UNIQUEIDENTIFIER NULL DEFAULT(NEWID())
--    );

--    CREATE UNIQUE INDEX IX_ApplicationUser_NormalizedEmail ON ApplicationUser(NormalizedEmail) 
--END

---- Create ApplicationRole table
--IF OBJECT_ID(N'ApplicationRole', 'U') IS NULL
--BEGIN
--    CREATE TABLE ApplicationRole (
--        RoleId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
--        Name NVARCHAR(256) NOT NULL,
--        NormalizedName NVARCHAR(256) NOT NULL
--    );

--    --Indice para la busqueda por nombre de rol normalizado
--    CREATE UNIQUE INDEX IX_ApplicationRole_NormalizedName ON ApplicationRole(NormalizedName) 
--END

---- Create UserRole table, representa relaciones entre pares Usuario, Rol
--IF OBJECT_ID(N'UserRole', 'U') IS NULL
--BEGIN
--    CREATE TABLE UserRole (
--        UserId UNIQUEIDENTIFIER NOT NULL,
--        RoleId UNIQUEIDENTIFIER NOT NULL,

--        CONSTRAINT FK_UserRole_User FOREIGN KEY(UserId)
--            REFERENCES ApplicationUser(UserId)
--            ON DELETE CASCADE,

--        CONSTRAINT FK_UserRole_Role FOREIGN KEY(RoleId)
--            REFERENCES ApplicationRole(RoleId)
--            ON DELETE CASCADE,

--        CONSTRAINT PK_UserRole PRIMARY KEY(UserId, RoleId)
--    );
--END

---- Create UserSession table
--IF OBJECT_ID(N'UserSession', 'U') IS NULL
--BEGIN
--    CREATE TABLE UserSession (
--        SessionId UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
--        UserId UNIQUEIDENTIFIER NOT NULL,
--        CreatedAt DATETIMEOFFSET NOT NULL,
--        ExpiresAt DATETIMEOFFSET,
--        SessionInfo NVARCHAR(1024),

--        CONSTRAINT FK_UserSession_User FOREIGN KEY(UserId)
--            REFERENCES ApplicationUser(UserId)
--            ON DELETE CASCADE
--    );
--END

---- Create Driver table
--IF OBJECT_ID(N'Driver', 'U') IS NULL
--BEGIN
--    CREATE TABLE Driver (
--        DriverId INT IDENTITY(1,1) NOT NULL,
--        IdentificationNumber INT NOT NULL,
--        FirstName NVARCHAR(50) NOT NULL,
--        LastName NVARCHAR(50) NOT NULL,
--        LicenseNumber NVARCHAR(20) NOT NULL,
--        LicenseExpiryDate DATE NOT NULL,
--        CONSTRAINT PK_Driver PRIMARY KEY (DriverId),
--        CONSTRAINT UQ_Driver_IdentificationNumber UNIQUE (IdentificationNumber),
--        CONSTRAINT UQ_Driver_LicenseNumber UNIQUE (LicenseNumber)
--    );
--END

---- Create VehicleModel table
--IF OBJECT_ID(N'VehicleModel', 'U') IS NULL
--BEGIN
--    CREATE TABLE VehicleModel (
--        VehicleModelId INT IDENTITY(1,1) NOT NULL,
--        Name NVARCHAR(50) NOT NULL,
--        Manufacturer NVARCHAR(50) NOT NULL,
--        CONSTRAINT PK_VehicleModel PRIMARY KEY (VehicleModelId)
--    );
--END

---- Create Vehicle table
--IF OBJECT_ID(N'Vehicle', 'U') IS NULL
--BEGIN
--    CREATE TABLE Vehicle (
--        VehicleId INT IDENTITY(1,1) NOT NULL,
--        VehicleModelId INT NOT NULL,
--        LicensePlate NVARCHAR(10) NOT NULL,
--        Status NVARCHAR(20) NOT NULL,
--        CONSTRAINT PK_Vehicle PRIMARY KEY (VehicleId),
--        CONSTRAINT FK_Vehicle_Model FOREIGN KEY(VehicleModelId) REFERENCES VehicleModel(VehicleModelId) ON DELETE CASCADE,
--        CONSTRAINT UQ_Vehicle_LicensePlate UNIQUE (LicensePlate)
--    );
--END

---- Create Station table
--IF OBJECT_ID(N'Station', 'U') IS NULL
--BEGIN
--    CREATE TABLE Station (
--        StationId INT IDENTITY(1,1) NOT NULL,
--        Name NVARCHAR(100) NOT NULL,
--        -- NormalizedName NVARCHAR(100) NOT NULL,
--        NormalizedName AS UPPER(LTRIM(RTRIM(Name))) PERSISTED,
--        Code NVARCHAR(10) NOT NULL,
--        -- NormalizedCode NVARCHAR(10) NOT NULL,
--        NormalizedCode AS UPPER(LTRIM(RTRIM(Code))) PERSISTED,
--        City NVARCHAR(50) NOT NULL,
--        -- NormalizedCity NVARCHAR(50) NOT NULL,
--        NormalizedCity AS UPPER(LTRIM(RTRIM(City))) PERSISTED,
--        Region NVARCHAR(50) NOT NULL,
--        -- NormalizedRegion NVARCHAR(50) NOT NULL,
--        NormalizedRegion AS UPPER(LTRIM(RTRIM(Region))) PERSISTED,
--        Country NVARCHAR(50) NOT NULL,
--        -- NormalizedCountry NVARCHAR(50) NOT NULL,
--        NormalizedCountry AS UPPER(LTRIM(RTRIM(Country))) PERSISTED,
--        TimeZone NVARCHAR(50) NOT NULL,
--        CONSTRAINT PK_Station PRIMARY KEY (StationId),
--        CONSTRAINT UQ_Station_Code UNIQUE (Code)
--    );
--END

---- Create Trip table
--IF OBJECT_ID(N'Trip', 'U') IS NULL
--BEGIN
--    CREATE TABLE Trip (
--        TripId INT IDENTITY(1,1) NOT NULL,
--        VehicleId INT NOT NULL,
--        DepartureId INT NOT NULL,
--        StationId INT NOT NULL,
--        DriverId INT NOT NULL,
--        DepartureTime DATETIME NOT NULL,
--        ArrivalTime DATETIME NOT NULL,
--        CONSTRAINT PK_Trip PRIMARY KEY (TripId),
--        CONSTRAINT FK_Trip_Vehicle FOREIGN KEY(VehicleId) REFERENCES Vehicle(VehicleId) ON DELETE CASCADE,
--        CONSTRAINT FK_Trip_Departure FOREIGN KEY(DepartureId) REFERENCES Station(StationId),
--        CONSTRAINT FK_Trip_Station FOREIGN KEY(StationId) REFERENCES Station(StationId),
--        CONSTRAINT FK_Trip_Driver FOREIGN KEY(DriverId) REFERENCES Driver(DriverId) ON DELETE CASCADE,
--        CONSTRAINT CK_Trip_Departure_Station CHECK (DepartureId <> StationId)
--    );
--END

---- Add trigger for manual cascading deletes on Trip when Station is deleted
---- NOTE: A trigger is used instead of ON DELETE CASCADE because SQL Server does not support cascading deletes
---- for multiple foreign key references to the same table (Trip.DepartureId and Trip.StationId both reference Station).
--IF OBJECT_ID(N'tr_DeleteTripsOnStationDelete', 'TR') IS NULL
--BEGIN
--    EXEC('CREATE TRIGGER tr_DeleteTripsOnStationDelete ON Station
--    AFTER DELETE
--    AS
--    BEGIN
--        DELETE Trip
--        FROM Trip
--        INNER JOIN (SELECT StationId FROM deleted) AS d
--            ON Trip.DepartureId = d.StationId
--            OR Trip.StationId = d.StationId;
--    END');
--END

---- Create Seat table
--IF OBJECT_ID(N'Seat', 'U') IS NULL
--BEGIN
--    CREATE TABLE Seat (
--        SeatId INT IDENTITY(1,1) NOT NULL,
--        VehicleModelId INT NOT NULL,
--        SeatRow INT NOT NULL,
--        SeatColumn INT NOT NULL,
--        IsAtWindow BIT NOT NULL DEFAULT (0),
--        IsAtAisle BIT NOT NULL DEFAULT (0),
--        IsInFront BIT NOT NULL DEFAULT (0),
--        IsInBack BIT NOT NULL DEFAULT (0),
--        IsAccessible BIT NOT NULL DEFAULT (0),
--        CONSTRAINT PK_Seat PRIMARY KEY (SeatId),
--        CONSTRAINT FK_Seat_VehicleModel FOREIGN KEY(VehicleModelId) REFERENCES VehicleModel(VehicleModelId) ON DELETE CASCADE,
--        CONSTRAINT UQ_Seat_Position UNIQUE (VehicleModelId, SeatRow, SeatColumn)
--    );
--END

---- Create Reservation table
--IF OBJECT_ID(N'Reservation', 'U') IS NULL
--BEGIN
--    CREATE TABLE Reservation (
--        ReservationId INT IDENTITY(1,1) NOT NULL,
--        TripId INT NOT NULL,
--        SeatId INT NOT NULL,
--        UserId UNIQUEIDENTIFIER NOT NULL,
--        ReservedAt DATETIME NOT NULL DEFAULT (GETDATE()),
--        CONSTRAINT PK_Reservation PRIMARY KEY (ReservationId),
--        CONSTRAINT FK_Reservation_User FOREIGN KEY(UserId) REFERENCES ApplicationUser(UserId),
--        CONSTRAINT FK_Reservation_Trip FOREIGN KEY(TripId) REFERENCES Trip(TripId) ON DELETE CASCADE,
--        CONSTRAINT FK_Reservation_Seat FOREIGN KEY(SeatId) REFERENCES Seat(SeatId),
--        CONSTRAINT UQ_Reservation_Trip_Seat UNIQUE (TripId, SeatId)
--    );
--END

---- Add trigger for manual cascading deletes on Reservation when Seat is deleted
--IF OBJECT_ID(N'tr_DeleteReservationsOnSeatDelete', 'TR') IS NULL
--BEGIN
--    EXEC('CREATE TRIGGER tr_DeleteReservationsOnSeatDelete ON Seat
--    AFTER DELETE
--    AS
--    BEGIN
--        DELETE FROM Reservation WHERE SeatId IN (SELECT SeatId FROM deleted);
--    END');
--END
    
---- Create Station table
--IF OBJECT_ID(N'Station', 'U') IS NULL
--BEGIN
--    CREATE TABLE Station (
--        StationId INT IDENTITY(1,1) NOT NULL,
--        Name NVARCHAR(100) NOT NULL,
--        -- NormalizedName NVARCHAR(100) NOT NULL,
--        NormalizedName AS UPPER(LTRIM(RTRIM(Name))) PERSISTED,
--        Code NVARCHAR(10) NOT NULL,
--        -- NormalizedCode NVARCHAR(10) NOT NULL,
--        NormalizedCode AS UPPER(LTRIM(RTRIM(Code))) PERSISTED,
--        City NVARCHAR(50) NOT NULL,
--        -- NormalizedCity NVARCHAR(50) NOT NULL,
--        NormalizedCity AS UPPER(LTRIM(RTRIM(City))) PERSISTED,
--        Region NVARCHAR(50) NOT NULL,
--        -- NormalizedRegion NVARCHAR(50) NOT NULL,
--        NormalizedRegion AS UPPER(LTRIM(RTRIM(Region))) PERSISTED,
--        Country NVARCHAR(50) NOT NULL,
--        -- NormalizedCountry NVARCHAR(50) NOT NULL,
--        NormalizedCountry AS UPPER(LTRIM(RTRIM(Country))) PERSISTED,
--        TimeZone NVARCHAR(50) NOT NULL,
--        CONSTRAINT PK_Station PRIMARY KEY (StationId),
--        CONSTRAINT UQ_Station_Code UNIQUE (Code)
--    );
--END