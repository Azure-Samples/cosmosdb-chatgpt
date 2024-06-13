targetScope = 'subscription'

@minLength(1)
@maxLength(64)
@description('Name of the environment that can be used as part of naming resource convention.')
param environmentName string

@minLength(1)
@allowed([
  'australiaeast'
  'westeurope'
  'japaneast'
  'uksouth'
  'eastus'
  'southcentralus'
  'eastus2'
])
@description('Primary location for all resources.')
param location string

@description('Id of the principal to assign database and application roles.')
param principalId string = ''

// Optional parameters
param openAiAccountName string = ''
param cosmosDbAccountName string = ''
param userAssignedIdentityName string = ''
param appServicePlanName string = ''
param appServiceWebAppName string = ''

// serviceName is used as value for the tag (azd-service-name) azd uses to identify deployment host
param serviceName string = 'web'

var abbreviations = loadJsonContent('abbreviations.json')
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))
var tags = {
  'azd-env-name': environmentName
  repo: 'https://github.com/azure-samples/cosmosdb-chatgpt'
}

resource resourceGroup 'Microsoft.Resources/resourceGroups@2022-09-01' = {
  name: environmentName
  location: location
  tags: tags
}

module identity 'app/identity.bicep' = {
  name: 'identity'
  scope: resourceGroup
  params: {
    identityName: !empty(userAssignedIdentityName) ? userAssignedIdentityName : '${abbreviations.userAssignedIdentity}-${resourceToken}'
    location: location
    tags: tags
  }
}

module ai 'app/ai.bicep' = {
  name: 'ai'
  scope: resourceGroup
  params: {
    accountName: !empty(openAiAccountName) ? openAiAccountName : '${abbreviations.openAiAccount}-${resourceToken}'
    location: location
    tags: tags
  }
}

module web 'app/web.bicep' = {
  name: 'web'
  scope: resourceGroup
  params: {
    appName: !empty(appServiceWebAppName) ? appServiceWebAppName : '${abbreviations.appServiceWebApp}-${resourceToken}'
    planName: !empty(appServicePlanName) ? appServicePlanName : '${abbreviations.appServicePlan}-${resourceToken}'
    databaseAccountEndpoint: database.outputs.endpoint
    openAiAccountEndpoint: ai.outputs.endpoint
    cosmosDbSettings: {
      database: database.outputs.database.name
      chatContainer: database.outputs.containers[0].name
      cacheContainer: database.outputs.containers[1].name
    }
    openAiSettings: {
      maxConversationTokens: ai.outputs.maxConversationTokens
      completionDeploymentName: ai.outputs.deployments[0].name
      embeddingDeploymentName: ai.outputs.deployments[1].name
    }
    userAssignedManagedIdentity: {
      resourceId: identity.outputs.resourceId
      clientId: identity.outputs.clientId
    }
    location: location
    tags: tags
    serviceTag: serviceName
  }
}

module database 'app/database.bicep' = {
  name: 'database'
  scope: resourceGroup
  params: {
    accountName: !empty(cosmosDbAccountName) ? cosmosDbAccountName : '${abbreviations.cosmosDbAccount}-${resourceToken}'
    location: location
    tags: tags
  }
}

module security 'app/security.bicep' = {
  name: 'security'
  scope: resourceGroup
  params: {
    databaseAccountName: database.outputs.accountName
    appPrincipalId: identity.outputs.principalId
    userPrincipalId: !empty(principalId) ? principalId : null
  }
}

// Database outputs
output AZURE_COSMOS_DB_ENDPOINT string = database.outputs.endpoint
output AZURE_COSMOS_DB_DATABASE_NAME string = database.outputs.database.name
output AZURE_COSMOS_DB_CHAT_CONTAINER_NAME string = database.outputs.containers[0].name
output AZURE_COSMOS_DB_CACHE_CONTAINER_NAME string = database.outputs.containers[1].name

// AI outputs
output AZURE_OPENAI_ACCOUNT_ENDPOINT string = ai.outputs.endpoint
output AZURE_OPENAI_COMPLETION_DEPLOYMENT_NAME string = ai.outputs.deployments[0].name
output AZURE_OPENAI_EMBEDDING_DEPLOYMENT_NAME string = ai.outputs.deployments[1].name
