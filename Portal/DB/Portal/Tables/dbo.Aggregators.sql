CREATE TABLE [dbo].[Aggregators]
(
[Id] [bigint] NOT NULL IDENTITY(1, 1),
[OperatorId] [bigint] NOT NULL,
[AggregatorName] [nvarchar] (100) COLLATE Persian_100_CI_AI NOT NULL,
[AggregatorUsername] [nvarchar] (100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL,
[AggregatorPassword] [nvarchar] (100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Aggregators] ADD CONSTRAINT [PK_AggregatorList] PRIMARY KEY CLUSTERED  ([Id]) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Aggregators] ADD CONSTRAINT [FK_Aggregators_Operators] FOREIGN KEY ([OperatorId]) REFERENCES [dbo].[Operators] ([Id])
GO
