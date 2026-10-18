# ==============================================================================
# Script de Generacion de Paquete de Entrega (Sin Codigo Fuente) - InnovaFemIA
# ==============================================================================
# Este script compila la aplicacion a binarios cerrados (.dll) y genera:
# 1. La carpeta 'dist\InnovaFemIA-Entrega'
# 2. El archivo comprimido 'dist\InnovaFemIA-Despliegue.zip' (~7 MB)
# NO CONTIENE CODIGO FUENTE (.cs). Propiedad intelectual protegida.

$ErrorActionPreference = "Stop"
$Raiz = Get-Location
$Destino = Join-Path $Raiz "dist\InnovaFemIA-Entrega"

Write-Host "`n[1/5] Preparando carpetas de distribucion..." -ForegroundColor Cyan
if (Test-Path $Destino) {
    Remove-Item -Path $Destino -Recurse -Force
}
New-Item -ItemType Directory -Path "$Destino\app" -Force | Out-Null
New-Item -ItemType Directory -Path "$Destino\docker\mysql" -Force | Out-Null

# Intentar compilar Tailwind si npm esta disponible
if (Get-Command npm -ErrorAction SilentlyContinue) {
    Write-Host "Compilando TailwindCSS con npm..." -ForegroundColor Yellow
    Set-Location "$Raiz\src\InnovaFemIA.Web"
    npm run tw:build
    Set-Location $Raiz
}

Write-Host "[2/5] Compilando solucion a binarios de produccion (.dll)..." -ForegroundColor Yellow
dotnet publish "$Raiz\src\InnovaFemIA.Web\InnovaFemIA.Web.csproj" -c Release -o "$Destino\app" /p:UseAppHost=false | Out-Null

# Limpiar archivos de cache locales y asegurar carpetas limpias
if (Test-Path "$Destino\app\wwwroot\media_cache") {
    Remove-Item -Path "$Destino\app\wwwroot\media_cache\*" -Recurse -Force -ErrorAction SilentlyContinue
}
if (Test-Path "$Destino\app\wwwroot\uploads") {
    Remove-Item -Path "$Destino\app\wwwroot\uploads\*" -Recurse -Force -ErrorAction SilentlyContinue
}
New-Item -ItemType Directory -Path "$Destino\app\wwwroot\media_cache" -Force | Out-Null
New-Item -ItemType Directory -Path "$Destino\app\wwwroot\uploads" -Force | Out-Null

# Asegurar que app.css exista para los estilos
$CssDir = "$Destino\app\wwwroot\css"
if (-not (Test-Path $CssDir)) {
    New-Item -ItemType Directory -Path $CssDir -Force | Out-Null
}
$AppCssDestino = "$CssDir\app.css"
if (-not (Test-Path $AppCssDestino)) {
    Copy-Item "$Raiz\src\InnovaFemIA.Web\wwwroot\css\app.src.css" $AppCssDestino -Force
}

# Asegurar copia integra y explicita de todas las imagenes estaticas
$ImgDestino = "$Destino\app\wwwroot\images"
if (-not (Test-Path $ImgDestino)) {
    New-Item -ItemType Directory -Path $ImgDestino -Force | Out-Null
}
Copy-Item "$Raiz\src\InnovaFemIA.Web\wwwroot\images\*" $ImgDestino -Recurse -Force

Write-Host "[3/5] Generando Dockerfile y configuraciones optimizadas..." -ForegroundColor Yellow
Copy-Item "$Raiz\docker\mysql\my.cnf" "$Destino\docker\mysql\my.cnf" -Force
Copy-Item "$Raiz\.env.example" "$Destino\.env" -Force

# Dockerfile ultra-rapido: ejecuta el runtime de ASP.NET 9 sobre los binarios
$DockerfileContenido = @"
# ==============================================================================
# InnovaFemIA - Runtime Container (Produccion Pre-Compilada)
# ==============================================================================
FROM mcr.microsoft.com/dotnet/aspnet:9.0-bookworm-slim AS final

RUN apt-get update && apt-get install -y --no-install-recommends curl tzdata \
    && rm -rf /var/lib/apt/lists/*

ENV TZ=America/Bogota
RUN ln -snf /usr/share/zoneinfo/`$TZ /etc/localtime && echo `$TZ > /etc/timezone

WORKDIR /app
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Production
ENV DOTNET_RUNNING_IN_CONTAINER=true

# Copiar binarios pre-compilados (.dll)
COPY app/ .

# Otorgar permisos 755 completos y propiedad al usuario no privilegiado 'app'
RUN mkdir -p /app/wwwroot/media_cache /app/wwwroot/uploads \
    && chmod -R 755 /app \
    && chown -R app:app /app

USER app

EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "InnovaFemIA.Web.dll"]
"@
Set-Content -Path "$Destino\docker\Dockerfile" -Value $DockerfileContenido -Encoding UTF8

# Docker Compose listo para el paquete de entrega
$ComposeContenido = @"
# ==============================================================================
# InnovaFemIA - Orquestacion de Produccion
# ==============================================================================
services:
  innovafemia-db:
    image: mysql:8.0
    container_name: innovafemia-db
    restart: unless-stopped
    command: >
      --default-authentication-plugin=mysql_native_password
      --character-set-server=utf8mb4
      --collation-server=utf8mb4_unicode_ci
      --max-allowed-packet=536870912
      --wait-timeout=28800
      --interactive-timeout=28800
      --innodb-buffer-pool-size=536870912
      --sql-mode=STRICT_TRANS_TABLES,NO_ENGINE_SUBSTITUTION
    environment:
      MYSQL_DATABASE: `${DB_DATABASE:-InnovaFemIA}
      MYSQL_USER: `${DB_USER:-innovafemia_user}
      MYSQL_PASSWORD: `${DB_PASSWORD:-InnovaFemIA_Pass_2026!}
      MYSQL_ROOT_PASSWORD: `${DB_ROOT_PASSWORD:-InnovaFemIA_Root_2026!}
      TZ: America/Bogota
    ports:
      - "`${DB_PORT:-127.0.0.1:3306}:3306"
    volumes:
      - innovafemia_mysql_data:/var/lib/mysql
      - ./docker/mysql/my.cnf:/etc/mysql/conf.d/custom.cnf:ro
    healthcheck:
      test: ["CMD", "mysqladmin", "ping", "-h", "localhost"]
      interval: 10s
      timeout: 5s
      retries: 10
      start_period: 45s
    networks:
      - innovafemia_net

  innovafemia-web:
    container_name: innovafemia-web
    restart: unless-stopped
    build:
      context: .
      dockerfile: docker/Dockerfile
    environment:
      ConnectionStrings__DefaultConnection: >-
        Server=innovafemia-db;Port=3306;Database=`${DB_DATABASE:-InnovaFemIA};User=`${DB_USER:-innovafemia_user};Password=`${DB_PASSWORD:-InnovaFemIA_Pass_2026!};Allow User Variables=true;SslMode=None;
      ASPNETCORE_ENVIRONMENT: `${ASPNETCORE_ENVIRONMENT:-Production}
      ASPNETCORE_URLS: http://+:8080
      DOTNET_RUNNING_IN_CONTAINER: "true"
      Admin__UsuarioPorDefecto: `${ADMIN_USER:-admin}
      Admin__ContrasenaPorDefecto: `${ADMIN_PASSWORD:-CambiarEstaContrasena123!}
      Admin__EmailPorDefecto: `${ADMIN_EMAIL:-admin@innovafemia.local}
    ports:
      - "`${APP_PORT:-8080}:8080"
    volumes:
      - innovafemia_media_cache:/app/wwwroot/media_cache
    depends_on:
      innovafemia-db:
        condition: service_healthy
    healthcheck:
      test: ["CMD-SHELL", "curl -f http://localhost:8080/health || exit 1"]
      interval: 20s
      timeout: 5s
      retries: 3
      start_period: 30s
    networks:
      - innovafemia_net

volumes:
  innovafemia_mysql_data:
    name: innovafemia_mysql_data
  innovafemia_media_cache:
    name: innovafemia_media_cache

networks:
  innovafemia_net:
    name: innovafemia_net
    driver: bridge
"@
Set-Content -Path "$Destino\docker-compose.yml" -Value $ComposeContenido -Encoding UTF8

Copy-Item "$Raiz\manage.sh" "$Destino\manage.sh" -Force

# Guia de despliegue compacta dentro del paquete
$ReadmeContenido = @"
# Despliegue de InnovaFemIA en Servidor Linux

Este paquete contiene la version de produccion pre-compilada de **InnovaFemIA** lista para iniciar con Docker.

## Instrucciones de Inicio Rapido:

1. Configure sus contrasenas en el archivo de entorno .env:
   nano .env

2. Inicie los contenedores (usando --build para compilar la imagen local con todos los recursos y permisos):
   docker compose up -d --build
   (o ejecute: ./manage.sh rebuild)

3. Verifique el estado de salud:
   docker compose ps
   (ambos contenedores deben mostrar estado 'healthy')

Para realizar copias de seguridad:
   ./manage.sh backup
"@
Set-Content -Path "$Destino\INSTRUCCIONES_DESPLIEGUE.md" -Value $ReadmeContenido -Encoding UTF8

Write-Host "[4/5] Comprimiendo paquete en dist\InnovaFemIA-Despliegue.zip (formato ZIP estándar)..." -ForegroundColor Yellow
$ZipDest = Join-Path $Raiz "dist\InnovaFemIA-Despliegue.zip"
if (Test-Path $ZipDest) { Remove-Item $ZipDest -Force }

Compress-Archive -Path "$Destino\*" -DestinationPath $ZipDest -Force

Write-Host "`n[5/5] Paquete generado con exito!" -ForegroundColor Green
Write-Host "Carpeta lista: $Destino" -ForegroundColor White
Write-Host "Archivo ZIP listo para enviar: $ZipDest" -ForegroundColor Cyan
Write-Host "Cero codigo expuesto: No hay archivos .cs. Solo binarios compilados." -ForegroundColor Green
