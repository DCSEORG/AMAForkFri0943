// main.bicep - Main orchestration file for Expense Management Application

targetScope = 'resourceGroup'

param location string = 'uksouth'
param deployGenAI bool = false

// Deploy App Service and Managed Identity
module appService 'app-service.bicep' = {
  name: 'appServiceDeployment'
  params: {
    location: location
  }
}

// Conditionally deploy GenAI resources
module genai 'genai.bicep' = if (deployGenAI) {
  name: 'genaiDeployment'
  params: {
    location: location
    managedIdentityId: appService.outputs.managedIdentityId
    managedIdentityPrincipalId: appService.outputs.managedIdentityPrincipalId
  }
}

output webAppName string = appService.outputs.webAppName
output webAppUrl string = appService.outputs.webAppUrl
output managedIdentityId string = appService.outputs.managedIdentityId
output managedIdentityClientId string = appService.outputs.managedIdentityClientId
output managedIdentityName string = appService.outputs.managedIdentityName

output openAIEndpoint string = deployGenAI ? genai.outputs.openAIEndpoint : ''
output openAIName string = deployGenAI ? genai.outputs.openAIName : ''
output openAIModelName string = deployGenAI ? genai.outputs.openAIModelName : ''
output searchEndpoint string = deployGenAI ? genai.outputs.searchEndpoint : ''
output searchName string = deployGenAI ? genai.outputs.searchName : ''
