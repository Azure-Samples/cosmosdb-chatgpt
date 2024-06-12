metadata description = 'Create AI accounts.'

param name string
param location string = resourceGroup().location
param tags object = {}
param gptVersion string

param customSubDomainName string = name

param deployments array = []
param kind string = 'OpenAI'
param publicNetworkAccess string = 'Enabled'
param sku object = {
  name: 'S0'
}
var maxConversationTokens = 2000


resource account 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: name
  location: location
  tags: tags
  kind: kind
  properties: {
    customSubDomainName: customSubDomainName
    publicNetworkAccess: publicNetworkAccess
  }
  sku: sku
}

@batchSize(1)
resource deployment 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = [for deployment in deployments: {
  parent: account
  name: deployment.name
  properties: {
    model: deployment.model
    raiPolicyName: contains(deployment, 'raiPolicyName') ? deployment.raiPolicyName : null
  }
  sku: contains(deployment, 'sku') ? deployment.sku : {
    name: 'Standard'
    capacity: 20
  }
}]

resource gpt4deployment 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = {
  parent: account
  name: 'gpt-4'
  properties: {
    model: {
      format: 'OpenAI'
      name: 'gpt-35-turbo'
      version: gptVersion
    }
  }
  sku: {
    capacity: 10
    name: 'Standard'
  }
}


resource adaEmbeddingsdeployment 'Microsoft.CognitiveServices/accounts/deployments@2023-05-01' = {
  parent: account
  name: 'text-embedding-ada-002'
  properties: {
    model: {
      format: 'OpenAI'
      name: 'text-embedding-ada-002'
      version: '2'
    }
  }
  sku: {
    capacity: 10
    name: 'Standard'
  }
  dependsOn: [gpt4deployment]
}

output endpoint string = account.properties.endpoint
output accountName string = account.name
output openaiEndpoint string = account.properties.endpoint
output maxConversationTokens int = maxConversationTokens
