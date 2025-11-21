# Azure Services Architecture Diagram
# Expense Management Application

```
┌─────────────────────────────────────────────────────────────────────────┐
│                          Expense Management System                       │
│                         Cloud-Native Azure Architecture                  │
└─────────────────────────────────────────────────────────────────────────┘

┌──────────────────────────┐
│   Internet User          │
│   (Browser/Client)       │
└────────┬─────────────────┘
         │
         │ HTTPS
         ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                    Azure App Service (Web App)                           │
│  ┌─────────────────────────────────────────────────────────────────┐   │
│  │  • ASP.NET Core 8.0 Razor Pages                                 │   │
│  │  • REST APIs (Expenses, Categories, Users, Statuses)            │   │
│  │  • Swagger/OpenAPI Documentation                                │   │
│  │  • Chat UI Interface                                            │   │
│  └─────────────────────────────────────────────────────────────────┘   │
│                                                                           │
│  Authenticated with:                                                     │
│  ┌────────────────────────────────────────────────┐                     │
│  │  User-Assigned Managed Identity                │                     │
│  │  (mid-AppModAssist-[timestamp])                │                     │
│  └─────────┬──────────────────────────────────────┘                     │
└────────────┼─────────────────────────────────────────────────────────────┘
             │
             │ Uses identity to authenticate
             ▼
┌─────────────────────────────────────────────────────────────────────────┐
│                     Data & AI Services Layer                             │
└─────────────────────────────────────────────────────────────────────────┘
             │
      ┌──────┴──────┬─────────────┬──────────────┐
      │             │             │              │
      ▼             ▼             ▼              ▼
┏━━━━━━━━━━━┓ ┏━━━━━━━━━━━┓ ┏━━━━━━━━━━━━┓ ┏━━━━━━━━━━━━━┓
┃  Azure    ┃ ┃  Azure    ┃ ┃   Azure    ┃ ┃   Azure     ┃
┃   SQL     ┃ ┃  OpenAI   ┃ ┃    AI      ┃ ┃   Search    ┃
┃ Database  ┃ ┃   GPT-4o  ┃ ┃  Cognitive ┃ ┃   Service   ┃
┃           ┃ ┃  (Sweden) ┃ ┃  Services  ┃ ┃   (Basic)   ┃
┗━━━━━━━━━━━┛ ┗━━━━━━━━━━━┛ ┗━━━━━━━━━━━━┛ ┗━━━━━━━━━━━━━┛
     │             │              │               │
     │             │              │               │
     │             └──────┬───────┴───────────────┘
     │                    │
     │            GenAI Chat Features
     │            • Natural Language Queries
     │            • Expense Management via Chat
     │            • RAG Pattern Implementation
     │
     ▼
Database Schema:
• Users (with Roles)
• Expenses
• ExpenseCategories
• ExpenseStatus
• Relationships


┌─────────────────────────────────────────────────────────────────────────┐
│                         Security & Authentication                        │
└─────────────────────────────────────────────────────────────────────────┘

  • User-Assigned Managed Identity for all service connections
  • No API keys or connection strings stored in code
  • Azure RBAC roles:
    - Cognitive Services OpenAI User (for OpenAI)
    - Search Index Data Contributor (for AI Search)
    - db_datareader + db_datawriter (for SQL Database)


┌─────────────────────────────────────────────────────────────────────────┐
│                         Deployment Architecture                          │
└─────────────────────────────────────────────────────────────────────────┘

  Infrastructure as Code (Bicep):
  ┌────────────────────────────────────────────────────────────────┐
  │  main.bicep (Orchestrator)                                     │
  │    ├─ app-service.bicep (App Service + Managed Identity)       │
  │    └─ genai.bicep (OpenAI + AI Search + Role Assignments)      │
  └────────────────────────────────────────────────────────────────┘

  Deployment Scripts:
  • deploy.sh          → Deploys core infrastructure + application
  • deploy-chatui.sh   → Deploys GenAI services (optional)
  • run-sql.py         → Configures database permissions


┌─────────────────────────────────────────────────────────────────────────┐
│                            Resource Locations                            │
└─────────────────────────────────────────────────────────────────────────┘

  • Primary Region:      UK South (uksouth)
  • OpenAI Region:       Sweden Central (swedencentral) - GPT-4o availability
  • All other services:  UK South


┌─────────────────────────────────────────────────────────────────────────┐
│                            Cost Optimization                             │
└─────────────────────────────────────────────────────────────────────────┘

  • App Service Plan:    B1 (Basic) - Low-cost development SKU
  • Azure OpenAI:        S0 Standard - Pay-as-you-go
  • AI Search:           Basic tier
  • SQL Database:        Uses existing database (sql-expense-mgmt-xyz)


┌─────────────────────────────────────────────────────────────────────────┐
│                          Application Features                            │
└─────────────────────────────────────────────────────────────────────────┘

  Web UI:
  • Dashboard with expense summary cards
  • Expense list with filtering
  • Create/Edit/Delete expense forms
  • Approval workflow UI
  • AI Chat Assistant

  REST APIs:
  • GET/POST/PUT/DELETE /api/expenses
  • POST /api/expenses/{id}/submit
  • POST /api/expenses/{id}/approve
  • POST /api/expenses/{id}/reject
  • GET /api/categories
  • GET /api/statuses
  • GET /api/users

  AI Features:
  • Natural language expense queries
  • AI-powered expense creation
  • Context-aware responses using RAG
  • Integration with expense APIs via function calling
```

## Connection Flow

1. **User Access**: User accesses the web application via HTTPS
2. **App Service**: Serves Razor Pages UI and REST APIs
3. **Database Access**: App Service uses Managed Identity to connect to Azure SQL
4. **GenAI Features**: When enabled, uses Managed Identity to access:
   - Azure OpenAI for chat completions
   - Azure AI Search for RAG functionality
5. **API Integration**: GenAI can call expense APIs via function calling

## Best Practices Implemented

✓ Managed Identity authentication (no secrets in code)
✓ Infrastructure as Code with Bicep
✓ Separation of concerns (core vs GenAI deployment)
✓ Modern UI with Bootstrap and custom styling
✓ Comprehensive API documentation with Swagger
✓ Error handling with dummy data fallback
✓ Cost-optimized development SKUs
✓ HTTPS-only configuration
✓ Azure best practices for OpenAI integration
