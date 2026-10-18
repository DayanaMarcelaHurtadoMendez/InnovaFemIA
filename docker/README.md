# 🐳 InnovaFemIA - Configuración de Contenedores Docker

Este directorio centraliza todos los componentes de infraestructura y contenedores del proyecto **InnovaFemIA**.

## 📂 Estructura de este Directorio

```
docker/
├── Dockerfile          # Definición Multi-Stage (.NET 9 SDK + TailwindCSS -> Runtime ASP.NET Core 9)
├── mysql/
│   └── my.cnf          # Configuración de MySQL 8.0 (512MB max_allowed_packet, utf8mb4, InnoDB)
├── manage.sh           # Script con comandos rápidos (start, stop, rebuild, logs, backup)
└── README.md           # Este documento de referencia
```

## 🚀 Uso Rápido

Desde la raíz del proyecto:
```bash
# 1. Configurar variables de entorno (si no existe .env)
cp .env.example .env

# 2. Iniciar contenedores en segundo plano
./manage.sh start
# o:
docker compose up -d --build

# 3. Ver estado de salud
./manage.sh status

# 4. Generar copia de seguridad de la base de datos
./manage.sh backup
```

Para documentación exhaustiva sobre proxy inverso (Nginx), SSL y persistencia de migraciones, consulte `GUIA_DESPLIEGUE_DOCKER.md` en la raíz del repositorio.
