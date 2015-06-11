--drop table Roles
--drop table UserClaims
--drop table UserLogins
--drop table UserRoles
--drop table Users

CREATE TABLE [dbo].UserRoles (
    [Id]	 INT IDENTITY (1 ,1) NOT NULL,
    [UserId] int NOT NULL,
    [RoleId] INT NOT NULL, 
    CONSTRAINT [PK_UserRoles] PRIMARY KEY ([Id])
);
GO

CREATE INDEX [IX_UserRoles_UserId_RoleId] ON [dbo].[UserRoles] (UserId,RoleId)
go
CREATE INDEX [IX_UserRoles_RoleId_UserId] ON [dbo].[UserRoles] (RoleId,UserId)
