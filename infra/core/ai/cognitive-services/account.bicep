metadata description = 'Creates an Azure Cognitive Services account.'

param name string
param location string = resourceGroup().location
param tags object = {}

@allowed([ 'OpenAI', 'ComputerVision', 'TextTranslation', 'CognitiveServices' ])
@description('Sets the kind of account.')
param kind string

@allowed([
  'S0'
])
@description('SKU for the account. Defaults to "S0".')
param sku string = 'S0'

@description('Enables access from public networks. Defaults to true.')
param enablePublicNetworkAccess bool = true

resource account 'Microsoft.CognitiveServices/accounts@2023-05-01' = {
  name: name
  location: location
  tags: tags
  kind: kind
  sku: {
    name: sku
  }
  properties: {
    customSubDomainName: name
    publicNetworkAccess: enablePublicNetworkAccess ? 'Enabled' : 'Disabled'
  }
}

output endpoint string = account.properties.endpoint
output name string = account.name
