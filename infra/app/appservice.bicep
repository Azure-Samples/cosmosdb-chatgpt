param location string
param appServicePlanName string
param appServiceName string

param sku string = 'S1'
param tags object = {}

type managedIdentity = {
  resourceId: string
  clientId: string
}
param userAssignedManagedIdentity managedIdentity
@description('Endpoint for Azure Cosmos DB for NoSQL account.')
param databaseAccountEndpoint string

@description('Endpoint for Azure OpenAI account.')
param openAiAccountEndpoint string

@description('Maximum number of conversation tokens. Defaults to 1000.')
param openAiMaxConversationTokens int = 1000

param openaiName string

param openaiEndpoint string
param cosmosEndpoint string

resource openai 'Microsoft.CognitiveServices/accounts@2023-05-01' existing = {
  name: openaiName
}

resource appServicePlan 'Microsoft.Web/serverfarms@2020-06-01' = {
  name: appServicePlanName
  location: location
  tags: tags
  sku: {
    name: sku
  }
}

resource appService 'Microsoft.Web/sites@2022-09-01' = {
  name: appServiceName
  location: location
  tags: union(tags, { 'azd-service-name': 'web' })
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
    enableSystemAssignedManagedIdentity: false
    userAssignedManagedIdentityIds: [
      userAssignedManagedIdentity.clientId
    ]
    secrets: [
      {
        name: 'azure-cosmos-db-nosql-endpoint' // Create a uniquely-named secret
        value: databaseAccountEndpoint // NoSQL database account endpoint
      }
      {
        name: 'azure-openai-endpoint' // Create a uniquely-named secret
        value: openAiAccountEndpoint // OpenAI model endpoint
      }
      {
        name: 'azure-managed-identity-client-id' // Create a uniquely-named secret
        value: userAssignedManagedIdentity.clientId // Client ID of user-assigned managed identity
      }
    ]
    siteConfig: {
      http20Enabled: true
      appSettings: [
        {
          name: 'MicrosoftAppTenantId'
          value: tenant().tenantId
        }
        {
          name: 'OPENAI__ENDPOINT'
          value: openaiEndpoint
        }
        {
          name: 'OPENAI__KEY'
          value: openai.listKeys().key1
        }
        {
          name: 'OPENAI__COMPLETIONDEPLOYMENTNAME'
          value: 'completions'
        }
        {
          name: 'OPENAI__EMBEDDINGDEPLOYMENTNAME'
          value: 'embeddings'
        }
        {
          name: 'COSMOSDB__ENDPOINT'
          value: cosmosEndpoint
        }
        { name: 'COSMOSDB__CONTAINER', value: 'chatcontainer' }
        { name: 'COSMOSDB__DATABASE', value: 'chatdatabase' }
        { name: 'OPENAI__MAXCONVERSATIONTOKENS', value: string(openAiMaxConversationTokens)  }
      ]
    }
  }
}

output appName string = appService.name
output hostName string = appService.properties.defaultHostName
