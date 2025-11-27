// Main Bicep - Orchestrates deployment of all resources
// Expense Management System Infrastructure

@description('Location for main resources')
param location string = 'uksouth'

@description('Base name for resources')
param baseName string = 'expensemgmt'

@description('Entra ID Admin Object ID for SQL Server')
param adminObjectId string

@description('Entra ID Admin Login (User Principal Name) for SQL Server')
param adminLogin string

@description('Deploy GenAI resources (Azure OpenAI and AI Search)')
param deployGenAI bool = false

// Unique suffix using resource group ID
var uniqueSuffix = uniqueString(resourceGroup().id)

// App Service and Managed Identity
module appService 'app-service.bicep' = {
  name: 'appServiceDeployment'
  params: {
    location: location
    baseName: baseName
    uniqueSuffix: uniqueSuffix
  }
}

// Azure SQL Database
module azureSql 'azure-sql.bicep' = {
  name: 'azureSqlDeployment'
  params: {
    location: location
    baseName: baseName
    uniqueSuffix: uniqueSuffix
    adminObjectId: adminObjectId
    adminLogin: adminLogin
    managedIdentityPrincipalId: appService.outputs.managedIdentityPrincipalId
  }
}

// GenAI Resources (conditional deployment)
module genai 'genai.bicep' = if (deployGenAI) {
  name: 'genaiDeployment'
  params: {
    location: 'swedencentral'
    baseName: baseName
    uniqueSuffix: uniqueSuffix
    managedIdentityPrincipalId: appService.outputs.managedIdentityPrincipalId
  }
}

// Outputs - App Service
output appServiceName string = appService.outputs.appServiceName
output appServiceUrl string = 'https://${appService.outputs.appServiceDefaultHostName}'

// Outputs - Managed Identity
output managedIdentityName string = appService.outputs.managedIdentityName
output managedIdentityClientId string = appService.outputs.managedIdentityClientId
output managedIdentityPrincipalId string = appService.outputs.managedIdentityPrincipalId

// Outputs - SQL Server
output sqlServerName string = azureSql.outputs.sqlServerName
output sqlServerFQDN string = azureSql.outputs.sqlServerFQDN
output sqlDatabaseName string = azureSql.outputs.sqlDatabaseName
output sqlConnectionString string = azureSql.outputs.connectionString

// Outputs - GenAI (conditional, with null-safe operators)
output openAIEndpoint string = deployGenAI ? genai.outputs.openAIEndpoint : ''
output openAIModelName string = deployGenAI ? genai.outputs.openAIModelName : ''
output openAIName string = deployGenAI ? genai.outputs.openAIName : ''
output searchEndpoint string = deployGenAI ? genai.outputs.searchEndpoint : ''
output searchServiceName string = deployGenAI ? genai.outputs.searchServiceName : ''
