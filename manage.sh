#!/bin/bash
# ==============================================================================
# InnovaFemIA - Script de Gestión y Mantenimiento de Contenedores Docker
# ==============================================================================
# Uso:
#   ./manage.sh start      - Iniciar servicios en segundo plano
#   ./manage.sh stop       - Detener servicios sin borrar datos
#   ./manage.sh restart    - Reiniciar servicios
#   ./manage.sh rebuild    - Recompilar imagen y actualizar contenedores
#   ./manage.sh logs       - Ver registros en tiempo real
#   ./manage.sh status     - Ver estado de salud de los contenedores
#   ./manage.sh backup     - Generar copia de seguridad (dump) de la base de datos
# ==============================================================================

set -e

# Detectar directorio raíz del proyecto dinámicamente
SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
if [ -f "$SCRIPT_DIR/docker-compose.yml" ]; then
    PROJECT_ROOT="$SCRIPT_DIR"
elif [ -f "$SCRIPT_DIR/../docker-compose.yml" ]; then
    PROJECT_ROOT="$SCRIPT_DIR/.."
else
    PROJECT_ROOT="$(pwd)"
fi

cd "$PROJECT_ROOT"

# Cargar variables de entorno si existe .env
if [ -f .env ]; then
    export $(grep -v '^#' .env | xargs)
fi

DB_NAME="${DB_DATABASE:-InnovaFemIA}"
DB_USER="root"
DB_PASS="${DB_ROOT_PASSWORD}"

case "$1" in
    start)
        echo "🚀 Iniciando contenedores de InnovaFemIA..."
        docker compose up -d
        echo "✅ Servicios iniciados. Use './manage.sh status' para verificar la salud."
        ;;
    stop)
        echo "🛑 Deteniendo contenedores de InnovaFemIA..."
        docker compose stop
        echo "✅ Servicios detenidos."
        ;;
    restart)
        echo "🔄 Reiniciando contenedores..."
        docker compose restart
        ;;
    rebuild)
        echo "🔨 Reconstruyendo imágenes y aplicando cambios..."
        docker compose up -d --build
        echo "✅ Despliegue completado con éxito."
        ;;
    logs)
        docker compose logs -f --tail=100
        ;;
    status)
        docker compose ps
        ;;
    backup)
        FECHA=$(date +'%Y%m%d_%H%M%S')
        DIR_BACKUP="./backups"
        mkdir -p "$DIR_BACKUP"
        ARCHIVO="$DIR_BACKUP/backup_${DB_NAME}_${FECHA}.sql"
        echo "💾 Generando respaldo de la base de datos en: $ARCHIVO ..."
        DB_CONTAINER="innovafemia-mapa-db"
        if ! docker ps --format '{{.Names}}' 2>/dev/null | grep -q "^innovafemia-mapa-db$"; then
            if docker ps --format '{{.Names}}' 2>/dev/null | grep -q "^innovafemia-db$"; then
                DB_CONTAINER="innovafemia-db"
            fi
        fi
        docker exec "$DB_CONTAINER" mysqldump -u "$DB_USER" -p"$DB_PASS" --single-transaction --quick "$DB_NAME" > "$ARCHIVO"
        echo "✅ Respaldo generado con éxito en: $ARCHIVO"
        ;;
    *)
        echo "InnovaFemIA - Script de Control Docker"
        echo "Comandos disponibles:"
        echo "  start    - Iniciar contenedores en segundo plano"
        echo "  stop     - Detener contenedores"
        echo "  restart  - Reiniciar contenedores"
        echo "  rebuild  - Recompilar código y reiniciar contenedores"
        echo "  logs     - Ver logs en vivo"
        echo "  status   - Ver estado y salud de los contenedores"
        echo "  backup   - Generar respaldo SQL de la base de datos"
        exit 1
        ;;
esac
