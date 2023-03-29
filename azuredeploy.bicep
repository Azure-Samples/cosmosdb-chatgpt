param location string = resourceGroup().location

@description('Name of the chat application. Needs to be unique for Cosmos DB, App Service and Open AI')
param chatAppName string

@description('OpenAI region')
@allowed([
  'East US'
  'South Central US'
  'West Europe'
])
param openAiRegion string = 'East US'

@description('OpenAI SKU')
@allowed([
  'S0'
])
param openAiSku string = 'S0'

@description('The deployment name for the Davinci-003 model used by this application')
param openAIModelDeploymentName string = ''

@description('Specifies App Service Sku (F1 = Free Tier)')
param appServicesSkuName string = 'F1'

@description('Specifies App Service capacity')
param appServicesSkuCapacity int = 1

@description('Enable Cosmos DB Free Tier')
param cosmosFreeTier bool = true

@description('Cosmos DB Container Throughput (<1000 for free tier)')
param cosmosContainerThroughput int = 400

var cosmosDBAccountName = '${chatAppName}-cosmos'
var openAiAccountName = '${chatAppName}-openai'
var hostingPlanName = '${chatAppName}-hostingplan'
var webSiteName = '${chatAppName}-webapp'
var webSiteRepository = 'https://github.com/Azure-Samples/cosmosdb-chatgpt.git'
var databaseName = 'ChatDatabase'
var containerName = 'ChatContainer'
var openAiMaxTokens = '3000'

resource cosmosDBAccount 'Microsoft.DocumentDB/databaseAccounts@2022-08-15' = {
  name: cosmosDBAccountName
  location: location
  kind: 'GlobalDocumentDB'
  properties: {
    consistencyPolicy: {
      defaultConsistencyLevel: 'Session'
    }
    databaseAccountOfferType: 'Standard'
    enableFreeTier: cosmosFreeTier
    locations: [
      {
        failoverPriority: 0
        isZoneRedundant: false
        locationName: location
      }
    ]
  }
}

resource cosmosDBAccountName_database 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2022-08-15' = {
  parent: cosmosDBAccount
  name: databaseName
  properties: {
    resource: {
      id: databaseName
    }
  }
}

resource cosmosDBAccountName_databaseName_container 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases/containers@2022-08-15' = {
  parent: cosmosDBAccountName_database
  name: containerName
  properties: {
    resource: {
      id: containerName
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
      throughput: cosmosContainerThroughput
    }
  }
}

resource openAiAccount 'Microsoft.CognitiveServices/accounts@2022-12-01' = {
  name: openAiAccountName
  location: openAiRegion
  sku: {
    name: openAiSku
  }
  kind: 'OpenAI'
  properties: {
    customSubDomainName: openAiAccountName
    publicNetworkAccess: 'Enabled'
  }
}

resource openAiAccountName_openAIModelDeployment 'Microsoft.CognitiveServices/accounts/deployments@2022-12-01' = {
  parent: openAiAccount
  name: openAIModelDeploymentName
  properties: {
    model: {
      format: 'OpenAI'
      name: 'text-davinci-003'
      version: '1'
    }
    scaleSettings: {
      scaleType: 'Standard'
    }
  }
}

resource hostingPlan 'Microsoft.Web/serverfarms@2020-12-01' = {
  name: hostingPlanName
  location: location
  sku: {
    name: appServicesSkuName
    capacity: appServicesSkuCapacity
  }
}

resource webSite 'Microsoft.Web/sites@2020-12-01' = {
  name: webSiteName
  location: location
  properties: {
    serverFarmId: hostingPlan.id
    siteConfig: {
      appSettings: [
        {
          name: 'CosmosUri'
          value: cosmosDBAccount.properties.documentEndpoint
        }
        {
          name: 'CosmosKey'
          value: cosmosDBAccount.listKeys().primaryMasterKey
        }
        {
          name: 'CosmosDatabase'
          value: databaseName
        }
        {
          name: 'CosmosContainer'
          value: containerName
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
          value: openAIModelDeploymentName
        }
        {
          name: 'OpenAiMaxTokens'
          value: openAiMaxTokens
        }
      ]
    }
  }
}

resource webSiteName_web 'Microsoft.Web/sites/sourcecontrols@2020-12-01' = {
  parent: webSite
  name: 'web'
  properties: {
    repoUrl: webSiteRepository
    branch: 'main'
    isManualIntegration: true
  }
}
