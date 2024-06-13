metadata description = 'Create an Azure Cosmos DB for NoSQL database.'

param name string
param tags object = {}

@description('Name of the parent Azure Cosmos DB account.')
param parentAccountName string

@description('Enables throughput setting at this resource level. Defaults to false.')
param setThroughput bool = false

@description('Enables autoscale. If setThroughput is enabled, defaults to false.')
param autoscale bool = false

@description('The amount of throughput set. If setThroughput is enabled, defaults to 400.')
param throughput int = 400

var options = setThroughput
  ? autoscale
      ? {
          autoscaleSettings: {
            maxThroughput: throughput
          }
        }
      : {
          throughput: throughput
        }
  : {}

resource account 'Microsoft.DocumentDB/databaseAccounts@2023-04-15' existing = {
  name: parentAccountName
}

resource database 'Microsoft.DocumentDB/databaseAccounts/sqlDatabases@2023-04-15' = {
  name: name
  parent: account
  tags: tags
  properties: {
    options: options
    resource: {
      id: name
    }
  }
}

output name string = database.name
