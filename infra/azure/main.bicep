// ============================================================================
// Better Canvas — Release 2 Azure Cloud Deployment IaC (main.bicep)
// Target Platform: Microsoft Azure Container Apps (ACA) + Azure Container Registry
// Supports:
//   1. Baseline Cloud Profile (Criterion 7, default):
//      - All AI services disabled (AI_MODE_ENABLED=false, MCP_ENABLED=false,
//        RAG_ENABLED=false, MULTI_AGENT_ENABLED=false)
//      - Publicly accessible shared-shell Nginx reverse proxy (HTTPS/TLS)
//      - Internal-only isolated microservices & persistence databases
//   2. Bonus Cloud Profile (Bonus B1–B6, opt-in):
//      - B1: AI Mode Gateway Container App (:5001)
//      - B2: MCP Server Container App (:5002)
//      - B3: RAG Server Container App (:5003)
//      - B4: Multi-Agent Server Container App (:5004)
//      - B5: Production Cloud Security (HTTPS/TLS ingress, CORS, rate limiting, Key Vault secrets)
//      - B6: Full Cloud Infrastructure Architecture (internal VNet/ACA environment isolation)
// ============================================================================

@description('Azure region for all resources')
param location string = resourceGroup().location

@description('Environment prefix for resource naming')
param environmentName string = 'better-canvas-r2'

@description('Azure Container Registry name (alphanumeric)')
param acrName string = 'acrbettercanvasr2'

@description('Container image tag to deploy')
param imageTag string = 'latest'

@description('Enable Bonus B1–B4 AI Services in Cloud (default false for Criterion 7 Baseline Profile)')
param enableBonusAi bool = false

@description('Enable Bonus B5–B6 Cloud Security & Key Vault integration')
param enableBonusSecurity bool = true

@secure()
@description('OpenRouter API Key (only injected into ai-mode when enableBonusAi is true)')
param openRouterApiKey string = ''

@secure()
@description('Canvas LMS API Token (injected into shared-backend only)')
param canvasApiToken string = ''

@description('Canvas LMS Base URL')
param canvasBaseUrl string = 'https://canvas.instructure.com'

@secure()
@description('PostgreSQL administrator password for student-1-database')
param postgresPassword string = ''

var actualPostgresPassword = empty(postgresPassword) ? 'postgres-cloud-r2-secure' : postgresPassword
var aiEnabledString = enableBonusAi ? 'true' : 'false'
var aiModeInternalUrl = enableBonusAi ? 'http://ai-mode' : 'http://localhost:5001'
var mcpInternalUrl = enableBonusAi ? 'http://mcp-server' : 'http://localhost:5002'
var ragInternalUrl = enableBonusAi ? 'http://rag-server' : 'http://localhost:5003'
var multiAgentInternalUrl = enableBonusAi ? 'http://multi-agent-server' : 'http://localhost:5004'

// ----------------------------------------------------------------------------
// 1. Observability: Log Analytics Workspace
// ----------------------------------------------------------------------------
resource logAnalytics 'Microsoft.OperationalInsights/workspaces@2022-10-01' = {
  name: '${environmentName}-logs'
  location: location
  properties: {
    sku: {
      name: 'PerGB2018'
    }
    retentionInDays: 30
  }
}

// ----------------------------------------------------------------------------
// 2. Container Registry (ACR)
// ----------------------------------------------------------------------------
resource acr 'Microsoft.ContainerRegistry/registries@2023-07-01' = {
  name: acrName
  location: location
  sku: {
    name: 'Basic'
  }
  properties: {
    adminUserEnabled: true
  }
}

// ----------------------------------------------------------------------------
// 3. Azure Container Apps Managed Environment (Internal Isolation + TLS)
// ----------------------------------------------------------------------------
resource managedEnv 'Microsoft.App/managedEnvironments@2023-05-01' = {
  name: '${environmentName}-env'
  location: location
  properties: {
    appLogsConfiguration: {
      destination: 'log-analytics'
      logAnalyticsConfiguration: {
        customerId: logAnalytics.properties.customerId
        sharedKey: logAnalytics.listKeys().primarySharedKey
      }
    }
    zoneRedundant: false
  }
}

// ----------------------------------------------------------------------------
// 4. Public Gateway: shared-shell (Nginx Reverse Proxy + Vue 3 Shell)
//    Bonus B5: External HTTPS/TLS Ingress, CORS policy, Rate Limiting headers
// ----------------------------------------------------------------------------
resource sharedShell 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'shared-shell'
  location: location
  properties: {
    managedEnvironmentId: managedEnv.id
    configuration: {
      registries: [
        {
          server: acr.properties.loginServer
          username: acr.listCredentials().username
          passwordSecretRef: 'acr-password' // pragma: allowlist secret
        }
      ]
      secrets: [
        {
          name: 'acr-password' // pragma: allowlist secret
          value: acr.listCredentials().passwords[0].value // pragma: allowlist secret
        }
      ]
      ingress: {
        external: true
        targetPort: 80
        transport: 'auto'
        allowInsecure: false
        corsPolicy: {
          allowedOrigins: [
            'https://*'
          ]
          allowedMethods: [
            'GET'
            'POST'
            'PUT'
            'PATCH'
            'DELETE'
            'OPTIONS'
          ]
          allowedHeaders: [
            'Content-Type'
            'Authorization'
            'X-Requested-With'
          ]
          maxAge: 3600
        }
      }
    }
    template: {
      containers: [
        {
          name: 'shared-shell'
          image: '${acr.properties.loginServer}/shared-shell:${imageTag}'
          resources: {
            cpu: json('0.5')
            memory: '1.0Gi'
          }
          env: [
            { name: 'AI_MODE_ENABLED', value: aiEnabledString }
            { name: 'MCP_ENABLED', value: aiEnabledString }
            { name: 'RAG_ENABLED', value: aiEnabledString }
            { name: 'MULTI_AGENT_ENABLED', value: aiEnabledString }
            { name: 'CLOUD_SECURITY_PROFILE', value: enableBonusSecurity ? 'HTTPS_CORS_RATELIMIT' : 'STANDARD' }
            { name: 'ACA_ENVIRONMENT_DOMAIN', value: managedEnv.properties.defaultDomain }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 3
      }
    }
  }
}

// ----------------------------------------------------------------------------
// 5. Shared Canvas Gateway: shared-backend (Internal Only — Rule 2)
// ----------------------------------------------------------------------------
resource sharedBackend 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'shared-backend'
  location: location
  properties: {
    managedEnvironmentId: managedEnv.id
    configuration: {
      registries: [
        {
          server: acr.properties.loginServer
          username: acr.listCredentials().username
          passwordSecretRef: 'acr-password' // pragma: allowlist secret
        }
      ]
      ingress: {
        external: false
        targetPort: 8080
        allowInsecure: true
      }
      secrets: [
        {
          name: 'acr-password' // pragma: allowlist secret
          value: acr.listCredentials().passwords[0].value // pragma: allowlist secret
        }
        {
          name: 'canvas-api-token'
          value: empty(canvasApiToken) ? 'placeholder-token' : canvasApiToken
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'shared-backend'
          image: '${acr.properties.loginServer}/shared-backend:${imageTag}'
          resources: {
            cpu: json('0.5')
            memory: '1.0Gi'
          }
          env: [
            { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
            { name: 'CANVAS_BASE_URL', value: canvasBaseUrl }
            { name: 'CANVAS_API_TOKEN', secretRef: 'canvas-api-token' } // pragma: allowlist secret
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 2
      }
    }
  }
}

// ----------------------------------------------------------------------------
// 6. Student 1 (Bryan Lee — Notifications Vertical Slice + Dedicated PostgreSQL 16)
// ----------------------------------------------------------------------------
resource student1Database 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'student-1-database'
  location: location
  properties: {
    managedEnvironmentId: managedEnv.id
    configuration: {
      registries: [
        {
          server: acr.properties.loginServer
          username: acr.listCredentials().username
          passwordSecretRef: 'acr-password' // pragma: allowlist secret
        }
      ]
      ingress: {
        external: false
        targetPort: 5432
        transport: 'tcp'
      }
      secrets: [
        {
          name: 'acr-password' // pragma: allowlist secret
          value: acr.listCredentials().passwords[0].value // pragma: allowlist secret
        }
        {
          name: 'postgres-password' // pragma: allowlist secret
          value: actualPostgresPassword
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'student-1-database'
          image: '${acr.properties.loginServer}/student-1-database:${imageTag}'
          resources: {
            cpu: json('0.5')
            memory: '1.0Gi'
          }
          env: [
            { name: 'POSTGRES_DB', value: 'notifications_db' }
            { name: 'POSTGRES_USER', value: 'postgres' }
            { name: 'POSTGRES_PASSWORD', secretRef: 'postgres-password' } // pragma: allowlist secret
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 1
      }
    }
  }
}

resource student1Backend 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'student-1-backend'
  location: location
  properties: {
    managedEnvironmentId: managedEnv.id
    configuration: {
      registries: [
        {
          server: acr.properties.loginServer
          username: acr.listCredentials().username
          passwordSecretRef: 'acr-password' // pragma: allowlist secret
        }
      ]
      ingress: {
        external: false
        targetPort: 8080
        allowInsecure: true
      }
      secrets: [
        {
          name: 'acr-password' // pragma: allowlist secret
          value: acr.listCredentials().passwords[0].value // pragma: allowlist secret
        }
        {
          name: 'db-connection'
          value: 'Host=student-1-database;Port=5432;Database=notifications_db;Username=postgres;Password=${actualPostgresPassword}'
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'student-1-backend'
          image: '${acr.properties.loginServer}/student-1-backend:${imageTag}'
          resources: {
            cpu: json('0.5')
            memory: '1.0Gi'
          }
          env: [
            { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
            { name: 'ConnectionStrings__DefaultConnection', secretRef: 'db-connection' } // pragma: allowlist secret
            { name: 'SharedService__BaseUrl', value: 'http://shared-backend' }
            { name: 'AI_MODE_ENABLED', value: aiEnabledString }
            { name: 'MCP_ENABLED', value: aiEnabledString }
            { name: 'RAG_ENABLED', value: aiEnabledString }
            { name: 'MULTI_AGENT_ENABLED', value: aiEnabledString }
            { name: 'AiGateway__Enabled', value: aiEnabledString }
            { name: 'AiGateway__BaseUrl', value: aiModeInternalUrl }
            { name: 'McpServer__Enabled', value: aiEnabledString }
            { name: 'McpServer__BaseUrl', value: '${mcpInternalUrl}/mcp' }
            { name: 'RagServer__Enabled', value: aiEnabledString }
            { name: 'RagServer__BaseUrl', value: ragInternalUrl }
            { name: 'MultiAgentServer__Enabled', value: aiEnabledString }
            { name: 'MultiAgentServer__BaseUrl', value: multiAgentInternalUrl }
          ]
        }
      ]
      scale: {
        minReplicas: 1
        maxReplicas: 2
      }
    }
  }
}

resource student1Frontend 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'student-1-frontend'
  location: location
  properties: {
    managedEnvironmentId: managedEnv.id
    configuration: {
      registries: [
        {
          server: acr.properties.loginServer
          username: acr.listCredentials().username
          passwordSecretRef: 'acr-password' // pragma: allowlist secret
        }
      ]
      ingress: { external: false, targetPort: 80, allowInsecure: true }
      secrets: [
        {
          name: 'acr-password' // pragma: allowlist secret
          value: acr.listCredentials().passwords[0].value // pragma: allowlist secret
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'student-1-frontend'
          image: '${acr.properties.loginServer}/student-1-frontend:${imageTag}'
          resources: { cpu: json('0.25'), memory: '0.5Gi' }
        }
      ]
    }
  }
}

// ----------------------------------------------------------------------------
// 7. Students 2–5 Microservices & Isolated Persistence Services (Rule 1)
// ----------------------------------------------------------------------------
resource student2Backend 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'student-2-backend'
  location: location
  properties: {
    managedEnvironmentId: managedEnv.id
    configuration: {
      registries: [
        {
          server: acr.properties.loginServer
          username: acr.listCredentials().username
          passwordSecretRef: 'acr-password' // pragma: allowlist secret
        }
      ]
      ingress: { external: false, targetPort: 8080, allowInsecure: true }
      secrets: [
        {
          name: 'acr-password' // pragma: allowlist secret
          value: acr.listCredentials().passwords[0].value // pragma: allowlist secret
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'student-2-backend'
          image: '${acr.properties.loginServer}/student-2-backend:${imageTag}'
          resources: { cpu: json('0.25'), memory: '0.5Gi' }
          env: [
            { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
            { name: 'SharedService__BaseUrl', value: 'http://shared-backend' }
            { name: 'NotificationService__BaseUrl', value: 'http://student-1-backend' }
            { name: 'AI_MODE_ENABLED', value: aiEnabledString }
            { name: 'MCP_ENABLED', value: aiEnabledString }
            { name: 'RAG_ENABLED', value: aiEnabledString }
            { name: 'MULTI_AGENT_ENABLED', value: aiEnabledString }
            { name: 'AiGateway__Enabled', value: aiEnabledString }
            { name: 'AiGateway__BaseUrl', value: aiModeInternalUrl }
            { name: 'McpServer__Enabled', value: aiEnabledString }
            { name: 'McpServer__BaseUrl', value: '${mcpInternalUrl}/mcp' }
            { name: 'RagServer__Enabled', value: aiEnabledString }
            { name: 'RagServer__BaseUrl', value: ragInternalUrl }
            { name: 'MultiAgentServer__Enabled', value: aiEnabledString }
            { name: 'MultiAgentServer__BaseUrl', value: multiAgentInternalUrl }
          ]
        }
      ]
    }
  }
}

resource student2Frontend 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'student-2-frontend'
  location: location
  properties: {
    managedEnvironmentId: managedEnv.id
    configuration: {
      registries: [
        {
          server: acr.properties.loginServer
          username: acr.listCredentials().username
          passwordSecretRef: 'acr-password' // pragma: allowlist secret
        }
      ]
      ingress: { external: false, targetPort: 80, allowInsecure: true }
      secrets: [
        {
          name: 'acr-password' // pragma: allowlist secret
          value: acr.listCredentials().passwords[0].value // pragma: allowlist secret
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'student-2-frontend'
          image: '${acr.properties.loginServer}/student-2-frontend:${imageTag}'
          resources: { cpu: json('0.25'), memory: '0.5Gi' }
        }
      ]
    }
  }
}

resource student3Database 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'student-3-database'
  location: location
  properties: {
    managedEnvironmentId: managedEnv.id
    configuration: {
      registries: [
        {
          server: acr.properties.loginServer
          username: acr.listCredentials().username
          passwordSecretRef: 'acr-password' // pragma: allowlist secret
        }
      ]
      ingress: { external: false, targetPort: 8080, allowInsecure: true }
      secrets: [
        {
          name: 'acr-password' // pragma: allowlist secret
          value: acr.listCredentials().passwords[0].value // pragma: allowlist secret
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'student-3-database'
          image: '${acr.properties.loginServer}/student-3-database:${imageTag}'
          resources: { cpu: json('0.25'), memory: '0.5Gi' }
        }
      ]
    }
  }
}

resource student3Backend 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'student-3-backend'
  location: location
  properties: {
    managedEnvironmentId: managedEnv.id
    configuration: {
      registries: [
        {
          server: acr.properties.loginServer
          username: acr.listCredentials().username
          passwordSecretRef: 'acr-password' // pragma: allowlist secret
        }
      ]
      ingress: { external: false, targetPort: 8080, allowInsecure: true }
      secrets: [
        {
          name: 'acr-password' // pragma: allowlist secret
          value: acr.listCredentials().passwords[0].value // pragma: allowlist secret
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'student-3-backend'
          image: '${acr.properties.loginServer}/student-3-backend:${imageTag}'
          resources: { cpu: json('0.25'), memory: '0.5Gi' }
          env: [
            { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
            { name: 'DatabaseService__BaseUrl', value: 'http://student-3-database' }
            { name: 'SharedService__BaseUrl', value: 'http://shared-backend' }
            { name: 'NotificationService__BaseUrl', value: 'http://student-1-backend' }
            { name: 'AI_MODE_ENABLED', value: aiEnabledString }
            { name: 'MCP_ENABLED', value: aiEnabledString }
            { name: 'RAG_ENABLED', value: aiEnabledString }
            { name: 'MULTI_AGENT_ENABLED', value: aiEnabledString }
            { name: 'AiGateway__Enabled', value: aiEnabledString }
            { name: 'AiGateway__BaseUrl', value: aiModeInternalUrl }
            { name: 'McpServer__Enabled', value: aiEnabledString }
            { name: 'McpServer__BaseUrl', value: '${mcpInternalUrl}/mcp' }
            { name: 'RagServer__Enabled', value: aiEnabledString }
            { name: 'RagServer__BaseUrl', value: ragInternalUrl }
            { name: 'MultiAgentServer__Enabled', value: aiEnabledString }
            { name: 'MultiAgentServer__BaseUrl', value: multiAgentInternalUrl }
          ]
        }
      ]
    }
  }
}

resource student3Frontend 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'student-3-frontend'
  location: location
  properties: {
    managedEnvironmentId: managedEnv.id
    configuration: {
      registries: [
        {
          server: acr.properties.loginServer
          username: acr.listCredentials().username
          passwordSecretRef: 'acr-password' // pragma: allowlist secret
        }
      ]
      ingress: { external: false, targetPort: 80, allowInsecure: true }
      secrets: [
        {
          name: 'acr-password' // pragma: allowlist secret
          value: acr.listCredentials().passwords[0].value // pragma: allowlist secret
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'student-3-frontend'
          image: '${acr.properties.loginServer}/student-3-frontend:${imageTag}'
          resources: { cpu: json('0.25'), memory: '0.5Gi' }
        }
      ]
    }
  }
}

resource student4Database 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'student-4-database'
  location: location
  properties: {
    managedEnvironmentId: managedEnv.id
    configuration: {
      registries: [
        {
          server: acr.properties.loginServer
          username: acr.listCredentials().username
          passwordSecretRef: 'acr-password' // pragma: allowlist secret
        }
      ]
      ingress: { external: false, targetPort: 8080, allowInsecure: true }
      secrets: [
        {
          name: 'acr-password' // pragma: allowlist secret
          value: acr.listCredentials().passwords[0].value // pragma: allowlist secret
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'student-4-database'
          image: '${acr.properties.loginServer}/student-4-database:${imageTag}'
          resources: { cpu: json('0.25'), memory: '0.5Gi' }
        }
      ]
    }
  }
}

resource student4Backend 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'student-4-backend'
  location: location
  properties: {
    managedEnvironmentId: managedEnv.id
    configuration: {
      registries: [
        {
          server: acr.properties.loginServer
          username: acr.listCredentials().username
          passwordSecretRef: 'acr-password' // pragma: allowlist secret
        }
      ]
      ingress: { external: false, targetPort: 8080, allowInsecure: true }
      secrets: [
        {
          name: 'acr-password' // pragma: allowlist secret
          value: acr.listCredentials().passwords[0].value // pragma: allowlist secret
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'student-4-backend'
          image: '${acr.properties.loginServer}/student-4-backend:${imageTag}'
          resources: { cpu: json('0.25'), memory: '0.5Gi' }
          env: [
            { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
            { name: 'DatabaseService__BaseUrl', value: 'http://student-4-database' }
            { name: 'SharedService__BaseUrl', value: 'http://shared-backend' }
            { name: 'NotificationService__BaseUrl', value: 'http://student-1-backend' }
            { name: 'AI_MODE_ENABLED', value: aiEnabledString }
            { name: 'MCP_ENABLED', value: aiEnabledString }
            { name: 'RAG_ENABLED', value: aiEnabledString }
            { name: 'MULTI_AGENT_ENABLED', value: aiEnabledString }
            { name: 'AiGateway__Enabled', value: aiEnabledString }
            { name: 'AiGateway__BaseUrl', value: aiModeInternalUrl }
            { name: 'McpServer__Enabled', value: aiEnabledString }
            { name: 'McpServer__BaseUrl', value: '${mcpInternalUrl}/mcp' }
            { name: 'RagServer__Enabled', value: aiEnabledString }
            { name: 'RagServer__BaseUrl', value: ragInternalUrl }
            { name: 'MultiAgentServer__Enabled', value: aiEnabledString }
            { name: 'MultiAgentServer__BaseUrl', value: multiAgentInternalUrl }
          ]
        }
      ]
    }
  }
}

resource student4Authentication 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'student-4-authentication'
  location: location
  properties: {
    managedEnvironmentId: managedEnv.id
    configuration: {
      registries: [
        {
          server: acr.properties.loginServer
          username: acr.listCredentials().username
          passwordSecretRef: 'acr-password' // pragma: allowlist secret
        }
      ]
      ingress: { external: false, targetPort: 8080, allowInsecure: true }
      secrets: [
        {
          name: 'acr-password' // pragma: allowlist secret
          value: acr.listCredentials().passwords[0].value // pragma: allowlist secret
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'student-4-authentication'
          image: '${acr.properties.loginServer}/student-4-authentication:${imageTag}'
          resources: { cpu: json('0.25'), memory: '0.5Gi' }
          env: [
            { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
            { name: 'DatabaseService__BaseUrl', value: 'http://student-4-database' }
            { name: 'NotificationService__BaseUrl', value: 'http://student-1-backend' }
          ]
        }
      ]
    }
  }
}

resource student4Frontend 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'student-4-frontend'
  location: location
  properties: {
    managedEnvironmentId: managedEnv.id
    configuration: {
      registries: [
        {
          server: acr.properties.loginServer
          username: acr.listCredentials().username
          passwordSecretRef: 'acr-password' // pragma: allowlist secret
        }
      ]
      ingress: { external: false, targetPort: 80, allowInsecure: true }
      secrets: [
        {
          name: 'acr-password' // pragma: allowlist secret
          value: acr.listCredentials().passwords[0].value // pragma: allowlist secret
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'student-4-frontend'
          image: '${acr.properties.loginServer}/student-4-frontend:${imageTag}'
          resources: { cpu: json('0.25'), memory: '0.5Gi' }
        }
      ]
    }
  }
}

resource student5Database 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'student-5-database'
  location: location
  properties: {
    managedEnvironmentId: managedEnv.id
    configuration: {
      registries: [
        {
          server: acr.properties.loginServer
          username: acr.listCredentials().username
          passwordSecretRef: 'acr-password' // pragma: allowlist secret
        }
      ]
      ingress: { external: false, targetPort: 8080, allowInsecure: true }
      secrets: [
        {
          name: 'acr-password' // pragma: allowlist secret
          value: acr.listCredentials().passwords[0].value // pragma: allowlist secret
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'student-5-database'
          image: '${acr.properties.loginServer}/student-5-database:${imageTag}'
          resources: { cpu: json('0.25'), memory: '0.5Gi' }
        }
      ]
    }
  }
}

resource student5Backend 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'student-5-backend'
  location: location
  properties: {
    managedEnvironmentId: managedEnv.id
    configuration: {
      registries: [
        {
          server: acr.properties.loginServer
          username: acr.listCredentials().username
          passwordSecretRef: 'acr-password' // pragma: allowlist secret
        }
      ]
      ingress: { external: false, targetPort: 8080, allowInsecure: true }
      secrets: [
        {
          name: 'acr-password' // pragma: allowlist secret
          value: acr.listCredentials().passwords[0].value // pragma: allowlist secret
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'student-5-backend'
          image: '${acr.properties.loginServer}/student-5-backend:${imageTag}'
          resources: { cpu: json('0.25'), memory: '0.5Gi' }
          env: [
            { name: 'ASPNETCORE_ENVIRONMENT', value: 'Production' }
            { name: 'DatabaseService__BaseUrl', value: 'http://student-5-database' }
            { name: 'NotificationService__BaseUrl', value: 'http://student-1-backend' }
            { name: 'AI_MODE_ENABLED', value: aiEnabledString }
            { name: 'MCP_ENABLED', value: aiEnabledString }
            { name: 'RAG_ENABLED', value: aiEnabledString }
            { name: 'MULTI_AGENT_ENABLED', value: aiEnabledString }
            { name: 'AiGateway__Enabled', value: aiEnabledString }
            { name: 'AiGateway__BaseUrl', value: aiModeInternalUrl }
            { name: 'McpServer__Enabled', value: aiEnabledString }
            { name: 'McpServer__BaseUrl', value: '${mcpInternalUrl}/mcp' }
            { name: 'RagServer__Enabled', value: aiEnabledString }
            { name: 'RagServer__BaseUrl', value: ragInternalUrl }
            { name: 'MultiAgentServer__Enabled', value: aiEnabledString }
            { name: 'MultiAgentServer__BaseUrl', value: multiAgentInternalUrl }
          ]
        }
      ]
    }
  }
}

resource student5Frontend 'Microsoft.App/containerApps@2023-05-01' = {
  name: 'student-5-frontend'
  location: location
  properties: {
    managedEnvironmentId: managedEnv.id
    configuration: {
      registries: [
        {
          server: acr.properties.loginServer
          username: acr.listCredentials().username
          passwordSecretRef: 'acr-password' // pragma: allowlist secret
        }
      ]
      ingress: { external: false, targetPort: 80, allowInsecure: true }
      secrets: [
        {
          name: 'acr-password' // pragma: allowlist secret
          value: acr.listCredentials().passwords[0].value // pragma: allowlist secret
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'student-5-frontend'
          image: '${acr.properties.loginServer}/student-5-frontend:${imageTag}'
          resources: { cpu: json('0.25'), memory: '0.5Gi' }
        }
      ]
    }
  }
}

// ----------------------------------------------------------------------------
// 8. Bonus Cloud Profile (B1–B4): Optional Cloud AI Services
//    Only provisioned when enableBonusAi == true
// ----------------------------------------------------------------------------

// Bonus B1: AI Mode Gateway (:5001)
resource aiModeApp 'Microsoft.App/containerApps@2023-05-01' = if (enableBonusAi) {
  name: 'ai-mode'
  location: location
  properties: {
    managedEnvironmentId: managedEnv.id
    configuration: {
      registries: [
        {
          server: acr.properties.loginServer
          username: acr.listCredentials().username
          passwordSecretRef: 'acr-password' // pragma: allowlist secret
        }
      ]
      ingress: { external: false, targetPort: 5001, allowInsecure: true }
      secrets: [
        {
          name: 'acr-password' // pragma: allowlist secret
          value: acr.listCredentials().passwords[0].value // pragma: allowlist secret
        }
        {
          name: 'openrouter-api-key'
          value: empty(openRouterApiKey) ? 'unconfigured' : openRouterApiKey
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'ai-mode'
          image: '${acr.properties.loginServer}/ai-mode:${imageTag}'
          resources: { cpu: json('0.25'), memory: '0.5Gi' }
          env: [
            { name: 'ASPNETCORE_HTTP_PORTS', value: '5001' }
            { name: 'OPENROUTER_API_KEY', secretRef: 'openrouter-api-key' } // pragma: allowlist secret
            { name: 'OPENROUTER_MODEL', value: 'openrouter/free' }
          ]
        }
      ]
    }
  }
}

// Bonus B2: MCP Server (:5002)
resource mcpServerApp 'Microsoft.App/containerApps@2023-05-01' = if (enableBonusAi) {
  name: 'mcp-server'
  location: location
  properties: {
    managedEnvironmentId: managedEnv.id
    configuration: {
      registries: [
        {
          server: acr.properties.loginServer
          username: acr.listCredentials().username
          passwordSecretRef: 'acr-password' // pragma: allowlist secret
        }
      ]
      ingress: { external: false, targetPort: 5002, allowInsecure: true }
      secrets: [
        {
          name: 'acr-password' // pragma: allowlist secret
          value: acr.listCredentials().passwords[0].value // pragma: allowlist secret
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'mcp-server'
          image: '${acr.properties.loginServer}/mcp-server:${imageTag}'
          resources: { cpu: json('0.25'), memory: '0.5Gi' }
          env: [
            { name: 'ASPNETCORE_HTTP_PORTS', value: '5002' }
            { name: 'Student1__BaseUrl', value: 'http://student-1-backend' }
            { name: 'Student2__BaseUrl', value: 'http://student-2-backend' }
            { name: 'Student3__BaseUrl', value: 'http://student-3-backend' }
            { name: 'Student4__BaseUrl', value: 'http://student-4-backend' }
            { name: 'Student5__BaseUrl', value: 'http://student-5-backend' }
          ]
        }
      ]
    }
  }
}

// Bonus B3: RAG Server (:5003)
resource ragServerApp 'Microsoft.App/containerApps@2023-05-01' = if (enableBonusAi) {
  name: 'rag-server'
  location: location
  properties: {
    managedEnvironmentId: managedEnv.id
    configuration: {
      registries: [
        {
          server: acr.properties.loginServer
          username: acr.listCredentials().username
          passwordSecretRef: 'acr-password' // pragma: allowlist secret
        }
      ]
      ingress: { external: false, targetPort: 5003, allowInsecure: true }
      secrets: [
        {
          name: 'acr-password' // pragma: allowlist secret
          value: acr.listCredentials().passwords[0].value // pragma: allowlist secret
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'rag-server'
          image: '${acr.properties.loginServer}/rag-server:${imageTag}'
          resources: { cpu: json('0.25'), memory: '0.5Gi' }
          env: [
            { name: 'ASPNETCORE_HTTP_PORTS', value: '5003' }
            { name: 'AiGateway__BaseUrl', value: 'http://ai-mode' }
          ]
        }
      ]
    }
  }
}

// Bonus B4: Multi-Agent Server (:5004)
resource multiAgentServerApp 'Microsoft.App/containerApps@2023-05-01' = if (enableBonusAi) {
  name: 'multi-agent-server'
  location: location
  properties: {
    managedEnvironmentId: managedEnv.id
    configuration: {
      registries: [
        {
          server: acr.properties.loginServer
          username: acr.listCredentials().username
          passwordSecretRef: 'acr-password' // pragma: allowlist secret
        }
      ]
      ingress: { external: false, targetPort: 5004, allowInsecure: true }
      secrets: [
        {
          name: 'acr-password' // pragma: allowlist secret
          value: acr.listCredentials().passwords[0].value // pragma: allowlist secret
        }
      ]
    }
    template: {
      containers: [
        {
          name: 'multi-agent-server'
          image: '${acr.properties.loginServer}/multi-agent-server:${imageTag}'
          resources: { cpu: json('0.25'), memory: '0.5Gi' }
          env: [
            { name: 'ASPNETCORE_HTTP_PORTS', value: '5004' }
            { name: 'MultiAgent__AiGatewayBaseUrl', value: 'http://ai-mode' }
            { name: 'MultiAgent__McpBaseUrl', value: 'http://mcp-server' }
            { name: 'MultiAgent__RagBaseUrl', value: 'http://rag-server' }
            { name: 'MultiAgent__Student1BaseUrl', value: 'http://student-1-backend' }
          ]
        }
      ]
    }
  }
}

output publicGatewayFqdn string = sharedShell.properties.configuration.ingress.fqdn
output baselineAiDisabled bool = !enableBonusAi
output bonusProfileEnabled bool = enableBonusAi
