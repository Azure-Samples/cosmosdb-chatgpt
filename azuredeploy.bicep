@description('Location where all resources will be deployed. This value defaults to the **East US** region.')
@allowed([
  'East US'
  'South Central US'
  'West Europe'
])
param location string = 'East US'

@description('''
Unique name for the chat application.  The name is required to be unique as it will be used as a prefix for the names of these resources:
- Azure Cosmos DB
- Azure App Service
- Azure Open AI
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

var openAiSettings = {
  name: '${name}-openai'
  sku: openAiSku
  maxTokens: '3000'
  model: {
    name: 'text-davinci-003'
    version: '1'
    deployment: {
      name: 'chatmodel'
    }
  }
}

var cosmosDbSettings = {
  name: '${name}-cosmos-nosql'
  enableFreeTier: cosmosDbEnableFreeTier
  database: {
    name: 'chatdb'
  }
  container: {
    name: 'conversations'
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
      repo: 'https://github.com/azure-samples/cosmos-chatgpt.git'
    }
  }
  sku: appServiceSku
  kind: 'linux'
  framework: 'DOTNETCORE:6.0'
  capacity: 1
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
          '/ChatSessionId'
        ]
        kind: 'Hash'
        version: 2
      }
      indexingPolicy: {
        indexingMode: 'Consistent'
        automatic: true
        includedPaths: [
          {
            path: '/ChatSessionId/?'
          }
          {
            path: '/Type/?'
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

resource hostingPlan 'Microsoft.Web/serverfarms@2022-03-01' = {
  name: appServiceSettings.plan.name
  dependsOn: [
    cosmosDbContainer
    openAiModelDeployment
  ]
  location: location
  sku: {
    name: appServiceSettings.sku
    capacity: appServiceSettings.capacity
  }
  kind: 'linux'
  properties: {
    reserved: true
  }
}

resource webSite 'Microsoft.Web/sites@2022-03-01' = {
  name: appServiceSettings.web.name
  location: location
  properties: {
    serverFarmId: hostingPlan.id
    httpsOnly: true
    siteConfig: {
      linuxFxVersion: appServiceSettings.framework
      appSettings: [
        {
          name: 'CosmosUri'
          value: cosmosDbAccount.properties.documentEndpoint
        }
        {
          name: 'CosmosKey'
          value: cosmosDbAccount.listKeys().primaryMasterKey
        }
        {
          name: 'CosmosDatabase'
          value: cosmosDbDatabase.name
        }
        {
          name: 'CosmosContainer'
          value: cosmosDbContainer.name
        }
        {
          name: 'OpenAiUri'
          value: openAiAccount.properties.endpoint
        }
        {
          name: 'OpenAiKey'
          value: openAiAccount.listKeys().key1
        }
        {
          name: 'OpenAiDeployment'
          value: openAiModelDeployment.name
        }
        {
          name: 'OpenAiMaxTokens'
          value: openAiSettings.maxTokens
        }
      ]
    }
  }
}

resource webSiteName_web 'Microsoft.Web/sites/sourcecontrols@2021-03-01' = {
  parent: webSite
  name: 'web'
  properties: {
    repoUrl: appServiceSettings.web.git.repo
    branch: 'main'
    isManualIntegration: true
    isGitHubAction: false
  }
}
