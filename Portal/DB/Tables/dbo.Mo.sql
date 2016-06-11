CREATE TABLE [dbo].[Mo]
(
[Id] [bigint] NOT NULL IDENTITY(1, 1),
[MobileNumber] [nvarchar] (50) COLLATE Persian_100_CI_AI NOT NULL,
[ShortCode] [nchar] (10) COLLATE Persian_100_CI_AI NOT NULL,
[ReceivedTime] [datetime] NOT NULL,
[MessageId] [nvarchar] (100) COLLATE Persian_100_CI_AI NULL
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Mo] ADD CONSTRAINT [PK_Mo] PRIMARY KEY CLUSTERED  ([Id]) ON [PRIMARY]
GO
