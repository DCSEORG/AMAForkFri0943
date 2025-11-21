# Deployment Instructions

This document provides step-by-step instructions for deploying the Expense Management Application to Azure.

## Prerequisites

- Azure CLI installed and logged in (`az login`)
- Azure subscription with appropriate permissions
- .NET 8.0 SDK (for local development)
- Python 3.x with pip (for SQL setup script)

## Quick Start

Deploy the entire application with these commands:

```bash
# 1. Set your Azure subscription
az account set --subscription "YOUR_SUBSCRIPTION_ID"

# 2. Deploy core infrastructure and application
./deploy.sh

# 3. (Optional) Add GenAI chat features
./deploy-chatui.sh
```

## Detailed Deployment Steps

### Step 1: Core Infrastructure Deployment

The `deploy.sh` script will:
1. Create a resource group (`rg-expenseapp-dev`)
2. Deploy App Service Plan (B1 Basic tier)
3. Deploy Web App with .NET 8.0
4. Create User-Assigned Managed Identity
5. Configure database connection strings
6. Set up database permissions (if Python packages available)
7. Deploy application code from app.zip

```bash
./deploy.sh
```

**Expected Duration**: 5-10 minutes

**Outputs**:
- Web App Name
- Web App URL (remember to navigate to /Index)
- Managed Identity details

### Step 2: GenAI Features (Optional)

The `deploy-chatui.sh` script will:
1. Deploy Azure OpenAI service (GPT-4o in Sweden Central)
2. Deploy Azure AI Search (Basic tier)
3. Configure Managed Identity role assignments
4. Update App Service settings with GenAI endpoints

```bash
./deploy-chatui.sh
```

**Expected Duration**: 10-15 minutes (model deployment takes time)

**Outputs**:
- OpenAI Endpoint
- Search Endpoint
- Model deployment name (gpt-4o)

## Manual Database Setup (If Automated Setup Fails)

If the Python script doesn't run automatically:

1. Install Python dependencies:
   ```bash
   pip3 install pyodbc azure-identity
   ```

2. Update `script.sql` with your managed identity name:
   ```bash
   # Find the managed identity name from deployment output
   # Replace MANAGED-IDENTITY-NAME in script.sql
   ```

3. Run the SQL setup:
   ```bash
   python3 run-sql.py
   ```

## Configuration Files

### Infrastructure (Bicep)
- `infrastructure/main.bicep` - Main orchestrator
- `infrastructure/app-service.bicep` - App Service and Managed Identity
- `infrastructure/genai.bicep` - OpenAI and AI Search resources

### Application
- `ExpenseManagementApp/` - ASP.NET Core application
- `app.zip` - Deployment package (pre-built)

### Database
- `Database-Schema/database_schema.sql` - Complete schema with sample data
- `script.sql` - Managed Identity permissions setup
- `run-sql.py` - Python script to execute SQL setup

## Accessing the Application

### Web Interface
```
https://[your-app-name].azurewebsites.net/Index
```

**Important**: Navigate to `/Index` endpoint, not just the root URL!

### API Documentation (Swagger)
```
https://[your-app-name].azurewebsites.net/swagger
```

### AI Chat Assistant (if GenAI deployed)
```
https://[your-app-name].azurewebsites.net/Chat
```

## Application Features

### Available APIs

**Expenses**:
- `GET /api/expenses` - List all expenses (filter by userId, statusId)
- `GET /api/expenses/{id}` - Get specific expense
- `POST /api/expenses` - Create new expense
- `PUT /api/expenses/{id}` - Update expense
- `DELETE /api/expenses/{id}` - Delete expense
- `POST /api/expenses/{id}/submit` - Submit for approval
- `POST /api/expenses/{id}/approve?reviewerId={id}` - Approve expense
- `POST /api/expenses/{id}/reject?reviewerId={id}` - Reject expense

**Reference Data**:
- `GET /api/categories` - List expense categories
- `GET /api/statuses` - List expense statuses
- `GET /api/users` - List users

### Web Pages

- `/Index` - Dashboard and expense list
- `/Chat` - AI-powered chat assistant
- `/swagger` - Interactive API documentation

## Troubleshooting

### Database Connection Issues

If you see dummy data with an error message:
1. Check that the managed identity has been granted database permissions
2. Verify the connection string in App Service configuration
3. Run `python3 run-sql.py` manually to set up permissions

### GenAI Not Working

If AI features aren't available:
1. Check that `deploy-chatui.sh` completed successfully
2. Verify OpenAI endpoint is set in App Service settings:
   ```bash
   az webapp config appsettings list --resource-group rg-expenseapp-dev --name [app-name]
   ```
3. Wait 5-10 minutes after deployment for model provisioning

### Build Issues

If you need to rebuild the application:
```bash
cd ExpenseManagementApp
dotnet publish -c Release -o ./publish
cd publish
zip -r ../../app.zip .
```

## Architecture

See `ARCHITECTURE.md` for detailed architecture diagrams and explanations.

## GenAI Configuration

See `GenAISettings.md` for detailed GenAI configuration and usage information.

## Cost Estimate

**Core Infrastructure** (per month):
- App Service Plan (B1): ~£40
- Existing Azure SQL: N/A (using existing database)

**GenAI Add-On** (per month):
- Azure OpenAI (S0): Pay-per-use (varies by usage)
- Azure AI Search (Basic): ~£65

**Total**: ~£105-150/month for full stack development environment

## Clean Up

To remove all resources:

```bash
az group delete --name rg-expenseapp-dev --yes --no-wait
```

## Support and Documentation

- Azure App Service: https://docs.microsoft.com/azure/app-service/
- Azure OpenAI: https://docs.microsoft.com/azure/cognitive-services/openai/
- Azure SQL: https://docs.microsoft.com/azure/azure-sql/
- Managed Identity: https://docs.microsoft.com/azure/active-directory/managed-identities-azure-resources/

## Security Best Practices

✓ All services use Managed Identity authentication
✓ No API keys or secrets stored in code
✓ HTTPS enforced on all endpoints
✓ TLS 1.2 minimum
✓ Role-based access control (RBAC) for all Azure services
✓ Separate deployment of GenAI for optional security boundary
