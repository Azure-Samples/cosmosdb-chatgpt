metadata description = 'Creates an Azure App Service configuration for a site.'

@description('Name of the parent App Service site for the configuration.')
param parentSiteName string

@secure()
param appSettings object = {}

resource site 'Microsoft.Web/sites@2022-09-01' existing = {
  name: parentSiteName
}

resource config 'Microsoft.Web/sites/config@2022-09-01' = {
  name: 'appsettings'
  parent: site
  kind: 'string'
  properties: appSettings
}
