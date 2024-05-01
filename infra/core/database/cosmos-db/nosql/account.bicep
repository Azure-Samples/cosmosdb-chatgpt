metadata description = 'Create an Azure Cosmos DB for NoSQL account.'

param name string
param location string = resourceGroup().location
param tags object = {}

@description('Enables serverless for this account. Defaults to false.')
param enableServerless bool = false

@description('Disables key-based authentication. Defaults to false.')
param disableKeyBasedAuth bool = false

module account '../account.bicep' = {
  name: 'cosmos-db-nosql-account'
  params: {
    name: name
    location: location
    tags: tags
    kind: 'GlobalDocumentDB'
    enableServerless: enableServerless
    disableKeyBasedAuth: disableKeyBasedAuth
  }
}

output endpoint string = account.outputs.endpoint
output name string = account.outputs.name
