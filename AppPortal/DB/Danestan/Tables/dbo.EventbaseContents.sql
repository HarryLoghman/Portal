CREATE TABLE [dbo].[EventbaseContents]
(
[Id] [bigint] NOT NULL IDENTITY(1, 1),
[Content] [nvarchar] (max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
[DateCreated] [datetime] NOT NULL,
[PersianDateCreated] [nvarchar] (50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
[Price] [int] NOT NULL,
[Point] [int] NOT NULL,
[SubscriberNotSendedMoInDays] [int] NOT NULL,
[IsAddingMessagesToSendQueue] [bit] NOT NULL,
[IsAddedToSendQueueFinished] [bit] NOT NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
ALTER TABLE [dbo].[EventbaseContents] ADD CONSTRAINT [PK_EventbaseContent] PRIMARY KEY CLUSTERED  ([Id]) ON [PRIMARY]
GO
