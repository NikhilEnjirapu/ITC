-- Run only after reviewing and replacing these values, after Database.sql.
-- Corporate Administrator app-role assignment must be done separately in Entra ID.
SET XACT_ABORT ON;
DECLARE @EmployeeId nvarchar(50) = N'REPLACE_EMPLOYEE_ID';
DECLARE @Email nvarchar(256) = N'REPLACE_CORPORATE_EMAIL';
DECLARE @UserId nvarchar(450) = N''; -- required only when MatchMode=UserId
IF @EmployeeId LIKE N'REPLACE%' OR @Email LIKE N'REPLACE%' THROW 50001, 'Replace bootstrap placeholders first.', 1;
IF UPPER(LEFT(@EmployeeId,1)) IN (N'T',N'S') THROW 50002, 'This employee ID is excluded.', 1;
BEGIN TRANSACTION;
IF NOT EXISTS (SELECT 1 FROM Departments WHERE Name = N'Administration')
 INSERT INTO Departments (Name, Description, CreatedAt) VALUES (N'Administration', N'Portal administration', SYSUTCDATETIME());
DECLARE @DepartmentId int = (SELECT TOP 1 Id FROM Departments WHERE Name=N'Administration');
IF NOT EXISTS (SELECT 1 FROM Employees WHERE EmployeeId=@EmployeeId OR Email=@Email)
 INSERT INTO Employees (EmployeeId, FirstName, LastName, Email, DepartmentId, UserId, WorkerType, CreatedAt)
 VALUES (@EmployeeId,N'Portal',N'Administrator',@Email,@DepartmentId,@UserId,N'Employee',SYSUTCDATETIME());
ELSE THROW 50003, 'An employee with this ID or email exists; review that record instead.', 1;
COMMIT;
