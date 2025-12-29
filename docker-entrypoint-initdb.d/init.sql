IF DB_ID(N'ImageNet') IS NULL
BEGIN
    PRINT 'Creating database ImageNet';
    CREATE DATABASE [ImageNet];
END
GO

IF NOT EXISTS (SELECT * FROM sys.sql_logins WHERE name = N'ImageNetUser')
BEGIN
    PRINT 'Creating login ImageNetUser';
    CREATE LOGIN [ImageNetUser] WITH PASSWORD = N'ImageNet@123!', CHECK_POLICY = OFF;
END
GO

-- Grant server-level permissions to allow creating and dropping databases
USE [master];
GO
IF EXISTS (SELECT 1 FROM sys.server_principals WHERE name = N'ImageNetUser')
BEGIN
    PRINT 'Granting server-level permissions (CREATE ANY DATABASE, ALTER ANY DATABASE) to ImageNetUser';
    GRANT CREATE ANY DATABASE TO [ImageNetUser];
    GRANT ALTER ANY DATABASE TO [ImageNetUser];
END
ELSE
BEGIN
    PRINT 'Login ImageNetUser not found when granting server-level permissions';
END
GO

USE [ImageNet];
GO

IF NOT EXISTS (SELECT * FROM sys.database_principals WHERE name = N'ImageNetUser')
BEGIN
    PRINT 'Creating user ImageNetUser';
    CREATE USER [ImageNetUser] FOR LOGIN [ImageNetUser];
END
GO

EXEC sp_addrolemember N'db_owner', N'ImageNetUser';
GO