CREATE TABLE [dbo].[SubscribersPoints]
(
[Id] [bigint] NOT NULL IDENTITY(1, 1),
[SubscriberId] [bigint] NOT NULL,
[ServiceId] [bigint] NOT NULL,
[Point] [int] NOT NULL
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[SubscribersPoints] ADD CONSTRAINT [PK_SubscribersPoints] PRIMARY KEY CLUSTERED  ([Id]) ON [PRIMARY]
GO
ALTER TABLE [dbo].[SubscribersPoints] ADD CONSTRAINT [FK_SubscribersPoints_Services] FOREIGN KEY ([ServiceId]) REFERENCES [dbo].[Services] ([Id])
GO
ALTER TABLE [dbo].[SubscribersPoints] ADD CONSTRAINT [FK_SubscribersPoints_Subscribers] FOREIGN KEY ([SubscriberId]) REFERENCES [dbo].[Subscribers] ([Id])
GO
