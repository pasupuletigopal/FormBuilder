CREATE TABLE [dbo].[ApiEnvironments] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [Name] NVARCHAR(100) NOT NULL,
    [Variables] NVARCHAR(MAX) NOT NULL DEFAULT '{}',
    [SortOrder] INT NOT NULL DEFAULT 0,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE TABLE [dbo].[ApiCollections] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [Name] NVARCHAR(200) NOT NULL,
    [Description] NVARCHAR(MAX) NULL,
    [SortOrder] INT NOT NULL DEFAULT 0,
    [IsActive] BIT NOT NULL DEFAULT 1,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE()
);

CREATE TABLE [dbo].[ApiFolders] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [CollectionId] INT NOT NULL,
    [ParentFolderId] INT NULL,
    [Name] NVARCHAR(200) NOT NULL,
    [SortOrder] INT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [FK_ApiFolders_ApiCollections] FOREIGN KEY ([CollectionId]) REFERENCES [dbo].[ApiCollections]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ApiFolders_ParentFolder] FOREIGN KEY ([ParentFolderId]) REFERENCES [dbo].[ApiFolders]([Id])
);

CREATE TABLE [dbo].[ApiRequests] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [CollectionId] INT NOT NULL,
    [FolderId] INT NULL,
    [Name] NVARCHAR(300) NOT NULL,
    [Method] NVARCHAR(10) NOT NULL DEFAULT 'GET',
    [Url] NVARCHAR(MAX) NOT NULL,
    [Headers] NVARCHAR(MAX) NULL,
    [QueryParams] NVARCHAR(MAX) NULL,
    [AuthType] NVARCHAR(50) NULL,
    [AuthConfig] NVARCHAR(MAX) NULL,
    [Body] NVARCHAR(MAX) NULL,
    [BodyType] NVARCHAR(20) NULL DEFAULT 'json',
    [SortOrder] INT NOT NULL DEFAULT 0,
    [CreatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    [UpdatedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [FK_ApiRequests_ApiCollections] FOREIGN KEY ([CollectionId]) REFERENCES [dbo].[ApiCollections]([Id]) ON DELETE CASCADE,
    CONSTRAINT [FK_ApiRequests_ApiFolders] FOREIGN KEY ([FolderId]) REFERENCES [dbo].[ApiFolders]([Id])
);

CREATE TABLE [dbo].[ApiRequestHistory] (
    [Id] INT IDENTITY(1,1) PRIMARY KEY,
    [RequestId] INT NOT NULL,
    [StatusCode] INT NOT NULL,
    [ResponseHeaders] NVARCHAR(MAX) NULL,
    [ResponseBody] NVARCHAR(MAX) NULL,
    [ResponseSizeBytes] BIGINT NOT NULL DEFAULT 0,
    [DurationMs] INT NOT NULL DEFAULT 0,
    [ExecutedAt] DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    CONSTRAINT [FK_ApiRequestHistory_ApiRequests] FOREIGN KEY ([RequestId]) REFERENCES [dbo].[ApiRequests]([Id]) ON DELETE CASCADE
);
