---
page_type: sample
languages:
- csharp
products:
- azure-cosmos-db
- azure-openai
name: Build a Copilot app using Azure Cosmos DB & Azure OpenAI Service
urlFragment: chat-app
description: Sample application that implements a Generative AI chat application that demonstrates context windows, semantic cache and Semantic Kernel integration.
azureDeploy: https://raw.githubusercontent.com/azure-samples/cosmosdb-chatgpt/main/azuredeploy.json
---

# Build a Copilot app using Azure Cosmos DB & Azure OpenAI Service

This sample application shows how to build a Generative-AI application using Azure Cosmos DB using its new vector search capabilities and Azure OpenAI Service and Semantic Kernel. The sample provides practical guidance on many concepts you will need to design and build these types of applications.

![Cosmos DB + ChatGPT user interface](screenshot.png)

## Concepts Covered

This application demonstrates the following concepts and how to implement them:

- The basics of building a highly scalable Generative-AI chat application using Azure Cosmos DB for NoSQL.
- Generating completions and embeddings using Azure OpenAI Service.
- Managing a context window (chat history) for natural conversational interactions with an LLM.
- Manage token consumption and payload sizes for Azure OpenAI Service requests.
- Building a semantic cache using Azure Cosmos DB for NoSQL vector search for improved performance and cost.
- Using the Semantic Kernel SDK for completion and embeddings generation.

## Getting Started

### Prerequisites

- Azure Subscription
- [Azure Developer CLI](https://aka.ms/azd-install)
- Subscription access to Azure OpenAI service. Start here to [Request Access to Azure OpenAI Service](https://aka.ms/oaiapply)
- Visual Studio, VS Code, GitHub Codespaces or another editor to edit or view the source for this sample.
- Azure Cosmos DB for NoSQL Vector Search Preview enrollment

This lab utilizes a preview feature, **Vector search for Azure Cosmos DB for NoSQL** which requires preview feature registration. Follow the below steps to register. You must be enrolled before you can deploy this solution:
 
1. Navigate to your Azure Cosmos DB for NoSQL resource page.
1. Select the "Features" pane under the "Settings" menu item.
1. Select for “Vector Search in Azure Cosmos DB for NoSQL”.
1. Read the description to confirm you want to enroll in the preview.
1. Select "Enable" to enroll in the preview.

### Instructions

1. Log in to AZD.
    
    ```bash
    azd auth login
    ```

    > [!NOTE]  
    > This is only required once per install.

1. Initialize the project in the current directory.
    
    ```bash
    azd init --template cosmosdb-chatgpt
    ```

1. Deploy the services to Azure, build your container, and deploy the application.
    
    ```bash
    azd up
    ```

### Quickstart

This solution has a number of quickstarts than you can run through to learn about the features in this sample and how to implement them yourself.

Please see [Quickstarts](quickstart.md)


## Clean up

To remove all the resources used by this sample, delete the resource group for your deployment.

## Resources

To learn more about the services and features demonstrated in this sample, see the following:

- [Azure Cosmos DB for NoSQL Vector Search announcement](https://aka.ms/CosmosDBDiskANNBlog/)
- [Azure OpenAI Service documentation](https://learn.microsoft.com/azure/cognitive-services/openai/)
- [Semantic Kernel](https://learn.microsoft.com/semantic-kernel/overview)
- [Azure App Service documentation](https://learn.microsoft.com/azure/app-service/)
- [ASP.NET Core Blazor documentation](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
