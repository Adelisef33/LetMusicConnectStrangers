-- This script makes the Comment column nullable in the Reviews table
-- Run this in Visual Studio's SQL Server Object Explorer

USE [LetMusicConnectStrangers]
GO

-- Check if the column exists and is NOT NULL
IF EXISTS (
    SELECT * FROM INFORMATION_SCHEMA.COLUMNS 
    WHERE TABLE_NAME = 'Reviews' 
    AND COLUMN_NAME = 'Comment' 
    AND IS_NULLABLE = 'NO'
)
BEGIN
    PRINT 'Altering Comment column to allow NULL values...'
    
    ALTER TABLE [dbo].[Reviews]
    ALTER COLUMN [Comment] NVARCHAR(1000) NULL
    
    PRINT 'Comment column is now nullable!'
END
ELSE
BEGIN
    PRINT 'Comment column is already nullable or does not exist.'
END
GO

-- Verify the change
SELECT 
    COLUMN_NAME,
    DATA_TYPE,
    CHARACTER_MAXIMUM_LENGTH,
    IS_NULLABLE
FROM INFORMATION_SCHEMA.COLUMNS
WHERE TABLE_NAME = 'Reviews' AND COLUMN_NAME = 'Comment'
GO
