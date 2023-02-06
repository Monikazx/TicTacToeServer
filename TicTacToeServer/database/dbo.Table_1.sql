﻿CREATE TABLE [dbo].[Game]
(
	[Id] INT IDENTITY(1,1) NOT NULL PRIMARY KEY, 
    [player1] INT FOREIGN KEY REFERENCES [dbo].[User](Id) NOT NULL,
    [player2] INT FOREIGN KEY REFERENCES [dbo].[User](Id) NOT NULL,
    [winner] INT NULL FOREIGN KEY REFERENCES [dbo].[User](Id)
)
