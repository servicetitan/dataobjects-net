﻿-- Dropping the database
USE master
GO
if exists (select * from sysdatabases where name='DO-Tests')
  drop database [DO-Tests]
GO

-- Creating the database
CREATE DATABASE [DO-Tests]
GO

USE [DO-Tests]
GO

ALTER DATABASE [DO-Tests]
SET ALLOW_SNAPSHOT_ISOLATION ON

ALTER DATABASE [DO-Tests]
SET READ_COMMITTED_SNAPSHOT ON
GO


-- Enabling full-text indexing there
IF (1 = FULLTEXTSERVICEPROPERTY('IsFullTextInstalled'))
begin
EXEC [dbo].[sp_fulltext_database] @action = 'enable'
end
GO

EXECUTE (N'CREATE FULLTEXT CATALOG [Default]
WITH ACCENT_SENSITIVITY = ON
AS DEFAULT
AUTHORIZATION [dbo]')
GO


CREATE SCHEMA Model1
GO
CREATE SCHEMA Model2
GO
CREATE SCHEMA Model3
GO
CREATE SCHEMA Model4
GO
CREATE SCHEMA Model5
GO
CREATE SCHEMA Model6
GO
CREATE SCHEMA Model7
GO
CREATE SCHEMA Model8
GO
CREATE SCHEMA Model9
GO
CREATE SCHEMA Model10
GO
CREATE SCHEMA Model11
GO
CREATE SCHEMA Model12
GO

EXEC sp_configure 'CONTAINED DATABASE AUTHENTICATION'
GO

CREATE LOGIN readonlydotest WITH PASSWORD = 'readonlydotest', CHECK_POLICY = OFF
CREATE USER readonlydotest FOR LOGIN readonlydotest;
GO

