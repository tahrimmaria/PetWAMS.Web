/*
  Run this AFTER PetWAMSDB_ddl.sql and BEFORE seed_data.sql.
  Adds a few columns your pet-detail pages need that weren't in the original DDL.
*/

USE PetWAMSDB;
GO

-- "About [Pet]" paragraph shown on the pet detail page
ALTER TABLE Pets ADD Description NVARCHAR(500) NULL;
GO

-- Vaccinated / Not Vaccinated badge
ALTER TABLE HealthRecords ADD Vaccinated BIT DEFAULT 1;
GO

-- Shelter contact email + open hours, shown in the sidebar shelter card
ALTER TABLE Shelters ADD Email NVARCHAR(100) NULL;
ALTER TABLE Shelters ADD OpenHours NVARCHAR(100) NULL;
GO
