#!/bin/bash
set -e

# ===================================================================
# Expense Management App - Main Deployment Script
# ===================================================================
# This script deploys the core infrastructure and application
# Run deploy-chatui.sh separately to add GenAI features (optional)
# ===================================================================

echo "=========================================="
echo "Expense Management App - Deployment"
echo "=========================================="

# Configuration
RESOURCE_GROUP="rg-expenseapp-dev"
LOCATION="uksouth"

# Create resource group
echo ""
echo "Creating resource group: $RESOURCE_GROUP"
az group create --name $RESOURCE_GROUP --location $LOCATION

# Deploy infrastructure (App Service + Managed Identity only)
echo ""
echo "Deploying infrastructure..."
DEPLOYMENT_OUTPUT=$(az deployment group create \
  --resource-group $RESOURCE_GROUP \
  --template-file infrastructure/main.bicep \
  --parameters location=$LOCATION deployGenAI=false \
  --query 'properties.outputs' \
  --output json)

echo "Deployment completed!"

# Extract outputs
WEB_APP_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.webAppName.value')
WEB_APP_URL=$(echo $DEPLOYMENT_OUTPUT | jq -r '.webAppUrl.value')
MANAGED_IDENTITY_CLIENT_ID=$(echo $DEPLOYMENT_OUTPUT | jq -r '.managedIdentityClientId.value')
MANAGED_IDENTITY_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.managedIdentityName.value')

echo ""
echo "Deployment Outputs:"
echo "  Web App Name: $WEB_APP_NAME"
echo "  Web App URL: $WEB_APP_URL"
echo "  Managed Identity Client ID: $MANAGED_IDENTITY_CLIENT_ID"
echo "  Managed Identity Name: $MANAGED_IDENTITY_NAME"

# Configure App Settings (Database connection)
echo ""
echo "Configuring App Settings..."
az webapp config appsettings set \
  --resource-group $RESOURCE_GROUP \
  --name $WEB_APP_NAME \
  --settings \
    "ConnectionStrings__DefaultConnection=Server=tcp:sql-expense-mgmt-xyz.database.windows.net,1433;Database=ExpenseManagementDB;Authentication=Active Directory Managed Identity;User Id=$MANAGED_IDENTITY_CLIENT_ID;Encrypt=True;TrustServerCertificate=False;Connection Timeout=30;" \
    "ManagedIdentityClientId=$MANAGED_IDENTITY_CLIENT_ID" \
  --output none

# Update script.sql with actual managed identity name
echo ""
echo "Updating SQL script with Managed Identity name..."
sed -i "s/MANAGED-IDENTITY-NAME/$MANAGED_IDENTITY_NAME/g" script.sql

# Install required Python packages if not already installed
echo ""
echo "Installing Python dependencies..."
pip3 install --quiet pyodbc azure-identity 2>/dev/null || echo "Python packages already installed or installation failed (continuing anyway)"

# Run the Python script to set up database permissions
echo ""
echo "Setting up database permissions for Managed Identity..."
if [ -f "run-sql.py" ]; then
    python3 run-sql.py || echo "Warning: SQL script execution failed. You may need to run this manually."
else
    echo "Warning: run-sql.py not found. Skipping database permission setup."
fi

# Deploy application if app.zip exists
if [ -f "app.zip" ]; then
    echo ""
    echo "Deploying application code..."
    az webapp deploy \
      --resource-group $RESOURCE_GROUP \
      --name $WEB_APP_NAME \
      --src-path ./app.zip \
      --type zip
    
    echo ""
    echo "=========================================="
    echo "Deployment Complete!"
    echo "=========================================="
    echo ""
    echo "Application URL: $WEB_APP_URL/Index"
    echo ""
    echo "IMPORTANT: Navigate to /Index to view the app"
    echo "           Don't just use the root URL"
    echo ""
    echo "To add GenAI chat features, run: ./deploy-chatui.sh"
else
    echo ""
    echo "=========================================="
    echo "Infrastructure Deployment Complete!"
    echo "=========================================="
    echo ""
    echo "Application URL: $WEB_APP_URL"
    echo ""
    echo "Note: app.zip not found. Build and deploy the application separately."
    echo "To add GenAI chat features, run: ./deploy-chatui.sh"
fi
