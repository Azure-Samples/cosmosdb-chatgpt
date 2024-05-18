---
page_type: sample
languages:
- csharp
products:
- azure-cosmos-db
- azure-openai
name: Hands on Lab to build a Chat App using Azure Cosmos DB for NoSQL, Azure OpenAI Service and Semantic Kernel
urlFragment: chat-app
description: Hands on Lab to implement a Generative AI chat application that demonstrates context windows, semantic cache and Semantic Kernel integration.
azureDeploy: https://raw.githubusercontent.com/azure-samples/cosmosdb-chatgpt/start/azuredeploy.json
---

# Hands-on-Lab: Build a Copilot app using Azure Cosmos DB & Azure OpenAI Service

This Hands on Lab shows how to build a Generative-AI application using Azure Cosmos DB using its new vector search capabilities and Azure OpenAI Service and Semantic Kernel. The sample provides practical guidance on many concepts you will need to design and build these types of applications.

![Cosmos DB + ChatGPT user interface](screenshot.png)

## Exercises

This lab will walk users through the following exercises:

- Setting up a local environment to complete the lab.
- The basics of building a highly scalable Generative-AI chat application using Azure Cosmos DB for NoSQL.
- Generating completions and embeddings using Azure OpenAI Service.
- Managing a context window (chat history) for natural conversational interactions with an LLM.
- Manage token consumption and payload sizes for Azure OpenAI Service requests.
- Building a semantic cache using Azure Cosmos DB for NoSQL vector search for improved performance and cost.
- Using the Semantic Kernel SDK for completion and embeddings generation.

## Getting Started

### Prerequisites

- Azure Subscription
- Subscription access to Azure OpenAI service. Start here to [Request Access to Azure OpenAI Service](https://aka.ms/oaiapply)
- Visual Studio, VS Code, GitHub Codespaces or another editor to edit or view the source for this sample.
- Azure Cosmos DB for NoSQL Vector Search Preview enrollment

This lab utilizes a preview feature, **Vector search for Azure Cosmos DB for NoSQL** which requires preview feature registration. Follow the below steps to register. You must be enrolled before you can deploy this solution:
 
1. Navigate to your Azure Cosmos DB for NoSQL resource page.
1. Select the "Features" pane under the "Settings" menu item.
1. Select for “Vector Search in Azure Cosmos DB for NoSQL”.
1. Read the description to confirm you want to enroll in the preview.
1. Select "Enable" to enroll in the preview.

### Service Deployment

1. Click one of the ***Deploy to Azure*** buttons belows and follow the prompts in Azure Portal to deploy this solution. There is one that deploys a new Azure OpenAI account and a second that allows you to use an existing Azure OpenAI account.

The provided ARM templates will provision the following resources:

1. **Azure Cosmos DB** Serverless account with database and container with a vector embedding policy on the container and vector indexes defined.
1. **Azure OpenAI Service** Service for generating completions and embeddings used for Generative-AI applications.
1. **Azure App service** Web application host for the ASP.NET Blazor application.

**Note:** You must have access to Azure Open AI service from your subscription before attempting to deploy this application.

All connection information for Azure Cosmos DB and Azure Open AI is zero-touch and injected as environment variables in the Azure App Service instance at deployment time.

[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FAzure-Samples%2Fcosmosdb-chatgpt%2Fmain%2Fazuredeploy.json)

**Deploy with existing Azure OpenAI account:** Use a pre-existing Azure OpenAI service account with GPT 3.5 Turbo and ADA-002. Use this Deploy to Azure button below. Provide Azure OpenAI account name, key, and deployment names for GPT 3.5 Turbo and ADA-002 models.

[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FAzure-Samples%2Fcosmosdb-chatgpt%2Fmain%2Fazuredeploy-no-aoai.json)


### Configuring a lab environment

This lab can be run in GitHub Codespaces or locally in Visual Studio Code in a DevContainer. For either, select an option below.

|GitHub Codespaces|Visual Studio Code|
|---|---|
|[![Open in GitHub Codespaces](https://github.com/codespaces/badge.svg)](https://codespaces.new/Azure-Samples/cosmosdb-chatgpt/tree/lab-start)|[![Open in Dev Container](https://img.shields.io/static/v1?style=for-the-badge&label=Dev+Containers&message=Open&color=blue&logo=visualstudiocode)](https://vscode.dev/redirect?url=vscode://ms-vscode-remote.remote-containers/cloneInVolume?url=https://github.com/azure-samples/cosmosdb-chatpgpt)|


For any changes, this GitHub repository needs to be cloned to your local machine. The lab manual include details on how to configure to connect with the services deployed in Azure. Only Cosmos DB and Azure OpenAI Service require connection information to run locally.

To run locally, copy the contents of the **appsettings.json** file in the /src folder of the project into a new **appsettings.Development.json** file in the same folder, then copy the Azure Cosmos DB and Azure OpenAI endpoint and key values from the environment variables in Azure App Service into.

Here is an example appsettings.json file you will need to update.

**Note:** If you deploy using an existing Azure OpenAI account you will need to update the Completion and Embedding model names to match those in your existing account.

```json
{
  "DetailedErrors": true,
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "CosmosDb": {
    "Endpoint": "your-azure-cosmos-endpoint",
    "Key": "your-azure-cosmos-key",
    "Database": "ChatDatabase",
    "ChatContainer": "ChatContainer",
    "CacheContainer":  "CacheContainer"
  },
  "OpenAi": {
    "Endpoint": "your-azure-openai-endpoint",
    "Key": "your-azure-openai-key",
    "CompletionDeploymentName": "completions",
    "EmbeddingDeploymentName": "embeddings"
  },
  "Chat": {
    "MaxConversationTokens": "100",
    "CacheSimilarityScore": "0.99"
  }
}
```

### Running the lab

To get started navigate to the [Lab Manual](lab-guide.md) and begin!


## Clean up

To remove all the resources used by this sample, delete the resource group for your deployment. This will delete any provisioned resources.

## Resources

To learn more about the services and features demonstrated in this sample, see the following:

- [Azure Cosmos DB for NoSQL Vector Search announcement](https://aka.ms/CosmosDBDiskANNBlog/)
- [Azure OpenAI Service documentation](https://learn.microsoft.com/azure/cognitive-services/openai/)
- [Semantic Kernel](https://learn.microsoft.com/semantic-kernel/overview)
- [Azure App Service documentation](https://learn.microsoft.com/azure/app-service/)
- [ASP.NET Core Blazor documentation](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
