metadata description = 'Creates an Azure Container Apps app.'

param name string
param location string = resourceGroup().location
param tags object = {}

@description('Name of the parent environment for the app.')
param parentEnvironmentName string

@description('Specifies the docker container image to deploy.')
param containerImage string = 'mcr.microsoft.com/azuredocs/containerapps-helloworld:latest'

@description('Specifies the container port.')
param targetPort int = 80

@description('Number of CPU cores the container can use. Can have a maximum of two decimals.')
param cpuCores string = '0.25'

@description('Amount of memory (in gibibytes, GiB) allocated to the container up to 4GiB. Can have a maximum of two decimals. Ratio with CPU cores must be equal to 2.')
param memorySize string = '0.5Gi'

@description('Minimum number of replicas that will be deployed.')
@minValue(1)
@maxValue(25)
param minReplicas int = 1

@description('Maximum number of replicas that will be deployed.')
@minValue(1)
param maxReplicas int = 1

type envVar = {
  name: string
  secretRef: string?
  value: string?
}

@description('The environment variables for the container.')
param environmentVariables envVar[] = []

type secret = {
  name: string
  identity: string?
  keyVaultUrl: string?
  value: string?
}

@description('The secrets required for the container')
param secrets secret[] = []

@description('Specifies if the resource ingress is exposed externally.')
param externalAccess bool = true

@description('Specifies if Ingress is enabled for the container app.')
param ingressEnabled bool = true

@description('Allowed CORS origins.')
param allowedOrigins string[] = []

type registry = {
  server: string
  identity: string?
  username: string?
  passwordSecretRef: string?
}

@description('List of registries. Defaults to an empty list.')
param registries registry[] = []

@description('Enable system-assigned managed identity. Defaults to false.')
param enableSystemAssignedManagedIdentity bool = false

@description('List of user-assigned managed identities. Defaults to an empty array.')
param userAssignedManagedIdentityIds string[] = []

resource environment 'Microsoft.App/managedEnvironments@2023-05-01' existing = {
  name: parentEnvironmentName
}

resource app 'Microsoft.App/containerApps@2023-05-01' = {
  name: name
  location: location
  tags: tags
  identity: {
    type: enableSystemAssignedManagedIdentity ? !empty(userAssignedManagedIdentityIds) ? 'SystemAssigned,UserAssigned' : 'SystemAssigned' : !empty(userAssignedManagedIdentityIds) ? 'UserAssigned' : 'None'
    userAssignedIdentities: !empty(userAssignedManagedIdentityIds) ? toObject(userAssignedManagedIdentityIds, uaid => uaid, uaid => {}) : null
  }
  properties: {
    environmentId: environment.id
    configuration: {
      ingress: ingressEnabled ? {
        external: externalAccess
        targetPort: targetPort
        transport: 'auto'
        corsPolicy: {
          allowedOrigins: union([ 'https://portal.azure.com', 'https://ms.portal.azure.com' ], allowedOrigins)
        }
      } : null
      secrets: secrets
      registries: !empty(registries) ? registries : null
    }
    template: {
      containers: [
        {
          image: containerImage
          name: name
          resources: {
            cpu: json(cpuCores)
            memory: memorySize
          }
          env: environmentVariables
        }
      ]
      scale: {
        minReplicas: minReplicas
        maxReplicas: maxReplicas
      }
    }
  }
}

output endpoint string = ingressEnabled ? 'https://${app.properties.configuration.ingress.fqdn}' : ''
output name string = app.name
output systemAssignedManagedIdentityPrincipalId string = enableSystemAssignedManagedIdentity ? app.identity.principalId : ''
