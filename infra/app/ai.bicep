metadata description = 'Create AI accounts.'

param accountName string
param location string = resourceGroup().location
param tags object = {}

var maxConversationTokens = 2000
var modelName = 'chatmodel'

module openAiAccount '../core/ai/cognitive-services/account.bicep' = {
  name: 'openai-account'
  params: {
    name: accountName
    location: location
    tags: tags
    kind: 'OpenAI'
    sku: 'S0'
    enablePublicNetworkAccess: true
  }
}

module openAiModelDeployment '../core/ai/cognitive-services/deployment.bicep' = {
  name: 'openai-model-deployment'
  params: {
    name: modelName
    parentAccountName: openAiAccount.outputs.name
    skuName: 'Standard'
    skuCapacity: 20
    modelName: 'gpt-35-turbo'
    modelFormat: 'OpenAI'
    modelVersion: '0301'
  }
}

output endpoint string = openAiAccount.outputs.endpoint
output accountName string = openAiAccount.outputs.name

output modelDeploymentName string = modelName
output maxConversationTokens int = maxConversationTokens
