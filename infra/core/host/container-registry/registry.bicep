metadata description = 'Creates an Azure Container Registry registry.'

param name string
param location string = resourceGroup().location
param tags object = {}

@description('Whether admin user is enabled. Defaults to false.')
param adminUserEnabled bool = false

@description('Whether anonymous pull is enabled. Defaults to false.')
param anonymousPullEnabled bool = false

@description('Enables public network access. Defaults to false.')
param publicNetworkAccessEnabled bool = false

@allowed([
  'Basic'
  'Standard'
  'Premium'
])
@description('Name of the SKU. Defaults to "Standard".')
param skuName string = 'Standard'

resource registry 'Microsoft.ContainerRegistry/registries@2023-01-01-preview' = {
  name: name
  location: location
  tags: tags
  sku: {
    name: skuName
  }
  properties: {
    adminUserEnabled: adminUserEnabled
    anonymousPullEnabled: anonymousPullEnabled
    publicNetworkAccess: publicNetworkAccessEnabled ? 'Enabled' : 'Disabled'
  }
}

output endpoint string = registry.properties.loginServer
output name string = registry.name
