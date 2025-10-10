-- Create database
IF DB_ID(N'AppDb') IS NULL
BEGIN
    CREATE DATABASE AppDb;
END
GO

USE AppDb;
GO

-- Create admin login and user
IF SUSER_ID(N'admin_user') IS NULL
BEGIN
    CREATE LOGIN admin_user WITH PASSWORD = '$(AdminUserPassword)';
END
GO

IF DATABASE_PRINCIPAL_ID(N'admin_user') IS NULL
BEGIN
    CREATE USER admin_user FOR LOGIN admin_user;
    ALTER ROLE db_owner ADD MEMBER admin_user;
END
GO

-- Create app login and user
IF SUSER_ID(N'app_user') IS NULL
BEGIN
    CREATE LOGIN app_user WITH PASSWORD = '$(AppUserPassword)';
END
GO

IF DATABASE_PRINCIPAL_ID(N'app_user') IS NULL
BEGIN
    CREATE USER app_user FOR LOGIN app_user;
    ALTER ROLE db_datareader ADD MEMBER app_user;
    ALTER ROLE db_datawriter ADD MEMBER app_user;
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