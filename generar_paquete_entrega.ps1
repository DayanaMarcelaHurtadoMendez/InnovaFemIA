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
ENV PATH_BASE=/innovafemia

# Copiar binarios pre-compilados (.dll)
COPY app/ .

# Otorgar permisos 777 completos a las carpetas de medios y subidas, y propiedad al usuario 'app'
RUN mkdir -p /app/wwwroot/media_cache /app/wwwroot/uploads \
    && chmod -R 777 /app/wwwroot/media_cache /app/wwwroot/uploads \
    && chown -R app:app /app

USER app

EXPOSE 8080
HEALTHCHECK --interval=30s --timeout=5s --start-period=30s --retries=3 \
    CMD curl -f http://localhost:8080/health || exit 1

ENTRYPOINT ["dotnet", "InnovaFemIA.Web.dll"]
"@
# Guardar Dockerfile tanto en la raiz del paquete como en docker/ para compatibilidad con cualquier orquestador
Set-Content -Path "$Destino\docker\Dockerfile" -Value $DockerfileContenido -Encoding UTF8
Set-Content -Path "$Destino\Dockerfile" -Value $DockerfileContenido -Encoding UTF8

if (Test-Path "$Raiz\.dockerignore") {
    Copy-Item "$Raiz\.dockerignore" "$Destino\.dockerignore" -Force
}

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
      dockerfile: Dockerfile
    environment:
      ConnectionStrings__DefaultConnection: >-
        Server=innovafemia-db;Port=3306;Database=`${DB_DATABASE:-InnovaFemIA};User=`${DB_USER:-innovafemia_user};Password=`${DB_PASSWORD:-InnovaFemIA_Pass_2026!};Allow User Variables=true;SslMode=None;
      ASPNETCORE_ENVIRONMENT: `${ASPNETCORE_ENVIRONMENT:-Production}
      ASPNETCORE_URLS: http://+:8080
      DOTNET_RUNNING_IN_CONTAINER: "true"
      PATH_BASE: `${PATH_BASE:-/innovafemia}
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

# Guia de despliegue y actualizacion detallada dentro del paquete
$ReadmeContenido = @"
# Guia de Despliegue y Actualizacion - InnovaFemIA (Proyecto Completo)

Este documento detalla el procedimiento para:
1. **Actualizacion en un servidor de produccion existente** que ya tiene los contenedores en ejecucion (``innovafemia-web`` e ``innovafemia-db``), **preservando al 100% la base de datos MySQL y sin borrar ningun registro**.
2. **Despliegue inicial desde cero.**

---

## Opcion A: Actualizar un Servidor que YA esta en Produccion (Sin Borrar la BD)

Si ya tienes la plataforma publicada y solo deseas subir los nuevos cambios (correcciones del chatbot, recursos multimedia, nuevas vistas o ajustes de interfaz):

### Paso 0: Respaldo Preventivo de la Base de Datos (Recomendado)
Antes de actualizar, genera una copia de seguridad en caliente de la base de datos MySQL existente:

````bash
# Opcion 1: Usando el script de control
./manage.sh backup

# Opcion 2: Ejecutando mysqldump directamente en el contenedor
docker exec innovafemia-db mysqldump -u innovafemia_user -p"InnovaFemIA_Pass_2026!" --single-transaction InnovaFemIA > backup_previo_`$(date +%Y%m%d_%H%M%S).sql
````
*Nota: El volumen de datos de MySQL (``innovafemia_mysql_data``) se conserva intacto en los siguientes pasos.*

---

### Paso 1: Extraer los Nuevos Archivos en el Servidor
1. Transfiere el archivo ``InnovaFemIA-Despliegue.zip`` al servidor de produccion.
2. Extrae el contenido en el directorio del proyecto para actualizar la carpeta ``app/`` y el ``Dockerfile``:
   ````bash
   unzip -o InnovaFemIA-Despliegue.zip -d /ruta/al/proyecto/InnovaFemIA
   cd /ruta/al/proyecto/InnovaFemIA
   ````
   > **ATENCION con el archivo ``.env``:**
   > Si ya configuraste contrasenas o puertos en el ``.env`` existente del servidor, **NO lo sobreescribas**. Solo verifica que contenga la variable de sub-ruta:
   > ````env
   > PATH_BASE=/innovafemia
   > ````

---

### Paso 2: Detener y Remover Unicamente el Contenedor Web
Para actualizar la aplicacion sin interrumpir ni reiniciar el motor de base de datos MySQL:

````bash
# 1. Detener unicamente el servicio web
docker compose stop innovafemia-web

# 2. Eliminar el contenedor web anterior
docker compose rm -f innovafemia-web
````

> **ADVERTENCIA CRITICA:**
> **JAMAS ejecutes ``docker compose down -v``**, ya que el parametro ``-v`` (volumes) borraria permanentemente los datos fisicos de la base de datos.
> La base de datos ``innovafemia-db`` puede seguir corriendo mientras reemplazas ``innovafemia-web``.

---

### Paso 3: Reconstruir la Imagen Web
Reconstruye la imagen de Docker para incorporar los nuevos binarios precompilados y los permisos en la cache:

````bash
docker compose build --no-cache innovafemia-web
````

---

### Paso 4: Iniciar el Servicio Web Actualizado
Inicia el nuevo contenedor web en segundo plano:

````bash
docker compose up -d innovafemia-web
````

*(Atajo alternativo en un solo paso: ``docker compose up -d --build --no-deps innovafemia-web`` o ``./manage.sh rebuild``)*

---

### Paso 5: Verificar Estado y Logs
1. Comprueba que ambos servicios esten saludables (``healthy``):
   ````bash
   docker compose ps
   ````
   *Ambos servicios (``innovafemia-db`` e ``innovafemia-web``) deben figurar como ``Up`` y ``(healthy)``.*

2. Revisa los registros del arranque:
   ````bash
   docker compose logs -f --tail=30 innovafemia-web
   ````
   *(Presiona ``Ctrl + C`` para salir de los logs).*

---

### Paso 6: Verificacion en el Navegador
Accede a traves del dominio con el sub-path configurado (``/innovafemia``):
- **Inicio / Portal:** ``https://tudominio.com/innovafemia``
- **Asistente Educativo (Chatbot Asalia):** ``https://tudominio.com/innovafemia/chatbot``
- **Modo Leccion (Trivias Campesinas):** ``https://tudominio.com/innovafemia/chatbot/lecciones``
- **Biocomercio y Deteccion IA:** ``https://tudominio.com/innovafemia/biocomercio``
- **Mapa Interactivo:** ``https://tudominio.com/innovafemia/mapa``
- **Calendario Lunar Agricola:** ``https://tudominio.com/innovafemia/mapa/lunar``
- **Indicadores DANE SIPSA:** ``https://tudominio.com/innovafemia/mapa/precios``
- **Panel Administrador:** ``https://tudominio.com/innovafemia/admin/login``

---

## Opcion B: Despliegue Inicial Desde Cero (Primer Despliegue)

Si estas instalando la aplicacion por primera vez en un servidor nuevo:

1. Configura tus credenciales y puertos en ``.env``:
   ````bash
   nano .env
   ````
2. Inicia los contenedores por primera vez:
   ````bash
   docker compose up -d --build
   ````
3. Verifica que ambos contenedores esten listos:
   ````bash
   docker compose ps
   ````

---

## Plan de Reversion (Rollback de Emergencia)
Si por cualquier motivo requieres volver a la version previa:
1. Reemplaza los archivos de la carpeta ``app/`` con tu respaldo anterior.
2. Ejecuta:
   ````bash
   docker compose build --no-cache innovafemia-web
   docker compose up -d innovafemia-web
   ````
"@
[System.IO.File]::WriteAllText("$Destino\INSTRUCCIONES_DESPLIEGUE.md", $ReadmeContenido, [System.Text.Encoding]::UTF8)

Write-Host "[4/5] Comprimiendo paquete en dist\InnovaFemIA-Despliegue.zip (formato ZIP estándar)..." -ForegroundColor Yellow
$ZipDest = Join-Path $Raiz "dist\InnovaFemIA-Despliegue.zip"
if (Test-Path $ZipDest) { Remove-Item $ZipDest -Force }

Compress-Archive -Path "$Destino\*" -DestinationPath $ZipDest -Force

Write-Host "`n[5/5] Paquete generado con exito!" -ForegroundColor Green
Write-Host "Carpeta lista: $Destino" -ForegroundColor White
Write-Host "Archivo ZIP listo para enviar: $ZipDest" -ForegroundColor Cyan
Write-Host "Cero codigo expuesto: No hay archivos .cs. Solo binarios compilados." -ForegroundColor Green
