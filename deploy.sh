#!/bin/bash
# deploy.sh - Deploy Expense Management System infrastructure and application
# This script deploys the App Service, SQL Database, and application code
# Does NOT deploy GenAI resources - use deploy-with-chat.sh for AI features

set -e

# Configuration - Update these values
RESOURCE_GROUP="rg-expensemgmt-demo"
LOCATION="uksouth"
ADMIN_LOGIN="${ADMIN_LOGIN:-$(az ad signed-in-user show --query userPrincipalName -o tsv)}"
ADMIN_OBJECT_ID="${ADMIN_OBJECT_ID:-$(az ad signed-in-user show --query id -o tsv)}"

echo "=========================================="
echo "Expense Management System Deployment"
echo "=========================================="
echo "Resource Group: $RESOURCE_GROUP"
echo "Location: $LOCATION"
echo "Admin: $ADMIN_LOGIN"
echo ""

# Step 1: Create Resource Group
echo "Step 1: Creating resource group..."
az group create --name "$RESOURCE_GROUP" --location "$LOCATION" --output none
echo "  ✓ Resource group created"

# Step 2: Deploy Infrastructure (without GenAI)
echo ""
echo "Step 2: Deploying infrastructure..."
DEPLOYMENT_OUTPUT=$(az deployment group create \
    --resource-group "$RESOURCE_GROUP" \
    --template-file infrastructure/main.bicep \
    --parameters adminObjectId="$ADMIN_OBJECT_ID" adminLogin="$ADMIN_LOGIN" deployGenAI=false \
    --query "properties.outputs" \
    --output json)

# Extract outputs
APP_SERVICE_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.appServiceName.value')
APP_SERVICE_URL=$(echo $DEPLOYMENT_OUTPUT | jq -r '.appServiceUrl.value')
SQL_SERVER_FQDN=$(echo $DEPLOYMENT_OUTPUT | jq -r '.sqlServerFQDN.value')
SQL_DATABASE_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.sqlDatabaseName.value')
MANAGED_IDENTITY_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.managedIdentityName.value')
MANAGED_IDENTITY_CLIENT_ID=$(echo $DEPLOYMENT_OUTPUT | jq -r '.managedIdentityClientId.value')

echo "  ✓ Infrastructure deployed"
echo "    App Service: $APP_SERVICE_NAME"
echo "    SQL Server: $SQL_SERVER_FQDN"

# Step 3: Configure App Service settings
echo ""
echo "Step 3: Configuring App Service settings..."
CONNECTION_STRING="Server=tcp:${SQL_SERVER_FQDN},1433;Database=${SQL_DATABASE_NAME};Authentication=Active Directory Managed Identity;User Id=${MANAGED_IDENTITY_CLIENT_ID};Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;"

az webapp config appsettings set \
    --name "$APP_SERVICE_NAME" \
    --resource-group "$RESOURCE_GROUP" \
    --settings \
        "ConnectionStrings__DefaultConnection=$CONNECTION_STRING" \
        "ManagedIdentityClientId=$MANAGED_IDENTITY_CLIENT_ID" \
        "AZURE_CLIENT_ID=$MANAGED_IDENTITY_CLIENT_ID" \
    --output none
echo "  ✓ App Service settings configured"

# Step 4: Wait for SQL Server to be ready
echo ""
echo "Step 4: Waiting 30 seconds for SQL Server to be fully ready..."
sleep 30
echo "  ✓ Wait complete"

# Step 5: Add current IP to SQL firewall
echo ""
echo "Step 5: Adding current IP to SQL firewall..."
SQL_SERVER_NAME=$(echo $SQL_SERVER_FQDN | cut -d'.' -f1)
CURRENT_IP=$(curl -s https://api.ipify.org)
az sql server firewall-rule create \
    --resource-group "$RESOURCE_GROUP" \
    --server "$SQL_SERVER_NAME" \
    --name "DeploymentMachine" \
    --start-ip-address "$CURRENT_IP" \
    --end-ip-address "$CURRENT_IP" \
    --output none 2>/dev/null || echo "  (Firewall rule may already exist)"
echo "  ✓ Firewall rule configured"

# Step 6: Install Python dependencies and run database scripts
echo ""
echo "Step 6: Setting up database..."
pip3 install --quiet pyodbc azure-identity

# Update Python scripts with actual server name
sed -i.bak "s/sql-expensemgmt-UNIQUESUFFIX.database.windows.net/$SQL_SERVER_FQDN/g" scripts/run-sql.py && rm -f scripts/run-sql.py.bak
sed -i.bak "s/sql-expensemgmt-UNIQUESUFFIX.database.windows.net/$SQL_SERVER_FQDN/g" scripts/run-sql-dbrole.py && rm -f scripts/run-sql-dbrole.py.bak
sed -i.bak "s/sql-expensemgmt-UNIQUESUFFIX.database.windows.net/$SQL_SERVER_FQDN/g" scripts/run-sql-stored-procs.py && rm -f scripts/run-sql-stored-procs.py.bak
sed -i.bak "s/MANAGED-IDENTITY-NAME/$MANAGED_IDENTITY_NAME/g" scripts/script.sql && rm -f scripts/script.sql.bak

echo "  Running database schema import..."
python3 scripts/run-sql.py

echo "  Running database role configuration..."
python3 scripts/run-sql-dbrole.py

echo "  Running stored procedures creation..."
python3 scripts/run-sql-stored-procs.py

echo "  ✓ Database setup complete"

# Step 7: Build and deploy application
echo ""
echo "Step 7: Building and deploying application..."
cd src/ExpenseManagement

# Build the application
dotnet publish -c Release -o ./publish

# Create zip file with correct structure (files at root, not in subfolder)
cd publish
zip -r ../../../app.zip ./*
cd ../../..

# Deploy to App Service
az webapp deploy \
    --resource-group "$RESOURCE_GROUP" \
    --name "$APP_SERVICE_NAME" \
    --src-path ./app.zip \
    --type zip \
    --output none

echo "  ✓ Application deployed"

# Step 8: Summary
echo ""
echo "=========================================="
echo "Deployment Complete!"
echo "=========================================="
echo ""
echo "Application URL: ${APP_SERVICE_URL}/Index"
echo "Swagger API Docs: ${APP_SERVICE_URL}/swagger"
echo ""
echo "Resources created:"
echo "  - Resource Group: $RESOURCE_GROUP"
echo "  - App Service: $APP_SERVICE_NAME"
echo "  - SQL Server: $SQL_SERVER_NAME"
echo "  - Database: $SQL_DATABASE_NAME"
echo "  - Managed Identity: $MANAGED_IDENTITY_NAME"
echo ""
echo "NOTE: Navigate to ${APP_SERVICE_URL}/Index (not just the root URL)"
echo ""
echo "To enable AI chat features, run: ./deploy-with-chat.sh"
