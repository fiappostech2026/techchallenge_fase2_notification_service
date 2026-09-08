// Infraestrutura como código da FCG.Notifications.Function (Azure Functions, Consumption plan).
// Reproduz os recursos criados manualmente via `az` durante a migração da Fase 3: storage
// account (exigida pelo runtime do Functions), Application Insights (telemetria/logs) e a
// Function App em si. O resource group é criado antes, fora deste template
// (`az group create`), e este arquivo é implantado com `az deployment group create`.

@description('Nome da Function App (também usado como base para os demais nomes de recurso).')
param functionAppName string

@description('Região dos recursos.')
param location string = resourceGroup().location

var storageAccountName = toLower(replace('${functionAppName}st', '-', ''))
var appInsightsName = '${functionAppName}-insights'
var hostingPlanName = '${functionAppName}-plan'

resource storageAccount 'Microsoft.Storage/storageAccounts@2023-01-01' = {
  name: take(storageAccountName, 24)
  location: location
  sku: {
    name: 'Standard_LRS'
  }
  kind: 'StorageV2'
}

resource appInsights 'Microsoft.Insights/components@2020-02-02' = {
  name: appInsightsName
  location: location
  kind: 'web'
  properties: {
    Application_Type: 'web'
  }
}

resource hostingPlan 'Microsoft.Web/serverfarms@2023-01-01' = {
  name: hostingPlanName
  location: location
  sku: {
    name: 'Y1'
    tier: 'Dynamic'
  }
}

resource functionApp 'Microsoft.Web/sites@2023-01-01' = {
  name: functionAppName
  location: location
  kind: 'functionapp'
  properties: {
    serverFarmId: hostingPlan.id
    siteConfig: {
      netFrameworkVersion: 'v8.0'
      appSettings: [
        {
          name: 'AzureWebJobsStorage'
          value: 'DefaultEndpointsProtocol=https;AccountName=${storageAccount.name};AccountKey=${storageAccount.listKeys().keys[0].value};EndpointSuffix=${environment().suffixes.storage}'
        }
        {
          name: 'FUNCTIONS_WORKER_RUNTIME'
          value: 'dotnet-isolated'
        }
        {
          name: 'FUNCTIONS_EXTENSION_VERSION'
          value: '~4'
        }
        {
          name: 'APPLICATIONINSIGHTS_CONNECTION_STRING'
          value: appInsights.properties.ConnectionString
        }
        // RabbitMqConnection NÃO entra aqui de propósito — contém credencial do CloudAMQP e é
        // configurada à parte via `az functionapp config appsettings set` (ver README, passo 3),
        // pra não ficar em texto plano neste arquivo versionado no repositório.
      ]
    }
  }
}

output functionAppName string = functionApp.name
output functionAppHostname string = functionApp.properties.defaultHostName
