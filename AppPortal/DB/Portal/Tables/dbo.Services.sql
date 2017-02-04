CREATE TABLE [dbo].[Services]
(
[Id] [bigint] NOT NULL IDENTITY(1, 1),
[Name] [nvarchar] (50) COLLATE Persian_100_CI_AI NOT NULL,
[ServiceCode] [nvarchar] (10) COLLATE Persian_100_CI_AI NOT NULL,
[DateCreated] [datetime] NOT NULL,
[OnKeywords] [nvarchar] (200) COLLATE Persian_100_CI_AI NOT NULL,
[IsServiceActive] [bit] NOT NULL
) ON [PRIMARY]
ALTER TABLE [dbo].[Services] ADD 
CONSTRAINT [PK_Services] PRIMARY KEY CLUSTERED  ([Id]) ON [PRIMARY]
GO
