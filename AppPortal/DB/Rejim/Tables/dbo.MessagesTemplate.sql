CREATE TABLE [dbo].[MessagesTemplate]
(
[Id] [bigint] NOT NULL IDENTITY(1, 1),
[Title] [nvarchar] (50) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
[PersianTitle] [nvarchar] (100) COLLATE SQL_Latin1_General_CP1_CI_AS NOT NULL,
[Content] [nvarchar] (max) COLLATE SQL_Latin1_General_CP1_CI_AS NULL
) ON [PRIMARY] TEXTIMAGE_ON [PRIMARY]
GO
ALTER TABLE [dbo].[MessagesTemplate] ADD CONSTRAINT [PK_MessagesTemplate] PRIMARY KEY CLUSTERED  ([Id]) ON [PRIMARY]
GO
