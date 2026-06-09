# Dockerfile - Multi-stage build para optimizar tamaño de imagen final.
# 
# Stage 1: BUILD - compila la aplicación usando el SDK completo (.NET 8).
# Stage 2: RUNTIME - solo copia los binarios compilados a una imagen ligera (aspnet).
# 
# Resultado: imagen final mínima (~120MB vs ~700MB sin multi-stage).
# 
# IMPORTANTE: La ruta del archivo .sln y los .csproj debe ser relativa al contexto
# de build definido en docker-compose.yml (normalmente la raíz del repositorio).

FROM mcr.microsoft.com/dotnet/sdk:8.0 AS build
WORKDIR /src

# Copiar archivos de proyecto y restaurar dependencias como capa separada.
# Esto permite que Docker cachee las dependencias: mientras no cambien los .csproj,
# no se reinstalarán paquetes NuGet, acelerando builds subsecuentes.
COPY ["src/Domain/UsersApi.Domain.csproj", "src/Domain/"]
COPY ["src/Application/UsersApi.Application.csproj", "src/Application/"]
COPY ["src/Infrastructure/UsersApi.Infrastructure.csproj", "src/Infrastructure/"]
COPY ["src/Api/UsersApi.csproj", "src/Api/"]
COPY ["tests/UnitTests/UsersApi.UnitTests.csproj", "tests/UnitTests/"]
COPY ["UsersApi.sln", "./"]

RUN dotnet restore "UsersApi.sln"

# Copiar el resto del código fuente y compilar en modo Release.
COPY . .
RUN dotnet publish "src/Api/UsersApi.csproj" -c Release -o /app/publish --no-restore

# Stage 2: RUNTIME - imagen ligera solo con el runtime de ASP.NET.
FROM mcr.microsoft.com/dotnet/aspnet:8.0 AS runtime
WORKDIR /app

# Crear usuario no-root para ejecutar la aplicación (buena práctica de seguridad).
# Evita que un atacante que comprometa la app tenga acceso root al contenedor.
RUN adduser --disabled-password --gecos "" appuser && chown -R appuser /app
USER appuser

# Copiar binarios compilados desde el stage de build.
COPY --from=build /app/publish .

# ASPNETCORE_URLS: escucha en el puerto 8080 (estándar para Azure Container Apps).
# El puerto se expone mediante EXPOSE y docker-compose lo mapea al host.
ENV ASPNETCORE_URLS="http://+:8080"
EXPOSE 8080

ENTRYPOINT ["dotnet", "UsersApi.dll"]
