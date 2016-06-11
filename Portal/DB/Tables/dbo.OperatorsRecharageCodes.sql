CREATE TABLE [dbo].[OperatorsRecharageCodes]
(
[Id] [bigint] NOT NULL IDENTITY(1, 1),
[OperatorId] [bigint] NOT NULL,
[ChargeSerialNumber] [nvarchar] (100) COLLATE Persian_100_CI_AI NOT NULL,
[ChargeCode] [nvarchar] (100) COLLATE Persian_100_CI_AI NOT NULL,
[DateAdded] [datetime] NOT NULL,
[DateUsed] [datetime] NULL,
[PersianDateAdded] [nvarchar] (50) COLLATE Persian_100_CI_AI NOT NULL,
[PersianDateUsed] [nvarchar] (50) COLLATE Persian_100_CI_AI NULL,
[IsUsed] [bit] NOT NULL
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[OperatorsRecharageCodes] ADD CONSTRAINT [PK_OperatorsRecharageCodes] PRIMARY KEY CLUSTERED  ([Id]) ON [PRIMARY]
GO
