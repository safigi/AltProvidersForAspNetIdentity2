CREATE TABLE [dbo].UserClaims (
    [Id]         INT IDENTITY (1, 1) NOT NULL,
    [UserId]     int NOT NULL,
    [ClaimType]  NVARCHAR (MAX) NULL,
    [ClaimValue] NVARCHAR (MAX) NULL, 
    CONSTRAINT [PK_UserClaims] PRIMARY KEY ([Id])
);
GO

CREATE INDEX [IX_UserClaims_UserId] ON [dbo].[UserClaims] (UserId)
