AltProvidersForAspNetIdentity2
==============================

Goal is provide alternate data access implementations (besides EF) for the ASP.NET Identity 2.0 framework.

This is a dapper implementation. Id-s converted from guid to int. 

for example, instead of this:

CREATE TABLE [dbo].[AspNetUsers](
	[Id] varchar(max) NOT NULL,


CREATE TABLE [dbo].[AspNetUsers](
	[Id] [int] IDENTITY(1,1) NOT NULL,

etc.

