CREATE TABLE [dbo].[RPS_SubscribersAdditionalInfo]
(
[Id] [bigint] NOT NULL IDENTITY(1, 1),
[SubscriberId] [bigint] NOT NULL,
[Point] [int] NULL,
[UniqueId] [nvarchar] (10) COLLATE Persian_100_CI_AI NULL,
[ReferralId] [nvarchar] (10) COLLATE Persian_100_CI_AI NULL,
[IsRechargeThroughReferall] [bit] NULL,
[RechargeThroughReferall] [nvarchar] (10) COLLATE Persian_100_CI_AI NULL,
[TimesWinned] [int] NULL,
[CurrentGameWinned] [int] NULL,
[TimesChargeWinned] [int] NULL
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[RPS_SubscribersAdditionalInfo] ADD CONSTRAINT [PK_RPS_SubscribersAdditionalInfo] PRIMARY KEY CLUSTERED  ([Id]) ON [PRIMARY]
GO
ALTER TABLE [dbo].[RPS_SubscribersAdditionalInfo] ADD CONSTRAINT [FK_RPS_SubscribersAdditionalInfo_Subscribers] FOREIGN KEY ([SubscriberId]) REFERENCES [dbo].[Subscribers] ([Id])
GO
