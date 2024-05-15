@description('Deployment template for the Azure Cosmos DB & Azure Open AI chat sample app')
param deploymentName string = 'Cosmos-chat-app'

@description('Location where all resources are deployed. Is limited by the regions with Azure OpenAI availability. Defaults to **East US** region.')
@allowed([
  'Australia East'
  'Brazil South'
  'Canada Central'
  'Canada East'
  'East US'
  'East US 2'
  'France Central'
  'Germany West Central'
  'Japan East'
  'Korea Central'
  'North Central US'
  'Norway East'
  'Poland Central'
  'South Africa North'
  'South Central US'
  'South India'
  'Sweden Central'
  'Switzerland North'
  'UAE North'
  'UK South'
  'West Europe'
  'West US'
  'West US 3'
])
param location string = 'East US'

@description('Location override for just serverless Cosmos account. Leave blank to use locations property value')
@allowed([
  'North Central US'
  'South Central US'
])
param cosmosLocation string

@description('Location override where Azure OpenAI is deployed. Leave blank to use locations property value')
@allowed([
  'Sweden Central'
  'Canada East'
])
param openAilocation string

@description('Unique name for the chat application.  The name is required to be unique as it will be used as a prefix for the names of these resources:\r\n- Azure Cosmos DB\r\n- Azure App Service\r\n- Azure OpenAI\r\nThe name defaults to a unique string generated from the resource group identifier.\r\n')
param name string = uniqueString(resourceGroup().id)

@description('Specifies the SKU for the Azure App Service plan. Defaults to **F1**')
@allowed([
  'F1'
  'D1'
  'B1'
])
param appServiceSku string = 'F1'

@description('Git repository URL for the chat application. This defaults to the [`azure-samples/cosmosdb-chatgpt`](https://github.com/azure-samples/cosmosdb-chatgpt) repository.')
param appGitRepository string = 'https://github.com/azure-samples/cosmosdb-chatgpt.git'

@description('Git repository branch for the chat application. This defaults to the [**main** branch of the `azure-samples/cosmosdb-chatgpt`](https://github.com/azure-samples/cosmosdb-chatgpt/tree/main) repository.')
param appGetRepositoryBranch string = 'main'

var openAiSettings = {
  name: 'openai-${name}'
  location: (empty(openAilocation) ? location : openAilocation)
  sku: 'S0'
  completionsModel: {
    name: 'gpt-35-turbo'
    version: '1106'
    deployment: {
      name: 'completions'
      capacity: 10
    }
  }
  embeddingsModel: {
    name: 'text-embedding-ada-002'
    version: '2'
    deployment: {
      name: 'embeddings'
      capacity: 5
    }
  }
}
var cosmosDbSettings = {
  account: {
    name: 'cosmos-nosql-${name}'
    location: (empty(cosmosLocation) ? location : cosmosLocation)
  }
  database: {
    name: 'ChatDatabase'
  }
  chat: {
    name: 'ChatContainer'
  }
  cache: {
    name: 'CacheContainer'
  }
}
var chatSettings = {
  maxConversationTokens: '100'
  cacheSimilarityScore: '0.99'
}
var appServiceSettings = {
  plan: {
    name: 'web-plan-${name}'
  }
  web: {
    name: 'web-${name}'
    git: {
      repo: appGitRepository
      branch: appGetRepositoryBranch
    }
  }
  sku: appServiceSku
}

resource cosmosDbSettings_account_name 'Microsoft.DocumentDB/databaseAccounts@2023-04-15' = {
  name: cosmosDbSettings.account.name
  location: cosmosDbSettings.account.location
  kind: 'GlobalDocumentDB'
  properties: {
    capabilities: [
      {
        name: 'EnableNoSQLVectorSearch'
      }
      {
        name: 'EnableServerless'
      }
    ]
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    databaseAccountOfferType: 'Standard'
    locations: [
      {
        failoverPriority: 0
        isZoneRedundant: false
        locationName: location
      }
    ]
  }
}

resource cosmosDbSettings_account_name_cosmosDbSettings_database_name 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2023-04-15' = {
  name: '${cosmosDbSettings.account.name}/${cosmosDbSettings.database.name}'
  properties: {
    resource: {
      id: cosmosDbSettings.database.name
    }
  }
  dependsOn: [
    cosmosDbSettings_account_name
  ]
}

resource cosmosDbSettings_account_name_cosmosDbSettings_database_name_cosmosDbSettings_chat_name 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-04-15' = {
  name: '${cosmosDbSettings.account.name}/${cosmosDbSettings.database.name}/${cosmosDbSettings.chat.name}'
  properties: {
    resource: {
      id: cosmosDbSettings.chat.name
      partitionKey: {
        paths: [
          '/sessionId'
        ]
        kind: 'Hash'
      }
      indexingPolicy: {
        indexingMode: 'consistent'
        automatic: true
        includedPaths: [
          {
            path: '/sessionId/?'
          }
        ]
        excludedPaths: [
          {
            path: '/*'
          }
        ]
      }
    }
    options: {}
  }
  dependsOn: [
    cosmosDbSettings_account_name_cosmosDbSettings_database_name
  ]
}

resource cosmosDbSettings_account_name_cosmosDbSettings_database_name_cosmosDbSettings_cache_name 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-04-15' = {
  name: '${cosmosDbSettings.account.name}/${cosmosDbSettings.database.name}/${cosmosDbSettings.cache.name}'
  properties: {
    resource: {
      id: cosmosDbSettings.cache.name
      partitionKey: {
        paths: [
          '/id'
        ]
        kind: 'Hash'
      }
      indexingPolicy: {
        indexingMode: 'consistent'
        automatic: true
        includedPaths: [
          {
            path: '/*'
          }
        ]
        excludedPaths: [
          {
            path: '/vectors/?'
          }
        ]
        vectorIndexes: [
          {
            path: '/vectors'
            type: 'quantizedFlat'
          }
        ]
      }
      vectorEmbeddingPolicy: {
        vectorEmbeddings: [
          {
            path: '/vectors'
            dataType: 'float32'
            dimensions: 1536
            distanceFunction: 'cosine'
          }
        ]
      }
    }
    options: {}
  }
  dependsOn: [
    cosmosDbSettings_account_name_cosmosDbSettings_database_name
  ]
}

resource openAiSettings_name 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: openAiSettings.name
  location: openAiSettings.location
  sku: {
    name: openAiSettings.sku
  }
  kind: 'OpenAI'
  properties: {
    customSubDomainName: openAiSettings.name
    publicNetworkAccess: 'Enabled'
  }
}

resource openAiSettings_name_openAiSettings_embeddingsModel_deployment_name 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = {
  name: '${openAiSettings.name}/${openAiSettings.embeddingsModel.deployment.name}'
  sku: {
    name: 'Standard'
    capacity: openAiSettings.embeddingsModel.deployment.capacity
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: openAiSettings.embeddingsModel.name
      version: openAiSettings.embeddingsModel.version
    }
  }
  dependsOn: [
    openAiSettings_name
  ]
}

resource openAiSettings_name_openAiSettings_completionsModel_deployment_name 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = {
  name: '${openAiSettings.name}/${openAiSettings.completionsModel.deployment.name}'
  sku: {
    name: 'Standard'
    capacity: openAiSettings.completionsModel.deployment.capacity
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: openAiSettings.completionsModel.name
      version: openAiSettings.completionsModel.version
    }
  }
  dependsOn: [
    openAiSettings_name
    openAiSettings_name_openAiSettings_embeddingsModel_deployment_name
  ]
}

resource appServiceSettings_plan_name 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: appServiceSettings.plan.name
  location: location
  sku: {
    name: appServiceSettings.sku
  }
}

resource appServiceSettings_web_name 'Microsoft.Web/sites@2022-09-01' = {
  name: appServiceSettings.web.name
  location: location
  properties: {
    serverFarmId: appServiceSettings_plan_name.id
    httpsOnly: true
  }
}

resource appServiceSettings_web_name_web 'Microsoft.Web/sites/config@2022-09-01' = {
  name: '${appServiceSettings.web.name}/web'
  kind: 'string'
  properties: {
    netFrameworkVersion: 'v8.0'
  }
  dependsOn: [
    appServiceSettings_web_name
  ]
}

resource appServiceSettings_web_name_metadata 'Microsoft.Web/sites/config@2022-09-01' = {
  name: '${appServiceSettings.web.name}/metadata'
  kind: 'string'
  properties: {
    CURRENT_STACK: 'dotnet'
  }
  dependsOn: [
    appServiceSettings_web_name
  ]
}

resource appServiceSettings_web_name_appsettings 'Microsoft.Web/sites/config@2022-09-01' = {
  name: '${appServiceSettings.web.name}/appsettings'
  kind: 'string'
  properties: {
    COSMOSDB__ENDPOINT: reference(cosmosDbSettings_account_name.id, '2023-04-15').documentEndpoint
    COSMOSDB__KEY: listKeys(cosmosDbSettings_account_name.id, '2023-04-15').primaryMasterKey
    COSMOSDB__DATABASE: cosmosDbSettings.database.name
    COSMOSDB__CHATCONTAINER: cosmosDbSettings.chat.name
    COSMOSDB__CACHECONTAINER: cosmosDbSettings.cache.name
    OPENAI__ENDPOINT: reference(openAiSettings_name.id, '2023-05-01').endpoint
    OPENAI__KEY: listKeys(openAiSettings_name.id, '2023-05-01').key1
    OPENAI__COMPLETIONDEPLOYMENTNAME: openAiSettings.completionsModel.deployment.name
    OPENAI__EMBEDDINGDEPLOYMENTNAME: openAiSettings.embeddingsModel.deployment.name
    CHAT__MAXCONVERSATIONTOKENS: chatSettings.maxConversationTokens
    CHAT__CACHESIMILARITYSCORE: chatSettings.cacheSimilarityScore
  }
  dependsOn: [
    appServiceSettings_web_name

    cosmosDbSettings_account_name_cosmosDbSettings_database_name
    cosmosDbSettings_account_name_cosmosDbSettings_database_name_cosmosDbSettings_chat_name
    cosmosDbSettings_account_name_cosmosDbSettings_database_name_cosmosDbSettings_cache_name

    openAiSettings_name_openAiSettings_completionsModel_deployment_name
    openAiSettings_name_openAiSettings_embeddingsModel_deployment_name
  ]
}

resource Microsoft_Web_sites_sourcecontrols_appServiceSettings_web_name_web 'Microsoft.Web/sites/sourcecontrols@2021-03-01' = {
  name: '${appServiceSettings.web.name}/web'
  properties: {
    repoUrl: appServiceSettings.web.git.repo
    branch: appServiceSettings.web.git.branch
    isManualIntegration: true
  }
  dependsOn: [
    appServiceSettings_web_name
  ]
}

output DeployedUrl string = reference(appServiceSettings_web_name.id, '2022-09-01').defaultHostName
output CosmosDbEndpoint string = reference(cosmosDbSettings_account_name.id, '2023-04-15').documentEndpoint
output CosmosDbKey string = listKeys(cosmosDbSettings_account_name.id, '2023-04-15').primaryMasterKey
output OpenAiEndpoint string = reference(openAiSettings_name.id, '2023-05-01').endpoint
output OpenAiKey string = listKeys(openAiSettings_name.id, '2023-05-01').key1
