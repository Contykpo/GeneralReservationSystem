-- Drop "Reservation" table
DROP TABLE IF EXISTS grsdb."Reservation";

-- Drop "Trip" table
DROP TABLE IF EXISTS grsdb."Trip";

-- Drop trigger and "Station" table
DROP TRIGGER IF EXISTS "tr_DeleteTripsOnStationDelete" ON grsdb."Station";
DROP TABLE IF EXISTS grsdb."Station";

-- Drop "ApplicationUser" table
DROP TABLE IF EXISTS grsdb."ApplicationUser";
