# Users API - Prueba Técnica Senior .NET

API REST para gestión de usuarios desarrollada con .NET 8 y Clean Architecture.

## Stack Tecnológico

| Categoría       | Tecnología                       |
| --------------- | -------------------------------- |
| Backend         | .NET 8 / ASP.NET Core Web API    |
| ORM             | Entity Framework Core 8          |
| Base de Datos   | SQL Server 2022                  |
| Validación      | FluentValidation                 |
| Testing         | xUnit, Moq, FluentAssertions     |
| Documentación   | Swagger / OpenAPI                |
| Contenedores    | Docker, Docker Compose           |
| CI/CD           | GitHub Actions                   |
| Cloud           | Azure Container Apps             |

## Arquitectura

El proyecto sigue **Clean Architecture** con 4 capas:

```
src/
├── Api/              # Controllers, Middleware, Program.cs (capa más externa)
├── Application/      # Casos de uso, DTOs, Validaciones (lógica de aplicación)
├── Domain/           # Entidades, Interfaces de repositorio (reglas de negocio)
└── Infrastructure/   # EF Core, Repositorios, DbContext (acceso a datos)

tests/
└── UnitTests/        # Pruebas unitarias con xUnit + Moq
```

**Principios aplicados:**
- **Dependency Inversion**: las capas internas definen interfaces, las externas las implementan.
- **Single Responsibility**: cada clase tiene una única razón para cambiar.
- **Repository Pattern**: abstrae el acceso a datos detrás de `IUserRepository`.

## Endpoints

| Método | Ruta              | Descripción               | Respuestas |
| ------ | ----------------- | ------------------------- | ---------- |
| POST   | `/api/users`      | Crear usuario             | 201, 400   |
| GET    | `/api/users`      | Listar todos los usuarios | 200        |
| GET    | `/api/users/{id}` | Obtener por ID            | 200, 404   |
| DELETE | `/api/users/{id}` | Eliminar usuario          | 204, 404   |
| GET    | `/health`         | Health check              | 200        |

## Configuración: fuente única de verdad

Toda la configuración de ambiente se maneja con **variables de entorno**.
El archivo `.env` es la única fuente para desarrollo local y Docker.
En Azure Container Apps se configuran las mismas variables en el portal/CLI.

| Variable                                 | Descripción                    | Local/Docker          | Azure                 |
| ---------------------------------------- | ------------------------------ | --------------------- | --------------------- |
| `ConnectionStrings__DefaultConnection`   | Cadena de conexión SQL Server  | `.env`                | Portal > Container App > Secrets |
| `ASPNETCORE_ENVIRONMENT`                 | Entorno (Development/Production) | `.env`              | Portal > Container App > Environment variables |
| `MSSQL_SA_PASSWORD`                      | Contraseña SA de SQL Server    | `.env`                | Azure SQL auth        |

```bash
# Al clonar el proyecto por primera vez:
cp .env.example .env
# Editar .env con los valores de tu ambiente
```

## Ejecución Local

### Requisitos previos

- [Docker Desktop](https://www.docker.com/products/docker-desktop/)

### Con Docker Compose (recomendado)

```bash
# 1. Configurar variables (única vez)
cp .env.example .env

# 2. Construir e iniciar los contenedores (api + sqlserver)
docker compose up -d

# La API estará disponible en http://localhost:8080
# Swagger: http://localhost:8080/swagger
# Health:  http://localhost:8080/health

# Detener los contenedores
docker compose down
```

### Sin Docker (requiere .NET 8 SDK + SQL Server local)

```bash
# Configurar .env con Server=localhost
# Luego cargar las variables en la sesión y ejecutar:
set -a; source .env; set +a
dotnet run --project src/Api
```

## Testing

```bash
# Local (requiere .NET 8 SDK)
dotnet test UsersApi.sln --configuration Release

# Con Docker
docker run --rm -v "${PWD}:/src" -w /src mcr.microsoft.com/dotnet/sdk:8.0 dotnet test
```

**Pruebas incluidas (5 tests):**
- `CreateUserService`: creacion exitosa, email invalido, nombre vacio
- `GetUserByIdService`: usuario existe, usuario no existe

## Docker

### Dockerfile

Multi-stage build que separa la etapa de compilacion (SDK) de la etapa de ejecucion (runtime), resultando en una imagen final optimizada (~120 MB).

### Docker Compose

Orquesta dos servicios. La configuracion se lee de `.env`:

| Servicio    | Imagen                                          | Puerto |
| ----------- | ----------------------------------------------- | ------ |
| `api`       | Construida desde Dockerfile                     | 8080   |
| `sqlserver` | `mcr.microsoft.com/mssql/server:2022-latest`    | 1433   |

## Despliegue en Azure

### Requisitos

1. Azure Container Registry (ACR)
2. Azure Container Apps
3. Azure SQL Server

### Configuracion en Azure

Las mismas variables de `.env` se configuran en el Container App:

```bash
# Desde Azure CLI
az containerapp update \
  --name users-api \
  --resource-group <rg> \
  --set-env-vars \
    "ConnectionStrings__DefaultConnection=Server=<azure-sql-host>;Database=UsersDb;..." \
    "ASPNETCORE_ENVIRONMENT=Production"
```

### Secrets de GitHub necesarios

```
Settings > Secrets and variables > Actions > New repository secret

AZURE_CLIENT_ID          = <service-principal-client-id>
AZURE_TENANT_ID          = <azure-tenant-id>
AZURE_SUBSCRIPTION_ID    = <azure-subscription-id>
AZURE_RESOURCE_GROUP     = <resource-group-name>
ACR_NAME                 = <nombre-acr>
AZURE_SQL_CONNECTION     = <connection-string-de-azure-sql>
```

### Flujo de despliegue

```
GitHub Push -> CI (restore, build, test)
            -> Docker Build
            -> Push a Azure Container Registry
            -> Deploy a Azure Container Apps (con env vars)
```

## CI/CD Pipeline

Definido en `.github/workflows/ci-cd.yml`:

1. **Restore**: descarga dependencias NuGet
2. **Build**: compila la solucion
3. **Test**: ejecuta pruebas unitarias
4. **Docker Build & Push**: construye y publica imagen en ACR
5. **Deploy**: actualiza Azure Container Apps con la nueva imagen y variables de entorno

## Licencia

Proyecto realizado como prueba tecnica. Uso educativo.
