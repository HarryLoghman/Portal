CREATE TABLE [dbo].[ServiceInfo]
(
[Id] [bigint] NOT NULL IDENTITY(1, 1),
[AggregatorId] [bigint] NOT NULL,
[ServiceId] [bigint] NOT NULL,
[ShortCode] [nvarchar] (50) COLLATE Persian_100_CI_AI NOT NULL,
[AggregatorServiceId] [int] NULL
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[ServiceInfo] ADD CONSTRAINT [PK_ServiceShortCodes] PRIMARY KEY CLUSTERED  ([Id]) ON [PRIMARY]
GO
ALTER TABLE [dbo].[ServiceInfo] ADD CONSTRAINT [FK_ServiceInfo_Aggregators] FOREIGN KEY ([AggregatorId]) REFERENCES [dbo].[Aggregators] ([Id])
GO
ALTER TABLE [dbo].[ServiceInfo] ADD CONSTRAINT [FK_ServiceShortCodes_Services] FOREIGN KEY ([ServiceId]) REFERENCES [dbo].[Services] ([Id])
GO
