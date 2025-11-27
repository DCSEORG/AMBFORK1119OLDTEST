# Azure Services Architecture Diagram
# Expense Management System

```
┌─────────────────────────────────────────────────────────────────────────────────┐
│                          Azure Resource Group                                    │
│                         (rg-expensemgmt-demo)                                   │
│                                                                                  │
│  ┌──────────────────────────────────────────────────────────────────────────┐  │
│  │                         UK South Region                                   │  │
│  │                                                                           │  │
│  │   ┌─────────────────────┐        ┌─────────────────────┐                │  │
│  │   │                     │        │                     │                │  │
│  │   │   App Service       │◄──────►│   Azure SQL        │                │  │
│  │   │   (S1 Standard)     │        │   Database         │                │  │
│  │   │                     │   MI   │   (Basic)          │                │  │
│  │   │   ┌─────────────┐   │        │                     │                │  │
│  │   │   │ Expense     │   │        │   ┌─────────────┐   │                │  │
│  │   │   │ Management  │   │        │   │ Northwind   │   │                │  │
│  │   │   │ App (.NET 8)│   │        │   │ Database    │   │                │  │
│  │   │   └─────────────┘   │        │   └─────────────┘   │                │  │
│  │   │                     │        │                     │                │  │
│  │   └──────────┬──────────┘        └─────────────────────┘                │  │
│  │              │                                                           │  │
│  │              │ User Assigned                                            │  │
│  │              │ Managed Identity                                         │  │
│  │              ▼                                                           │  │
│  │   ┌─────────────────────┐                                               │  │
│  │   │ mid-appmodassist-*  │                                               │  │
│  │   │ Managed Identity    │                                               │  │
│  │   └──────────┬──────────┘                                               │  │
│  │              │                                                           │  │
│  └──────────────┼───────────────────────────────────────────────────────────┘  │
│                 │                                                                │
│  ┌──────────────┼───────────────────────────────────────────────────────────┐  │
│  │              │           Sweden Central Region                            │  │
│  │              │           (Optional - with GenAI)                          │  │
│  │              ▼                                                            │  │
│  │   ┌─────────────────────┐        ┌─────────────────────┐                │  │
│  │   │                     │        │                     │                │  │
│  │   │   Azure OpenAI      │        │   Azure AI Search   │                │  │
│  │   │   (S0)              │        │   (Basic)           │                │  │
│  │   │                     │        │                     │                │  │
│  │   │   ┌─────────────┐   │        │   RAG Support       │                │  │
│  │   │   │ GPT-4o      │   │        │   (Future use)      │                │  │
│  │   │   │ Model       │   │        │                     │                │  │
│  │   │   └─────────────┘   │        │                     │                │  │
│  │   │                     │        │                     │                │  │
│  │   └─────────────────────┘        └─────────────────────┘                │  │
│  │                                                                           │  │
│  └───────────────────────────────────────────────────────────────────────────┘  │
│                                                                                  │
└─────────────────────────────────────────────────────────────────────────────────┘

Data Flow:
─────────
1. Users access the App Service via HTTPS
2. App Service authenticates to SQL using Managed Identity
3. App Service authenticates to Azure OpenAI using Managed Identity
4. Chat UI queries Azure OpenAI for natural language interactions
5. Function calling enables AI to query database via stored procedures

Authentication:
───────────────
• Azure AD-Only Authentication for SQL (MCAPS compliant)
• Managed Identity for all service-to-service communication
• No passwords or connection strings with credentials

Deployment Options:
──────────────────
• deploy.sh       - Basic deployment (App + SQL only)
• deploy-with-chat.sh - Full deployment (includes GenAI resources)
```

## Resource Summary

| Resource | Type | SKU | Location |
|----------|------|-----|----------|
| App Service Plan | Microsoft.Web/serverfarms | S1 Standard | UK South |
| App Service | Microsoft.Web/sites | - | UK South |
| SQL Server | Microsoft.Sql/servers | - | UK South |
| SQL Database | Microsoft.Sql/servers/databases | Basic | UK South |
| Managed Identity | Microsoft.ManagedIdentity/userAssignedIdentities | - | UK South |
| Azure OpenAI* | Microsoft.CognitiveServices/accounts | S0 | Sweden Central |
| AI Search* | Microsoft.Search/searchServices | Basic | Sweden Central |

*Only deployed with `deploy-with-chat.sh`

## Security Features

- ✅ Azure AD-Only Authentication (SQL)
- ✅ User Assigned Managed Identity
- ✅ No SQL authentication credentials
- ✅ HTTPS only
- ✅ TLS 1.2 minimum
- ✅ RBAC role assignments for AI services
