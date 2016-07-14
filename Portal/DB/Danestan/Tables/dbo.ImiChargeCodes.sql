CREATE TABLE [dbo].[ImiChargeCodes]
(
[Id] [bigint] NOT NULL IDENTITY(1, 1),
[ChargeCode] [int] NOT NULL,
[Price] [int] NOT NULL,
[ChargeKey] [nvarchar] (100) COLLATE SQL_Latin1_General_CP1_CI_AS NULL
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[ImiChargeCodes] ADD CONSTRAINT [PK_ImiChargeCodes] PRIMARY KEY CLUSTERED  ([Id]) ON [PRIMARY]
GO
