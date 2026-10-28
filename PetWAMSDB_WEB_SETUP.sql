/*
        SQL PROJECT NAME : Pet Welfare & Adoption Management System
-------------------------------------------------------------------------------------
                Trainee Name : Tahrim Maria

INDEX:
-------------------------------------------------------------------------------------
PART_01 ------- CHECK DATABASE EXISTENCE & CREATE DATABASE WITH FILE DETAILS.
PART_02 ------- CREATE TABLES.
PART_03 ------- ALTER TABLE.
PART_04 ------- CREATE INDEX & SEQUENCE.
PART_05 ------- CREATE VIEW.
PART_06 ------- CREATE STORED PROCEDURE.
PART_07 ------- CREATE FUNCTION.
PART_08 ------- CREATE TRIGGER.
-------------------------------------------------------------------------------------
*/

USE master;
GO

IF DB_ID('PetWAMSDB') IS NOT NULL
BEGIN
    ALTER DATABASE PetWAMSDB SET SINGLE_USER WITH ROLLBACK IMMEDIATE;
    DROP DATABASE PetWAMSDB;
END
GO

CREATE DATABASE PetWAMSDB;
GO

/*=========================================================
                PART_02 : CREATE TABLES
=========================================================*/

USE PetWAMSDB;
GO

----------------------------------------------------------
-- Users
----------------------------------------------------------

CREATE TABLE Users
(
    UserID INT IDENTITY(1,1) PRIMARY KEY,
    FullName NVARCHAR(100) NOT NULL,
    Email NVARCHAR(100) UNIQUE NOT NULL,
    Phone VARCHAR(20) UNIQUE,
    Address NVARCHAR(200),
    UserType VARCHAR(20)
    CHECK(UserType IN('Adopter','Admin','Staff'))
);
GO

----------------------------------------------------------
-- Shelters
----------------------------------------------------------

CREATE TABLE Shelters
(
    ShelterID INT IDENTITY(1,1) PRIMARY KEY,
    ShelterName NVARCHAR(100) NOT NULL,
    Address NVARCHAR(200),
    ContactNumber VARCHAR(20)
);
GO

----------------------------------------------------------
-- Pet Categories
----------------------------------------------------------

CREATE TABLE PetCategories
(
    CategoryID INT IDENTITY(1,1) PRIMARY KEY,
    CategoryName NVARCHAR(50) UNIQUE NOT NULL
);
GO

----------------------------------------------------------
-- Pets
----------------------------------------------------------

CREATE TABLE Pets
(
    PetID INT IDENTITY(1,1) PRIMARY KEY,

    PetName NVARCHAR(50) NOT NULL,

    Breed NVARCHAR(50),

    Age INT
    CHECK(Age>=0),

    Gender VARCHAR(10)
    CHECK(Gender IN('Male','Female')),

    Color NVARCHAR(50),

    Status VARCHAR(20)
    DEFAULT 'Available'
    CHECK(Status IN('Available','Pending','Adopted')),

    ShelterID INT,

    CategoryID INT,

    CONSTRAINT FK_Pets_Shelters
    FOREIGN KEY(ShelterID)
    REFERENCES Shelters(ShelterID),

    CONSTRAINT FK_Pets_Category
    FOREIGN KEY(CategoryID)
    REFERENCES PetCategories(CategoryID)
);
GO

----------------------------------------------------------
-- Adoption Requests
----------------------------------------------------------

CREATE TABLE AdoptionRequests
(
    RequestID INT IDENTITY(1,1) PRIMARY KEY,

    UserID INT,

    PetID INT,

    RequestDate DATE DEFAULT GETDATE(),

    Status VARCHAR(20)
    DEFAULT 'Pending',

    FOREIGN KEY(UserID)
    REFERENCES Users(UserID),

    FOREIGN KEY(PetID)
    REFERENCES Pets(PetID)
);
GO

----------------------------------------------------------
-- Adoptions
----------------------------------------------------------

CREATE TABLE Adoptions
(
    AdoptionID INT IDENTITY(1,1) PRIMARY KEY,

    PetID INT,

    UserID INT,

    AdoptionDate DATE,

    FOREIGN KEY(PetID)
    REFERENCES Pets(PetID),

    FOREIGN KEY(UserID)
    REFERENCES Users(UserID)
);
GO

----------------------------------------------------------
-- Health Records
----------------------------------------------------------

CREATE TABLE HealthRecords
(
    HealthID INT IDENTITY(1,1) PRIMARY KEY,

    PetID INT,

    HealthStatus NVARCHAR(100),

    MedicalNotes NVARCHAR(300),

    CheckupDate DATE,

    FOREIGN KEY(PetID)
    REFERENCES Pets(PetID)
);
GO

----------------------------------------------------------
-- Found Pets
----------------------------------------------------------

CREATE TABLE FoundPets
(
    FoundID INT IDENTITY(1,1) PRIMARY KEY,

    PetID INT NOT NULL,

    FoundLocation NVARCHAR(200),

    FoundDate DATE,

    ReporterName NVARCHAR(100),

    ContactNumber VARCHAR(20),

    FOREIGN KEY(PetID)
    REFERENCES Pets(PetID)
);
GO

----------------------------------------------------------
-- Lost Pets
----------------------------------------------------------

CREATE TABLE LostPets
(
    LostID INT IDENTITY(1,1) PRIMARY KEY,

    PetID INT NOT NULL,

    LostLocation NVARCHAR(200),

    LostDate DATE,

    ContactNumber VARCHAR(20),

    Reward MONEY DEFAULT 0,

    FOREIGN KEY(PetID)
    REFERENCES Pets(PetID)
);
GO

----------------------------------------------------------
-- FollowUps
----------------------------------------------------------

CREATE TABLE FollowUps
(
    FollowUpID INT IDENTITY(1,1) PRIMARY KEY,

    AdoptionID INT NOT NULL,

    FollowUpDate DATE,

    HealthCondition NVARCHAR(200),

    Remarks NVARCHAR(300),

    FOREIGN KEY(AdoptionID)
    REFERENCES Adoptions(AdoptionID)
);
GO

/*=========================================================
                PART_03 : ALTER TABLE
=========================================================*/

----------------------------------------------------------
-- ALTER Pets Table
----------------------------------------------------------

ALTER TABLE Pets
ADD
    MicrochipNumber VARCHAR(30),
    ArrivalDate DATE DEFAULT GETDATE(),
    AIRecommendation NVARCHAR(250);
GO

----------------------------------------------------------
-- ALTER Users Table
----------------------------------------------------------

ALTER TABLE Users
ALTER COLUMN Address NVARCHAR(300);
GO


----------------------------------------------------------
-- MVC WEBSITE REQUIRED COLUMNS
----------------------------------------------------------
ALTER TABLE Pets
ADD Description NVARCHAR(500) NULL;
GO

ALTER TABLE HealthRecords
ADD Vaccinated BIT NOT NULL CONSTRAINT DF_HealthRecords_Vaccinated DEFAULT(1);
GO

ALTER TABLE Shelters
ADD Email NVARCHAR(100) NULL,
    OpenHours NVARCHAR(100) NULL;
GO

----------------------------------------------------------
-- INDEX
----------------------------------------------------------

CREATE NONCLUSTERED INDEX IX_Users_Email
ON Users(Email);
GO

CREATE NONCLUSTERED INDEX IX_Pets_Breed
ON Pets(Breed);
GO

----------------------------------------------------------
-- SEQUENCE
----------------------------------------------------------

CREATE SEQUENCE Seq_AdoptionNumber
AS INT
START WITH 1
INCREMENT BY 1;
GO

/*=========================================================
                PART_04 : CREATE VIEW
=========================================================*/

----------------------------------------------------------
-- View : Available Pets
----------------------------------------------------------

CREATE VIEW vwAvailablePets
AS
SELECT
    PetID,
    PetName,
    Breed,
    Age,
    Gender,
    Color,
    Status,
    ShelterID,
    CategoryID,
    MicrochipNumber,
    ArrivalDate,
    AIRecommendation
FROM Pets
WHERE Status='Available';
GO

----------------------------------------------------------
-- View : Adopted Pets
----------------------------------------------------------

CREATE VIEW vwAdoptedPets
AS
SELECT
    PetID,
    PetName,
    Breed,
    Age,
    Gender,
    Color,
    Status,
    ShelterID,
    CategoryID,
    MicrochipNumber,
    ArrivalDate,
    AIRecommendation
FROM Pets
WHERE Status='Adopted';
GO

----------------------------------------------------------
-- View : Adoption Details
----------------------------------------------------------

CREATE VIEW vwAdoptionDetails
AS
SELECT
    A.AdoptionID,
    U.FullName,
    P.PetName,
    A.AdoptionDate
FROM Adoptions A
JOIN Users U
ON A.UserID=U.UserID
JOIN Pets P
ON A.PetID=P.PetID;
GO

/*=========================================================
                PART_05 : CREATE STORED PROCEDURE
=========================================================*/

----------------------------------------------------------
-- Add New Pet
----------------------------------------------------------

CREATE PROCEDURE spAddPet
(
    @PetName NVARCHAR(50),
    @Breed NVARCHAR(50),
    @Age INT,
    @Gender VARCHAR(10),
    @Color NVARCHAR(50),
    @Status VARCHAR(20),
    @ShelterID INT,
    @CategoryID INT,
    @MicrochipNumber VARCHAR(30),
    @ArrivalDate DATE,
    @AIRecommendation NVARCHAR(250)
)
AS
BEGIN

INSERT INTO Pets
(
PetName,
Breed,
Age,
Gender,
Color,
Status,
ShelterID,
CategoryID,
MicrochipNumber,
ArrivalDate,
AIRecommendation
)

VALUES
(
@PetName,
@Breed,
@Age,
@Gender,
@Color,
@Status,
@ShelterID,
@CategoryID,
@MicrochipNumber,
@ArrivalDate,
@AIRecommendation
);

END
GO

----------------------------------------------------------
-- Show Available Pets
----------------------------------------------------------

CREATE PROCEDURE spAvailablePets
AS
BEGIN

SELECT *
FROM Pets
WHERE Status='Available';

END
GO

----------------------------------------------------------
-- Search Pet By Breed
----------------------------------------------------------

CREATE PROCEDURE spSearchBreed
@Breed NVARCHAR(50)

AS
BEGIN

SELECT *
FROM Pets
WHERE Breed=@Breed;

END
GO

----------------------------------------------------------
-- Adoption History
----------------------------------------------------------

CREATE PROCEDURE spAdoptionHistory
AS
BEGIN

SELECT

A.AdoptionID,

P.PetName,

U.FullName,

A.AdoptionDate

FROM Adoptions A

INNER JOIN Pets P
ON A.PetID=P.PetID

INNER JOIN Users U
ON A.UserID=U.UserID;

END
GO

----------------------------------------------------------
-- Health Records
----------------------------------------------------------

CREATE PROCEDURE spHealthRecords
AS
BEGIN

SELECT

P.PetName,

H.HealthStatus,

H.CheckupDate

FROM HealthRecords H

INNER JOIN Pets P

ON H.PetID=P.PetID;

END
GO

/*=========================================================
                PART_06 : CREATE FUNCTION
=========================================================*/

----------------------------------------------------------
-- Total Pets
----------------------------------------------------------

CREATE FUNCTION fn_TotalPets()
RETURNS INT
AS
BEGIN
    DECLARE @Total INT

    SELECT @Total = COUNT(*)
    FROM Pets

    RETURN @Total
END
GO

----------------------------------------------------------
-- Available Pets
----------------------------------------------------------

CREATE FUNCTION fn_AvailablePets()
RETURNS INT
AS
BEGIN
    DECLARE @Total INT

    SELECT @Total = COUNT(*)
    FROM Pets
    WHERE Status='Available'

    RETURN @Total
END
GO

----------------------------------------------------------
-- Shelter Wise Pet Count
----------------------------------------------------------

CREATE FUNCTION fn_ShelterPetCount
(
    @ShelterID INT
)
RETURNS INT
AS
BEGIN
    DECLARE @Total INT

    SELECT @Total = COUNT(*)
    FROM Pets
    WHERE ShelterID=@ShelterID

    RETURN @Total
END
GO

----------------------------------------------------------
-- Pet Status
----------------------------------------------------------

CREATE FUNCTION fn_PetStatus
(
    @PetID INT
)
RETURNS VARCHAR(20)
AS
BEGIN
    DECLARE @Status VARCHAR(20)

    SELECT @Status = Status
    FROM Pets
    WHERE PetID=@PetID

    RETURN @Status
END
GO

----------------------------------------------------------
-- Pet Age
----------------------------------------------------------

CREATE FUNCTION fn_PetAge
(
    @PetID INT
)
RETURNS INT
AS
BEGIN
    DECLARE @Age INT

    SELECT @Age = Age
    FROM Pets
    WHERE PetID=@PetID

    RETURN @Age
END
GO

/*=========================================================
                PART_07 : CREATE TRIGGER
=========================================================*/

----------------------------------------------------------
-- Pet Log Table
----------------------------------------------------------

CREATE TABLE PetLog
(
    LogID INT IDENTITY(1,1) PRIMARY KEY,
    PetID INT,
    ActionType NVARCHAR(50),
    ActionDate DATETIME DEFAULT GETDATE()
);
GO

----------------------------------------------------------
-- Trigger : New Pet Insert
----------------------------------------------------------

CREATE TRIGGER trg_PetInsert
ON Pets
AFTER INSERT
AS
BEGIN

    INSERT INTO PetLog
    (
        PetID,
        ActionType
    )

    SELECT
        PetID,
        'New Pet Added'

    FROM inserted;

END
GO

----------------------------------------------------------
-- Trigger : Pet Status Update
----------------------------------------------------------

CREATE TRIGGER trg_PetStatusUpdate
ON Pets
AFTER UPDATE
AS
BEGIN

    IF UPDATE(Status)

    BEGIN

        INSERT INTO PetLog
        (
            PetID,
            ActionType
        )

        SELECT
            PetID,
            'Status Updated'

        FROM inserted;

    END

END
GO

----------------------------------------------------------
-- Trigger : Prevent Delete
----------------------------------------------------------

CREATE TRIGGER trg_PreventDelete
ON Pets
INSTEAD OF DELETE
AS
BEGIN

    PRINT 'Pet Delete is Not Allowed';

END
GO

----------------------------------------------------------
-- Trigger : Arrival Date
----------------------------------------------------------

CREATE TRIGGER trg_ArrivalDate
ON Pets
AFTER INSERT
AS
BEGIN

    UPDATE Pets

    SET ArrivalDate = GETDATE()

    WHERE ArrivalDate IS NULL;

END
GO

/*=========================================================
        MVC WEBSITE COMPATIBILITY
=========================================================*/
-- The original SQL assignment demonstrates an INSTEAD OF DELETE trigger.
-- The MVC Shelter/Pets/Delete action needs normal DELETE behavior.
IF OBJECT_ID('trg_PreventDelete', 'TR') IS NOT NULL
    DROP TRIGGER trg_PreventDelete;
GO


/*
            SQL PROJECT NAME : Pet Welfare and Adoption Management System (PetWAMSDB)
-------------------------------------------------------------------------------------
                Trainee Name : Tahrim Maria

                 Module 02 : SQL Server

INDEX:
----------------------------------------------------------------------------------------
       PART_01--------INSERT DATA INTO TABLES.
       PART_02--------USING an INDEX and SEQUENCE.
       PART_03--------VIEW.
       PART_04--------STORED PROCEDURE.
       PART_05--------FUNCTION.
       PART_06--------TRIGGER.
       PART_07--------QUERY.
       PART_08--------SUB-QUERY
       PART_09--------BUILT IN FUNCTION.
       PART_10--------TRANSACTION.
       PART_11--------TEMPORARY TABLE.
       PART_12--------DECLARE VARIABLE.
----------------------------------------------------------------------------------------
*/


/*=========================================================
                PART_1 : INSERT DATA
=========================================================*/

USE PetWAMSDB
GO

----------------------------------------------------------
-- INSERT USERS
----------------------------------------------------------

INSERT INTO Users
(FullName,Email,Phone,Address,UserType)
VALUES
('Rahim Uddin','rahim@gmail.com','01711111111','Dhaka','Adopter'),
('Karim Hasan','karim@gmail.com','01712222222','Chattogram','Adopter'),
('Nusrat Jahan','nusrat@gmail.com','01713333333','Khulna','Adopter'),
('Sabbir Ahmed','sabbir@gmail.com','01714444444','Rajshahi','Adopter'),
('Farzana Akter','farzana@gmail.com','01715555555','Sylhet','Adopter'),
('Tamim Hossain','tamim@gmail.com','01716666666','Barishal','Staff'),
('Mim Akter','mim@gmail.com','01717777777','Rangpur','Staff'),
('Admin User','admin@gmail.com','01718888888','Dhaka','Admin'),
('Shakib Hasan','shakib@gmail.com','01719999999','Cumilla','Adopter'),
('Tania Sultana','tania@gmail.com','01610000001','Mymensingh','Adopter'),
('Rifat Islam','rifat@gmail.com','01610000002','Gazipur','Adopter'),
('Jannat Mim','jannat@gmail.com','01610000003','Narsingdi','Adopter'),
('Mahmud Hasan','mahmud@gmail.com','01610000004','Bogura','Staff'),
('Sadia Khan','sadia@gmail.com','01610000005','Jessore','Adopter'),
('Arif Hossain','arif@gmail.com','01610000006','Pabna','Adopter');
GO

----------------------------------------------------------
-- INSERT SHELTERS
----------------------------------------------------------

INSERT INTO Shelters
(ShelterName,Address,ContactNumber)
VALUES
('Happy Paws Shelter','Dhaka','01811111111'),
('Pet Care Home','Khulna','01812222222'),
('Safe Animal Center','Chattogram','01813333333'),
('Animal Rescue BD','Rajshahi','01814444444'),
('Hope Shelter','Sylhet','01815555555'),
('Smile Pet Home','Barishal','01816666666'),
('Love Animals Shelter','Rangpur','01817777777'),
('Green Paws Shelter','Cumilla','01818888888');
GO

----------------------------------------------------------
-- INSERT PET CATEGORIES
----------------------------------------------------------

INSERT INTO PetCategories
(CategoryName)
VALUES
('Cat'),
('Dog'),
('Bird'),
('Rabbit'),
('Fish'),
('Parrot'),
('Hamster'),
('Other');
GO

----------------------------------------------------------
-- INSERT PETS
----------------------------------------------------------

INSERT INTO Pets
(
PetName,
Breed,
Age,
Gender,
Color,
Status,
ShelterID,
CategoryID,
MicrochipNumber,
ArrivalDate,
AIRecommendation
)

VALUES

('Tom','Persian',2,'Male','White','Available',1,1,'MC1001','2026-07-01','Healthy'),
('Rocky','Labrador',3,'Male','Brown','Available',1,2,'MC1002','2026-07-02','Friendly'),
('Mimi','Local',1,'Female','Black','Available',2,1,'MC1003','2026-07-03','Needs Care'),
('Bella','Golden Retriever',2,'Female','Golden','Available',3,2,'MC1004','2026-07-04','Healthy'),
('Max','German Shepherd',4,'Male','Black & Brown','Available',4,2,'MC1005','2026-07-05','Good Guard Dog'),
('Lucy','Siamese',2,'Female','Cream','Available',2,1,'MC1006','2026-07-06','Healthy'),
('Charlie','Persian',1,'Male','Gray','Available',5,1,'MC1007','2026-07-07','Friendly'),
('Daisy','Beagle',3,'Female','Brown & White','Available',6,2,'MC1008','2026-07-08','Vaccination Due'),
('Buddy','Husky',2,'Male','White & Gray','Available',7,2,'MC1009','2026-07-09','Healthy'),
('Luna','Bengal',1,'Female','Golden Brown','Available',8,1,'MC1010','2026-07-10','Playful'),

('Coco','Parrot',2,'Female','Green','Available',3,6,'MC1011','2026-07-11','Healthy'),
('Kiwi','Parrot',1,'Male','Green','Available',4,6,'MC1012','2026-07-12','Healthy'),
('Snow','Rabbit',1,'Female','White','Available',5,4,'MC1013','2026-07-13','Healthy'),
('Bunny','Rabbit',2,'Male','Brown','Available',6,4,'MC1014','2026-07-14','Friendly'),
('Goldie','Gold Fish',1,'Female','Golden','Available',7,5,'MC1015','2026-07-15','Healthy'),

('Shadow','Local',4,'Male','Black','Available',2,1,'MC1016','2026-07-16','Needs Grooming'),
('Tiger','Persian',3,'Male','Orange','Available',1,1,'MC1017','2026-07-17','Healthy'),
('Oscar','Labrador',5,'Male','Cream','Available',3,2,'MC1018','2026-07-18','Senior Dog'),
('Rosie','Poodle',2,'Female','White','Available',4,2,'MC1019','2026-07-19','Healthy'),
('Jack','German Shepherd',3,'Male','Brown','Available',5,2,'MC1020','2026-07-20','Friendly'),

('Leo','Maine Coon',2,'Male','Gray','Available',6,1,'MC1021','2026-07-21','Healthy'),
('Milo','British Shorthair',1,'Male','Blue Gray','Available',7,1,'MC1022','2026-07-22','Healthy'),
('Nala','Persian',2,'Female','White','Available',8,1,'MC1023','2026-07-23','Needs Care'),
('Ruby','Golden Retriever',4,'Female','Golden','Available',1,2,'MC1024','2026-07-24','Healthy'),
('Bruno','Rottweiler',3,'Male','Black','Available',2,2,'MC1025','2026-07-25','Strong & Healthy'),

('Simba','Local',2,'Male','Orange','Available',3,1,'MC1026','2026-07-26','Healthy'),
('Molly','Rabbit',1,'Female','Gray','Available',4,4,'MC1027','2026-07-27','Healthy'),
('Rio','Parrot',3,'Male','Blue','Available',5,6,'MC1028','2026-07-28','Friendly'),
('Lucky','Hamster',1,'Male','Brown','Available',6,7,'MC1029','2026-07-29','Healthy'),
('Angel','Persian',2,'Female','White','Available',7,1,'MC1030','2026-07-30','Healthy');
GO


----------------------------------------------------------
-- INSERT ADOPTION REQUESTS
----------------------------------------------------------

INSERT INTO AdoptionRequests
(UserID,PetID,RequestDate,Status)

VALUES

(1,1,'2026-07-10','Approved'),
(2,2,'2026-07-11','Pending'),
(3,3,'2026-07-12','Rejected'),
(4,4,'2026-07-13','Approved'),
(5,5,'2026-07-14','Pending'),
(6,6,'2026-07-15','Approved'),
(7,7,'2026-07-16','Pending'),
(8,8,'2026-07-17','Approved'),
(9,9,'2026-07-18','Rejected'),
(10,10,'2026-07-19','Approved'),
(11,11,'2026-07-20','Pending'),
(12,12,'2026-07-21','Approved'),
(13,13,'2026-07-22','Pending'),
(14,14,'2026-07-23','Approved'),
(15,15,'2026-07-24','Rejected'),
(1,16,'2026-07-25','Approved'),
(2,17,'2026-07-26','Pending'),
(3,18,'2026-07-27','Approved'),
(4,19,'2026-07-28','Pending'),
(5,20,'2026-07-29','Approved');
GO

----------------------------------------------------------
-- INSERT ADOPTIONS
----------------------------------------------------------

INSERT INTO Adoptions
(PetID,UserID,AdoptionDate)

VALUES

(1,1,'2026-07-12'),
(4,4,'2026-07-15'),
(6,6,'2026-07-17'),
(8,8,'2026-07-19'),
(10,10,'2026-07-21'),
(12,12,'2026-07-23'),
(14,14,'2026-07-25'),
(16,1,'2026-07-27'),
(18,3,'2026-07-29'),
(20,5,'2026-07-30'),
(2,2,'2026-08-01'),
(5,5,'2026-08-02'),
(7,7,'2026-08-03'),
(9,9,'2026-08-04'),
(11,11,'2026-08-05');
GO

----------------------------------------------------------
-- INSERT HEALTH RECORDS
----------------------------------------------------------

INSERT INTO HealthRecords
(PetID,HealthStatus,MedicalNotes,CheckupDate)

VALUES

(1,'Healthy','Vaccinated','2026-07-12'),
(2,'Healthy','Routine Checkup','2026-07-13'),
(3,'Needs Care','Skin Infection','2026-07-14'),
(4,'Healthy','Vaccinated','2026-07-15'),
(5,'Healthy','General Checkup','2026-07-16'),
(6,'Healthy','Vaccinated','2026-07-17'),
(7,'Minor Injury','Leg Dressing Done','2026-07-18'),
(8,'Healthy','Routine Checkup','2026-07-19'),
(9,'Healthy','Vaccinated','2026-07-20'),
(10,'Needs Care','Eye Treatment','2026-07-21'),
(11,'Healthy','Routine Checkup','2026-07-22'),
(12,'Healthy','Vaccinated','2026-07-23'),
(13,'Healthy','General Checkup','2026-07-24'),
(14,'Minor Injury','Medicine Given','2026-07-25'),
(15,'Healthy','Routine Checkup','2026-07-26'),
(16,'Healthy','Vaccinated','2026-07-27'),
(17,'Needs Care','Weight Monitoring','2026-07-28'),
(18,'Healthy','Routine Checkup','2026-07-29'),
(19,'Healthy','Vaccinated','2026-07-30'),
(20,'Healthy','General Checkup','2026-07-31');
GO

----------------------------------------------------------
-- INSERT LOST PETS
----------------------------------------------------------

INSERT INTO LostPets
(PetID,LostLocation,LostDate,ContactNumber,Reward)

VALUES

(2,'Mirpur','2026-07-10','01755550001',1000),
(5,'Uttara','2026-07-11','01755550002',1500),
(7,'Dhanmondi','2026-07-12','01755550003',1200),
(9,'Mohammadpur','2026-07-13','01755550004',2000),
(11,'Banani','2026-07-14','01755550005',1800),
(13,'Badda','2026-07-15','01755550006',1000),
(15,'Farmgate','2026-07-16','01755550007',2500),
(17,'Gulshan','2026-07-17','01755550008',1500),
(19,'Jatrabari','2026-07-18','01755550009',1700),
(20,'Motijheel','2026-07-19','01755550010',2000);
GO

----------------------------------------------------------
-- INSERT FOUND PETS
----------------------------------------------------------

INSERT INTO FoundPets
(PetID,FoundLocation,FoundDate,ReporterName,ContactNumber)

VALUES

(2,'Uttara','2026-07-12','Sabbir Ahmed','01860000001'),
(5,'Mirpur','2026-07-13','Nusrat Jahan','01860000002'),
(7,'Banani','2026-07-14','Tamim Hossain','01860000003'),
(9,'Farmgate','2026-07-15','Rifat Islam','01860000004'),
(11,'Mohakhali','2026-07-16','Mim Akter','01860000005'),
(13,'Bashundhara','2026-07-17','Farzana Akter','01860000006'),
(15,'Rampura','2026-07-18','Arif Hossain','01860000007'),
(17,'Tejgaon','2026-07-19','Sadia Khan','01860000008'),
(19,'Shyamoli','2026-07-20','Mahmud Hasan','01860000009'),
(20,'Khilgaon','2026-07-21','Jannat Mim','01860000010');
GO

----------------------------------------------------------
-- INSERT FOLLOW UPS
----------------------------------------------------------

INSERT INTO FollowUps
(AdoptionID,FollowUpDate,HealthCondition,Remarks)

VALUES

(1,'2026-07-20','Healthy','Pet is adapting well'),
(2,'2026-07-22','Healthy','Very active'),
(3,'2026-07-24','Healthy','Owner satisfied'),
(4,'2026-07-26','Minor Allergy','Medication prescribed'),
(5,'2026-07-28','Healthy','Vaccination completed'),
(6,'2026-07-30','Healthy','No issues found'),
(7,'2026-08-01','Healthy','Doing well'),
(8,'2026-08-03','Healthy','Friendly behavior'),
(9,'2026-08-05','Needs Care','Diet plan suggested'),
(10,'2026-08-07','Healthy','Excellent condition'),
(11,'2026-08-09','Healthy','Routine follow-up'),
(12,'2026-08-11','Healthy','Weight normal'),
(13,'2026-08-13','Healthy','No health issues'),
(14,'2026-08-15','Minor Injury','Recovered successfully'),
(15,'2026-08-17','Healthy','Adoption successful');
GO


/*==========================================================
        MVC WEBSITE EXTRA DATA
==========================================================*/

-- Shelter email/open hours used by the MVC shelter card
UPDATE Shelters SET Email='happypaws@gmail.com',       OpenHours='9:00 AM - 6:00 PM' WHERE ShelterID=1;
UPDATE Shelters SET Email='petcarehome@gmail.com',      OpenHours='9:00 AM - 6:00 PM' WHERE ShelterID=2;
UPDATE Shelters SET Email='safeanimal@gmail.com',       OpenHours='10:00 AM - 7:00 PM' WHERE ShelterID=3;
UPDATE Shelters SET Email='animalrescuebd@gmail.com',   OpenHours='9:00 AM - 5:00 PM' WHERE ShelterID=4;
UPDATE Shelters SET Email='hopeshelter@gmail.com',      OpenHours='9:00 AM - 6:00 PM' WHERE ShelterID=5;
UPDATE Shelters SET Email='smilepethome@gmail.com',     OpenHours='10:00 AM - 6:00 PM' WHERE ShelterID=6;
UPDATE Shelters SET Email='loveanimals@gmail.com',      OpenHours='9:00 AM - 5:00 PM' WHERE ShelterID=7;
UPDATE Shelters SET Email='greenpaws@gmail.com',        OpenHours='9:00 AM - 6:00 PM' WHERE ShelterID=8;
GO

-- Description used on the pet details page
UPDATE Pets
SET Description = CONCAT(
    PetName,
    ' is a ',
    COALESCE(Breed, 'lovely'),
    ' pet looking for a caring forever home.'
)
WHERE Description IS NULL;
GO

-- Convert the existing medical note into the Vaccinated badge
UPDATE HealthRecords
SET Vaccinated = CASE
    WHEN MedicalNotes LIKE '%Vaccinated%' THEN 1
    ELSE 0
END;
GO

PRINT 'PetWAMS website seed data inserted successfully.';
GO
