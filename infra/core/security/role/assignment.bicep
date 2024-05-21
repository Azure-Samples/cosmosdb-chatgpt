metadata description = 'Creates a role-based access control assignment.'

@description('Id of the role definition to assign to the targeted principal and account.')
param roleDefinitionId string

@description('Id of the principal to assign the role definition for the account.')
param principalId string

@allowed([
  'Device'
  'ForeignGroup'
  'Group'
  'ServicePrincipal'
  'User'
  'None'
])
@description('Type of principal associated with the principal Id.')
param principalType string

resource assignment 'Microsoft.Authorization/roleAssignments@2022-04-01' = {
  name: guid(subscription().id, resourceGroup().id, principalId, roleDefinitionId)
  scope: resourceGroup()
  properties: {
    principalId: principalId
    roleDefinitionId: roleDefinitionId
    principalType: principalType != 'None' ? principalType : null
  }
}

output id string = assignment.id
