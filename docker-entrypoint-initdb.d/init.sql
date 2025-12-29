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