CREATE TABLE [dbo].[Services]
(
[Id] [bigint] NOT NULL IDENTITY(1, 1),
[Name] [nvarchar] (50) COLLATE Persian_100_CI_AI NOT NULL,
[DateCreated] [datetime] NOT NULL,
[OnKeywords] [nvarchar] (200) COLLATE Persian_100_CI_AI NOT NULL,
[ServiceIsActive] [bit] NOT NULL,
[WelcomeMessage] [nvarchar] (max) COLLATE Persian_100_CI_AI NOT NULL,
[LeaveMessage] [nvarchar] (max) COLLATE Persian_100_CI_AI NOT NULL,
[InvalidContentWhenSubscribed] [nvarchar] (max) COLLATE Persian_100_CI_AI NOT NULL,
[InvalidContentWhenNotSubscribed] [nvarchar] (max) COLLATE Persian_100_CI_AI NOT NULL,
[IsEnabled] [bit] NOT NULL,
[ServiceHelp] [nvarchar] (max) COLLATE Persian_100_CI_AI NOT NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
ALTER TABLE [dbo].[Services] ADD CONSTRAINT [PK_Services] PRIMARY KEY CLUSTERED  ([Id]) ON [PRIMARY]
GO
