-- ================================================
-- Template generated from Template Explorer using:
-- Create Procedure (New Menu).SQL
--
-- Use the Specify Values for Template Parameters 
-- command (Ctrl-Shift-M) to fill in the parameter 
-- values below.
--
-- This block of comments will not be included in
-- the definition of the procedure.
-- ================================================
SET ANSI_NULLS ON
GO
SET QUOTED_IDENTIFIER ON
GO
-- =============================================
-- Author:		<Author,,Name>
-- Create date: <Create Date,,>
-- Description:	<Description,,>
-- =============================================

CREATE DATABASE Argon
GO
USE Argon
GO
CREATE TABLE LoginInfo
(
	ID int IDENTITY PRIMARY KEY,
	username varchar(20) NOT NULL,
	password varchar(20) NOT NULL
)

CREATE TABLE AccountInfo
(
	ID int FOREIGN KEY REFERENCES LoginInfo(ID),
	xLoc int NOT NULL,
	yLoc int NOT NULL,
	currency money NOT NULL,
	strength int NOT NULL,
	MaxHealth int NOT NULL,
	CurrentHealth int NOT NULL
)

GO
CREATE FUNCTION CheckLoginCredentials(
	-- Add the parameters for the stored procedure here
	@username varchar(20), 
	@password varchar(20)
	)
	RETURNS int
BEGIN
    -- Insert statements for procedure here
	IF((SELECT COUNT(*) FROM LoginInfo  WHERE username = @username AND [password] = @password) = 1)
		RETURN (SELECT ID FROM LoginInfo WHERE username = @username AND [password] = @password);
	RETURN 0;
END
GO

CREATE PROCEDURE CreateAccount(
	@username varchar(20), 
	@password varchar(20)
	)
	AS
	BEGIN
		IF (SELECT COUNT(*) FROM LoginInfo  WHERE username = @username) = 0
		BEGIN
			INSERT INTO LoginInfo(username, [password])
				VALUES(@username, @password);
			
			INSERT INTO AccountInfo(ID, xLoc, yLoc, currency, strength, MaxHealth, CurrentHealth)
				VALUES((SELECT ID FROM LoginInfo WHERE username = @username),0,0,0,0,0,0)
			SELECT 1;
		END
		ELSE
			SELECT 0;
	END
GO

CREATE PROCEDURE UpdateAccountLocation
(
	@username varchar(20),
	@xLoc INT,
	@yLoc INT
)
AS
BEGIN
	UPDATE AccountInfo
		SET xLoc = @xLoc, yLoc = @yLoc
		WHERE ID = (SELECT ID FROM LoginInfo WHERE @username = username)		
END
GO

CREATE FUNCTION GetLocation
(
	@AccountID INT
)
RETURNS TABLE
	RETURN (SELECT xLoc AS X, yLoc AS Y FROM AccountInfo WHERE ID = @AccountID);
GO

EXEC CreateAccount @username = 'fermak', @password = 'password'
EXEC CreateAccount @username = 'sermak', @password = 'password'
SELECT * FROM dbo.LoginInfo
SELECT * FROM dbo.GetLocation(1)

GO