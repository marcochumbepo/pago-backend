# Guía de Despliegue Completo — Users API

Esta guía cubre todos los pasos necesarios para levantar el proyecto desde cero:
desarrollo local, Docker, Azure y CI/CD con GitHub Actions.

---

## Índice

1. [Requisitos previos](#1-requisitos-previos)
2. [Clonar y configurar el proyecto](#2-clonar-y-configurar-el-proyecto)
3. [Desarrollo local con Docker](#3-desarrollo-local-con-docker)
4. [Crear recursos en Azure](#4-crear-recursos-en-azure)
5. [Configurar CI/CD en GitHub](#5-configurar-cicd-en-github)
6. [Estrategia de ramas y branch protection](#6-estrategia-de-ramas-y-branch-protection)
7. [Flujo de trabajo diario](#7-flujo-de-trabajo-diario)
8. [Migrar de InMemory a SQL Server](#8-migrar-de-inmemory-a-sql-server)
9. [Migrar a PostgreSQL](#9-migrar-a-postgresql)
10. [Comandos de referencia rápida](#10-comandos-de-referencia-rápida)

---

## 1. Requisitos previos

| Herramienta | Instalación |
|---|---|
| Docker Desktop | https://www.docker.com/products/docker-desktop/ |
| Git | https://git-scm.com/download/win |
| Azure CLI | https://aka.ms/installazurecliwindows |
| Cuenta Azure | https://portal.azure.com (Pay-as-you-go) |
| Cuenta GitHub | https://github.com |

---

## 2. Clonar y configurar el proyecto

```bash
# Clonar el repositorio
git clone git@github.com:marcochumbepo/pago-backend.git
cd pago-backend

# Crear archivo .env desde el template
cp .env.example .env

# Editar .env con tus valores (o dejar los defaults para desarrollo)
# Los valores críticos a revisar:
#   Database__Provider=InMemory
#   Jwt__SecretKey=<tu-clave-minimo-32-chars>
#   ASPNETCORE_ENVIRONMENT=Development
```

### Estructura del proyecto

```
pago-backend/
├── src/
│   ├── Api/                    # Controllers, Middleware, Program.cs
│   ├── Application/            # Servicios, DTOs, Validaciones
│   ├── Domain/                 # Entidades, Interfaces
│   └── Infrastructure/         # EF Core, Repositorios
├── tests/
│   └── UnitTests/              # Pruebas unitarias (xUnit + Moq)
├── .github/workflows/          # CI/CD pipelines
├── Dockerfile                  # Multi-stage build
├── docker-compose.yml          # Orquestación local
├── .env                        # Variables de entorno (NO se versiona)
└── .env.example                # Template de variables
```

---

## 3. Desarrollo local con Docker

### Modo InMemory (default, sin SQL Server)

```bash
# Levantar solo la API
docker compose up -d --build

# Ver logs
docker compose logs -f api

# Endpoints disponibles:
#   API:      http://localhost:8080
#   Swagger:  http://localhost:8080/swagger
#   Health:   http://localhost:8080/health
```

### Modo SQL Server

```bash
# 1. Cambiar el provider en .env
#    Database__Provider=SqlServer

# 2. Levantar con perfil sql
docker compose --profile sql up -d --build

# 3. Para detener todo (incluyendo SQL Server)
docker compose --profile sql down
```

### Ejecutar tests en Docker

```bash
docker run --rm -v "${PWD}:/src" -w /src mcr.microsoft.com/dotnet/sdk:8.0 dotnet test
```

### Probar la API

```bash
# 1. Login (obtener token)
curl -X POST http://localhost:8080/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}'

# 2. Copiar el token y usarlo en los endpoints protegidos
curl http://localhost:8080/api/users \
  -H "Authorization: Bearer <TOKEN>"

curl -X POST http://localhost:8080/api/users \
  -H "Content-Type: application/json" \
  -H "Authorization: Bearer <TOKEN>" \
  -d '{"firstName":"Juan","lastName":"Perez","email":"juan@test.com"}'
```

---

## 4. Crear recursos en Azure

Ejecutar estos comandos en **Azure Cloud Shell** (PowerShell) o en terminal local con Azure CLI.

### 4.1 Variables

```bash
$rg = "rg-users-api"
$location = "eastus"
$acrName = "usersapicontainer"
```

### 4.2 Resource Group

```bash
az group create --name $rg --location $location
```

### 4.3 Registrar providers (requerido en cuentas nuevas)

```bash
az provider register --namespace Microsoft.ContainerRegistry --wait
az provider register --namespace Microsoft.App --wait
az provider register --namespace Microsoft.OperationalInsights --wait
```

### 4.4 Azure Container Registry

```bash
az acr create --resource-group $rg --name $acrName --sku Basic --admin-enabled true
```

### 4.5 Log Analytics Workspace (requerido para Container Environment)

```bash
az monitor log-analytics workspace create `
  --resource-group $rg `
  --workspace-name "usersapi-logs"

$logCustomerId = (az monitor log-analytics workspace show `
  --resource-group $rg `
  --workspace-name "usersapi-logs" `
  --query customerId -o tsv)

$logSharedKey = (az monitor log-analytics workspace get-shared-keys `
  --resource-group $rg `
  --workspace-name "usersapi-logs" `
  --query primarySharedKey -o tsv)
```

### 4.6 Container Apps Environment

```bash
az containerapp env create `
  --name "usersapi-env" `
  --resource-group $rg `
  --location $location `
  --logs-workspace-id $logCustomerId `
  --logs-workspace-key $logSharedKey
```

### 4.7 Container App

```bash
az containerapp create `
  --name "users-api" `
  --resource-group $rg `
  --environment "usersapi-env" `
  --image "mcr.microsoft.com/azuredocs/containerapps-helloworld:latest" `
  --target-port 8080 `
  --ingress external `
  --min-replicas 1 --max-replicas 1 `
  --registry-server "$acrName.azurecr.io" `
  --env-vars `
    "Database__Provider=InMemory" `
    "ASPNETCORE_ENVIRONMENT=Production" `
    "Jwt__SecretKey=<TU-JWT-SECRET-KEY>" `
    "Jwt__Issuer=UsersApi" `
    "Jwt__Audience=UsersApi" `
    "Jwt__ExpirationMinutes=60"
```

> Reemplazar `<TU-JWT-SECRET-KEY>` por la misma clave que usaste en `.env`.
> La clave debe tener mínimo 32 caracteres y sin caracteres especiales que el shell pueda interpretar (`#`, `$`, `!`, `&`, etc).

### 4.8 Service Principal para GitHub Actions

```bash
$subscriptionId = (az account show --query id -o tsv)

az ad sp create-for-rbac `
  --name "github-actions-users-api" `
  --role contributor `
  --scopes "/subscriptions/$subscriptionId/resourceGroups/$rg" `
  --sdk-auth -o json
```

**Guardar el JSON completo.** Contiene `clientId`, `clientSecret`, `tenantId`, `subscriptionId`.

### 4.9 Obtener URL de la aplicación

```bash
az containerapp show `
  --name users-api `
  --resource-group $rg `
  --query properties.configuration.ingress.fqdn -o tsv
```

### 4.10 Activar Swagger temporalmente (para pruebas)

```bash
az containerapp update `
  --name users-api `
  --resource-group $rg `
  --set-env-vars "ASPNETCORE_ENVIRONMENT=Development"
```

### 4.11 Volver a Production (obligatorio después de probar)

```bash
az containerapp update `
  --name users-api `
  --resource-group $rg `
  --set-env-vars "ASPNETCORE_ENVIRONMENT=Production"
```

---

## 5. Configurar CI/CD en GitHub

### 5.1 Secrets de GitHub Actions

Ir a: **Repo → Settings → Secrets and variables → Actions → New repository secret**

| Secret | Valor |
|---|---|
| `AZURE_CREDENTIALS` | El **JSON completo** del Service Principal (paso 4.8) |
| `ACR_NAME` | `usersapicontainer` |
| `AZURE_RESOURCE_GROUP` | `rg-users-api` |
| `JWT_SECRET_KEY` | La misma clave JWT usada en `.env` y en Azure |

### 5.2 Pipeline automático

El pipeline (`.github/workflows/ci-cd.yml`) se ejecuta automáticamente:

| Evento | Resultado |
|---|---|
| PR → `develop` | Build + Test |
| PR → `main` | Build + Test |
| Push a `develop` | Build + Test |
| Push a `main` | Build + Test + Docker build/push + Deploy a Azure |
| Push a `feature/*` | **No se ejecuta** |

---

## 6. Estrategia de ramas y branch protection

### 6.1 Ramas

```
main       → Producción (solo merge vía PR)
develop    → Desarrollo (solo merge vía PR)
feature/*  → Rama de trabajo del desarrollador
fix/*      → Corrección de bugs
hotfix/*   → Corrección urgente en producción
```

### 6.2 Branch protection rules

Ir a: **Repo → Settings → Rules → Rulesets → New ruleset**

#### Ruleset: `protect-main`

| Campo | Valor |
|---|---|
| **Ruleset Name** | `protect-main` |
| **Enforcement status** | Active |
| **Target branches** | Include by pattern: `main` |

**Rules:**

| Regla | Valor |
|---|---|
| Require a pull request before merging | ✅ Activado |
| Required approvals | `0` |
| Require status checks to pass | ✅ `Build & Test` |
| Block force pushes | ✅ |
| Restrict deletions | ✅ |

#### Ruleset: `protect-develop`

| Campo | Valor |
|---|---|
| **Ruleset Name** | `protect-develop` |
| **Enforcement status** | Active |
| **Target branches** | Include by pattern: `develop` |

**Rules:**

| Regla | Valor |
|---|---|
| Require a pull request before merging | ✅ Activado |
| Required approvals | `0` |
| Require status checks to pass | ✅ `Build & Test` |
| Block force pushes | ✅ |
| Restrict deletions | ✅ |

---

## 7. Flujo de trabajo diario

### 7.1 Crear una nueva funcionalidad

```bash
# 1. Actualizar develop
git checkout develop
git pull origin develop

# 2. Crear rama de feature
git checkout -b feature/nombre-funcionalidad

# 3. Codificar, commitear cambios
git add .
git commit -m "feat: descripción del cambio"

# 4. Pushear la rama
git push origin feature/nombre-funcionalidad

# 5. Crear PR en GitHub: feature/nombre-funcionalidad → develop
#    Esperar que Build & Test pase en verde

# 6. Mergear el PR (botón "Merge pull request")

# 7. Eliminar la rama feature (opcional)
git branch -d feature/nombre-funcionalidad
```

### 7.2 Desplegar a producción

```bash
# 1. Crear PR: develop → main
#    Esperar que Build & Test pase en verde

# 2. Mergear el PR

# 3. El merge a main dispara automáticamente el deploy a Azure
#    Verificar en: Actions → CI/CD Pipeline
```

### 7.3 Rollback

```bash
# Revertir a un commit anterior
git checkout main
git revert <commit-hash>
git push origin main
# El pipeline despliega la versión revertida automáticamente
```

---

## 8. Migrar de InMemory a SQL Server

### 8.1 En Azure (producción)

```bash
# 1. Crear Azure SQL Database
az sql server create `
  --name "usersapi-sql" `
  --resource-group $rg `
  --location $location `
  --admin-user "sqladmin" `
  --admin-password "<TU-PASSWORD>"

az sql db create `
  --resource-group $rg `
  --server "usersapi-sql" `
  --name "UsersDb" `
  --service-objective Basic

# 2. Permitir acceso desde Azure
az sql server firewall-rule create `
  --resource-group $rg `
  --server "usersapi-sql" `
  --name "AllowAzure" `
  --start-ip-address 0.0.0.0 `
  --end-ip-address 0.0.0.0

# 3. Actualizar Container App con SQL Server
$connString = "Server=tcp:usersapi-sql.database.windows.net,1433;Database=UsersDb;User Id=sqladmin;Password=<TU-PASSWORD>;Encrypt=True;TrustServerCertificate=False;"

az containerapp update `
  --name users-api `
  --resource-group $rg `
  --set-env-vars `
    "Database__Provider=SqlServer" `
    "ConnectionStrings__DefaultConnection=$connString"
```

### 8.2 En desarrollo local

```bash
# .env
Database__Provider=SqlServer
ConnectionStrings__DefaultConnection=Server=sqlserver,1433;Database=UsersDb;User Id=sa;Password=YourStrong!Passw0rd;TrustServerCertificate=True;

# Levantar con SQL Server
docker compose --profile sql up -d --build
```

---

## 9. Migrar a PostgreSQL

Cambios necesarios (sin tocar Domain ni Application):

```bash
# 1. Agregar paquete NuGet
cd src/Infrastructure
dotnet add package Npgsql.EntityFrameworkCore.PostgreSQL
```

```csharp
// 2. En src/Infrastructure/DependencyInjection.cs, agregar el case:
case "postgresql":
    var pgConnectionString = configuration.GetConnectionString("DefaultConnection");
    options.UseNpgsql(pgConnectionString);
    break;
```

```bash
# 3. En Azure, actualizar variable de entorno:
#    Database__Provider=PostgreSql
#    ConnectionStrings__DefaultConnection=<connection-string-de-postgres>
```

---

## 10. Comandos de referencia rápida

### Docker

```bash
docker compose up -d --build          # InMemory
docker compose --profile sql up -d    # SQL Server
docker compose down                   # Detener
docker compose logs -f api            # Ver logs
docker compose ps                     # Estado de servicios
docker run --rm -v "${PWD}:/src" -w /src mcr.microsoft.com/dotnet/sdk:8.0 dotnet test  # Tests
```

### Git

```bash
git checkout -b feature/nombre        # Crear rama
git add . && git commit -m "msg"      # Commit
git push origin feature/nombre        # Push
git checkout develop && git pull      # Actualizar develop
```

### Azure (consultas)

```bash
az containerapp show --name users-api --resource-group $rg --query properties.configuration.ingress.fqdn -o tsv
az containerapp logs show --name users-api --resource-group $rg --follow
az acr show --name $acrName --query loginServer -o tsv
```

### API (probar endpoints)

```bash
# Login
curl -X POST https://<URL>/api/auth/login \
  -H "Content-Type: application/json" \
  -d '{"username":"admin","password":"admin123"}'

# CRUD con token
curl https://<URL>/api/users -H "Authorization: Bearer <TOKEN>"
curl -X POST https://<URL>/api/users -H "Authorization: Bearer <TOKEN>" -H "Content-Type: application/json" -d '{"firstName":"X","lastName":"Y","email":"x@y.com"}'
curl -X DELETE https://<URL>/api/users/<ID> -H "Authorization: Bearer <TOKEN>"

# Health (público)
curl https://<URL>/health
```

---

## Resumen de URLs importantes

| Qué | URL |
|---|---|
| Azure Portal | https://portal.azure.com |
| GitHub Repo | https://github.com/marcochumbepo/pago-backend |
| GitHub Actions | https://github.com/marcochumbepo/pago-backend/actions |
| GitHub Secrets | https://github.com/marcochumbepo/pago-backend/settings/secrets/actions |
| Branch Rules | https://github.com/marcochumbepo/pago-backend/settings/rules |
| App en Azure | `https://users-api.<region>.azurecontainerapps.io` |
