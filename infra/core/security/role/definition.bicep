metadata description = 'Creates a role-based access control role definition.'

@description('Name of the role definiton.')
param definitionName string

@description('Description for the role definition.')
param definitionDescription string

@description('Array of control-plane actions allowed for the role definition.')
param actions string[] = []

@description('Array of control-plane actions disallowed for the role definition.')
param notActions string[] = []

@description('Array of data-plane actions allowed for the role definition.')
param dataActions string[] = []

@description('Array of data-plane actions disallowed for the role definition.')
param notDataActions string[] = []

resource definition 'Microsoft.Authorization/roleDefinitions@2022-04-01' = {
  name: guid(subscription().id, resourceGroup().id)
  scope: resourceGroup()
  properties: {
    roleName: definitionName
    description: definitionDescription
    type: 'CustomRole'
    permissions: [
      {
        actions: actions
        notActions: notActions
        dataActions: dataActions
        notDataActions: notDataActions
      }
    ]
    assignableScopes: [
      resourceGroup().id
    ]
  }
}

output id string = definition.id
