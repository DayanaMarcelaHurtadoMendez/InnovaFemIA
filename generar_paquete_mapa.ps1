# ==============================================================================
# Script de Generacion de Paquete: Contenedor Especializado "Modo Mapa + DANE"
# InnovaFemIA - Modo Reducido (Mapa Interactivo, Directorio, DANE y Calendario Lunar con Juego)
# ==============================================================================
# Este contenedor incluye:
#  - /mapa y /mapa/mapa (Mapa Interactivo)
#  - /mapa/directorio (Directorio de Familias)
#  - /mapa/lunar (Calendario Lunar)
#  - /mapa/lunar/juego (Juego interactivo "Aprende con la Luna" - Saberes ASCAMPESINA)
#  - /mapa/precios (Modulo de Indicadores Socioeconomicos y Precios DANE)
#  - /admin (Panel Administrador completo adaptado con prefijo dinámico /innovafemia)
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
    $json | Add-Member -MemberType NoteProperty -Name "PATH_BASE" -Value "/innovafemia" -Force
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
ENV PATH_BASE=/innovafemia

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
      PATH_BASE: "`${PATH_BASE:-/innovafemia}"
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

# Path Base para Proxy Reverso (ej. /innovafemia para https://i2thub.icesi.edu.co:5443/innovafemia, o dejar vacio para raiz /)
PATH_BASE=/innovafemia

# Administrador inicial
ADMIN_USER=admin
ADMIN_PASSWORD=CambiarEstaContrasena123!
ADMIN_EMAIL=admin@innovafemia.local
"@
Set-Content -Path "$Destino\.env" -Value $EnvContenido -Encoding UTF8

Copy-Item "$Raiz\manage.sh" "$Destino\manage.sh" -Force

# Guia de despliegue para el contenedor de produccion
$ReadmeContenido = @'
# Guía de Actualización — InnovaFemIA (Modo Mapa)

Este documento detalla el procedimiento para actualizar la plataforma en un servidor de producción que ya tiene la versión anterior de los contenedores Docker en ejecución (`innovafemia-mapa-web` e `innovafemia-mapa-db`), preservando los datos de la base de datos MySQL.

---

### Paso 0: Respaldo Preventivo de la Base de Datos (Recomendado)
Antes de actualizar, genere una copia de seguridad en caliente de la base de datos MySQL existente:

```bash
# Opción 1: Usando el script manage.sh incluido
./manage.sh backup

# Opción 2: Ejecutando mysqldump directamente en el contenedor
docker exec innovafemia-mapa-db mysqldump -u innovafemia_user -p"InnovaFemIA_Pass_2026!" --single-transaction InnovaFemIA_Mapa > backup_previo_$(date +%Y%m%d_%H%M%S).sql
```
*Nota: El volumen de datos de MySQL (`innovafemia_mapa_mysql_data`) se conserva intacto en los siguientes pasos.*

---

### Paso 1: Extraer los Nuevos Binarios en el Servidor
1. Transfiera el archivo `InnovaFemIA-Mapa-Despliegue.zip` al servidor de producción.
2. Extraiga el contenido en el directorio del proyecto para actualizar la carpeta `app/`:
   ```bash
   unzip -o InnovaFemIA-Mapa-Despliegue.zip -d /ruta/al/proyecto/InnovaFemIA
   cd /ruta/al/proyecto/InnovaFemIA
   ```
   > ⚠️ **ATENCIÓN con el archivo `.env`:**
   > Si ya configuró contraseñas o puertos personalizados en el `.env` existente del servidor, **NO lo sobreescriba**. Solo verifique que contenga:
   > ```env
   > PATH_BASE=/innovafemia
   > ```

---

### Paso 2: Detener y Remover Únicamente el Contenedor Web
Para actualizar la aplicación sin interrumpir el almacenamiento de la base de datos:

```bash
# 1. Detener únicamente el servicio web
docker compose stop innovafemia-mapa-web

# 2. Eliminar el contenedor web viejo
docker compose rm -f innovafemia-mapa-web
```

> 🛑 **ADVERTENCIA:**
> **NO ejecute `docker compose down -v`**, ya que el parámetro `-v` borraría permanentemente los datos de la base de datos.

---

### Paso 3: Reconstruir la Imagen Web
Reconstruya la imagen de Docker para incorporar los nuevos binarios precompilados:

```bash
docker compose build --no-cache innovafemia-mapa-web
```

---

### Paso 4: Iniciar el Servicio Web Actualizado
Inicie el nuevo contenedor web en segundo plano:

```bash
docker compose up -d innovafemia-mapa-web
```

Si por alguna razón la base de datos también estaba apagada, inicie todo con:
```bash
docker compose up -d
```

---

### Paso 5: Verificar Estado y Logs
1. Compruebe que ambos servicios estén saludables:
   ```bash
   docker compose ps
   ```
   *Ambos servicios (`innovafemia-mapa-db` e `innovafemia-mapa-web`) deben figurar como `Up` y `(healthy)`.*

2. Revise los registros del arranque:
   ```bash
   docker compose logs -f --tail=30 innovafemia-mapa-web
   ```
   *(Presione `Ctrl + C` para salir de los logs).*

---

### Paso 6: Verificación en Navegador
Acceda a través del dominio institucional con el sub-path configurado:
`https://i2thub.icesi.edu.co:5443/innovafemia`

- **Directorio de Familias:** `https://i2thub.icesi.edu.co:5443/innovafemia/mapa/directorio`
- **Mapa Interactivo:** `https://i2thub.icesi.edu.co:5443/innovafemia/mapa`
- **Indicadores DANE:** `https://i2thub.icesi.edu.co:5443/innovafemia/mapa/precios`
- **Calendario Lunar:** `https://i2thub.icesi.edu.co:5443/innovafemia/mapa/lunar`
- **Login Administrador:** `https://i2thub.icesi.edu.co:5443/innovafemia/admin/login`

---

### Plan de Reversión (Rollback en caso de emergencia)
Si requiere volver a la versión previa:
1. Reemplace los archivos de la carpeta `app/` con su respaldo anterior.
2. Ejecute:
   ```bash
   docker compose build --no-cache innovafemia-mapa-web
   docker compose up -d innovafemia-mapa-web
   ```
'@
[System.IO.File]::WriteAllText("$Destino\INSTRUCCIONES_DESPLIEGUE.md", $ReadmeContenido, [System.Text.Encoding]::UTF8)

Write-Host "[4/5] Comprimiendo paquete en dist\InnovaFemIA-Mapa-Despliegue.zip (formato ZIP estándar)..." -ForegroundColor Yellow
$ZipDest = Join-Path $Raiz "dist\InnovaFemIA-Mapa-Despliegue.zip"
if (Test-Path $ZipDest) { Remove-Item $ZipDest -Force }

Compress-Archive -Path "$Destino\*" -DestinationPath $ZipDest -Force

Write-Host "`n[5/5] Paquete Modo Mapa + DANE generado con exito!" -ForegroundColor Green
Write-Host "Carpeta lista: $Destino" -ForegroundColor White
Write-Host "Archivo ZIP listo para enviar: $ZipDest" -ForegroundColor Cyan
Write-Host "Modo publico: Mapa interactivo, Directorio, DANE y Calendario Lunar con Juego." -ForegroundColor Yellow
Write-Host "Modo Administrador: 100% completo, adaptado para sub-path /innovafemia." -ForegroundColor Green
Write-Host "Cero codigo expuesto: No hay archivos .cs. Solo binarios compilados." -ForegroundColor Green
