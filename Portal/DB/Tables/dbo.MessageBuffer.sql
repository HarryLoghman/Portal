CREATE TABLE [dbo].[MessageBuffer]
(
[Id] [bigint] NOT NULL IDENTITY(1, 1),
[MobileNumber] [nvarchar] (50) COLLATE Persian_100_CI_AI NULL,
[Content] [nvarchar] (max) COLLATE Persian_100_CI_AI NOT NULL,
[ProcessStatus] [int] NULL,
[SentDate] [datetime] NULL,
[ReferenceId] [nvarchar] (100) COLLATE Persian_100_CI_AI NULL,
[TimesTryingToSend] [int] NULL,
[MessageType] [int] NULL,
[ServiceId] [bigint] NULL,
[SubscriberId] [bigint] NULL,
[ContentId] [bigint] NULL,
[ImiChargeCode] [int] NULL,
[ImiMessageType] [int] NULL,
[DeliveryStatus] [nvarchar] (100) COLLATE Persian_100_CI_AI NULL,
[DeliveryDate] [datetime] NULL,
[OperatorId] [int] NULL,
[DateAddedToQueue] [datetime] NULL,
[AggregatorServiceId] [int] NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
ALTER TABLE [dbo].[MessageBuffer] ADD CONSTRAINT [PK_MessageBuffer] PRIMARY KEY CLUSTERED  ([Id]) ON [PRIMARY]
GO
