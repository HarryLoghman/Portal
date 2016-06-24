CREATE TABLE [dbo].[OperatorsPlans]
(
[Id] [bigint] NOT NULL,
[OperatorPlanName] [nvarchar] (50) COLLATE Persian_100_CI_AI NOT NULL
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[OperatorsPlans] ADD CONSTRAINT [PK_OperatorsPlans] PRIMARY KEY CLUSTERED  ([Id]) ON [PRIMARY]
GO
