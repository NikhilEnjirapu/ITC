-- LOCAL DEVELOPMENT ONLY. Never run this against a client or production database.
--
-- Creates the two employee records that the "Development Testing Only" sign-in
-- buttons on /login expect (see docs/DEVELOPMENT-AUTHENTICATION.md). Without them,
-- clicking either button ends on "Access denied".
--
-- Run once against your local database (EmployeeTrainingPortal by default),
-- after deployment/Database.sql. Safe to run again: existing records are left as they are.
SET NOCOUNT ON;
SET XACT_ABORT ON;

IF OBJECT_ID(N'dbo.Employees') IS NULL OR OBJECT_ID(N'dbo.Departments') IS NULL
    THROW 50001, 'Tables not found. Select the portal database and run deployment/Database.sql first.', 1;

BEGIN TRANSACTION;

IF NOT EXISTS (SELECT 1 FROM Departments WHERE Name = N'Development Testing')
    INSERT INTO Departments (Name, Description, CreatedAt)
    VALUES (N'Development Testing', N'Local development sign-in accounts', SYSUTCDATETIME());

DECLARE @DepartmentId int = (SELECT TOP 1 Id FROM Departments WHERE Name = N'Development Testing' ORDER BY Id);

-- Employee IDs must not start with T or S, and WorkerType must be Employee, or eligibility rules deny sign-in.
IF NOT EXISTS (SELECT 1 FROM Employees WHERE Email = N'local-admin@training-portal.test')
    INSERT INTO Employees (EmployeeId, FirstName, LastName, Email, DepartmentId, UserId, WorkerType, CreatedAt)
    VALUES (N'900001', N'Local', N'Administrator', N'local-admin@training-portal.test', @DepartmentId,
            N'training-portal-dev-admin', N'Employee', SYSUTCDATETIME());

IF NOT EXISTS (SELECT 1 FROM Employees WHERE Email = N'local-employee@training-portal.test')
    INSERT INTO Employees (EmployeeId, FirstName, LastName, Email, DepartmentId, UserId, WorkerType, CreatedAt)
    VALUES (N'900002', N'Local', N'Employee', N'local-employee@training-portal.test', @DepartmentId,
            N'training-portal-dev-employee', N'Employee', SYSUTCDATETIME());

COMMIT;

SELECT EmployeeId, FirstName, LastName, Email, WorkerType
FROM Employees
WHERE Email IN (N'local-admin@training-portal.test', N'local-employee@training-portal.test');
