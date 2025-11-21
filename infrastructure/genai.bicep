// genai.bicep - Creates Azure OpenAI and AI Search resources with Managed Identity integration

param location string = 'uksouth'
param openAILocation string = 'swedencentral' // GPT-4o available in Sweden
param openAIAccountName string = 'openai-expenseapp-${uniqueString(resourceGroup().id)}'
param searchServiceName string = 'search-expenseapp-${uniqueString(resourceGroup().id)}'
param managedIdentityId string
param managedIdentityPrincipalId string

// Create Azure OpenAI Account
resource openAIAccount 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: openAIAccountName
  location: openAILocation
  kind: 'OpenAI'
  sku: {
    name: 'S0'
  }
  properties: {
    customSubDomainName: openAIAccountName
    publicNetworkAccess: 'Enabled'
  }
}

// Deploy GPT-4o model
resource gpt4oDeployment 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = {
  parent: openAIAccount
  name: 'gpt-4o'
  sku: {
    name: 'Standard'
    capacity: 10
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-4o'
      version: '2024-08-06'
    }
  }
}

// Grant Managed Identity "Cognitive Services OpenAI User" role
resource openAIRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(openAIAccount.id, managedIdentityId, 'CognitiveServicesOpenAIUser')
  scope: openAIAccount
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '5e0bd9bd-7b93-4f28-af87-19fc36ad61bd') // Cognitive Services OpenAI User
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

// Create Azure AI Search
resource searchService 'Microsoft.Search/searchServices@2023-11-01' = {
  name: searchServiceName
  location: location
  sku: {
    name: 'basic'
  }
  properties: {
    replicaCount: 1
    partitionCount: 1
    hostingMode: 'default'
    publicNetworkAccess: 'enabled'
    authOptions: {
      aadOrApiKey: {
        aadAuthFailureMode: 'http401WithBearerChallenge'
      }
    }
  }
}

// Grant Managed Identity "Search Index Data Contributor" role
resource searchRoleAssignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(searchService.id, managedIdentityId, 'SearchIndexDataContributor')
  scope: searchService
  properties: {
    roleDefinitionId: subscriptionResourceId('Microsoft.Authorization/roleDefinitions', '8ebe5a00-799e-43f5-93ac-243d3dce84a7') // Search Index Data Contributor
    principalId: managedIdentityPrincipalId
    principalType: 'ServicePrincipal'
  }
}

output openAIEndpoint string = openAIAccount.properties.endpoint
output openAIName string = openAIAccount.name
output openAIModelName string = gpt4oDeployment.name
output searchEndpoint string = 'https://${searchService.name}.search.windows.net'
output searchName string = searchService.name
