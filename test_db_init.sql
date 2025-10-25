-- Create database
IF DB_ID(N'TestGRSDb') IS NULL
BEGIN
    CREATE DATABASE TestGRSDb;
END
GO

USE TestGRSDb;
GO

-- Create admin login and user
IF SUSER_ID(N'grs_test_admin_user') IS NULL
BEGIN
    CREATE LOGIN grs_test_admin_user WITH PASSWORD = 'ChangeThisAdminPassword1234';
END
GO

IF DATABASE_PRINCIPAL_ID(N'grs_test_admin_user') IS NULL
BEGIN
    CREATE USER grs_test_admin_user FOR LOGIN grs_test_admin_user;
    ALTER ROLE db_owner ADD MEMBER grs_test_admin_user;
END
GO

-- Create app login and user
IF SUSER_ID(N'grs_test_app_user') IS NULL
BEGIN
    CREATE LOGIN grs_test_app_user WITH PASSWORD = 'ChangeThisUserPassword1234';
END
GO

IF DATABASE_PRINCIPAL_ID(N'grs_test_app_user') IS NULL
BEGIN
    CREATE USER grs_test_app_user FOR LOGIN grs_test_app_user;
    ALTER ROLE db_datareader ADD MEMBER grs_test_app_user;
    ALTER ROLE db_datawriter ADD MEMBER grs_test_app_user;
END
GO

-- Create migrations table
IF OBJECT_ID(N'__migrations', 'U') IS NULL
BEGIN
    CREATE TABLE __migrations (
        MigrationName NVARCHAR(256) NOT NULL PRIMARY KEY,
        AppliedAt DATETIME2 NOT NULL DEFAULT SYSDATETIME()
    );
END
GO