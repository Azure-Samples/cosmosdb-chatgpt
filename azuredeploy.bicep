@description('Location where all resources will be deployed. This value defaults to the **South Central US** region.')
@allowed([
  'South Central US'
  'East US'
  'France Central'
])
param location string = 'South Central US'

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

resource openAiAccount 'Microsoft.CognitiveServices/accounts@2022-12-01' = {
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

resource openAiModelDeployment 'Microsoft.CognitiveServices/accounts/deployments@2022-12-01' = {
  parent: openAiAccount
  name: openAiSettings.model.deployment.name
  properties: {
    model: {
      format: 'OpenAI'
      name: openAiSettings.model.name
      version: openAiSettings.model.version
    }
    scaleSettings: {
      scaleType: 'Standard'
    }
  }
}

resource appServicePlan 'Microsoft.Web/serverfarms@2022-03-01' = {
  name: appServiceSettings.plan.name
  location: location
  sku: {
    name: appServiceSettings.sku
  }
}

resource appServiceWeb 'Microsoft.Web/sites@2022-03-01' = {
  name: appServiceSettings.web.name
  location: location
  properties: {
    serverFarmId: appServicePlan.id
    httpsOnly: true
  }
}

resource appServiceWebSettings 'Microsoft.Web/sites/config@2022-03-01' = {
  parent: appServiceWeb
  name: 'appsettings'
  kind: 'string'
  properties: {
    COSMOSDB__ENDPOINT: cosmosDbAccount.properties.documentEndpoint
    COSMOSDB__KEY: cosmosDbAccount.listKeys().primaryMasterKey
    COSMOSDB__DATABASE: cosmosDbDatabase.name
    COSMOSDB__CONTAINER: cosmosDbContainer.name
    OPENAI__ENDPOINT: openAiAccount.properties.endpoint
    OPENAI__KEY: openAiAccount.listKeys().key1
    OPENAI__MODELNAME: openAiModelDeployment.name
    OPENAI__MAXCONVERSATIONTOKENS: openAiSettings.maxConversationTokens
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
