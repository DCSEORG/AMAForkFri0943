#!/bin/bash
set -e

# ===================================================================
# Expense Management App - GenAI Chat UI Deployment Script
# ===================================================================
# This script adds GenAI features to the deployed application
# Run this AFTER deploy.sh has completed successfully
# ===================================================================

echo "=========================================="
echo "GenAI Chat UI - Deployment"
echo "=========================================="

# Configuration
RESOURCE_GROUP="rg-expenseapp-dev"
LOCATION="uksouth"

# Check if resource group exists
if ! az group show --name $RESOURCE_GROUP &>/dev/null; then
    echo "Error: Resource group $RESOURCE_GROUP not found."
    echo "Please run deploy.sh first to create the base infrastructure."
    exit 1
fi

# Get existing deployment outputs
echo ""
echo "Retrieving existing infrastructure details..."
EXISTING_OUTPUT=$(az deployment group show \
  --resource-group $RESOURCE_GROUP \
  --name appServiceDeployment \
  --query 'properties.outputs' \
  --output json)

WEB_APP_NAME=$(echo $EXISTING_OUTPUT | jq -r '.webAppName.value')
MANAGED_IDENTITY_CLIENT_ID=$(echo $EXISTING_OUTPUT | jq -r '.managedIdentityClientId.value')

# Deploy GenAI infrastructure
echo ""
echo "Deploying GenAI infrastructure (Azure OpenAI + AI Search)..."
DEPLOYMENT_OUTPUT=$(az deployment group create \
  --resource-group $RESOURCE_GROUP \
  --template-file infrastructure/main.bicep \
  --parameters location=$LOCATION deployGenAI=true \
  --query 'properties.outputs' \
  --output json)

echo "GenAI deployment completed!"

# Extract GenAI outputs
OPENAI_ENDPOINT=$(echo $DEPLOYMENT_OUTPUT | jq -r '.openAIEndpoint.value')
OPENAI_MODEL_NAME=$(echo $DEPLOYMENT_OUTPUT | jq -r '.openAIModelName.value')
SEARCH_ENDPOINT=$(echo $DEPLOYMENT_OUTPUT | jq -r '.searchEndpoint.value')

echo ""
echo "GenAI Deployment Outputs:"
echo "  OpenAI Endpoint: $OPENAI_ENDPOINT"
echo "  OpenAI Model: $OPENAI_MODEL_NAME"
echo "  Search Endpoint: $SEARCH_ENDPOINT"

# Configure App Settings with GenAI configuration
echo ""
echo "Configuring GenAI settings in App Service..."
az webapp config appsettings set \
  --resource-group $RESOURCE_GROUP \
  --name $WEB_APP_NAME \
  --settings \
    "OpenAI__Endpoint=$OPENAI_ENDPOINT" \
    "OpenAI__DeploymentName=$OPENAI_MODEL_NAME" \
    "AzureSearch__Endpoint=$SEARCH_ENDPOINT" \
    "ManagedIdentityClientId=$MANAGED_IDENTITY_CLIENT_ID" \
  --output none

echo ""
echo "=========================================="
echo "GenAI Chat UI Deployment Complete!"
echo "=========================================="
echo ""
echo "The application now has GenAI-powered chat capabilities."
echo "Access the chat interface at: ${WEB_APP_NAME}.azurewebsites.net/Chat"
echo ""
echo "Configuration:"
echo "  - Azure OpenAI (GPT-4o) deployed in Sweden Central"
echo "  - Azure AI Search for RAG functionality"
echo "  - Managed Identity authentication (no API keys)"
