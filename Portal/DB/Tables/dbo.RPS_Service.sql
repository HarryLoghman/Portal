CREATE TABLE [dbo].[RPS_Service]
(
[Id] [bigint] NOT NULL IDENTITY(1, 1),
[ServiceId] [bigint] NOT NULL,
[HamrahAggregatorServiceId] [int] NULL
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[RPS_Service] ADD CONSTRAINT [PK_RPS_Service] PRIMARY KEY CLUSTERED  ([Id]) ON [PRIMARY]
GO
ALTER TABLE [dbo].[RPS_Service] ADD CONSTRAINT [FK_RPS_Service_Services] FOREIGN KEY ([ServiceId]) REFERENCES [dbo].[Services] ([Id])
GO
