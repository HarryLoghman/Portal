CREATE TABLE [dbo].[ReceievedMessages]
(
[Id] [bigint] NOT NULL IDENTITY(1, 1),
[MobileNumber] [nvarchar] (50) COLLATE Persian_100_CI_AI NOT NULL,
[ShortCode] [nvarchar] (50) COLLATE Persian_100_CI_AI NOT NULL,
[ReceivedTime] [datetime] NOT NULL,
[MessageId] [nvarchar] (100) COLLATE Persian_100_CI_AI NULL,
[IsProcessed] [bit] NOT NULL,
[Content] [nvarchar] (max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
ALTER TABLE [dbo].[ReceievedMessages] ADD CONSTRAINT [PK_ReceivedMessages] PRIMARY KEY CLUSTERED  ([Id]) ON [PRIMARY]
GO
