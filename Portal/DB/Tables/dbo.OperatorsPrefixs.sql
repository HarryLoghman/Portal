CREATE TABLE [dbo].[OperatorsPrefixs]
(
[Id] [bigint] NOT NULL IDENTITY(1, 1),
[OperatorId] [int] NOT NULL,
[OperatorPlan] [int] NOT NULL,
[Prefix] [varchar] (50) COLLATE Persian_100_CI_AI NOT NULL
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[OperatorsPrefixs] ADD CONSTRAINT [PK_OperatorsPrefixs] PRIMARY KEY CLUSTERED  ([Id]) ON [PRIMARY]
GO
