CREATE TABLE [dbo].[AutochargeContents]
(
[Id] [bigint] NOT NULL IDENTITY(1, 1),
[Content] [nvarchar] (max) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
[SendDate] [datetime] NOT NULL,
[DateCreated] [datetime] NOT NULL,
[Point] [int] NOT NULL,
[Price] [int] NOT NULL,
[IsAddedToSendQueue] [bit] NOT NULL,
[PersianDateCreated] [nvarchar] (50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
[PersianSendDate] [nvarchar] (50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
ALTER TABLE [dbo].[AutochargeContents] ADD CONSTRAINT [PK_AutochargeContents] PRIMARY KEY CLUSTERED  ([Id]) ON [PRIMARY]
GO
