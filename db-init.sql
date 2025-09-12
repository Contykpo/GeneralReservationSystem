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
    CREATE LOGIN admin_user WITH PASSWORD = 'Admin1234';
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
    CREATE LOGIN app_user WITH PASSWORD = 'Application1234';
END
GO

IF DATABASE_PRINCIPAL_ID(N'app_user') IS NULL
BEGIN
    CREATE USER app_user FOR LOGIN app_user;
    ALTER ROLE db_datareader ADD MEMBER app_user;
    ALTER ROLE db_datawriter ADD MEMBER app_user;
END
GO

-- Create ApplicationUser table
IF OBJECT_ID(N'ApplicationUser', 'U') IS NULL
BEGIN
    CREATE TABLE ApplicationUser (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        UserName NVARCHAR(256) NOT NULL,
        NormalizedUserName NVARCHAR(256) NOT NULL,
        Email NVARCHAR(256) NULL,
        NormalizedEmail NVARCHAR(256) NULL,
        EmailConfirmed BIT NOT NULL DEFAULT(0),
        PasswordHash VARBINARY(64) NOT NULL,             --Hash de 64 bytes guardo en crudo
        SecurityStamp UNIQUEIDENTIFIER NULL DEFAULT(NEWID())
    );

    CREATE UNIQUE INDEX IX_ApplicationUser_NormalizedEmail ON ApplicationUser(NormalizedEmail) 
END
GO

-- Create ApplicationRole table
IF OBJECT_ID(N'ApplicationRole', 'U') IS NULL
BEGIN
    CREATE TABLE ApplicationRole (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        Name NVARCHAR(256) NOT NULL,
        NormalizedName NVARCHAR(256) NOT NULL
    );

    --Indice para la busqueda por nombre de rol normalizado
    CREATE UNIQUE INDEX IX_ApplicationRole_NormalizedName ON ApplicationRole(NormalizedName) 
END
GO

-- Create UserRole table, representa relaciones entre pares Usuario, Rol
IF OBJECT_ID(N'UserRole', 'U') IS NULL
BEGIN
    CREATE TABLE UserRole (
        UserId UNIQUEIDENTIFIER NOT NULL,
        RoleId UNIQUEIDENTIFIER NOT NULL,

        CONSTRAINT FK_UserRole_User FOREIGN KEY(UserId)
            REFERENCES ApplicationUser(Id)
            ON DELETE CASCADE,

        CONSTRAINT FK_UserRole_Role FOREIGN KEY(RoleId)
            REFERENCES ApplicationRole(Id)
            ON DELETE CASCADE,

        CONSTRAINT PK_UserRole PRIMARY KEY(UserId, RoleId)
    );
END
GO

-- Create UserSession table
IF OBJECT_ID(N'UserSession', 'U') IS NULL
BEGIN
    CREATE TABLE UserSession (
        Id UNIQUEIDENTIFIER NOT NULL PRIMARY KEY,
        UserId UNIQUEIDENTIFIER NOT NULL,
        CreatedAt DATETIMEOFFSET NOT NULL,
        ExpiresAt DATETIMEOFFSET,
        SessionInfo NVARCHAR(1024),

        CONSTRAINT FK_UserSession_User FOREIGN KEY(UserId)
            REFERENCES ApplicationUser(Id)
            ON DELETE CASCADE
    );
END
GO