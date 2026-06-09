# Especificación Técnica Optimizada para Prueba Senior .NET (3 Horas)

## Objetivo

Desarrollar una API REST en .NET 8 que demuestre:

- Clean Architecture
- SOLID
- Testing
- Docker
- Azure
- CI/CD

Priorizando velocidad de implementación sin sacrificar calidad.

---

# Stack Tecnológico

## Backend

- .NET 8
- ASP.NET Core Web API
- Entity Framework Core
- SQL Server

## Testing

- xUnit
- Moq
- FluentAssertions

## Validación

- FluentValidation

## Documentación

- Swagger

## Contenedores

- Docker
- Docker Compose

## Cloud

- Azure Container Apps

## CI/CD

- GitHub Actions

---

# Dominio

Implementar una única entidad:

```csharp
User
{
    Guid Id;
    string FirstName;
    string LastName;
    string Email;
    DateTime CreatedAt;
}
```

---

# Endpoints

```http
POST   /api/users
GET    /api/users
GET    /api/users/{id}
DELETE /api/users/{id}
```

---

# Arquitectura

```text
src/
├── Api
├── Application
├── Domain
└── Infrastructure

tests/
└── UnitTests
```

---

# SOLID

## Repository Pattern

```csharp
IUserRepository
```

## Dependency Injection

Todo mediante DI.

Nunca instanciar dependencias manualmente.

---

# Application Layer

Crear servicios:

- CreateUserService
- GetUsersService
- GetUserByIdService
- DeleteUserService

No utilizar MediatR.

---

# Validaciones

Usar FluentValidation:

- Nombre obligatorio
- Apellido obligatorio
- Email obligatorio
- Email válido

---

# Manejo Global de Errores

Middleware único:

- Registrar errores
- Ocultar stack traces
- Retornar mensajes consistentes

---

# Logging

Usar ILogger.

---

# Persistencia

- Entity Framework Core
- SQL Server

---

# Swagger

Configurado y funcional.

---

# Health Check

```http
GET /health
```

---

# Testing

Pruebas mínimas:

## CreateUser

- éxito
- email inválido

## GetUserById

- usuario existe
- usuario no existe

Objetivo: 4 a 6 pruebas unitarias.

---

# Docker

## Dockerfile

Multi-stage build.

## Docker Compose

Servicios:

- api
- sqlserver

---

# GitHub Actions

Pipeline mínimo:

1. Restore
2. Build
3. Test
4. Docker Build

---

# Azure

Despliegue:

Docker
→ Azure Container Registry
→ Azure Container Apps

---

# README

Debe incluir:

- Arquitectura
- Ejecución local
- Docker
- Testing
- Despliegue

---

# No Implementar

Para ahorrar tiempo:

- MediatR
- CQRS
- Unit Of Work
- OpenTelemetry
- SonarCloud
- Trivy
- Domain Events
- Outbox Pattern
- API Versioning
- JWT
- Redis
- Message Brokers

---

# Orden de Prioridad

1. Arquitectura
2. CRUD
3. Validaciones
4. Testing
5. Docker
6. GitHub Actions
7. Azure

La solución debe compilar, ejecutar pruebas, construir imagen Docker y quedar lista para desplegarse en Azure Container Apps.
