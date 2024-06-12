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
@allowed([ 'gpt-35-turbo'])
param gptModel string
@allowed(['0301'])
param gptVersion string

// serviceName is used as value for the tag (azd-service-name) azd uses to identify deployment host

param appServicePlanName string = ''
var abbreviations = loadJsonContent('abbreviations.json')
var resourceToken = toLower(uniqueString(subscription().id, environmentName, location))
var uniqueSuffix = substring(uniqueString(subscription().id, resourceGroup.id), 1, 3) 
var tags = {
  'azd-env-name': environmentName
  repo: 'https://github.com/Azure-Samples/cosmosdb-chatgpt'
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
    name: !empty(openAiAccountName) ? openAiAccountName : '${abbreviations.openAiAccount}${resourceToken}'
    location: location
    gptVersion: gptVersion
    tags: tags
  }
}

module appService 'app/appservice.bicep'= {
  name: 'deploy_app'
  scope: resourceGroup
  params: {
    appServiceName: '${abbreviations.webSitesAppService}${environmentName}-${uniqueSuffix}'
    appServicePlanName: !empty(appServicePlanName) ? appServicePlanName : '${abbreviations.webServerFarms}${environmentName}-${uniqueSuffix}'
    cosmosEndpoint:  database.outputs.endpoint
    databaseAccountEndpoint: database.outputs.endpoint
    openAiAccountEndpoint: ai.outputs.endpoint    
    openaiName: ai.outputs.accountName
    openaiEndpoint: ai.outputs.endpoint
    userAssignedManagedIdentity: {
      resourceId: identity.outputs.resourceId
      clientId: identity.outputs.clientId
    }
    location: location
    tags: tags
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
output AZURE_COSMOS_ENDPOINT string = database.outputs.endpoint
output AZURE_COSMOS_DATABASE_NAME string = database.outputs.database.name
output AZURE_COSMOS_CONTAINER_NAMES array = map(database.outputs.containers, c => c.name)


// Identity outputs
output AZURE_USER_ASSIGNED_IDENTITY_NAME string = identity.outputs.name

// Security outputs
output AZURE_NOSQL_ROLE_DEFINITION_ID string = security.outputs.roleDefinitions.nosql

// Application environment variables
output COSMOSDB__ENDPOINT string = database.outputs.endpoint
output COSMOSDB__DATABASE string = database.outputs.database.name
output COSMOSDB__CONTAINER string = database.outputs.containers[0].name
output OPENAI__ENDPOINT string = ai.outputs.endpoint
output OPENAI__MAXCONVERSATIONTOKENS string = string(ai.outputs.maxConversationTokens)
