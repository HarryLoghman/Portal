SET QUOTED_IDENTIFIER ON
GO
SET ANSI_NULLS ON
GO
CREATE PROCEDURE [dbo].[ChangeMessageStatus] @MessageType INT, @ContentId BIGINT, @Tag INT, @PersianDate NVARCHAR(50), @CurrentStatus INT, @DesiredProcessStatus INT
AS
BEGIN
	IF @MessageType = 3 
		Begin
		UPDATE AutochargeMessagesBuffer SET ProcessStatus = @DesiredProcessStatus WHERE ProcessStatus = @CurrentStatus AND PersianDateAddedToQueue = @PersianDate AND Tag = @Tag
		UPDATE MessagesMonitoring SET Status = @DesiredProcessStatus WHERE MessageType = @MessageType AND PersianDateCreated = @PersianDate AND Tag = @Tag
		End
	ELSE
		Begin
		UPDATE EventbaseMessagesBuffer SET ProcessStatus = @DesiredProcessStatus WHERE PersianDateAddedToQueue = @PersianDate AND ContentId = @ContentId
		UPDATE MessagesMonitoring SET Status = @DesiredProcessStatus WHERE MessageType = @MessageType AND PersianDateCreated = @PersianDate AND Tag = @Tag
		End
	
END
GO
