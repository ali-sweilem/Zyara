DELETE FROM Notifications;
DELETE FROM PrescriptionItems;
DELETE FROM Prescriptions;
DELETE FROM ServiceNotes;
DELETE FROM Reviews;
DELETE FROM Payments;
DELETE FROM Bookings;
DELETE FROM TimeSlots;
DELETE FROM ProviderServices;
DELETE FROM ProviderCredentials;
DELETE FROM Addresses;
DELETE FROM Users;
/* =====================================================================
   Zyara: Seed data (SQL Server)   v4, enums as names (columns become text automatically)

   WHAT IT INSERTS
     Users 20 (5 Patients 1-5, 5 Doctors 6-10, 5 Nurses 11-15, 5 Admins 16-20)
     Addresses 5, ProviderCredentials 5, Services 5 (3 MVP + 2 inactive),
     ProviderServices 10, PlatformSettings 1, TimeSlots 15, Bookings 10,
     Payments 10, Reviews 5, ServiceNotes 5, Prescriptions 5,
     PrescriptionItems 5, Notifications 5

   ASSUMPTIONS (the script stops with a message if the first one is wrong)
     1) Table names are the plural of the entity (Users, Addresses, ...).
     2) Users is ONE table (TPH). If your Users table has no Discriminator
        column, the script skips it automatically.
     3) Enum values are written as NAMES (text):
        Gender: Male, Female
        UserRole: Patient, Doctor, Nurse, Admin
        VerificationStatus: Pending, Verified, Rejected
        ServiceType: DoctorHomeVisit, NurseHomeVisit, RemoteConsultation
        PaymentMethod: CashOnArrival, OnlinePayment
        PaymentStatus: Pending, Authorized, Captured, Collected, Released,
                       Refunded, PartiallyRefunded
        BookingStatus: PendingProviderApproval, Confirmed, ConfirmedCollectionPending,
                       Started, Completed, Cancelled, Rejected, Expired
        TimeSlotStatus: Available, Locked, Booked
        If a column is still int, Step 1 changes it to NVARCHAR(50) first.
        EF must read them as text: HasConversion<string>() in the DbContext.

   HOW TO RUN: on an EMPTY database (Drop-Database, then Update-Database).
   It runs inside one transaction: if anything fails, nothing is inserted.
   ===================================================================== */

/* ---------- Check before running ---------- */
DECLARE @missing NVARCHAR(MAX) = (
    SELECT STRING_AGG(t.n, ', ')
    FROM (VALUES ('Users'), ('Addresses'), ('ProviderCredentials'), ('Services'),
                 ('ProviderServices'), ('PlatformSettings'), ('TimeSlots'), ('Bookings'),
                 ('Payments'), ('Reviews'), ('ServiceNotes'), ('Prescriptions'),
                 ('PrescriptionItems'), ('Notifications')) t(n)
    WHERE OBJECT_ID(t.n, 'U') IS NULL);

IF @missing IS NOT NULL
BEGIN
    PRINT 'STOPPED. These tables were not found: ' + @missing + '. Rename them in this script to match your database.';
    SET NOEXEC ON;
END
ELSE IF EXISTS (SELECT 1 FROM Users)
BEGIN
    PRINT 'STOPPED. Users already has rows. Run this script on an empty database.';
    SET NOEXEC ON;
END
GO

/* ---------- Step 1: make the enum columns text (NVARCHAR) ----------
   If a column is still int, it becomes NVARCHAR(50). If it is already text,
   nothing changes. Numbers already in the table are replaced by names. */
SET XACT_ABORT ON;
BEGIN TRANSACTION;

CREATE TABLE #Map (
    TableName  sysname,
    ColumnName sysname,
    Num  NVARCHAR(10) COLLATE DATABASE_DEFAULT,
    Name NVARCHAR(50) COLLATE DATABASE_DEFAULT
);

INSERT INTO #Map (TableName, ColumnName, Num, Name) VALUES
 -- Users.Gender
 ('Users', 'Gender', '1', 'Male'),
 ('Users', 'Gender', '2', 'Female'),
 -- Users.Role
 ('Users', 'Role', '1', 'Patient'),
 ('Users', 'Role', '2', 'Doctor'),
 ('Users', 'Role', '3', 'Nurse'),
 ('Users', 'Role', '4', 'Admin'),
 -- Users.VerificationStatus
 ('Users', 'VerificationStatus', '0', 'Pending'),
 ('Users', 'VerificationStatus', '1', 'Verified'),
 ('Users', 'VerificationStatus', '2', 'Rejected'),
 -- Services.Type
 ('Services', 'Type', '0', 'DoctorHomeVisit'),
 ('Services', 'Type', '1', 'NurseHomeVisit'),
 ('Services', 'Type', '2', 'RemoteConsultation'),
 -- TimeSlots.Status
 ('TimeSlots', 'Status', '0', 'Available'),
 ('TimeSlots', 'Status', '1', 'Locked'),
 ('TimeSlots', 'Status', '2', 'Booked'),
 -- Bookings.Status
 ('Bookings', 'Status', '0', 'PendingProviderApproval'),
 ('Bookings', 'Status', '1', 'Confirmed'),
 ('Bookings', 'Status', '2', 'ConfirmedCollectionPending'),
 ('Bookings', 'Status', '3', 'Started'),
 ('Bookings', 'Status', '4', 'Completed'),
 ('Bookings', 'Status', '5', 'Cancelled'),
 ('Bookings', 'Status', '6', 'Rejected'),
 ('Bookings', 'Status', '7', 'Expired'),
 -- Payments.Method
 ('Payments', 'Method', '0', 'CashOnArrival'),
 ('Payments', 'Method', '1', 'OnlinePayment'),
 -- Payments.Status
 ('Payments', 'Status', '0', 'Pending'),
 ('Payments', 'Status', '1', 'Authorized'),
 ('Payments', 'Status', '2', 'Captured'),
 ('Payments', 'Status', '3', 'Collected'),
 ('Payments', 'Status', '4', 'Released'),
 ('Payments', 'Status', '5', 'Refunded'),
 ('Payments', 'Status', '6', 'PartiallyRefunded');

DECLARE @t sysname, @c sysname, @type NVARCHAR(50), @nullable NVARCHAR(3),
        @dc sysname, @sql NVARCHAR(MAX);

DECLARE cols CURSOR LOCAL FAST_FORWARD FOR
    SELECT DISTINCT TableName, ColumnName FROM #Map;
OPEN cols;
FETCH NEXT FROM cols INTO @t, @c;

WHILE @@FETCH_STATUS = 0
BEGIN
    SET @type     = (SELECT DATA_TYPE   FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @t AND COLUMN_NAME = @c);
    SET @nullable = (SELECT IS_NULLABLE FROM INFORMATION_SCHEMA.COLUMNS WHERE TABLE_NAME = @t AND COLUMN_NAME = @c);

    IF @type IS NULL
    BEGIN
        PRINT 'SKIPPED: column not found: ' + @t + '.' + @c;
    END
    ELSE
    BEGIN
        -- 1) int -> nvarchar(50)
        IF @type IN ('int', 'smallint', 'tinyint')
        BEGIN
            SET @dc = (SELECT dc.name
                       FROM sys.default_constraints dc
                       JOIN sys.columns col ON col.object_id = dc.parent_object_id
                                           AND col.column_id = dc.parent_column_id
                       WHERE dc.parent_object_id = OBJECT_ID(@t) AND col.name = @c);
            IF @dc IS NOT NULL
            BEGIN
                SET @sql = N'ALTER TABLE ' + QUOTENAME(@t) + N' DROP CONSTRAINT ' + QUOTENAME(@dc);
                EXEC (@sql);
            END

            SET @sql = N'ALTER TABLE ' + QUOTENAME(@t) + N' ALTER COLUMN ' + QUOTENAME(@c) +
                       N' NVARCHAR(50) ' + CASE WHEN @nullable = 'YES' THEN N'NULL' ELSE N'NOT NULL' END;
            EXEC (@sql);
        END

        -- 2) numbers -> names
        SET @sql = N'UPDATE x SET ' + QUOTENAME(@c) + N' = m.Name FROM ' + QUOTENAME(@t) + N' x ' +
                   N'JOIN #Map m ON m.TableName = N''' + @t + N''' AND m.ColumnName = N''' + @c + N''' ' +
                   N'AND m.Num = x.' + QUOTENAME(@c) + N';';
        EXEC (@sql);

        PRINT 'Converted: ' + @t + '.' + @c;
    END

    FETCH NEXT FROM cols INTO @t, @c;
END

CLOSE cols;
DEALLOCATE cols;
DROP TABLE #Map;

COMMIT TRANSACTION;
GO

/* ---------- Step 2: insert the data (enums as names) ---------- */
SET XACT_ABORT ON;
BEGIN TRANSACTION;

/* ===== 1) Users (built in a temp table, then copied to Users) ===== */
CREATE TABLE #U (
    Id INT NOT NULL, FullName NVARCHAR(200), Email NVARCHAR(200), Phone NVARCHAR(50),
    PasswordHash NVARCHAR(200) NOT NULL DEFAULT 'HASH',
    Gender NVARCHAR(20), Role NVARCHAR(20),
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    PhotoUrl NVARCHAR(400) NULL,
    Discriminator NVARCHAR(50),
    DateOfBirth DATE NULL,
    Specialty NVARCHAR(200) NULL, ExperienceYears INT NULL, Bio NVARCHAR(MAX) NULL,
    VerificationStatus NVARCHAR(20) NULL, RejectionReason NVARCHAR(MAX) NULL,
    VerifiedAt DATETIME2 NULL, VerifiedByAdminId INT NULL,
    AverageRating FLOAT NULL, RatingCount INT NULL, WalletBalance DECIMAL(18,2) NULL,
    BaseCity NVARCHAR(100) NULL, BaseLatitude FLOAT NULL, BaseLongitude FLOAT NULL,
    licenser NVARCHAR(100) NULL
);

INSERT INTO #U (Id, FullName, Email, Phone, Gender, Role, Discriminator, DateOfBirth) VALUES
 -- Patients
 (1,  N'Ahmed Hassan',     'ahmed.hassan@zyara.test',   '01000000001', 'Male', 'Patient', 'Patient', '1990-03-14'),
 (2,  N'Mona Ali',         'mona.ali@zyara.test',       '01000000002', 'Female', 'Patient', 'Patient', '1985-07-22'),
 (3,  N'Karim Samir',      'karim.samir@zyara.test',    '01000000003', 'Male', 'Patient', 'Patient', '1978-11-05'),
 (4,  N'Sara Mahmoud',     'sara.mahmoud@zyara.test',   '01000000004', 'Female', 'Patient', 'Patient', '1995-01-30'),
 (5,  N'Youssef Nabil',    'youssef.nabil@zyara.test',  '01000000005', 'Male', 'Patient', 'Patient', '2001-09-12'),
 -- Doctors
 (6,  N'Dr. Hany Fathy',   'hany.fathy@zyara.test',     '01100000006', 'Male', 'Doctor', 'Doctor', NULL),
 (7,  N'Dr. Laila Adel',   'laila.adel@zyara.test',     '01100000007', 'Female', 'Doctor', 'Doctor', NULL),
 (8,  N'Dr. Tarek Zaki',   'tarek.zaki@zyara.test',     '01100000008', 'Male', 'Doctor', 'Doctor', NULL),
 (9,  N'Dr. Nadia Sherif', 'nadia.sherif@zyara.test',   '01100000009', 'Female', 'Doctor', 'Doctor', NULL),
 (10, N'Dr. Amr Gamal',    'amr.gamal@zyara.test',      '01100000010', 'Male', 'Doctor', 'Doctor', NULL),
 -- Nurses
 (11, N'Heba Salah',       'heba.salah@zyara.test',     '01200000011', 'Female', 'Nurse', 'Nurse', NULL),
 (12, N'Mahmoud Reda',     'mahmoud.reda@zyara.test',   '01200000012', 'Male', 'Nurse', 'Nurse', NULL),
 (13, N'Rania Kamel',      'rania.kamel@zyara.test',    '01200000013', 'Female', 'Nurse', 'Nurse', NULL),
 (14, N'Sherif Ayman',     'sherif.ayman@zyara.test',   '01200000014', 'Male', 'Nurse', 'Nurse', NULL),
 (15, N'Dina Fouad',       'dina.fouad@zyara.test',     '01200000015', 'Female', 'Nurse', 'Nurse', NULL),
 -- Admins
 (16, N'Admin Mohamed',    'admin1@zyara.test',         '01500000016', 'Male', 'Admin', 'Admin', NULL),
 (17, N'Admin Salma',      'admin2@zyara.test',         '01500000017', 'Female', 'Admin', 'Admin', NULL),
 (18, N'Admin Omar',       'admin3@zyara.test',         '01500000018', 'Male', 'Admin', 'Admin', NULL),
 (19, N'Admin Nour',       'admin4@zyara.test',         '01500000019', 'Female', 'Admin', 'Admin', NULL),
 (20, N'Admin Khaled',     'admin5@zyara.test',         '01500000020', 'Male', 'Admin', 'Admin', NULL);

-- Provider details. Ver is the VerificationStatus name.
-- Rating / RatingCount match the Reviews inserted below. Nurses have no reviews yet ("New").
UPDATE u SET
    Specialty = p.Specialty, ExperienceYears = p.Exp, VerificationStatus = p.Ver,
    AverageRating = p.Rating, RatingCount = p.RatingCount, WalletBalance = 0,
    BaseCity = p.City, BaseLatitude = p.Lat, BaseLongitude = p.Lng, licenser = p.Lic,
    Bio = N'Provider on Zyara',
    VerifiedAt = CASE WHEN p.Ver = N'Pending' THEN NULL ELSE SYSUTCDATETIME() END,
    VerifiedByAdminId = CASE WHEN p.Ver = N'Pending' THEN NULL ELSE 16 END,
    RejectionReason = CASE WHEN p.Ver = N'Rejected' THEN N'Documents are not clear' ELSE NULL END
FROM #U u
JOIN (VALUES
    (6,  N'Cardiology',        12, N'Verified', 5.0, 2, N'Cairo',      30.0444, 31.2357, 'EG-DOC-1001'),
    (7,  N'Pediatrics',         8, N'Verified', 4.0, 1, N'Giza',       30.0131, 31.2089, 'EG-DOC-1002'),
    (8,  N'Dermatology',        5, N'Verified', 5.0, 1, N'Cairo',      30.0626, 31.3469, 'EG-DOC-1003'),
    (9,  N'Internal Medicine', 15, N'Verified', 4.0, 1, N'Alexandria', 31.2001, 29.9187, 'EG-DOC-1004'),
    (10, N'Orthopedics',        3, N'Pending', 0.0, 0, N'Cairo',      30.0561, 31.3301, 'EG-DOC-1005'),
    (11, N'Home Nursing',       6, N'Verified', 0.0, 0, N'Cairo',      30.0500, 31.2400, NULL),
    (12, N'Wound Care',         4, N'Verified', 0.0, 0, N'Giza',       30.0100, 31.2000, NULL),
    (13, N'Elderly Care',       9, N'Verified', 0.0, 0, N'Alexandria', 31.2100, 29.9200, NULL),
    (14, N'Injections',         2, N'Pending', 0.0, 0, N'Cairo',      30.0600, 31.3300, NULL),
    (15, N'Pediatric Nursing',  7, N'Rejected', 0.0, 0, N'Giza',       30.0200, 31.2100, NULL)
) p(Id, Specialty, Exp, Ver, Rating, RatingCount, City, Lat, Lng, Lic) ON u.Id = p.Id;

UPDATE #U SET IsActive = 0 WHERE Id = 20;   -- one inactive account to test activate / deactivate

-- Copy to Users using only the columns that exist in your table
-- (so it works with or without a Discriminator column).
DECLARE @cols NVARCHAR(MAX) = (
    SELECT STRING_AGG(QUOTENAME(c.name), ', ')
    FROM sys.columns c
    WHERE c.object_id = OBJECT_ID('Users')
      AND c.name COLLATE DATABASE_DEFAULT IN (
            SELECT name COLLATE DATABASE_DEFAULT FROM tempdb.sys.columns
            WHERE object_id = OBJECT_ID('tempdb..#U')));
DECLARE @sql NVARCHAR(MAX) =
    N'SET IDENTITY_INSERT Users ON; INSERT INTO Users (' + @cols + N') SELECT ' + @cols +
    N' FROM #U; SET IDENTITY_INSERT Users OFF;';
EXEC (@sql);

/* ===== 2) Addresses (one per patient) ===== */
SET IDENTITY_INSERT Addresses ON;
INSERT INTO Addresses (Id, PatientId, Label, City, District, StreetAndBuilding, Latitude, Longitude) VALUES
 (1, 1, N'Home', N'Giza',  N'Dokki',       N'12 Pyramids Road, Building 4',    29.9870, 31.2118),
 (2, 2, N'Home', N'Cairo', N'Nasr City',   N'45 Mostafa El-Nahas, Building 9', 30.0626, 31.3469),
 (3, 3, N'Home', N'Cairo', N'Downtown',    N'8 El-Tahrir Street, Building 2',  30.0444, 31.2357),
 (4, 4, N'Home', N'Giza',  N'Faisal',      N'21 Faisal Street, Building 15',   30.0131, 31.2089),
 (5, 5, N'Home', N'Cairo', N'Heliopolis',  N'30 Abbas El-Akkad, Building 6',   30.0561, 31.3301);
SET IDENTITY_INSERT Addresses OFF;

/* ===== 3) ProviderCredentials (note: the FK column is ProviderID) ===== */
SET IDENTITY_INSERT ProviderCredentials ON;
INSERT INTO ProviderCredentials (Id, DocumentType, FileUrl, ProviderID) VALUES
 (1, N'Medical License', N'/PrivateUploads/6-license.pdf',  6),
 (2, N'Medical License', N'/PrivateUploads/7-license.pdf',  7),
 (3, N'Medical License', N'/PrivateUploads/8-license.pdf',  8),
 (4, N'Nursing Diploma', N'/PrivateUploads/11-diploma.pdf', 11),
 (5, N'Nursing Diploma', N'/PrivateUploads/12-diploma.pdf', 12);
SET IDENTITY_INSERT ProviderCredentials OFF;

/* ===== 4) Services (adds only the ones that are missing) ===== */
INSERT INTO Services (Name, Description, [Type], IsActive)
SELECT v.Name, v.Description, v.[Type], v.IsActive
FROM (VALUES
    (N'Doctor Home Visit',         N'A doctor visits the patient at home',        N'DoctorHomeVisit', 1),
    (N'Nurse Home Visit',          N'A nurse visits the patient at home',         N'NurseHomeVisit', 1),
    (N'Remote Consultation',       N'Online consultation with a doctor',          N'RemoteConsultation', 1),
    (N'Doctor Home Visit (Night)', N'Night visit by a doctor (not active yet)',   N'DoctorHomeVisit', 0),
    (N'Nurse Home Visit (Night)',  N'Night visit by a nurse (not active yet)',    N'NurseHomeVisit', 0)
) v(Name, Description, [Type], IsActive)
WHERE NOT EXISTS (SELECT 1 FROM Services s WHERE s.Name = v.Name);

DECLARE @Doc    INT = (SELECT TOP 1 Id FROM Services WHERE [Type] = N'DoctorHomeVisit' AND IsActive = 1 ORDER BY Id);
DECLARE @Nurse  INT = (SELECT TOP 1 Id FROM Services WHERE [Type] = N'NurseHomeVisit' AND IsActive = 1 ORDER BY Id);
DECLARE @Remote INT = (SELECT TOP 1 Id FROM Services WHERE [Type] = N'RemoteConsultation' AND IsActive = 1 ORDER BY Id);

/* ===== 5) ProviderServices (price per service for each verified provider) ===== */
SET IDENTITY_INSERT ProviderServices ON;
INSERT INTO ProviderServices (Id, ProviderId, ServiceId, Fee, CommissionPercentage) VALUES
 (1,  6,  @Doc,    400.00, 15.00),
 (2,  6,  @Remote, 200.00, 15.00),
 (3,  7,  @Doc,    350.00, 15.00),
 (4,  7,  @Remote, 180.00, 15.00),
 (5,  8,  @Doc,    300.00, 15.00),
 (6,  9,  @Doc,    500.00, 15.00),
 (7,  9,  @Remote, 250.00, 15.00),
 (8,  11, @Nurse,  150.00, 15.00),
 (9,  12, @Nurse,  120.00, 15.00),
 (10, 13, @Nurse,  180.00, 15.00);
SET IDENTITY_INSERT ProviderServices OFF;

/* ===== 6) PlatformSettings: ONE row only (skipped if it already exists) ===== */
IF NOT EXISTS (SELECT 1 FROM PlatformSettings)
BEGIN
    IF OBJECTPROPERTY(OBJECT_ID('PlatformSettings'), 'TableHasIdentity') = 1
    BEGIN
        SET IDENTITY_INSERT PlatformSettings ON;
        INSERT INTO PlatformSettings (Id, ServiceFee, CommissionPercentage) VALUES (1, 30.00, 15.00);
        SET IDENTITY_INSERT PlatformSettings OFF;
    END
    ELSE
        INSERT INTO PlatformSettings (Id, ServiceFee, CommissionPercentage) VALUES (1, 30.00, 15.00);
END

/* ===== 7) TimeSlots: 1-10 are used by bookings, 11-15 stay Available =====
   Dates are relative to today, so they are never "in the past" when you demo. */
SET IDENTITY_INSERT TimeSlots ON;
INSERT INTO TimeSlots (Id, ProviderId, [Date], StartTime, EndTime, Status) VALUES
 (1,  6,  DATEADD(DAY, -8, CAST(GETDATE() AS DATE)), '10:00', '12:00', N'Booked'),
 (2,  7,  DATEADD(DAY, -8, CAST(GETDATE() AS DATE)), '12:00', '14:00', N'Booked'),
 (3,  8,  DATEADD(DAY, -7, CAST(GETDATE() AS DATE)), '10:00', '12:00', N'Booked'),
 (4,  9,  DATEADD(DAY, -7, CAST(GETDATE() AS DATE)), '14:00', '16:00', N'Booked'),
 (5,  6,  DATEADD(DAY, -6, CAST(GETDATE() AS DATE)), '10:00', '12:00', N'Booked'),
 (6,  11, DATEADD(DAY,  2, CAST(GETDATE() AS DATE)), '10:00', '12:00', N'Locked'),  -- Locked (booking 6 is pending)
 (7,  12, DATEADD(DAY,  2, CAST(GETDATE() AS DATE)), '12:00', '14:00', N'Booked'),
 (8,  13, DATEADD(DAY,  1, CAST(GETDATE() AS DATE)), '16:00', '18:00', N'Booked'),
 (9,  6,  DATEADD(DAY,  1, CAST(GETDATE() AS DATE)), '14:00', '16:00', N'Booked'),
 (10, 7,  DATEADD(DAY,  2, CAST(GETDATE() AS DATE)), '16:00', '18:00', N'Available'),  -- Available (booking 10 was cancelled)
 (11, 6,  DATEADD(DAY,  3, CAST(GETDATE() AS DATE)), '10:00', '12:00', N'Available'),
 (12, 7,  DATEADD(DAY,  3, CAST(GETDATE() AS DATE)), '12:00', '14:00', N'Available'),
 (13, 8,  DATEADD(DAY,  3, CAST(GETDATE() AS DATE)), '14:00', '16:00', N'Available'),
 (14, 11, DATEADD(DAY,  4, CAST(GETDATE() AS DATE)), '10:00', '12:00', N'Available'),
 (15, 12, DATEADD(DAY,  4, CAST(GETDATE() AS DATE)), '12:00', '14:00', N'Available');
SET IDENTITY_INSERT TimeSlots OFF;

/* ===== 8) Bookings: 1-5 Completed, 6-10 one of each other status =====
   TotalPrice = provider fee + 30 service fee. Commission = 15% of the fee.
   OtpCode is NOT NULL in your entity, so remote / not-yet-confirmed use ''. */
SET IDENTITY_INSERT Bookings ON;
INSERT INTO Bookings (Id, Status, ServiceFee, TotalPrice, Commission, OtpCode, PatientId, ProviderId, ServiceId, TimeSlotId) VALUES
 (1,  N'Completed', 30.00, 430.00, 60.00, N'7421', 1, 6,  @Doc,    1),    -- Completed, home visit
 (2,  N'Completed', 30.00, 210.00, 27.00, N'',     2, 7,  @Remote, 2),    -- Completed, remote
 (3,  N'Completed', 30.00, 330.00, 45.00, N'3158', 3, 8,  @Doc,    3),    -- Completed
 (4,  N'Completed', 30.00, 530.00, 75.00, N'6094', 4, 9,  @Doc,    4),    -- Completed
 (5,  N'Completed', 30.00, 230.00, 30.00, N'',     5, 6,  @Remote, 5),    -- Completed, remote
 (6,  N'PendingProviderApproval', 30.00, 180.00, 22.50, N'',     1, 11, @Nurse,  6),    -- PendingProviderApproval
 (7,  N'ConfirmedCollectionPending', 30.00, 150.00, 18.00, N'9031', 2, 12, @Nurse,  7),    -- ConfirmedCollectionPending (cash)
 (8,  N'Confirmed', 30.00, 210.00, 27.00, N'4827', 3, 13, @Nurse,  8),    -- Confirmed (online)
 (9,  N'Started', 30.00, 430.00, 60.00, N'1593', 4, 6,  @Doc,    9),    -- Started
 (10, N'Cancelled', 30.00, 380.00, 52.50, N'2276', 5, 7,  @Doc,    10);   -- Cancelled
SET IDENTITY_INSERT Bookings OFF;

/* ===== 9) Payments (one per booking) ===== */
SET IDENTITY_INSERT Payments ON;
INSERT INTO Payments (Id, Method, Status, Amount, RefundedAmount, BookingId) VALUES
 (1,  N'CashOnArrival', N'Collected', 430.00, 0.00,   1),    -- Cash, Collected
 (2,  N'OnlinePayment', N'Captured', 210.00, 0.00,   2),    -- Online, Captured
 (3,  N'OnlinePayment', N'Captured', 330.00, 0.00,   3),
 (4,  N'CashOnArrival', N'Collected', 530.00, 0.00,   4),
 (5,  N'OnlinePayment', N'Captured', 230.00, 0.00,   5),
 (6,  N'CashOnArrival', N'Pending', 180.00, 0.00,   6),    -- Cash, Pending
 (7,  N'CashOnArrival', N'Pending', 150.00, 0.00,   7),
 (8,  N'OnlinePayment', N'Captured', 210.00, 0.00,   8),
 (9,  N'CashOnArrival', N'Pending', 430.00, 0.00,   9),
 (10, N'OnlinePayment', N'Refunded', 380.00, 380.00, 10);   -- Online, Refunded
SET IDENTITY_INSERT Payments OFF;

/* ===== 10) Reviews, ServiceNotes, Prescriptions (for the 5 Completed bookings) ===== */
SET IDENTITY_INSERT Reviews ON;
INSERT INTO Reviews (Id, BookingId, Stars, Comment, CreatedAt) VALUES
 (1, 1, 5, N'Very professional and on time.',      DATEADD(DAY, -8, SYSUTCDATETIME())),
 (2, 2, 4, N'Clear explanation, good consultation.', DATEADD(DAY, -8, SYSUTCDATETIME())),
 (3, 3, 5, N'Excellent doctor.',                   DATEADD(DAY, -7, SYSUTCDATETIME())),
 (4, 4, 4, N'Helpful, a bit late.',                DATEADD(DAY, -7, SYSUTCDATETIME())),
 (5, 5, 5, N'Fast and friendly.',                  DATEADD(DAY, -6, SYSUTCDATETIME()));
SET IDENTITY_INSERT Reviews OFF;

SET IDENTITY_INSERT ServiceNotes ON;
INSERT INTO ServiceNotes (Id, Content, BookingId) VALUES
 (1, N'Blood pressure slightly high. Follow up in two weeks.', 1),
 (2, N'Child has a mild fever. Rest and fluids.',              2),
 (3, N'Skin allergy. Avoid the current soap.',                 3),
 (4, N'Stable condition. Continue the current treatment.',     4),
 (5, N'Follow-up consultation. Symptoms improved.',            5);
SET IDENTITY_INSERT ServiceNotes OFF;

SET IDENTITY_INSERT Prescriptions ON;
INSERT INTO Prescriptions (Id, IssuedAt, BookingId, DoctorId) VALUES
 (1, DATEADD(DAY, -8, SYSUTCDATETIME()), 1, 6),
 (2, DATEADD(DAY, -8, SYSUTCDATETIME()), 2, 7),
 (3, DATEADD(DAY, -7, SYSUTCDATETIME()), 3, 8),
 (4, DATEADD(DAY, -7, SYSUTCDATETIME()), 4, 9),
 (5, DATEADD(DAY, -6, SYSUTCDATETIME()), 5, 6);
SET IDENTITY_INSERT Prescriptions OFF;

SET IDENTITY_INSERT PrescriptionItems ON;
INSERT INTO PrescriptionItems (Id, MedicationName, Dosage, Frequency, Duration, Instructions, PrescriptionId) VALUES
 (1, N'Amlodipine 5 mg',   N'1 tablet',  N'Once daily',    N'30 days', N'Take in the morning', 1),
 (2, N'Paracetamol syrup', N'5 ml',      N'Every 6 hours', N'3 days',  N'After food',          2),
 (3, N'Cetirizine 10 mg',  N'1 tablet',  N'Once daily',    N'7 days',  N'Before sleep',        3),
 (4, N'Metformin 500 mg',  N'1 tablet',  N'Twice daily',   N'30 days', N'With meals',          4),
 (5, N'Vitamin D3',        N'1 capsule', N'Once weekly',   N'8 weeks', N'With a fatty meal',   5);
SET IDENTITY_INSERT PrescriptionItems OFF;

/* ===== 11) Notifications ===== */
SET IDENTITY_INSERT Notifications ON;
INSERT INTO Notifications (Id, Title, Message, IsRead, UserId) VALUES
 (1, N'Request submitted',     N'Your booking request was sent and is waiting for the provider.', 0, 1),
 (2, N'New booking request',   N'You have a new booking request.',                                0, 11),
 (3, N'Booking cancelled',     N'The patient cancelled a booking.',                               1, 7),
 (4, N'Provider needs review', N'A new provider is waiting for verification.',                    0, 16),
 (5, N'Request accepted',      N'Your booking was accepted by the provider.',                     1, 2);
SET IDENTITY_INSERT Notifications OFF;

DROP TABLE #U;
COMMIT TRANSACTION;
PRINT 'Seed completed.';
GO

SET NOEXEC OFF;
GO
/* =====================================================================
   Zyara: add 10 more providers (5 doctors + 5 nurses)

   Run it AFTER zyara_seed_data_v4.sql (enums are text, services exist).
   It does not touch the old data. New ids start after the last user id
   (21 to 30 if you have the 20 users from the seed).

   It adds:
     Users 10 (8 Verified + 2 Pending), ProviderCredentials 10,
     ProviderServices 12 (verified providers only)

   AverageRating / RatingCount are written directly (no Review rows),
   so the Landing page has providers with different ratings to sort.
   Running it twice is safe: the second run stops with a message.
   ===================================================================== */

SET XACT_ABORT ON;

IF EXISTS (SELECT 1 FROM Users WHERE Email = 'mohamed.fawzy@zyara.test')
BEGIN
    PRINT 'STOPPED. These providers were already added.';
    RETURN;
END

BEGIN TRANSACTION;

DECLARE @base  INT = (SELECT ISNULL(MAX(Id), 0) FROM Users);
DECLARE @admin INT = (SELECT TOP 1 Id FROM Users WHERE Role = N'Admin' ORDER BY Id);

/* ===== 1) Users (built in a temp table, then copied to Users) ===== */
CREATE TABLE #P (
    Id INT NOT NULL, FullName NVARCHAR(200), Email NVARCHAR(200), Phone NVARCHAR(50),
    PasswordHash NVARCHAR(200) NOT NULL DEFAULT 'HASH',
    Gender NVARCHAR(20), Role NVARCHAR(20),
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    PhotoUrl NVARCHAR(400) NULL,
    Discriminator NVARCHAR(50),
    Specialty NVARCHAR(200) NULL, ExperienceYears INT NULL, Bio NVARCHAR(MAX) NULL,
    VerificationStatus NVARCHAR(20) NULL, RejectionReason NVARCHAR(MAX) NULL,
    VerifiedAt DATETIME2 NULL, VerifiedByAdminId INT NULL,
    AverageRating FLOAT NULL, RatingCount INT NULL, WalletBalance DECIMAL(18,2) NOT NULL DEFAULT 0,
    BaseCity NVARCHAR(100) NULL, BaseLatitude FLOAT NULL, BaseLongitude FLOAT NULL,
    licenser NVARCHAR(100) NULL
);

INSERT INTO #P
 (Id, FullName, Email, Phone, Gender, Role, Discriminator, Specialty, ExperienceYears, Bio,
  VerificationStatus, AverageRating, RatingCount, BaseCity, BaseLatitude, BaseLongitude,
  licenser, VerifiedAt, VerifiedByAdminId)
VALUES
 -- Doctors
 (@base + 1,  N'Dr. Mohamed Fawzy',  'mohamed.fawzy@zyara.test',  '01100000021', 'Male',   'Doctor', 'Doctor', N'Neurology',        14, N'Consultant neurologist with 14 years of experience.',
  'Verified', 4.7, 23, N'Cairo',      30.0561, 31.3301, 'EG-DOC-2001', SYSUTCDATETIME(), @admin),
 (@base + 2,  N'Dr. Salma Hegazy',   'salma.hegazy@zyara.test',   '01100000022', 'Female', 'Doctor', 'Doctor', N'Gynecology',       10, N'Gynecology and obstetrics specialist.',
  'Verified', 4.9, 31, N'Giza',       30.0131, 31.2089, 'EG-DOC-2002', SYSUTCDATETIME(), @admin),
 (@base + 3,  N'Dr. Ibrahim Mansour','ibrahim.mansour@zyara.test','01100000023', 'Male',   'Doctor', 'Doctor', N'Pulmonology',      18, N'Chest and respiratory diseases consultant.',
  'Verified', 4.4, 12, N'Alexandria', 31.2001, 29.9187, 'EG-DOC-2003', SYSUTCDATETIME(), @admin),
 (@base + 4,  N'Dr. Yasmin Farouk',  'yasmin.farouk@zyara.test',  '01100000024', 'Female', 'Doctor', 'Doctor', N'Family Medicine',   6, N'Family doctor for all ages.',
  'Verified', 0,   0,  N'Cairo',      30.0444, 31.2357, 'EG-DOC-2004', SYSUTCDATETIME(), @admin),
 (@base + 5,  N'Dr. Khaled Wahba',   'khaled.wahba@zyara.test',   '01100000025', 'Male',   'Doctor', 'Doctor', N'General Practice',   9, N'General practitioner.',
  'Pending',  0,   0,  N'Giza',       30.0200, 31.2100, 'EG-DOC-2005', NULL, NULL),
 -- Nurses
 (@base + 6,  N'Nourhan Adel',       'nourhan.adel@zyara.test',   '01200000026', 'Female', 'Nurse',  'Nurse',  N'Home Nursing',        5, N'Home nursing and daily patient care.',
  'Verified', 4.6, 18, N'Cairo',      30.0500, 31.2400, NULL, SYSUTCDATETIME(), @admin),
 (@base + 7,  N'Ayman Soliman',      'ayman.soliman@zyara.test',  '01200000027', 'Male',   'Nurse',  'Nurse',  N'Critical Care Nursing', 8, N'Critical care nurse.',
  'Verified', 4.2, 9,  N'Giza',       30.0100, 31.2000, NULL, SYSUTCDATETIME(), @admin),
 (@base + 8,  N'Mariam Ashraf',      'mariam.ashraf@zyara.test',  '01200000028', 'Female', 'Nurse',  'Nurse',  N'Pediatric Nursing',   4, N'Pediatric nurse.',
  'Verified', 0,   0,  N'Alexandria', 31.2100, 29.9200, NULL, SYSUTCDATETIME(), @admin),
 (@base + 9,  N'Hossam Ezzat',       'hossam.ezzat@zyara.test',   '01200000029', 'Male',   'Nurse',  'Nurse',  N'Wound Care',         11, N'Wound care and dressing specialist.',
  'Verified', 4.8, 27, N'Cairo',      30.0600, 31.3300, NULL, SYSUTCDATETIME(), @admin),
 (@base + 10, N'Fatma Gaber',        'fatma.gaber@zyara.test',    '01200000030', 'Female', 'Nurse',  'Nurse',  N'Elderly Care',        7, N'Elderly care nurse.',
  'Pending',  0,   0,  N'Giza',       30.0200, 31.2100, NULL, NULL, NULL);

-- Copy to Users using only the columns that exist in your table
DECLARE @cols NVARCHAR(MAX) = (
    SELECT STRING_AGG(QUOTENAME(c.name), ', ')
    FROM sys.columns c
    WHERE c.object_id = OBJECT_ID('Users')
      AND c.name COLLATE DATABASE_DEFAULT IN (
            SELECT name COLLATE DATABASE_DEFAULT FROM tempdb.sys.columns
            WHERE object_id = OBJECT_ID('tempdb..#P')));
DECLARE @sql NVARCHAR(MAX) =
    N'SET IDENTITY_INSERT Users ON; INSERT INTO Users (' + @cols + N') SELECT ' + @cols +
    N' FROM #P; SET IDENTITY_INSERT Users OFF;';
EXEC (@sql);

/* ===== 2) ProviderCredentials (one per provider; the FK column is ProviderID) ===== */
INSERT INTO ProviderCredentials (DocumentType, FileUrl, ProviderID)
SELECT CASE WHEN Role = N'Doctor' THEN N'Medical License' ELSE N'Nursing Diploma' END,
       N'/PrivateUploads/' + CAST(Id AS NVARCHAR(10)) + N'-' +
       CASE WHEN Role = N'Doctor' THEN N'license.pdf' ELSE N'diploma.pdf' END,
       Id
FROM #P;

/* ===== 3) ProviderServices (price per service, verified providers only) ===== */
DECLARE @Doc    INT = (SELECT TOP 1 Id FROM Services WHERE [Type] = N'DoctorHomeVisit'    AND IsActive = 1 ORDER BY Id);
DECLARE @Nurse  INT = (SELECT TOP 1 Id FROM Services WHERE [Type] = N'NurseHomeVisit'     AND IsActive = 1 ORDER BY Id);
DECLARE @Remote INT = (SELECT TOP 1 Id FROM Services WHERE [Type] = N'RemoteConsultation' AND IsActive = 1 ORDER BY Id);

INSERT INTO ProviderServices (ProviderId, ServiceId, Fee, CommissionPercentage) VALUES
 (@base + 1, @Doc,    450.00, 15.00),
 (@base + 1, @Remote, 250.00, 15.00),
 (@base + 2, @Doc,    400.00, 15.00),
 (@base + 2, @Remote, 220.00, 15.00),
 (@base + 3, @Doc,    600.00, 15.00),
 (@base + 3, @Remote, 300.00, 15.00),
 (@base + 4, @Doc,    300.00, 15.00),
 (@base + 4, @Remote, 150.00, 15.00),
 (@base + 6, @Nurse,  140.00, 15.00),
 (@base + 7, @Nurse,  200.00, 15.00),
 (@base + 8, @Nurse,  130.00, 15.00),
 (@base + 9, @Nurse,  170.00, 15.00);

DROP TABLE #P;
COMMIT TRANSACTION;
PRINT 'Added 10 providers.';
GO