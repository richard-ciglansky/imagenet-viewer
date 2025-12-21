CREATE TABLE [dbo].[Structure]
(
    [Id] INT NOT NULL PRIMARY KEY,
    [Name] NVARCHAR(512) NOT NULL,
    [Title] NVARCHAR(192) NOT NULL,
    [Size] INT,
    [ParentId] INT,
    [Level] INT
)