-- Create Notifications table
IF NOT EXISTS (SELECT * FROM sys.objects WHERE object_id = OBJECT_ID(N'[dbo].[Notifications]') AND type in (N'U'))
BEGIN
    CREATE TABLE [dbo].[Notifications] (
        [NotificationId] INT IDENTITY(1,1) NOT NULL,
        [UserId] INT NOT NULL,
        [Title] NVARCHAR(200) NOT NULL,
        [Message] NVARCHAR(1000) NOT NULL,
        [Type] NVARCHAR(50) NULL,
        [Link] NVARCHAR(255) NULL,
        [IsRead] BIT NOT NULL DEFAULT 0,
        [CreatedAt] DATETIME NOT NULL DEFAULT GETUTCDATE(),
        CONSTRAINT [PK_Notifications] PRIMARY KEY CLUSTERED ([NotificationId] ASC),
        CONSTRAINT [FK_Notifications_Users] FOREIGN KEY ([UserId]) REFERENCES [dbo].[Users] ([UserId])
    );
END
GO
