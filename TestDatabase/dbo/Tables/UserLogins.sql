CREATE TABLE [dbo].UserLogins (
    [Id]			INT	IDENTITY (1, 1)  NOT NULL,
    [UserId]        int NOT NULL,
    [LoginProvider] NVARCHAR (128) NOT NULL,
    [ProviderKey]   NVARCHAR (128) NOT NULL, 
    CONSTRAINT [PK_UserLogins] PRIMARY KEY ([Id])
);
GO

CREATE INDEX [IX_UserLogins_UserId] ON [dbo].[UserLogins] (UserId)
