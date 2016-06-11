CREATE TABLE [dbo].[ServiceShortCodes]
(
[Id] [bigint] NOT NULL IDENTITY(1, 1),
[ServiceId] [bigint] NOT NULL,
[ShortCode] [nvarchar] (50) COLLATE Persian_100_CI_AI NULL
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[ServiceShortCodes] ADD CONSTRAINT [PK_ServiceShortCodes] PRIMARY KEY CLUSTERED  ([Id]) ON [PRIMARY]
GO
ALTER TABLE [dbo].[ServiceShortCodes] ADD CONSTRAINT [FK_ServiceShortCodes_Services] FOREIGN KEY ([ServiceId]) REFERENCES [dbo].[Services] ([Id])
GO
