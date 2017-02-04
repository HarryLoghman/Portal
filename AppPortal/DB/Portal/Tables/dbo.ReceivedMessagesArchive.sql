CREATE TABLE [dbo].[ReceivedMessagesArchive]
(
[Id] [bigint] NOT NULL IDENTITY(1, 1),
[MobileNumber] [nvarchar] (50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
[ShortCode] [nvarchar] (50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
[ReceivedTime] [datetime] NOT NULL,
[MessageId] [nvarchar] (100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
[IsProcessed] [bit] NOT NULL,
[Content] [nvarchar] (max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
ALTER TABLE [dbo].[ReceivedMessagesArchive] ADD CONSTRAINT [PK_ReceievedMessagesArchive] PRIMARY KEY CLUSTERED  ([Id]) ON [PRIMARY]
GO
