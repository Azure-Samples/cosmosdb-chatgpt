metadata description = 'Create identity resources.'

param identityName string
param location string = resourceGroup().location
param tags object = {}

module userAssignedIdentity '../core/security/identity/user-assigned.bicep' = {
  name: 'user-assigned-identity'
  params: {
    name: identityName
    location: location
    tags: tags
  }
}

output name string = userAssignedIdentity.outputs.name
output resourceId string = userAssignedIdentity.outputs.resourceId
output principalId string = userAssignedIdentity.outputs.principalId
output clientId string = userAssignedIdentity.outputs.clientId
