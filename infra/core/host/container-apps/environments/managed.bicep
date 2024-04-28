metadata description = 'Creates an Azure Container Apps managed environment.'

param name string
param location string = resourceGroup().location
param tags object = {}

resource environment 'Microsoft.App/managedEnvironments@2023-05-01' = {
  name: name
  location: location
  tags: tags
  properties: {}
}

output name string = environment.name
