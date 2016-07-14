CREATE TABLE [dbo].[AutochargeTimeTable]
(
[Id] [bigint] NOT NULL IDENTITY(1, 1),
[Tag] [int] NOT NULL,
[SendTime] [time] NOT NULL
) ON [PRIMARY]
GO
ALTER TABLE [dbo].[AutochargeTimeTable] ADD CONSTRAINT [PK_AutochargeTimeTable] PRIMARY KEY CLUSTERED  ([Id]) ON [PRIMARY]
GO
