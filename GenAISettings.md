# GenAI Settings Configuration
# This file documents the Azure OpenAI and AI Search configuration

## Azure OpenAI Configuration

**Service Name**: Dynamically generated (openai-expenseapp-{uniqueString})
**Location**: Sweden Central (swedencentral)
**SKU**: S0 (Standard)
**Model**: GPT-4o
**Deployment Name**: gpt-4o
**Model Version**: 2024-08-06
**Capacity**: 10 tokens per minute

### Environment Variables (Set by deploy-chatui.sh)

```bash
OpenAI__Endpoint=https://openai-expenseapp-{uniqueString}.openai.azure.com/
OpenAI__DeploymentName=gpt-4o
```

### Authentication
- **Method**: Azure Managed Identity (DefaultAzureCredential)
- **Role Assignment**: Cognitive Services OpenAI User
- **No API Keys Required**: All authentication handled via Managed Identity

## Azure AI Search Configuration

**Service Name**: Dynamically generated (search-expenseapp-{uniqueString})
**Location**: UK South (uksouth)
**SKU**: Basic
**Replica Count**: 1
**Partition Count**: 1

### Environment Variables (Set by deploy-chatui.sh)

```bash
AzureSearch__Endpoint=https://search-expenseapp-{uniqueString}.search.windows.net
```

### Authentication
- **Method**: Azure Managed Identity
- **Role Assignment**: Search Index Data Contributor
- **No API Keys Required**: All authentication handled via Managed Identity

## RAG (Retrieval-Augmented Generation) Setup

**Context Files**: Located in /RAG directory
- context.md: Contains database schema, API documentation, and common queries

**Purpose**: 
- Provides AI with knowledge about the expense management system
- Enables accurate responses to user queries
- Supports function calling for API integration

## Application Configuration

The application checks for GenAI configuration at runtime:
- If OpenAI__Endpoint is set: GenAI features are enabled
- If not set: Chat UI shows configuration instructions

## Deployment Instructions

1. **Core Deployment** (without GenAI):
   ```bash
   ./deploy.sh
   ```

2. **Add GenAI Features** (optional):
   ```bash
   ./deploy-chatui.sh
   ```

## Cost Considerations

- **Azure OpenAI**: Pay-per-use (S0 tier)
  - Charged per 1K tokens
  - GPT-4o pricing applies
  
- **Azure AI Search**: Basic tier
  - Fixed monthly cost for basic tier
  - 2GB storage included
  - 15 indexes maximum

## Best Practices

✓ Use Managed Identity instead of API keys
✓ Deploy GenAI services separately from core application
✓ Use GPT-4o for better accuracy and multimodal capabilities
✓ Implement RAG pattern for domain-specific knowledge
✓ Cache frequently used queries to reduce costs
✓ Monitor token usage in Azure Portal

## Function Calling Integration

The GenAI implementation supports function calling to:
- List expenses
- Create new expenses
- Update expense status
- Get expense details
- Filter by category or status

This enables natural language commands like:
- "Show me all pending expenses"
- "Create an expense for £50 taxi ride"
- "Approve expense ID 123"
