CREATE TABLE [dbo].[Operators]
(
[Id] [bigint] NOT NULL,
[OperatorName] [nvarchar] (50) COLLATE Persian_100_CI_AI NOT NULL
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Operators] ADD CONSTRAINT [PK_OperatorsList] PRIMARY KEY CLUSTERED  ([Id]) ON [PRIMARY]
GO
