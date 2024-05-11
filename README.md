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

This sample application shows how to build a Generative-AI application using Azure Cosmos DB using its new vector search capabilities and Azure OpenAI Service and Semantic Kernel. The sample provides practical guidance on many concepts you will need to design and build these types of applications including how to: generate embeddings on user input, generating responses from an LLM, managing chat history and token consumption for conversational context, building a semantic cache to enhance performance, and an introduction to using Semantic Kernel which can be used to build complex Generative-AI agents and scenarios.

![Cosmos DB + ChatGPT user interface](screenshot.png)

## Concepts Covered

This application demonstrates the following concepts and how to implement them:

- The basics of building a chat-based application with a data model that can scale using Azure Cosmos DB.
- Generating multiple different completions and embeddings using Azure OpenAI Service.
- Context window (chat history) for natural conversational interactions with an LLM.
- Manage tokens used by Azure OpenAI Service and how to use a tokenizer.
- Semantic cache using Azure Cosmos DB for NoSQL vector search for improved performance.
- Semantic Kernel SDK for Text Generation and Embedding Generation.

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

Click one of the ***Deploy to Azure*** buttons belows and follow the prompts in Azure Portal to deploy this solution. The first option deploys a new Azure OpenAI account. The second allows you to use an existing Azure OpenAI account.

The provided ARM Templates will provision the following resources:

1. **Azure Cosmos DB** Serverless account with database and container with a vector embedding policy on the container and vector indexes defined.
1. **Azure App service** This service can be configured to run on App Service free tier.
1. **Azure OpenAI** You must also specify a name for the deployment of the "gpt" and "embedding" models used by this application.

**Note:** You must have access to Azure Open AI service from your subscription before attempting to deploy this application.

All connection information for Azure Cosmos DB and Azure Open AI is zero-touch and injected as environment variables in the Azure App Service instance at deployment time. 

[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FAzure-Samples%2Fcosmosdb-chatgpt%2Fmain%2Fazuredeploy.json)

**Note:** If you already have an Azure OpenAI account deployed and wish to use it with this application, use this template instead. You will be prompted to provide the name of the Azure OpenAI account, a key, and the name of the GPT 3.5 Turbo model used for completions.
[![Deploy to Azure](https://aka.ms/deploytoazurebutton)](https://portal.azure.com/#create/Microsoft.Template/uri/https%3A%2F%2Fraw.githubusercontent.com%2FAzure-Samples%2Fcosmosdb-chatgpt%2Fmain%2Fazuredeploy-no-aoai.json)


### Quickstart

This solution has a number of quickstarts than you can run through to learn about the features in this sample and how to implement them yourself.

Please see [Quickstarts](quickstart.md)


## Clean up

To remove all the resources used by this sample, you must first manually delete the deployed model within the Azure AI service (if you originally provisioned it using this sample). You can then delete the resource group for your deployment. This will delete all remaining resources.

## Resources

- [Azure Cosmos DB + Azure OpenAI ChatGPT Blog Post Announcement](https://devblogs.microsoft.com/cosmosdb/chatgpt-azure-cosmos-db/)
- [Azure OpenAI Service documentation](https://learn.microsoft.com/azure/cognitive-services/openai/)
- [Azure App Service documentation](https://learn.microsoft.com/azure/app-service/)
- [ASP.NET Core Blazor documentation](https://dotnet.microsoft.com/apps/aspnet/web-apps/blazor)
