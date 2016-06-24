CREATE TABLE [dbo].[Subscribers]
(
[Id] [bigint] NOT NULL IDENTITY(1, 1),
[MobileNumber] [nvarchar] (50) COLLATE Persian_100_CI_AI NOT NULL,
[ServiceId] [bigint] NOT NULL,
[ActivationDate] [datetime] NULL,
[DeactivationDate] [datetime] NULL,
[OnMethod] [nvarchar] (50) COLLATE Persian_100_CI_AI NULL,
[OnKeyword] [nvarchar] (100) COLLATE Persian_100_CI_AI NULL,
[OffMethod] [nvarchar] (50) COLLATE Persian_100_CI_AI NULL,
[OffKeyword] [nvarchar] (100) COLLATE Persian_100_CI_AI NULL,
[PersianActivationDate] [nvarchar] (50) COLLATE Persian_100_CI_AI NULL,
[PersianDeactivationDate] [nvarchar] (50) COLLATE Persian_100_CI_AI NULL,
[MobileOperator] [bigint] NOT NULL,
[OperatorPlan] [bigint] NOT NULL
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Subscribers] ADD CONSTRAINT [PK_Subscribers] PRIMARY KEY CLUSTERED  ([Id]) ON [PRIMARY]
GO
ALTER TABLE [dbo].[Subscribers] ADD CONSTRAINT [FK_Subscribers_Subscribers] FOREIGN KEY ([ServiceId]) REFERENCES [dbo].[Services] ([Id])
GO
