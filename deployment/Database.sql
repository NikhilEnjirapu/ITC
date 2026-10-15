IF OBJECT_ID(N'[__EFMigrationsHistory]') IS NULL
BEGIN
    CREATE TABLE [__EFMigrationsHistory] (
        [MigrationId] nvarchar(150) NOT NULL,
        [ProductVersion] nvarchar(32) NOT NULL,
        CONSTRAINT [PK___EFMigrationsHistory] PRIMARY KEY ([MigrationId])
    );
END;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825190828_InitialCreate'
)
BEGIN
    CREATE TABLE [AuditLogs] (
        [Id] int NOT NULL IDENTITY,
        [Action] nvarchar(max) NOT NULL,
        [UserId] nvarchar(max) NULL,
        [EmployeeId] nvarchar(max) NULL,
        [Details] nvarchar(max) NULL,
        [IpAddress] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_AuditLogs] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825190828_InitialCreate'
)
BEGIN
    CREATE TABLE [Departments] (
        [Id] int NOT NULL IDENTITY,
        [Name] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Departments] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825190828_InitialCreate'
)
BEGIN
    CREATE TABLE [EligibilityRules] (
        [Id] int NOT NULL IDENTITY,
        [RuleType] nvarchar(max) NOT NULL,
        [RuleValue] nvarchar(max) NOT NULL,
        [IsActive] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_EligibilityRules] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825190828_InitialCreate'
)
BEGIN
    CREATE TABLE [TrainingPrograms] (
        [Id] int NOT NULL IDENTITY,
        [Title] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_TrainingPrograms] PRIMARY KEY ([Id])
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825190828_InitialCreate'
)
BEGIN
    CREATE TABLE [Employees] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] nvarchar(max) NOT NULL,
        [FirstName] nvarchar(max) NOT NULL,
        [LastName] nvarchar(max) NOT NULL,
        [Email] nvarchar(max) NOT NULL,
        [DepartmentId] int NOT NULL,
        [UserId] nvarchar(max) NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Employees] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Employees_Departments_DepartmentId] FOREIGN KEY ([DepartmentId]) REFERENCES [Departments] ([Id]) ON DELETE NO ACTION
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825190828_InitialCreate'
)
BEGIN
    CREATE TABLE [Quizzes] (
        [Id] int NOT NULL IDENTITY,
        [Title] nvarchar(max) NOT NULL,
        [PassingScorePercentage] int NOT NULL,
        [TrainingProgramId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_Quizzes] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_Quizzes_TrainingPrograms_TrainingProgramId] FOREIGN KEY ([TrainingProgramId]) REFERENCES [TrainingPrograms] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825190828_InitialCreate'
)
BEGIN
    CREATE TABLE [TrainingVideos] (
        [Id] int NOT NULL IDENTITY,
        [Title] nvarchar(max) NOT NULL,
        [Description] nvarchar(max) NULL,
        [VideoUrl] nvarchar(max) NOT NULL,
        [SequenceOrder] int NOT NULL,
        [TrainingProgramId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_TrainingVideos] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TrainingVideos_TrainingPrograms_TrainingProgramId] FOREIGN KEY ([TrainingProgramId]) REFERENCES [TrainingPrograms] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825190828_InitialCreate'
)
BEGIN
    CREATE TABLE [TrainingAssignments] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [TrainingProgramId] int NOT NULL,
        [AssignedAt] datetime2 NOT NULL,
        [DueDate] datetime2 NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_TrainingAssignments] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_TrainingAssignments_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_TrainingAssignments_TrainingPrograms_TrainingProgramId] FOREIGN KEY ([TrainingProgramId]) REFERENCES [TrainingPrograms] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825190828_InitialCreate'
)
BEGIN
    CREATE TABLE [QuizQuestions] (
        [Id] int NOT NULL IDENTITY,
        [QuestionText] nvarchar(max) NOT NULL,
        [OptionA] nvarchar(max) NOT NULL,
        [OptionB] nvarchar(max) NOT NULL,
        [OptionC] nvarchar(max) NOT NULL,
        [OptionD] nvarchar(max) NOT NULL,
        [CorrectOption] nvarchar(max) NOT NULL,
        [SequenceOrder] int NOT NULL,
        [QuizId] int NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_QuizQuestions] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_QuizQuestions_Quizzes_QuizId] FOREIGN KEY ([QuizId]) REFERENCES [Quizzes] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825190828_InitialCreate'
)
BEGIN
    CREATE TABLE [QuizResults] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [QuizId] int NOT NULL,
        [ScorePercentage] int NOT NULL,
        [IsPassed] bit NOT NULL,
        [AttemptDate] datetime2 NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_QuizResults] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_QuizResults_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_QuizResults_Quizzes_QuizId] FOREIGN KEY ([QuizId]) REFERENCES [Quizzes] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825190828_InitialCreate'
)
BEGIN
    CREATE TABLE [VideoProgresses] (
        [Id] int NOT NULL IDENTITY,
        [EmployeeId] int NOT NULL,
        [TrainingVideoId] int NOT NULL,
        [CurrentPositionSeconds] int NOT NULL,
        [IsCompleted] bit NOT NULL,
        [CreatedAt] datetime2 NOT NULL,
        [UpdatedAt] datetime2 NULL,
        CONSTRAINT [PK_VideoProgresses] PRIMARY KEY ([Id]),
        CONSTRAINT [FK_VideoProgresses_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE CASCADE,
        CONSTRAINT [FK_VideoProgresses_TrainingVideos_TrainingVideoId] FOREIGN KEY ([TrainingVideoId]) REFERENCES [TrainingVideos] ([Id]) ON DELETE CASCADE
    );
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825190828_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_Employees_DepartmentId] ON [Employees] ([DepartmentId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825190828_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_QuizQuestions_QuizId] ON [QuizQuestions] ([QuizId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825190828_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_QuizResults_EmployeeId] ON [QuizResults] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825190828_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_QuizResults_QuizId] ON [QuizResults] ([QuizId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825190828_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Quizzes_TrainingProgramId] ON [Quizzes] ([TrainingProgramId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825190828_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TrainingAssignments_EmployeeId] ON [TrainingAssignments] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825190828_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TrainingAssignments_TrainingProgramId] ON [TrainingAssignments] ([TrainingProgramId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825190828_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_TrainingVideos_TrainingProgramId] ON [TrainingVideos] ([TrainingProgramId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825190828_InitialCreate'
)
BEGIN
    CREATE UNIQUE INDEX [IX_VideoProgresses_EmployeeId_TrainingVideoId] ON [VideoProgresses] ([EmployeeId], [TrainingVideoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825190828_InitialCreate'
)
BEGIN
    CREATE INDEX [IX_VideoProgresses_TrainingVideoId] ON [VideoProgresses] ([TrainingVideoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260825190828_InitialCreate'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260825190828_InitialCreate', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260829171840_HardenEmployeeDepartmentFoundation'
)
BEGIN
    IF EXISTS (SELECT 1 FROM Employees WHERE LEN(EmployeeId) > 50)
        THROW 51000, 'Migration blocked: an EmployeeId exceeds 50 characters.', 1;
    IF EXISTS (SELECT 1 FROM Employees WHERE LEN(FirstName) > 100 OR LEN(LastName) > 100)
        THROW 51000, 'Migration blocked: an employee name exceeds 100 characters.', 1;
    IF EXISTS (SELECT 1 FROM Employees WHERE LEN(Email) > 256)
        THROW 51000, 'Migration blocked: an employee email exceeds 256 characters.', 1;
    IF EXISTS (SELECT 1 FROM Employees WHERE LEN(UserId) > 450)
        THROW 51000, 'Migration blocked: an employee UserId exceeds 450 characters.', 1;
    IF EXISTS (SELECT 1 FROM Departments WHERE LEN(Name) > 100 OR LEN(Description) > 500)
        THROW 51000, 'Migration blocked: a department value exceeds its new maximum length.', 1;
    IF EXISTS (SELECT EmployeeId FROM Employees GROUP BY EmployeeId HAVING COUNT(*) > 1)
        THROW 51000, 'Migration blocked: duplicate EmployeeId values exist.', 1;
    IF EXISTS (SELECT Email FROM Employees GROUP BY Email HAVING COUNT(*) > 1)
        THROW 51000, 'Migration blocked: duplicate employee email values exist.', 1;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260829171840_HardenEmployeeDepartmentFoundation'
)
BEGIN
    DECLARE @var nvarchar(max);
    SELECT @var = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Employees]') AND [c].[name] = N'UserId');
    IF @var IS NOT NULL EXEC(N'ALTER TABLE [Employees] DROP CONSTRAINT ' + @var + ';');
    ALTER TABLE [Employees] ALTER COLUMN [UserId] nvarchar(450) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260829171840_HardenEmployeeDepartmentFoundation'
)
BEGIN
    DECLARE @var1 nvarchar(max);
    SELECT @var1 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Employees]') AND [c].[name] = N'LastName');
    IF @var1 IS NOT NULL EXEC(N'ALTER TABLE [Employees] DROP CONSTRAINT ' + @var1 + ';');
    ALTER TABLE [Employees] ALTER COLUMN [LastName] nvarchar(100) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260829171840_HardenEmployeeDepartmentFoundation'
)
BEGIN
    DECLARE @var2 nvarchar(max);
    SELECT @var2 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Employees]') AND [c].[name] = N'FirstName');
    IF @var2 IS NOT NULL EXEC(N'ALTER TABLE [Employees] DROP CONSTRAINT ' + @var2 + ';');
    ALTER TABLE [Employees] ALTER COLUMN [FirstName] nvarchar(100) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260829171840_HardenEmployeeDepartmentFoundation'
)
BEGIN
    DECLARE @var3 nvarchar(max);
    SELECT @var3 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Employees]') AND [c].[name] = N'EmployeeId');
    IF @var3 IS NOT NULL EXEC(N'ALTER TABLE [Employees] DROP CONSTRAINT ' + @var3 + ';');
    ALTER TABLE [Employees] ALTER COLUMN [EmployeeId] nvarchar(50) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260829171840_HardenEmployeeDepartmentFoundation'
)
BEGIN
    DECLARE @var4 nvarchar(max);
    SELECT @var4 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Employees]') AND [c].[name] = N'Email');
    IF @var4 IS NOT NULL EXEC(N'ALTER TABLE [Employees] DROP CONSTRAINT ' + @var4 + ';');
    ALTER TABLE [Employees] ALTER COLUMN [Email] nvarchar(256) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260829171840_HardenEmployeeDepartmentFoundation'
)
BEGIN
    ALTER TABLE [Employees] ADD [RowVersion] rowversion NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260829171840_HardenEmployeeDepartmentFoundation'
)
BEGIN
    DECLARE @var5 nvarchar(max);
    SELECT @var5 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Departments]') AND [c].[name] = N'Name');
    IF @var5 IS NOT NULL EXEC(N'ALTER TABLE [Departments] DROP CONSTRAINT ' + @var5 + ';');
    ALTER TABLE [Departments] ALTER COLUMN [Name] nvarchar(100) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260829171840_HardenEmployeeDepartmentFoundation'
)
BEGIN
    DECLARE @var6 nvarchar(max);
    SELECT @var6 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[Departments]') AND [c].[name] = N'Description');
    IF @var6 IS NOT NULL EXEC(N'ALTER TABLE [Departments] DROP CONSTRAINT ' + @var6 + ';');
    ALTER TABLE [Departments] ALTER COLUMN [Description] nvarchar(500) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260829171840_HardenEmployeeDepartmentFoundation'
)
BEGIN
    ALTER TABLE [Departments] ADD [RowVersion] rowversion NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260829171840_HardenEmployeeDepartmentFoundation'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Employees_Email] ON [Employees] ([Email]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260829171840_HardenEmployeeDepartmentFoundation'
)
BEGIN
    CREATE UNIQUE INDEX [IX_Employees_EmployeeId] ON [Employees] ([EmployeeId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260829171840_HardenEmployeeDepartmentFoundation'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260829171840_HardenEmployeeDepartmentFoundation', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260829173754_AddTrainingManagementFoundation'
)
BEGIN
    IF EXISTS (SELECT 1 FROM TrainingPrograms WHERE LEN(Title) > 200 OR LEN(Description) > 2000)
        THROW 51000, 'Migration blocked: a training program value exceeds its new maximum length.', 1;
    IF EXISTS (SELECT 1 FROM TrainingVideos WHERE LEN(Title) > 200 OR LEN(Description) > 2000 OR LEN(VideoUrl) > 2048)
        THROW 51000, 'Migration blocked: a training video value exceeds its new maximum length.', 1;
    IF EXISTS (SELECT TrainingProgramId, SequenceOrder FROM TrainingVideos GROUP BY TrainingProgramId, SequenceOrder HAVING COUNT(*) > 1)
        THROW 51000, 'Migration blocked: duplicate video sequence orders exist within a training program.', 1;
    IF EXISTS (SELECT EmployeeId, TrainingProgramId FROM TrainingAssignments GROUP BY EmployeeId, TrainingProgramId HAVING COUNT(*) > 1)
        THROW 51000, 'Migration blocked: duplicate employee training assignments exist.', 1;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260829173754_AddTrainingManagementFoundation'
)
BEGIN
    ALTER TABLE [Quizzes] DROP CONSTRAINT [FK_Quizzes_TrainingPrograms_TrainingProgramId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260829173754_AddTrainingManagementFoundation'
)
BEGIN
    ALTER TABLE [TrainingAssignments] DROP CONSTRAINT [FK_TrainingAssignments_Employees_EmployeeId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260829173754_AddTrainingManagementFoundation'
)
BEGIN
    ALTER TABLE [TrainingAssignments] DROP CONSTRAINT [FK_TrainingAssignments_TrainingPrograms_TrainingProgramId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260829173754_AddTrainingManagementFoundation'
)
BEGIN
    ALTER TABLE [TrainingVideos] DROP CONSTRAINT [FK_TrainingVideos_TrainingPrograms_TrainingProgramId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260829173754_AddTrainingManagementFoundation'
)
BEGIN
    DROP INDEX [IX_TrainingVideos_TrainingProgramId] ON [TrainingVideos];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260829173754_AddTrainingManagementFoundation'
)
BEGIN
    DROP INDEX [IX_TrainingAssignments_EmployeeId] ON [TrainingAssignments];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260829173754_AddTrainingManagementFoundation'
)
BEGIN
    DECLARE @var7 nvarchar(max);
    SELECT @var7 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[TrainingVideos]') AND [c].[name] = N'VideoUrl');
    IF @var7 IS NOT NULL EXEC(N'ALTER TABLE [TrainingVideos] DROP CONSTRAINT ' + @var7 + ';');
    ALTER TABLE [TrainingVideos] ALTER COLUMN [VideoUrl] nvarchar(2048) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260829173754_AddTrainingManagementFoundation'
)
BEGIN
    DECLARE @var8 nvarchar(max);
    SELECT @var8 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[TrainingVideos]') AND [c].[name] = N'Title');
    IF @var8 IS NOT NULL EXEC(N'ALTER TABLE [TrainingVideos] DROP CONSTRAINT ' + @var8 + ';');
    ALTER TABLE [TrainingVideos] ALTER COLUMN [Title] nvarchar(200) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260829173754_AddTrainingManagementFoundation'
)
BEGIN
    DECLARE @var9 nvarchar(max);
    SELECT @var9 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[TrainingVideos]') AND [c].[name] = N'Description');
    IF @var9 IS NOT NULL EXEC(N'ALTER TABLE [TrainingVideos] DROP CONSTRAINT ' + @var9 + ';');
    ALTER TABLE [TrainingVideos] ALTER COLUMN [Description] nvarchar(2000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260829173754_AddTrainingManagementFoundation'
)
BEGIN
    ALTER TABLE [TrainingVideos] ADD [RowVersion] rowversion NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260829173754_AddTrainingManagementFoundation'
)
BEGIN
    DECLARE @var10 nvarchar(max);
    SELECT @var10 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[TrainingPrograms]') AND [c].[name] = N'Title');
    IF @var10 IS NOT NULL EXEC(N'ALTER TABLE [TrainingPrograms] DROP CONSTRAINT ' + @var10 + ';');
    ALTER TABLE [TrainingPrograms] ALTER COLUMN [Title] nvarchar(200) NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260829173754_AddTrainingManagementFoundation'
)
BEGIN
    DECLARE @var11 nvarchar(max);
    SELECT @var11 = QUOTENAME([d].[name])
    FROM [sys].[default_constraints] [d]
    INNER JOIN [sys].[columns] [c] ON [d].[parent_column_id] = [c].[column_id] AND [d].[parent_object_id] = [c].[object_id]
    WHERE ([d].[parent_object_id] = OBJECT_ID(N'[TrainingPrograms]') AND [c].[name] = N'Description');
    IF @var11 IS NOT NULL EXEC(N'ALTER TABLE [TrainingPrograms] DROP CONSTRAINT ' + @var11 + ';');
    ALTER TABLE [TrainingPrograms] ALTER COLUMN [Description] nvarchar(2000) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260829173754_AddTrainingManagementFoundation'
)
BEGIN
    ALTER TABLE [TrainingPrograms] ADD [RowVersion] rowversion NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260829173754_AddTrainingManagementFoundation'
)
BEGIN
    ALTER TABLE [TrainingAssignments] ADD [RowVersion] rowversion NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260829173754_AddTrainingManagementFoundation'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TrainingVideos_TrainingProgramId_SequenceOrder] ON [TrainingVideos] ([TrainingProgramId], [SequenceOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260829173754_AddTrainingManagementFoundation'
)
BEGIN
    CREATE UNIQUE INDEX [IX_TrainingAssignments_EmployeeId_TrainingProgramId] ON [TrainingAssignments] ([EmployeeId], [TrainingProgramId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260829173754_AddTrainingManagementFoundation'
)
BEGIN
    ALTER TABLE [Quizzes] ADD CONSTRAINT [FK_Quizzes_TrainingPrograms_TrainingProgramId] FOREIGN KEY ([TrainingProgramId]) REFERENCES [TrainingPrograms] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260829173754_AddTrainingManagementFoundation'
)
BEGIN
    ALTER TABLE [TrainingAssignments] ADD CONSTRAINT [FK_TrainingAssignments_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260829173754_AddTrainingManagementFoundation'
)
BEGIN
    ALTER TABLE [TrainingAssignments] ADD CONSTRAINT [FK_TrainingAssignments_TrainingPrograms_TrainingProgramId] FOREIGN KEY ([TrainingProgramId]) REFERENCES [TrainingPrograms] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260829173754_AddTrainingManagementFoundation'
)
BEGIN
    ALTER TABLE [TrainingVideos] ADD CONSTRAINT [FK_TrainingVideos_TrainingPrograms_TrainingProgramId] FOREIGN KEY ([TrainingProgramId]) REFERENCES [TrainingPrograms] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260829173754_AddTrainingManagementFoundation'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260829173754_AddTrainingManagementFoundation', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831181446_AddVideoDurationForPlaybackValidation'
)
BEGIN
    ALTER TABLE [TrainingVideos] ADD [DurationSeconds] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260831181446_AddVideoDurationForPlaybackValidation'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260831181446_AddVideoDurationForPlaybackValidation', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901181951_AddQuizAssessmentWorkflow'
)
BEGIN
    ALTER TABLE [QuizResults] DROP CONSTRAINT [FK_QuizResults_Employees_EmployeeId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901181951_AddQuizAssessmentWorkflow'
)
BEGIN
    ALTER TABLE [QuizResults] DROP CONSTRAINT [FK_QuizResults_Quizzes_QuizId];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901181951_AddQuizAssessmentWorkflow'
)
BEGIN
    DROP INDEX [IX_QuizResults_EmployeeId] ON [QuizResults];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901181951_AddQuizAssessmentWorkflow'
)
BEGIN
    DROP INDEX [IX_QuizQuestions_QuizId] ON [QuizQuestions];
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901181951_AddQuizAssessmentWorkflow'
)
BEGIN
    ALTER TABLE [Quizzes] ADD [IsActive] bit NOT NULL DEFAULT CAST(1 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901181951_AddQuizAssessmentWorkflow'
)
BEGIN
    ALTER TABLE [QuizResults] ADD [CompletedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901181951_AddQuizAssessmentWorkflow'
)
BEGIN
    ALTER TABLE [QuizResults] ADD [IsCompleted] bit NOT NULL DEFAULT CAST(1 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901181951_AddQuizAssessmentWorkflow'
)
BEGIN
    CREATE INDEX [IX_QuizResults_EmployeeId_QuizId_IsCompleted] ON [QuizResults] ([EmployeeId], [QuizId], [IsCompleted]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901181951_AddQuizAssessmentWorkflow'
)
BEGIN
    CREATE UNIQUE INDEX [IX_QuizQuestions_QuizId_SequenceOrder] ON [QuizQuestions] ([QuizId], [SequenceOrder]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901181951_AddQuizAssessmentWorkflow'
)
BEGIN
    ALTER TABLE [QuizResults] ADD CONSTRAINT [FK_QuizResults_Employees_EmployeeId] FOREIGN KEY ([EmployeeId]) REFERENCES [Employees] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901181951_AddQuizAssessmentWorkflow'
)
BEGIN
    ALTER TABLE [QuizResults] ADD CONSTRAINT [FK_QuizResults_Quizzes_QuizId] FOREIGN KEY ([QuizId]) REFERENCES [Quizzes] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260901181951_AddQuizAssessmentWorkflow'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260901181951_AddQuizAssessmentWorkflow', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908174708_AddEligibilityAndPlaybackCompliance'
)
BEGIN
    ALTER TABLE [VideoProgresses] ADD [PlaybackCreditSeconds] float NOT NULL DEFAULT 0.0E0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908174708_AddEligibilityAndPlaybackCompliance'
)
BEGIN
    ALTER TABLE [VideoProgresses] ADD [PlaybackState] nvarchar(max) NOT NULL DEFAULT N'paused';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908174708_AddEligibilityAndPlaybackCompliance'
)
BEGIN
    ALTER TABLE [VideoProgresses] ADD [RowVersion] rowversion NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908174708_AddEligibilityAndPlaybackCompliance'
)
BEGIN
    ALTER TABLE [Employees] ADD [WorkerType] nvarchar(30) NOT NULL DEFAULT N'Unknown';
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908174708_AddEligibilityAndPlaybackCompliance'
)
BEGIN
    UPDATE p SET IsCompleted = 0 FROM VideoProgresses p INNER JOIN TrainingVideos v ON v.Id = p.TrainingVideoId WHERE p.CurrentPositionSeconds < v.DurationSeconds;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908174708_AddEligibilityAndPlaybackCompliance'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260908174708_AddEligibilityAndPlaybackCompliance', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908175134_ProtectAssessmentHistory'
)
BEGIN
    ALTER TABLE [QuizResults] ADD [RowVersion] rowversion NOT NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908175134_ProtectAssessmentHistory'
)
BEGIN
    ALTER TABLE [QuizResults] ADD [SubmittedAnswersJson] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908175134_ProtectAssessmentHistory'
)
BEGIN
    EXEC(N'CREATE UNIQUE INDEX [IX_QuizResults_EmployeeId] ON [QuizResults] ([EmployeeId]) WHERE [IsCompleted] = 0');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260908175134_ProtectAssessmentHistory'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260908175134_ProtectAssessmentHistory', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910180615_AddProgramExamUnlockCondition'
)
BEGIN
    ALTER TABLE [TrainingPrograms] ADD [ExamUnlockCondition] int NOT NULL DEFAULT 0;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910180615_AddProgramExamUnlockCondition'
)
BEGIN
    ALTER TABLE [TrainingPrograms] ADD [ExamUnlockVideoId] int NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910180615_AddProgramExamUnlockCondition'
)
BEGIN
    CREATE INDEX [IX_TrainingPrograms_ExamUnlockVideoId] ON [TrainingPrograms] ([ExamUnlockVideoId]);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910180615_AddProgramExamUnlockCondition'
)
BEGIN
    EXEC(N'ALTER TABLE [TrainingPrograms] ADD CONSTRAINT [CK_TrainingPrograms_ExamUnlock] CHECK (([ExamUnlockCondition] = 0 AND [ExamUnlockVideoId] IS NULL) OR ([ExamUnlockCondition] = 1 AND [ExamUnlockVideoId] IS NOT NULL))');
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910180615_AddProgramExamUnlockCondition'
)
BEGIN
    ALTER TABLE [TrainingPrograms] ADD CONSTRAINT [FK_TrainingPrograms_TrainingVideos_ExamUnlockVideoId] FOREIGN KEY ([ExamUnlockVideoId]) REFERENCES [TrainingVideos] ([Id]) ON DELETE NO ACTION;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20260910180615_AddProgramExamUnlockCondition'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20260910180615_AddProgramExamUnlockCondition', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013173443_AddTypedAnswerReview'
)
BEGIN
    ALTER TABLE [QuizResults] ADD [ManualMarksJson] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013173443_AddTypedAnswerReview'
)
BEGIN
    ALTER TABLE [QuizResults] ADD [RequiresReview] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013173443_AddTypedAnswerReview'
)
BEGIN
    ALTER TABLE [QuizResults] ADD [ReviewedAt] datetime2 NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013173443_AddTypedAnswerReview'
)
BEGIN
    ALTER TABLE [QuizResults] ADD [ReviewedBy] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013173443_AddTypedAnswerReview'
)
BEGIN
    ALTER TABLE [QuizQuestions] ADD [IsTypedAnswer] bit NOT NULL DEFAULT CAST(0 AS bit);
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013173443_AddTypedAnswerReview'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261013173443_AddTypedAnswerReview', N'10.0.13');
END;

COMMIT;
GO

BEGIN TRANSACTION;
IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013183446_PersistShuffledQuestionOrder'
)
BEGIN
    ALTER TABLE [QuizResults] ADD [QuestionOrderJson] nvarchar(max) NULL;
END;

IF NOT EXISTS (
    SELECT * FROM [__EFMigrationsHistory]
    WHERE [MigrationId] = N'20261013183446_PersistShuffledQuestionOrder'
)
BEGIN
    INSERT INTO [__EFMigrationsHistory] ([MigrationId], [ProductVersion])
    VALUES (N'20261013183446_PersistShuffledQuestionOrder', N'10.0.13');
END;

COMMIT;
GO

