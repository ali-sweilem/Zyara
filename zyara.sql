/* =====================================================================
   Zyara: Seed data (SQL Server)   v2, built from the real Entities

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
     3) Enums are stored as int, using the values in your enum files:
        Gender: Male 1, Female 2
        UserRole: Patient 1, Doctor 2, Nurse 3, Admin 4
        VerificationStatus: Pending 0, Verified 1, Rejected 2
        ServiceType: DoctorHomeVisit 0, NurseHomeVisit 1, RemoteConsultation 2
        PaymentMethod: CashOnArrival 0, OnlinePayment 1
        PaymentStatus: Pending 0, Authorized 1, Captured 2, Collected 3,
                       Released 4, Refunded 5, PartiallyRefunded 6
        BookingStatus: PendingProviderApproval 0, Confirmed 1,
                       ConfirmedCollectionPending 2, Started 3, Completed 4,
                       Cancelled 5, Rejected 6, Expired 7
        TimeSlotStatus: Available 0, Locked 1, Booked 2

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

/* ---------- Main script ---------- */
SET XACT_ABORT ON;
BEGIN TRANSACTION;

/* ===== 1) Users (built in a temp table, then copied to Users) ===== */
CREATE TABLE #U (
    Id INT NOT NULL, FullName NVARCHAR(200), Email NVARCHAR(200), Phone NVARCHAR(50),
    PasswordHash NVARCHAR(200) NOT NULL DEFAULT 'HASH',
    Gender INT, Role INT,
    IsActive BIT NOT NULL DEFAULT 1,
    CreatedAt DATETIME2 NOT NULL DEFAULT SYSUTCDATETIME(),
    PhotoUrl NVARCHAR(400) NULL,
    Discriminator NVARCHAR(50),
    DateOfBirth DATE NULL,
    Specialty NVARCHAR(200) NULL, ExperienceYears INT NULL, Bio NVARCHAR(MAX) NULL,
    VerificationStatus INT NULL, RejectionReason NVARCHAR(MAX) NULL,
    VerifiedAt DATETIME2 NULL, VerifiedByAdminId INT NULL,
    AverageRating FLOAT NULL, RatingCount INT NULL, WalletBalance DECIMAL(18,2) NULL,
    BaseCity NVARCHAR(100) NULL, BaseLatitude FLOAT NULL, BaseLongitude FLOAT NULL,
    licenser NVARCHAR(100) NULL
);

INSERT INTO #U (Id, FullName, Email, Phone, Gender, Role, Discriminator, DateOfBirth) VALUES
 -- Patients (Role 1)
 (1,  N'Ahmed Hassan',     'ahmed.hassan@zyara.test',   '01000000001', 1, 1, 'Patient', '1990-03-14'),
 (2,  N'Mona Ali',         'mona.ali@zyara.test',       '01000000002', 2, 1, 'Patient', '1985-07-22'),
 (3,  N'Karim Samir',      'karim.samir@zyara.test',    '01000000003', 1, 1, 'Patient', '1978-11-05'),
 (4,  N'Sara Mahmoud',     'sara.mahmoud@zyara.test',   '01000000004', 2, 1, 'Patient', '1995-01-30'),
 (5,  N'Youssef Nabil',    'youssef.nabil@zyara.test',  '01000000005', 1, 1, 'Patient', '2001-09-12'),
 -- Doctors (Role 2)
 (6,  N'Dr. Hany Fathy',   'hany.fathy@zyara.test',     '01100000006', 1, 2, 'Doctor', NULL),
 (7,  N'Dr. Laila Adel',   'laila.adel@zyara.test',     '01100000007', 2, 2, 'Doctor', NULL),
 (8,  N'Dr. Tarek Zaki',   'tarek.zaki@zyara.test',     '01100000008', 1, 2, 'Doctor', NULL),
 (9,  N'Dr. Nadia Sherif', 'nadia.sherif@zyara.test',   '01100000009', 2, 2, 'Doctor', NULL),
 (10, N'Dr. Amr Gamal',    'amr.gamal@zyara.test',      '01100000010', 1, 2, 'Doctor', NULL),
 -- Nurses (Role 3)
 (11, N'Heba Salah',       'heba.salah@zyara.test',     '01200000011', 2, 3, 'Nurse', NULL),
 (12, N'Mahmoud Reda',     'mahmoud.reda@zyara.test',   '01200000012', 1, 3, 'Nurse', NULL),
 (13, N'Rania Kamel',      'rania.kamel@zyara.test',    '01200000013', 2, 3, 'Nurse', NULL),
 (14, N'Sherif Ayman',     'sherif.ayman@zyara.test',   '01200000014', 1, 3, 'Nurse', NULL),
 (15, N'Dina Fouad',       'dina.fouad@zyara.test',     '01200000015', 2, 3, 'Nurse', NULL),
 -- Admins (Role 4)
 (16, N'Admin Mohamed',    'admin1@zyara.test',         '01500000016', 1, 4, 'Admin', NULL),
 (17, N'Admin Salma',      'admin2@zyara.test',         '01500000017', 2, 4, 'Admin', NULL),
 (18, N'Admin Omar',       'admin3@zyara.test',         '01500000018', 1, 4, 'Admin', NULL),
 (19, N'Admin Nour',       'admin4@zyara.test',         '01500000019', 2, 4, 'Admin', NULL),
 (20, N'Admin Khaled',     'admin5@zyara.test',         '01500000020', 1, 4, 'Admin', NULL);

-- Provider details. Ver: 0 = Pending, 1 = Verified, 2 = Rejected.
-- Rating / RatingCount match the Reviews inserted below. Nurses have no reviews yet ("New").
UPDATE u SET
    Specialty = p.Specialty, ExperienceYears = p.Exp, VerificationStatus = p.Ver,
    AverageRating = p.Rating, RatingCount = p.RatingCount, WalletBalance = 0,
    BaseCity = p.City, BaseLatitude = p.Lat, BaseLongitude = p.Lng, licenser = p.Lic,
    Bio = N'Provider on Zyara',
    VerifiedAt = CASE WHEN p.Ver = 0 THEN NULL ELSE SYSUTCDATETIME() END,
    VerifiedByAdminId = CASE WHEN p.Ver = 0 THEN NULL ELSE 16 END,
    RejectionReason = CASE WHEN p.Ver = 2 THEN N'Documents are not clear' ELSE NULL END
FROM #U u
JOIN (VALUES
    (6,  N'Cardiology',        12, 1, 5.0, 2, N'Cairo',      30.0444, 31.2357, 'EG-DOC-1001'),
    (7,  N'Pediatrics',         8, 1, 4.0, 1, N'Giza',       30.0131, 31.2089, 'EG-DOC-1002'),
    (8,  N'Dermatology',        5, 1, 5.0, 1, N'Cairo',      30.0626, 31.3469, 'EG-DOC-1003'),
    (9,  N'Internal Medicine', 15, 1, 4.0, 1, N'Alexandria', 31.2001, 29.9187, 'EG-DOC-1004'),
    (10, N'Orthopedics',        3, 0, 0.0, 0, N'Cairo',      30.0561, 31.3301, 'EG-DOC-1005'),
    (11, N'Home Nursing',       6, 1, 0.0, 0, N'Cairo',      30.0500, 31.2400, NULL),
    (12, N'Wound Care',         4, 1, 0.0, 0, N'Giza',       30.0100, 31.2000, NULL),
    (13, N'Elderly Care',       9, 1, 0.0, 0, N'Alexandria', 31.2100, 29.9200, NULL),
    (14, N'Injections',         2, 0, 0.0, 0, N'Cairo',      30.0600, 31.3300, NULL),
    (15, N'Pediatric Nursing',  7, 2, 0.0, 0, N'Giza',       30.0200, 31.2100, NULL)
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
    (N'Doctor Home Visit',         N'A doctor visits the patient at home',        0, 1),
    (N'Nurse Home Visit',          N'A nurse visits the patient at home',         1, 1),
    (N'Remote Consultation',       N'Online consultation with a doctor',          2, 1),
    (N'Doctor Home Visit (Night)', N'Night visit by a doctor (not active yet)',   0, 0),
    (N'Nurse Home Visit (Night)',  N'Night visit by a nurse (not active yet)',    1, 0)
) v(Name, Description, [Type], IsActive)
WHERE NOT EXISTS (SELECT 1 FROM Services s WHERE s.Name = v.Name);

DECLARE @Doc    INT = (SELECT TOP 1 Id FROM Services WHERE [Type] = 0 AND IsActive = 1 ORDER BY Id);
DECLARE @Nurse  INT = (SELECT TOP 1 Id FROM Services WHERE [Type] = 1 AND IsActive = 1 ORDER BY Id);
DECLARE @Remote INT = (SELECT TOP 1 Id FROM Services WHERE [Type] = 2 AND IsActive = 1 ORDER BY Id);

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
 (1,  6,  DATEADD(DAY, -8, CAST(GETDATE() AS DATE)), '10:00', '12:00', 2),
 (2,  7,  DATEADD(DAY, -8, CAST(GETDATE() AS DATE)), '12:00', '14:00', 2),
 (3,  8,  DATEADD(DAY, -7, CAST(GETDATE() AS DATE)), '10:00', '12:00', 2),
 (4,  9,  DATEADD(DAY, -7, CAST(GETDATE() AS DATE)), '14:00', '16:00', 2),
 (5,  6,  DATEADD(DAY, -6, CAST(GETDATE() AS DATE)), '10:00', '12:00', 2),
 (6,  11, DATEADD(DAY,  2, CAST(GETDATE() AS DATE)), '10:00', '12:00', 1),  -- Locked (booking 6 is pending)
 (7,  12, DATEADD(DAY,  2, CAST(GETDATE() AS DATE)), '12:00', '14:00', 2),
 (8,  13, DATEADD(DAY,  1, CAST(GETDATE() AS DATE)), '16:00', '18:00', 2),
 (9,  6,  DATEADD(DAY,  1, CAST(GETDATE() AS DATE)), '14:00', '16:00', 2),
 (10, 7,  DATEADD(DAY,  2, CAST(GETDATE() AS DATE)), '16:00', '18:00', 0),  -- Available (booking 10 was cancelled)
 (11, 6,  DATEADD(DAY,  3, CAST(GETDATE() AS DATE)), '10:00', '12:00', 0),
 (12, 7,  DATEADD(DAY,  3, CAST(GETDATE() AS DATE)), '12:00', '14:00', 0),
 (13, 8,  DATEADD(DAY,  3, CAST(GETDATE() AS DATE)), '14:00', '16:00', 0),
 (14, 11, DATEADD(DAY,  4, CAST(GETDATE() AS DATE)), '10:00', '12:00', 0),
 (15, 12, DATEADD(DAY,  4, CAST(GETDATE() AS DATE)), '12:00', '14:00', 0);
SET IDENTITY_INSERT TimeSlots OFF;

/* ===== 8) Bookings: 1-5 Completed, 6-10 one of each other status =====
   TotalPrice = provider fee + 30 service fee. Commission = 15% of the fee.
   OtpCode is NOT NULL in your entity, so remote / not-yet-confirmed use ''. */
SET IDENTITY_INSERT Bookings ON;
INSERT INTO Bookings (Id, Status, ServiceFee, TotalPrice, Commission, OtpCode, PatientId, ProviderId, ServiceId, TimeSlotId) VALUES
 (1,  4, 30.00, 430.00, 60.00, N'7421', 1, 6,  @Doc,    1),    -- Completed, home visit
 (2,  4, 30.00, 210.00, 27.00, N'',     2, 7,  @Remote, 2),    -- Completed, remote
 (3,  4, 30.00, 330.00, 45.00, N'3158', 3, 8,  @Doc,    3),    -- Completed
 (4,  4, 30.00, 530.00, 75.00, N'6094', 4, 9,  @Doc,    4),    -- Completed
 (5,  4, 30.00, 230.00, 30.00, N'',     5, 6,  @Remote, 5),    -- Completed, remote
 (6,  0, 30.00, 180.00, 22.50, N'',     1, 11, @Nurse,  6),    -- PendingProviderApproval
 (7,  2, 30.00, 150.00, 18.00, N'9031', 2, 12, @Nurse,  7),    -- ConfirmedCollectionPending (cash)
 (8,  1, 30.00, 210.00, 27.00, N'4827', 3, 13, @Nurse,  8),    -- Confirmed (online)
 (9,  3, 30.00, 430.00, 60.00, N'1593', 4, 6,  @Doc,    9),    -- Started
 (10, 5, 30.00, 380.00, 52.50, N'2276', 5, 7,  @Doc,    10);   -- Cancelled
SET IDENTITY_INSERT Bookings OFF;

/* ===== 9) Payments (one per booking). Method: 0 Cash, 1 Online ===== */
SET IDENTITY_INSERT Payments ON;
INSERT INTO Payments (Id, Method, Status, Amount, RefundedAmount, BookingId) VALUES
 (1,  0, 3, 430.00, 0.00,   1),    -- Cash, Collected
 (2,  1, 2, 210.00, 0.00,   2),    -- Online, Captured
 (3,  1, 2, 330.00, 0.00,   3),
 (4,  0, 3, 530.00, 0.00,   4),
 (5,  1, 2, 230.00, 0.00,   5),
 (6,  0, 0, 180.00, 0.00,   6),    -- Cash, Pending
 (7,  0, 0, 150.00, 0.00,   7),
 (8,  1, 2, 210.00, 0.00,   8),
 (9,  0, 0, 430.00, 0.00,   9),
 (10, 1, 5, 380.00, 380.00, 10);   -- Online, Refunded
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