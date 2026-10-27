# ==============================================================================
# InnovaFemIA - Dockerfile Multi-Stage de Producción (.NET 9 + TailwindCSS)
# ==============================================================================

# ------------------------------------------------------------------------------
# 1. ETAPA DE CONSTRUCCIÓN (BUILD): SDK .NET 9 + Node.js para compilar TailwindCSS
# ------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Instalar Node.js (v20 LTS) para compilar los estilos de TailwindCSS
RUN apt-get update && apt-get install -y --no-install-recommends \
    curl \
    gnupg \
    && mkdir -p /etc/apt/keyrings \
    && curl -fsSL https://deb.nodesource.com/gpgkey/nodesource-repo.gpg.key | gpg --dearmor -o /etc/apt/keyrings/nodesource.gpg \
    && echo "deb [signed-by=/etc/apt/keyrings/nodesource.gpg] https://deb.nodesource.com/node_20.x nodistro main" | tee /etc/apt/sources.list.d/nodesource.list \
    && apt-get update \
    && apt-get install -y --no-install-recommends nodejs \
    && rm -rf /var/lib/apt/lists/*

# Copiar archivos de definición de dependencias para aprovechar la caché de Docker
COPY ["src/InnovaFemIA.Web/InnovaFemIA.Web.csproj", "src/InnovaFemIA.Web/"]
COPY ["src/InnovaFemIA.Web/package.json", "src/InnovaFemIA.Web/package-lock.json*", "src/InnovaFemIA.Web/"]

# Restaurar paquetes NuGet
RUN dotnet restore "src/InnovaFemIA.Web/InnovaFemIA.Web.csproj"

# Instalar dependencias de TailwindCSS
WORKDIR /src/src/InnovaFemIA.Web
RUN npm ci || npm install

# Regresar a la raíz de compilación y copiar el código fuente completo
WORKDIR /src
COPY . .

# Compilar y minificar estilos de TailwindCSS a wwwroot/css/app.css
WORKDIR /src/src/InnovaFemIA.Web
RUN npm run tw:build

# Publicar la aplicación en modo Release optimizado
WORKDIR /src
RUN dotnet publish "src/InnovaFemIA.Web/InnovaFemIA.Web.csproj" \
    -c Release \
    -o /app/publish \
    /p:UseAppHost=false

# ------------------------------------------------------------------------------
# 2. ETAPA FINAL (RUNTIME): Runtime mínimo de ASP.NET Core 9 (Debian Bookworm Slim)
# ------------------------------------------------------------------------------
FROM mcr.microsoft.com/dotnet/aspnet:9.0-bookworm-slim AS final

# Parámetros y utilidades del sistema (curl para Healthchecks, tzdata para hora colombiana)
RUN apt-get update && apt-get install -y --no-install-recommends \
    curl \
    tzdata \
    && rm -rf /var/lib/apt/lists/*

# Configurar zona horaria de Colombia (America/Bogota, UTC-5)
ENV TZ=America/Bogota
RUN ln -snf /usr/share/zoneinfo/$TZ /etc/localtime && echo $TZ > /etc/timezone

WORKDIR /app

# Variables de entorno estándar de ASP.NET Core
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true
ENV PATH_BASE=/innovafemia

# Copiar artefactos compilados desde la etapa de construcción
COPY --from=build /app/publish .

# Crear carpetas requeridas y otorgar permisos de lectura/escritura y propiedad al usuario 'app'
RUN mkdir -p /app/wwwroot/media_cache /app/wwwroot/uploads \
    && chmod -R 777 /app/wwwroot/media_cache /app/wwwroot/uploads \
    && chown -R app:app /app

# Ejecutar el contenedor bajo usuario no privilegiado por seguridad
USER app

# Exponer el puerto HTTP configurado
EXPOSE 8080

# Chequeo de salud del contenedor
HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

# Comando de inicio del servidor
ENTRYPOINT ["dotnet", "InnovaFemIA.Web.dll"]
