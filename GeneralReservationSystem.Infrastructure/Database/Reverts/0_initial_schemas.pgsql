DROP SCHEMA IF EXISTS "public" CASCADE;
CREATE SCHEMA "public";

ALTER SCHEMA "public" OWNER TO "postgres";

-- Drop "Reservation" table
DROP TABLE IF EXISTS "Reservation";

-- Drop "Trip" table
DROP TABLE IF EXISTS "Trip";

-- Drop trigger and "Station" table
DROP TRIGGER IF EXISTS "tr_DeleteTripsOnStationDelete" ON "Station";
DROP TABLE IF EXISTS "Station";

-- Drop "ApplicationUser" table
DROP TABLE IF EXISTS "ApplicationUser";
