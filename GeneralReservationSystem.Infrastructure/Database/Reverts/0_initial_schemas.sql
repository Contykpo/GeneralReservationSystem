BEGIN TRANSACTION;

-- Migration identifier (unique per script)
DECLARE @MigrationNameIdentifier NVARCHAR(256) = '0_initial_schemas';

-- Check if this migration was already applied
IF EXISTS (SELECT 1 FROM __migrations WHERE MigrationName = @MigrationNameIdentifier)
BEGIN
    SET QUOTED_IDENTIFIER ON;
    SET ANSI_NULLS ON;

    ---- Drop Reservation table
    IF OBJECT_ID(N'Reservation', 'U') IS NOT NULL
    BEGIN
        DROP TABLE Reservation;
    END

    ---- Drop Trip table
    IF OBJECT_ID(N'Trip', 'U') IS NOT NULL
    BEGIN
        DROP TABLE Trip;
    END

    ---- Drop Station table and trigger
    IF OBJECT_ID(N'tr_DeleteTripsOnStationDelete', 'TR') IS NOT NULL
    BEGIN
        DROP TRIGGER tr_DeleteTripsOnStationDelete;
    END
    IF OBJECT_ID(N'Station', 'U') IS NOT NULL
    BEGIN
        DROP TABLE Station;
    END

    ---- Drop ApplicationUser table
    IF OBJECT_ID(N'ApplicationUser', 'U') IS NOT NULL
    BEGIN
        DROP TABLE ApplicationUser;
    END

    -- Remove migration record
    DELETE FROM __migrations WHERE MigrationName = @MigrationNameIdentifier;
END

COMMIT TRANSACTION;
