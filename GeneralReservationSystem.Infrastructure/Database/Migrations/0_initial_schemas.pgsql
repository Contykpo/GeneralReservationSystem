-- Create "ApplicationUser" table
CREATE TABLE IF NOT EXISTS grsdb."ApplicationUser" (
    "UserId" SERIAL PRIMARY KEY,
    "UserName" VARCHAR(256) NOT NULL,
    "NormalizedUserName" VARCHAR(256) GENERATED ALWAYS AS (UPPER(TRIM("UserName"))) STORED,
    "Email" VARCHAR(256),
    "NormalizedEmail" VARCHAR(256) GENERATED ALWAYS AS (UPPER(TRIM("Email"))) STORED,
    "PasswordHash" BYTEA NOT NULL,
    "PasswordSalt" BYTEA NOT NULL,
    "IsAdmin" BOOLEAN NOT NULL DEFAULT FALSE,
    CONSTRAINT "UQ_ApplicationUser_NormalizedUserName" UNIQUE ("NormalizedUserName"),
    CONSTRAINT "UQ_ApplicationUser_NormalizedEmail" UNIQUE ("NormalizedEmail")
);

-- Create "Station" table
CREATE TABLE IF NOT EXISTS grsdb."Station" (
    "StationId" SERIAL PRIMARY KEY,
    "StationName" VARCHAR(100) NOT NULL,
    "NormalizedStationName" VARCHAR(100) GENERATED ALWAYS AS (UPPER(TRIM("StationName"))) STORED,
    "City" VARCHAR(50) NOT NULL,
    "NormalizedCity" VARCHAR(50) GENERATED ALWAYS AS (UPPER(TRIM("City"))) STORED,
    "Province" VARCHAR(50) NOT NULL,
    "NormalizedProvince" VARCHAR(50) GENERATED ALWAYS AS (UPPER(TRIM("Province"))) STORED,
    "Country" VARCHAR(50) NOT NULL,
    "NormalizedCountry" VARCHAR(50) GENERATED ALWAYS AS (UPPER(TRIM("Country"))) STORED,
    CONSTRAINT "UQ_Station" UNIQUE ("NormalizedStationName", "NormalizedCity", "NormalizedProvince", "NormalizedCountry")
);

-- Create "Trip" table
CREATE TABLE IF NOT EXISTS grsdb."Trip" (
    "TripId" SERIAL PRIMARY KEY,
    "DepartureStationId" INT NOT NULL REFERENCES grsdb."Station"("StationId"),
    "DepartureTime" TIMESTAMPTZ NOT NULL,
    "ArrivalStationId" INT NOT NULL REFERENCES grsdb."Station"("StationId"),
    "ArrivalTime" TIMESTAMPTZ NOT NULL,
    "AvailableSeats" INT NOT NULL CHECK ("AvailableSeats" > 0),
    CONSTRAINT "CK_Trip_Departure_Arrival" CHECK ("DepartureStationId" <> "ArrivalStationId"),
    CONSTRAINT "CK_Trip_Times" CHECK ("ArrivalTime" > "DepartureTime")
);

-- Create "Reservation" table
CREATE TABLE IF NOT EXISTS grsdb."Reservation" (
    "TripId" INT NOT NULL REFERENCES grsdb."Trip"("TripId") ON DELETE CASCADE,
    "UserId" INT NOT NULL REFERENCES grsdb."ApplicationUser"("UserId") ON DELETE CASCADE,
    "Seat" INT NOT NULL,
    PRIMARY KEY ("TripId", "Seat")
);

-- Trigger for manual cascading deletes on "Trip" when "Station" is deleted
CREATE OR REPLACE FUNCTION grsdb."delete_trips_on_station_delete"() RETURNS TRIGGER AS $$
BEGIN
    DELETE FROM grsdb."Trip" WHERE "DepartureStationId" = OLD."StationId" OR "ArrivalStationId" = OLD."StationId";
    RETURN OLD;
END;
$$ LANGUAGE plpgsql;

CREATE TRIGGER "tr_DeleteTripsOnStationDelete"
BEFORE DELETE ON grsdb."Station"
FOR EACH ROW EXECUTE FUNCTION grsdb."delete_trips_on_station_delete"();