metadata description = 'Creates an Azure App Service site.'

param name string
param location string = resourceGroup().location
param tags object = {}

@description('Name of the parent plan for the site.')
param parentPlanName string

@allowed([
  'dotnet'
  'dotnetcore'
  'dotnet-isolated'
  'node'
  'python'
  'java'
  'powershell'
  'custom'
])
@description('Runtime to use for the site.')
param runtimeName string

@description('Version of the runtime to use for the site.')
param runtimeVersion string

@description('The OS kind of the site. Defaults to "app, linux"')
param kind string = 'app,linux'

@description('If the site should be always on. Defaults to true.')
param alwaysOn bool = true

@description('Allowed origins for client-side CORS request on the site.')
param allowedCorsOrigins string[] = []

@description('Enable system-assigned managed identity. Defaults to false.')
param enableSystemAssignedManagedIdentity bool = false

@description('List of user-assigned managed identities. Defaults to an empty array.')
param userAssignedManagedIdentityIds string[] = []

var linuxFxVersion = '${runtimeName}|${runtimeVersion}'

resource plan 'Microsoft.Web/serverfarms@2022-09-01' existing = {
  name: parentPlanName
}

resource site 'Microsoft.Web/sites@2022-09-01' = {
  name: name
  location: location
  tags: tags
  kind: kind
  identity: {
    type: enableSystemAssignedManagedIdentity
      ? !empty(userAssignedManagedIdentityIds) ? 'SystemAssigned, UserAssigned' : 'SystemAssigned'
      : !empty(userAssignedManagedIdentityIds) ? 'UserAssigned' : 'None'
    userAssignedIdentities: !empty(userAssignedManagedIdentityIds)
      ? toObject(userAssignedManagedIdentityIds, uaid => uaid, uaid => {})
      : null
  }
  properties: {
    serverFarmId: plan.id
    siteConfig: {
      linuxFxVersion: linuxFxVersion
      alwaysOn: alwaysOn
      http20Enabled: true
      minTlsVersion: '1.2'
      cors: {
        allowedOrigins: union(['https://portal.azure.com', 'https://ms.portal.azure.com'], allowedCorsOrigins)
      }
    }
    httpsOnly: true
  }
}

output endpoint string = 'https://${site.properties.defaultHostName}'
output name string = site.name
output managedIdentityPrincipalId string = enableSystemAssignedManagedIdentity ? site.identity.principalId : ''
