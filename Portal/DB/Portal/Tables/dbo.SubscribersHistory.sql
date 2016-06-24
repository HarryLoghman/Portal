CREATE TABLE [dbo].[SubscribersHistory]
(
[Id] [bigint] NOT NULL IDENTITY(1, 1),
[MobileNumber] [nvarchar] (50) COLLATE Persian_100_CI_AI NOT NULL,
[ServiceId] [bigint] NULL,
[ShortCode] [nvarchar] (50) COLLATE Persian_100_CI_AI NULL,
[MCIServiceId] [int] NULL,
[ServiceName] [nvarchar] (100) COLLATE Persian_100_CI_AI NULL,
[ServiceStatusForSubscriber] [int] NULL,
[Date] [date] NULL,
[Time] [time] NULL,
[WhoChangedSubscriberStatus] [int] NULL,
[InvalidContent] [nvarchar] (max) COLLATE Persian_100_CI_AI NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
ALTER TABLE [dbo].[SubscribersHistory] ADD CONSTRAINT [PK_History] PRIMARY KEY CLUSTERED  ([Id]) ON [PRIMARY]
GO
