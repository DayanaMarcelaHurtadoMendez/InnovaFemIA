# ==============================================================================
# Script de Generacion de Paquete: Contenedor Especializado "Solo Mapa"
# InnovaFemIA - Modo Reducido (Mapa Interactivo, Directorio y Calendario Lunar)
# ==============================================================================
# Este contenedor incluye:
#  - /mapa y /mapa/mapa (Mapa Interactivo)
#  - /mapa/directorio (Directorio de Familias)
#  - /mapa/lunar (Calendario Lunar)
#  - /admin (Panel Administrador completo con todas sus funciones normales)
# Excluye del menu publico y bloquea el Asistente y Biocomercio.
# NO CONTIENE CODIGO FUENTE (.cs). Propiedad intelectual protegida.

$ErrorActionPreference = "Stop"
$Raiz = Get-Location
$Destino = Join-Path $Raiz "dist\InnovaFemIA-Mapa-Entrega"

Write-Host "`n[1/5] Preparando carpetas de distribucion para Modo Mapa..." -ForegroundColor Cyan
if (Test-Path $Destino) {
    Remove-Item -Path $Destino -Recurse -Force
}
New-Item -ItemType Directory -Path "$Destino\app" -Force | Out-Null
New-Item -ItemType Directory -Path "$Destino\docker\mysql" -Force | Out-Null

# Compilar Tailwind si npm esta disponible
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

# Configurar appsettings.json especificamente para ModoMapaSolo
$AppSettingsPath = "$Destino\app\appsettings.json"
if (Test-Path $AppSettingsPath) {
    $json = Get-Content $AppSettingsPath -Raw | ConvertFrom-Json
    $json | Add-Member -MemberType NoteProperty -Name "ModoMapaSolo" -Value $true -Force
    $json | ConvertTo-Json -Depth 10 | Set-Content -Path $AppSettingsPath -Encoding UTF8
}

Write-Host "[3/5] Generando Dockerfile y configuraciones para Modo Mapa..." -ForegroundColor Yellow
Copy-Item "$Raiz\docker\mysql\my.cnf" "$Destino\docker\mysql\my.cnf" -Force

# Dockerfile ultra-rapido con permisos 755
$DockerfileContenido = @"
# ==============================================================================
# InnovaFemIA (Modo Mapa) - Runtime Container
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
ENV ModoMapaSolo=true

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

# Docker Compose exclusivo para Modo Mapa (con nombres y puertos sin colisiones)
$ComposeContenido = @"
# ==============================================================================
# InnovaFemIA (Modo Mapa) - Orquestacion de Produccion
# ==============================================================================
services:
  innovafemia-mapa-db:
    image: mysql:8.0
    container_name: innovafemia-mapa-db
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
      MYSQL_DATABASE: `${DB_DATABASE:-InnovaFemIA_Mapa}
      MYSQL_USER: `${DB_USER:-innovafemia_user}
      MYSQL_PASSWORD: `${DB_PASSWORD:-InnovaFemIA_Pass_2026!}
      MYSQL_ROOT_PASSWORD: `${DB_ROOT_PASSWORD:-InnovaFemIA_Root_2026!}
      TZ: America/Bogota
    ports:
      - "`${DB_PORT:-127.0.0.1:3307}:3306"
    volumes:
      - innovafemia_mapa_mysql_data:/var/lib/mysql
      - ./docker/mysql/my.cnf:/etc/mysql/conf.d/custom.cnf:ro
    healthcheck:
      test: ["CMD", "mysqladmin", "ping", "-h", "localhost"]
      interval: 10s
      timeout: 5s
      retries: 10
      start_period: 45s
    networks:
      - innovafemia_mapa_net

  innovafemia-mapa-web:
    container_name: innovafemia-mapa-web
    restart: unless-stopped
    build:
      context: .
      dockerfile: docker/Dockerfile
    environment:
      ConnectionStrings__DefaultConnection: >-
        Server=innovafemia-mapa-db;Port=3306;Database=`${DB_DATABASE:-InnovaFemIA_Mapa};User=`${DB_USER:-innovafemia_user};Password=`${DB_PASSWORD:-InnovaFemIA_Pass_2026!};Allow User Variables=true;SslMode=None;
      ASPNETCORE_ENVIRONMENT: `${ASPNETCORE_ENVIRONMENT:-Production}
      ASPNETCORE_URLS: http://+:8080
      DOTNET_RUNNING_IN_CONTAINER: "true"
      ModoMapaSolo: "true"
      Admin__UsuarioPorDefecto: `${ADMIN_USER:-admin}
      Admin__ContrasenaPorDefecto: `${ADMIN_PASSWORD:-CambiarEstaContrasena123!}
      Admin__EmailPorDefecto: `${ADMIN_EMAIL:-admin@innovafemia.local}
    ports:
      - "`${APP_PORT:-8081}:8080"
    volumes:
      - innovafemia_mapa_media_cache:/app/wwwroot/media_cache
    depends_on:
      innovafemia-mapa-db:
        condition: service_healthy
    healthcheck:
      test: ["CMD-SHELL", "curl -f http://localhost:8080/health || exit 1"]
      interval: 20s
      timeout: 5s
      retries: 3
      start_period: 30s
    networks:
      - innovafemia_mapa_net

volumes:
  innovafemia_mapa_mysql_data:
    name: innovafemia_mapa_mysql_data
  innovafemia_mapa_media_cache:
    name: innovafemia_mapa_media_cache

networks:
  innovafemia_mapa_net:
    name: innovafemia_mapa_net
    driver: bridge
"@
Set-Content -Path "$Destino\docker-compose.yml" -Value $ComposeContenido -Encoding UTF8

# Archivo de entorno adaptado para el contenedor Modo Mapa
$EnvContenido = @"
# ==============================================================================
# InnovaFemIA (Modo Mapa) - Variables de Entorno (.env)
# ==============================================================================

# Puerto Web (por defecto 8081 para no colisionar si corre junto al contenedor completo)
APP_PORT=8081

# Puerto MySQL (por defecto 3307 para evitar colisiones)
DB_PORT=127.0.0.1:3307

# Base de datos
DB_DATABASE=InnovaFemIA_Mapa
DB_USER=innovafemia_user
DB_PASSWORD=InnovaFemIA_Pass_2026!
DB_ROOT_PASSWORD=InnovaFemIA_Root_2026!

# Entorno
ASPNETCORE_ENVIRONMENT=Production

# Administrador inicial
ADMIN_USER=admin
ADMIN_PASSWORD=CambiarEstaContrasena123!
ADMIN_EMAIL=admin@innovafemia.local
"@
Set-Content -Path "$Destino\.env" -Value $EnvContenido -Encoding UTF8

Copy-Item "$Raiz\manage.sh" "$Destino\manage.sh" -Force

# Guia de despliegue para el contenedor de produccion
$ReadmeContenido = @"
# Guia de Despliegue de InnovaFemIA

Este paquete contiene la solucion de produccion pre-compilada de **InnovaFemIA** lista para iniciar con Docker.

## Instrucciones de Despliegue:

1. Configure las contrasenas de la base de datos y el puerto web en el archivo .env:
   nano .env

2. Inicie los contenedores:
   docker compose up -d --build
   (o ejecute: ./manage.sh start)

3. Verifique el estado de salud:
   docker compose ps
   (los contenedores deben mostrar estado 'healthy' / saludable)

4. Acceda a la plataforma:
   http://<IP-DEL-SERVIDOR>:8081

Para realizar copias de seguridad de la base de datos:
   ./manage.sh backup
"@
Set-Content -Path "$Destino\INSTRUCCIONES_DESPLIEGUE.md" -Value $ReadmeContenido -Encoding UTF8

Write-Host "[4/5] Comprimiendo paquete en dist\InnovaFemIA-Mapa-Despliegue.zip (formato POSIX)..." -ForegroundColor Yellow
$ZipDest = Join-Path $Raiz "dist\InnovaFemIA-Mapa-Despliegue.zip"
if (Test-Path $ZipDest) { Remove-Item $ZipDest -Force }

if (Get-Command tar.exe -ErrorAction SilentlyContinue) {
    tar.exe -a -c -f "$ZipDest" -C "$Destino" .
} else {
    Compress-Archive -Path "$Destino\*" -DestinationPath $ZipDest -Force
}

Write-Host "`n[5/5] Paquete Modo Mapa generado con exito!" -ForegroundColor Green
Write-Host "Carpeta lista: $Destino" -ForegroundColor White
Write-Host "Archivo ZIP listo para enviar: $ZipDest" -ForegroundColor Cyan
Write-Host "Modo publico: Solo Mapa interactivo, Directorio y Calendario Lunar." -ForegroundColor Yellow
Write-Host "Modo Administrador: 100% completo y normal." -ForegroundColor Green
Write-Host "Cero codigo expuesto: No hay archivos .cs. Solo binarios compilados." -ForegroundColor Green
