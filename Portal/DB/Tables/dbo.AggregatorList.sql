CREATE TABLE [dbo].[AggregatorList]
(
[Id] [bigint] NOT NULL IDENTITY(1, 1),
[OperatorId] [bigint] NOT NULL,
[AggregatorName] [nvarchar] (100) COLLATE Persian_100_CI_AI NOT NULL
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[AggregatorList] ADD CONSTRAINT [PK_AggregatorList] PRIMARY KEY CLUSTERED  ([Id]) ON [PRIMARY]
GO
