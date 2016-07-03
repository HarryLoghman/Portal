CREATE TABLE [dbo].[SubscribersPoints]
(
[Id] [bigint] NOT NULL IDENTITY(1, 1),
[SubscriberId] [bigint] NOT NULL,
[Point] [int] NOT NULL
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[SubscribersPoints] ADD CONSTRAINT [PK_SubscribersPoints] PRIMARY KEY CLUSTERED  ([Id]) ON [PRIMARY]
GO
