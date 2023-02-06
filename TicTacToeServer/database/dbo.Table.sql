CREATE TABLE [dbo].[User]
(
	[Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY, 
    [name] NCHAR(20) NOT NULL, 
    [password] NCHAR(20) NULL
)
