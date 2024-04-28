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

@description('''
Unique name for the chat application.  The name is required to be unique as it will be used as a prefix for the names of these resources:
- Azure Cosmos DB
- Azure App Service
The name defaults to a unique string generated from the resource group identifier.
''')
param name string = uniqueString(resourceGroup().id)

@description('Specifies the SKU for the Azure App Service plan. Defaults to **F1** Free Tier')
@allowed([
  'F1'
  'D1'
  'B1'
])
param appServiceSku string = 'F1'

@description('Specifies the Azure OpenAI account name.')
param openAiAccountName string = ''

@description('Specifies the key for Azure OpenAI account.')
@secure()
param openAiAccountKey string = ''

@description('Specifies the deployed model name for your Azure OpenAI account GPT model.')
param openAiGptModelName string = ''

@description('Git repository URL for the chat application. This defaults to the [`azure-samples/cosmosdb-chatgpt`](https://github.com/azure-samples/cosmosdb-chatgpt) repository.')
param appGitRepository string = 'https://github.com/azure-samples/cosmosdb-chatgpt.git'

@description('Git repository branch for the chat application. This defaults to the [**main** branch of the `azure-samples/cosmosdb-chatgpt`](https://github.com/azure-samples/cosmosdb-chatgpt/tree/main) repository.')
param appGetRepositoryBranch string = 'main'

var openAiSettings = {
  name: openAiAccountName
  endpoint: 'https://${openAiAccountName}.openai.azure.com'
  key: openAiAccountKey
  sku: 'S0'
  maxConversationTokens: '2000'
  model: {
    deployment: {
      name: openAiGptModelName
    }
  }
}

var cosmosDbSettings = {
  name: '${name}-cosmos-nosql'
  database: {
    name: 'chatdatabase'
  }
  container: {
    name: 'chatcontainer'
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

resource cosmosDbAccount 'Microsoft.DocumentDB/databaseAccounts@2023-04-15' = {
  name: cosmosDbSettings.name
  location: location
  kind: 'GlobalDocumentDB'
  properties: {
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    databaseAccountOfferType: 'Standard'
    capabilities: [ { name: 'EnableServerless' } ]
    locations: [
      {
        failoverPriority: 0
        isZoneRedundant: false
        locationName: location
      }
    ]
  }
}

resource cosmosDbDatabase 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2023-04-15' = {
  parent: cosmosDbAccount
  name: cosmosDbSettings.database.name
  properties: {
    resource: {
      id: cosmosDbSettings.database.name
    }
  }
}

resource cosmosDbContainer 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2023-04-15' = {
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

resource appServiceWebSettings 'Microsoft.Web/sites/config@2022-09-01' = {
  parent: appServiceWeb
  name: 'appsettings'
  kind: 'string'
  properties: {
    COSMOSDB__ENDPOINT: cosmosDbAccount.properties.documentEndpoint
    COSMOSDB__KEY: cosmosDbAccount.listKeys().primaryMasterKey
    COSMOSDB__DATABASE: cosmosDbDatabase.name
    COSMOSDB__CONTAINER: cosmosDbContainer.name
    OPENAI__ENDPOINT: openAiSettings.endpoint
    OPENAI__KEY: openAiSettings.key
    OPENAI__MODELNAME: openAiSettings.model.deployment.name
    OPENAI__MAXCONVERSATIONTOKENS: openAiSettings.maxConversationTokens
  }
}

resource appServiceWebDeployment 'Microsoft.Web/sites/sourcecontrols@2022-09-01' = {
  parent: appServiceWeb
  name: 'web'
  properties: {
    repoUrl: appServiceSettings.web.git.repo
    branch: appServiceSettings.web.git.branch
    isManualIntegration: true
  }
}

output deployedUrl string = appServiceWeb.properties.defaultHostName
