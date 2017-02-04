CREATE TABLE [dbo].[MessagesMonitoring]
(
[Id] [bigint] NOT NULL IDENTITY(1, 1),
[ContentId] [bigint] NULL,
[MessageType] [int] NOT NULL,
[TotalMessages] [int] NOT NULL,
[TotalSuccessfulySended] [int] NOT NULL,
[TotalFailed] [int] NOT NULL,
[TotalWithoutCharge] [int] NOT NULL,
[Status] [int] NOT NULL,
[DateCreated] [datetime] NOT NULL,
[PersianDateCreated] [nvarchar] (50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
[Tag] [int] NULL
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[MessagesMonitoring] ADD CONSTRAINT [PK_MessagesMonitoring] PRIMARY KEY CLUSTERED  ([Id]) ON [PRIMARY]
GO
