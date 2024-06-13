metadata description = 'Create database accounts.'

param accountName string
param location string = resourceGroup().location
param tags object = {}

var database = {
  name: 'chatdatabase' // Database for chat application
}

var containers = [
  {
    name: 'chatcontainer' // Container for chat sessions and messages
    partitionKeyPaths: [
      '/sessionId' // Partition on the session identifier
    ]
    indexingPolicy: {
      automatic: true
      indexingMode: 'consistent'
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
    vectorEmbeddingPolicy: {
      vectorEmbeddings: []
    }
  }
  {
    name: 'cachecontainer' // Container for chat sessions and messages
    partitionKeyPaths: [
      '/id' // Partition on the session identifier
    ]
    indexingPolicy: {
      automatic: true
      indexingMode: 'consistent'
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
]

module cosmosDbAccount '../core/database/cosmos-db/nosql/account.bicep' = {
  name: 'cosmos-db-account'
  params: {
    name: accountName
    location: location
    tags: tags
    enableServerless: true
    enableVectorSearch: true
  }
}

module cosmosDbDatabase '../core/database/cosmos-db/nosql/database.bicep' = {
  name: 'cosmos-db-database-${database.name}'
  params: {
    name: database.name
    parentAccountName: cosmosDbAccount.outputs.name
    tags: tags
    setThroughput: false
  }
}

module cosmosDbContainers '../core/database/cosmos-db/nosql/container.bicep' = [
  for (container, _) in containers: {
    name: 'cosmos-db-container-${container.name}'
    params: {
      name: container.name
      parentAccountName: cosmosDbAccount.outputs.name
      parentDatabaseName: cosmosDbDatabase.outputs.name
      tags: tags
      setThroughput: false
      partitionKeyPaths: container.partitionKeyPaths
      indexingPolicy: container.indexingPolicy
      vectorEmbeddingPolicy: container.vectorEmbeddingPolicy
    }
  }
]

output endpoint string = cosmosDbAccount.outputs.endpoint
output accountName string = cosmosDbAccount.outputs.name

output database object = {
  name: cosmosDbDatabase.outputs.name
}
output containers array = [
  for (_, index) in containers: {
    name: cosmosDbContainers[index].outputs.name
  }
]
