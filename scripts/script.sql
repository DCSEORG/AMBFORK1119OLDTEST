-- Script to configure managed identity database access
-- Replace MANAGED-IDENTITY-NAME with actual managed identity name before running

-- Drop and recreate the managed identity user with correct SID
IF EXISTS (SELECT * FROM sys.database_principals WHERE name = 'MANAGED-IDENTITY-NAME')
BEGIN
    DROP USER [MANAGED-IDENTITY-NAME];
END
GO

CREATE USER [MANAGED-IDENTITY-NAME] FROM EXTERNAL PROVIDER;
GO

ALTER ROLE db_datareader ADD MEMBER [MANAGED-IDENTITY-NAME];
GO

ALTER ROLE db_datawriter ADD MEMBER [MANAGED-IDENTITY-NAME];
GO

GRANT EXECUTE TO [MANAGED-IDENTITY-NAME];
GO

PRINT 'Managed identity database roles configured successfully';
GO
