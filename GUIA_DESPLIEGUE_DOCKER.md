# 🐳 Guía de Despliegue en Contenedores Docker — InnovaFemIA

Este documento contiene las especificaciones técnicas y la guía de operación para desplegar **InnovaFemIA** en un entorno de producción usando **Docker** y **Docker Compose**.

---

## 🏛️ Arquitectura de Contenedores

El despliegue está compuesto por dos servicios orquestados en una red privada aislada (`innovafemia_net`):

```
                       [ Internet / Usuarios ]
                                  │
                                  ▼
                   [ Proxy Inverso (Nginx / SSL) ]
                                  │ :80 / :443
                                  ▼
                    ┌───────────────────────────┐
                    │      innovafemia-web      │
                    │   (.NET 9 + TailwindCSS)  │
                    │         Puerto 8080       │
                    └─────────────┬─────────────┘
                                  │ Red interna Docker
                                  ▼
                    ┌───────────────────────────┐
                    │      innovafemia-db       │
                    │         (MySQL 8.0)       │
                    │         Puerto 3306       │
                    └─────────────┬─────────────┘
                                  │
                   ┌──────────────┴──────────────┐
                   ▼                             ▼
       [ innovafemia_mysql_data ]    [ innovafemia_media_cache ]
         (Datos persistentes BD)       (Videos / PDFs en streaming)
```

1. **`innovafemia-web`:** Aplicación web monolítica modular en ASP.NET Core (.NET 9), compilada mediante Docker Multi-Stage con Node.js para minificación de TailwindCSS, ejecutada bajo el usuario seguro no-root `app`.
2. **`innovafemia-db`:** Servidor MySQL 8.0 oficial con optimización de paquetes masivos (512 MB) para archivos de video en `LONGBLOB` y codificación `utf8mb4`.

### 📂 Estructura Modular de Archivos Docker

```
InnovaFemIA/
├── docker/
│   ├── Dockerfile          # Definición Multi-Stage (.NET 9 SDK + TailwindCSS -> Runtime ASP.NET Core 9)
│   ├── mysql/
│   │   └── my.cnf          # Configuración de MySQL (512MB max_allowed_packet, utf8mb4, buffers)
│   ├── manage.sh           # Script de gestión operativo
│   └── README.md           # Resumen técnico de la carpeta docker
├── docker-compose.yml      # Orquestación de servicios y volúmenes
├── manage.sh               # Acceso rápido en la raíz para ejecutar comandos
├── .env.example            # Plantilla documentada de variables de entorno
└── .dockerignore           # Exclusiones de contexto para optimizar compilación
```

---

## 🛡️ Adaptabilidad a Futuras Migraciones (Cero Pérdida de Datos)

El sistema está diseñado para que **ninguna actualización o migración futura borre datos**:

1. **Persistencia Física en Volúmenes Nombrados:**
   - La base de datos reside en el volumen `innovafemia_mysql_data` montado en `/var/lib/mysql`.
   - Cuando se recrea el contenedor o se actualiza la imagen web, los datos de MySQL **permanecen intactos en el disco del host**.
2. **Migraciones Automáticas Idempotentes:**
   - En el arranque (`Program.cs`), la aplicación ejecuta `await db.Database.MigrateAsync()`.
   - EF Core consulta la tabla interna `__EFMigrationsHistory`. Si hay una nueva versión con nuevas columnas o tablas, **solo aplica las diferencias (deltas)**. Jamás elimina tablas ni borra registros existentes.
3. **Caché Persistente de Medios:**
   - El volumen `innovafemia_media_cache` guarda localmente en el host los videos y archivos pesados extraídos de la BD para que no se pierdan al reiniciar contenedores.

---

## 🚀 Despliegue Rápido (Paso a Paso)

### 1. Clonar el repositorio en el servidor
```bash
git clone https://github.com/DayanaMarcelaHurtadoMendez/InnovaFemIA.git
cd InnovaFemIA
```

### 2. Configurar variables de entorno
```bash
cp .env.example .env
nano .env  # o vim .env
```
> **Importante:** Defina contraseñas seguras para `DB_PASSWORD` y `DB_ROOT_PASSWORD`.

### 3. Iniciar los contenedores
```bash
chmod +x manage.sh
./manage.sh start
# o con Docker Compose directamente:
docker compose up -d --build
```

### 4. Verificar salud de los contenedores
```bash
./manage.sh status
# o:
docker compose ps
```
Ambos contenedores deben mostrar estado `healthy` (saludable):
- `innovafemia-db`: `healthy` (vía `mysqladmin ping`)
- `innovafemia-web`: `healthy` (vía `curl http://localhost:8080/health`)

---

## ⚙️ Puertos y Configuración de Red

| Servicio | Puerto Interno | Variable `.env` | Puerto Host por Defecto | Recomendación de Seguridad |
| :--- | :---: | :---: | :---: | :--- |
| **Aplicación Web** | `8080` | `APP_PORT` | `8080` | Apuntar desde el Proxy Inverso (Nginx) a `127.0.0.1:8080` |
| **Base de Datos** | `3306` | `DB_PORT` | `127.0.0.1:3306` | **No exponer al internet público**. Atar a `127.0.0.1:3306` solo para administración local / túnel SSH |

---

## 💾 Comandos Útiles de Mantenimiento

```bash
# Ver logs en tiempo real
./manage.sh logs

# Realizar copia de seguridad inmediata de la base de datos (genera dump .sql en ./backups/)
./manage.sh backup

# Detener los contenedores (los datos quedan guardados)
./manage.sh stop

# Recompilar y aplicar cambios tras un git pull
git pull origin main
./manage.sh rebuild
```

---

## 🔒 Configuración Típica con Nginx (Proxy Inverso con SSL)

Si el servidor utiliza Nginx con certificado SSL (Let's Encrypt / Certbot), agregue este bloque:

```nginx
server {
    listen 80;
    server_name innovafemia.dominio.com;
    return 301 https://$host$request_uri;
}

server {
    listen 443 ssl http2;
    server_name innovafemia.dominio.com;

    ssl_certificate /etc/letsencrypt/live/innovafemia.dominio.com/fullchain.pem;
    ssl_certificate_key /etc/letsencrypt/live/innovafemia.dominio.com/privkey.pem;

    # Permitir subida de videos y medios de hasta 500 MB
    client_max_body_size 500M;

    location / {
        proxy_pass http://127.0.0.1:8080;
        proxy_http_version 1.1;
        proxy_set_header Upgrade $http_upgrade;
        proxy_set_header Connection keep-alive;
        proxy_set_header Host $host;
        proxy_cache_bypass $http_upgrade;
        proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
        proxy_set_header X-Forwarded-Proto $scheme;
    }
}
```

---

## 🏛️ Despliegue con Sub-Ruta / PathBase (Caso Universidad ICESI i2T Hub)

Cuando la aplicación se despliega en un puerto unificado (ej. `:5443`) detrás de un proxy inverso que utiliza una sub-ruta fija como `https://i2thub.icesi.edu.co:5443/innovafemia`:

1. **Configurar la variable en `.env`:**
   ```env
   PATH_BASE=/innovafemia
   ```
2. **Comportamiento automático del backend (.NET 9):**
   - El pipeline HTTP activa automáticamente `app.UsePathBase("/innovafemia")`.
   - Las rutas internas, redirecciones (`RedirectToAction`), APIs (`/api/mapa/*`) y archivos estáticos (`/css`, `/images`, etc.) se mapean automáticamente bajo `/innovafemia`.
   - Si no se define o se deja vacía (`PATH_BASE=`), la aplicación funciona de forma transparente en la raíz `/`.
3. **Ejemplo de configuración en Nginx del Proxy Inverso:**
   ```nginx
   location /innovafemia/ {
       proxy_pass http://127.0.0.1:8081/innovafemia/;
       proxy_http_version 1.1;
       proxy_set_header Host $host;
       proxy_set_header X-Real-IP $remote_addr;
       proxy_set_header X-Forwarded-For $proxy_add_x_forwarded_for;
       proxy_set_header X-Forwarded-Proto $scheme;
       proxy_set_header X-Forwarded-Host $host:$server_port;
       proxy_set_header X-Forwarded-Prefix /innovafemia;
   }
   ```
