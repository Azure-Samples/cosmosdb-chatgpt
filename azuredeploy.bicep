@description('Location where all resources will be deployed. This value defaults to the **East US** region.')
@allowed([
  'Australia East'
  'Canada East'
  'East US'
  'East US 2'
  'France Central'
  'Japan East'
  'North Central US'
  'Switzerland North'
  'UK South'
  'West Europe'
])
param location string = 'East US'

@description('''
Unique name for the chat application.  The name is required to be unique as it will be used as a prefix for the names of these resources:
- Azure Cosmos DB
- Azure App Service
- Azure OpenAI
The name defaults to a unique string generated from the resource group identifier.
''')
param name string = uniqueString(resourceGroup().id)

@description('Boolean indicating whether Azure Cosmos DB free tier should be used for the account. This defaults to **true**.')
param cosmosDbEnableFreeTier bool = true

@description('Specifies the SKU for the Azure App Service plan. Defaults to **F1**')
@allowed([
  'F1'
  'D1'
  'B1'
])
param appServiceSku string = 'F1'

@description('Specifies the SKU for the Azure OpenAI resource. Defaults to **S0**')
@allowed([
  'S0'
])
param openAiSku string = 'S0'

@description('Git repository URL for the chat application. This defaults to the [`azure-samples/cosmosdb-chatgpt`](https://github.com/azure-samples/cosmosdb-chatgpt) repository.')
param appGitRepository string = 'https://github.com/azure-samples/cosmosdb-chatgpt.git'

@description('Git repository branch for the chat application. This defaults to the [**main** branch of the `azure-samples/cosmosdb-chatgpt`](https://github.com/azure-samples/cosmosdb-chatgpt/tree/main) repository.')
param appGetRepositoryBranch string = 'main'

@description('Determines if Azure OpenAI should be deployed. Defaults to true.')
param deployOpenAi bool = true

var openAiSettings = {
  name: '${name}-openai'
  sku: openAiSku
  maxConversationTokens: '2000'
  model: {
    name: 'gpt-35-turbo'
    version: '0301'
    deployment: {
      name: 'chatmodel'
    }
  }
}

var cosmosDbSettings = {
  name: '${name}-cosmos-nosql'
  enableFreeTier: cosmosDbEnableFreeTier
  database: {
    name: 'chatdatabase'
  }
  container: {
    name: 'chatcontainer'
    throughput: 400
  }
}

var appServiceSettings = {
  plan: {
    name: '${name}-web-plan'
  }
  web: {
    name: '${name}-web'
    git: {
      repo: appGitRepository
      branch: appGetRepositoryBranch
    }
  }
  sku: appServiceSku
}

resource cosmosDbAccount 'Microsoft.DocumentDB/databaseAccounts@2022-08-15' = {
  name: cosmosDbSettings.name
  location: location
  kind: 'GlobalDocumentDB'
  properties: {
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    databaseAccountOfferType: 'Standard'
    enableFreeTier: cosmosDbSettings.enableFreeTier
    locations: [
      {
        failoverPriority: 0
        isZoneRedundant: false
        locationName: location
      }
    ]
  }
}

resource cosmosDbDatabase 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2022-08-15' = {
  parent: cosmosDbAccount
  name: cosmosDbSettings.database.name
  properties: {
    resource: {
      id: cosmosDbSettings.database.name
    }
  }
}

resource cosmosDbContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2022-08-15' = {
  parent: cosmosDbDatabase
  name: cosmosDbSettings.container.name
  properties: {
    resource: {
      id: cosmosDbSettings.container.name
      partitionKey: {
        paths: [
          '/sessionId'
        ]
        kind: 'Hash'
        version: 2
      }
      indexingPolicy: {
        indexingMode: 'Consistent'
        automatic: true
        includedPaths: [
          {
            path: '/sessionId/?'
          }
          {
            path: '/type/?'
          }
        ]
        excludedPaths: [
          {
            path: '/*'
          }
        ]
      }
    }
    options: {
      throughput: cosmosDbSettings.container.throughput
    }
  }
}

resource openAiAccount 'Microsoft.CognitiveServices/accounts@2023-05-01' = if (deployOpenAi) {
  name: openAiSettings.name
  location: location
  sku: {
    name: openAiSettings.sku
  }
  kind: 'OpenAI'
  properties: {
    customSubDomainName: openAiSettings.name
    publicNetworkAccess: 'Enabled'
  }
}

resource openAiModelDeployment 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = if (deployOpenAi) {
  parent: openAiAccount
  name: openAiSettings.model.deployment.name
  sku: {
    name: 'Standard'
    capacity: 20
  }
  properties: {
    model: {
      format: 'OpenAI'
      name: openAiSettings.model.name
      version: openAiSettings.model.version
    }
  }
}

resource appServicePlan 'Microsoft.Web/serverfarms@2022-09-01' = {
  name: appServiceSettings.plan.name
  location: location
  sku: {
    name: appServiceSettings.sku
  }
}

resource appServiceWeb 'Microsoft.Web/sites@2022-09-01' = {
  name: appServiceSettings.web.name
  location: location
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
  }
}

resource appServiceWebSettingsFramework 'Microsoft.Web/sites/config@2022-09-01' = {
  parent: appServiceWeb
  name: 'web'
  kind: 'string'
  properties: {
    netFrameworkVersion: 'v8.0'
  }
}

resource appServiceWebSettingsMetadata 'Microsoft.Web/sites/config@2022-09-01' = {
  parent: appServiceWeb
  name: 'metadata'
  kind: 'string'
  properties: {
    CURRENT_STACK: 'dotnet'
  }
}

resource appServiceWebSettingsApplication 'Microsoft.Web/sites/config@2022-09-01' = {
  parent: appServiceWeb
  name: 'appsettings'
  kind: 'string'
  properties: deployOpenAi ? {
    COSMOSDB__ENDPOINT: cosmosDbAccount.properties.documentEndpoint
    COSMOSDB__KEY: cosmosDbAccount.listKeys().primaryMasterKey
    COSMOSDB__DATABASE: cosmosDbDatabase.name
    COSMOSDB__CONTAINER: cosmosDbContainer.name
    OPENAI__ENDPOINT: openAiAccount.properties.endpoint
    OPENAI__KEY: openAiAccount.listKeys().key1
    OPENAI__MODELNAME: openAiModelDeployment.name
    OPENAI__MAXCONVERSATIONTOKENS: openAiSettings.maxConversationTokens
  } : {
    COSMOSDB__ENDPOINT: cosmosDbAccount.properties.documentEndpoint
    COSMOSDB__KEY: cosmosDbAccount.listKeys().primaryMasterKey
    COSMOSDB__DATABASE: cosmosDbDatabase.name
    COSMOSDB__CONTAINER: cosmosDbContainer.name
  }
}

resource appServiceWebDeployment 'Microsoft.Web/sites/sourcecontrols@2021-03-01' = {
  parent: appServiceWeb
  name: 'web'
  properties: {
    repoUrl: appServiceSettings.web.git.repo
    branch: appServiceSettings.web.git.branch
    isManualIntegration: true
  }
}

output deployedUrl string = appServiceWeb.properties.defaultHostName
