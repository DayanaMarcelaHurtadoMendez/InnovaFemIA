# InnovaFemIA

Plataforma tecnológica integral para la gestión productiva, pedagógica y comercial desarrollada para la Asociación Municipal de Mujeres Campesinas de Caldono (ASCAMPESINA), Cauca, en el marco del Proyecto Orquídeas.

La plataforma articula tres módulos verticales sobre una misma infraestructura, gestionados y supervisados de manera centralizada desde un módulo de administración integral:

1. **Cartografía Productiva y Calendario Lunar:** Georreferenciación de predios campesinos, red de actores territoriales, saberes ancestrales de siembra según fases lunares e indicadores de precios mayoristas DANE SIPSA.
2. **Asistente Educativo y Modo Lección (Chatbot Asalia):** Asistente conversacional agroecológico, catálogo de recursos formativos multimedia y módulo interactivo de evaluación campesina.
3. **Biocomercio y Diagnóstico Fitosanitario con IA:** Monitoreo descentralizado de inventario de cosechas (Mi Stock), calculadoras agronómicas/financieras y diagnóstico foliar de enfermedades de café mediante visión artificial.
4. **Panel de Administración Integral:** Módulo transversal de control administrativo que centraliza el padrón de productoras asociadas, el catálogo de especies agrícolas, el monitor consolidado de oferta municipal, la asignación de credenciales campesinas, la biblioteca de contenidos y el visor de auditoría técnica del sistema.

---

## 1. Stack Tecnológico y Dependencias

### Núcleo de la Aplicación
- **Lenguaje y Plataforma:** C# 13 sobre **.NET 9** (`net9.0`).
- **Framework Web:** **ASP.NET Core 9** (modelo híbrido: vistas MVC renderizadas con Razor y controladores de API REST).
- **Servidor Web:** Kestrel integrado bajo proxy inverso (Nginx / Docker Swarm).

### Base de Datos y Persistencia
- **Motor Relacional:** **MySQL 8.0** oficial.
  - Conjunto de caracteres: `utf8mb4`.
  - Intercalación: `utf8mb4_unicode_ci`.
  - Autenticación: `mysql_native_password`.
  - Soporte de paquetes de red: 512 MB (`max_allowed_packet = 536870912`) para streaming y almacenamiento de medios.
- **ORM / Mapeo Objeto-Relacional:** **Entity Framework Core 9** con enfoque Code-First.

### Paquetes NuGet (Backend)
- `MediatR` (versión 12.5.0): Implementación del patrón Mediador y segregación CQRS (comandos y consultas desacopladas).
- `Microsoft.EntityFrameworkCore.Design` (versión 9.0.7): Herramientas en tiempo de diseño para generación y control de migraciones.
- `Pomelo.EntityFrameworkCore.MySql` (versión 9.0.0-preview.2): Proveedor optimizado de Entity Framework Core para MySQL.
- `BCrypt.Net-Next` (versión 4.2.0): Funciones de hash criptográfico con sal dinámica para resguardo de contraseñas.
- `HtmlAgilityPack` (versión 1.12.4): Analizador sintáctico de HTML para el servicio de extracción de boletines del DANE.
- `ExcelDataReader` (versión 3.9.0) y `ExcelDataReader.DataSet` (versión 3.9.0): Lectura y conversión estructurada de boletines mayoristas en formato Excel.
- `SquareWidget.Astronomy.Core` (versión 1.0.0): Motor astronómico basado en los algoritmos de Jean Meeus para cálculo de posición, iluminación y fase lunar.

### Frontend y Diseño
- **Framework CSS:** **TailwindCSS** (versión 3.4.0), compilado mediante Node.js / npm.
- **JavaScript:** Vanilla JavaScript modular organizado por dominios (`wwwroot/js/`), sin dependencias de frameworks cliente pesados (cero JS inline en vistas).
- **Cartografía Web:** Leaflet 1.9.4 para visualización de mapas interactivos y capas GeoJSON.

### Servicios Externos
- **Roboflow Serverless API:** Modelo de visión artificial para clasificación de imágenes foliares de café (detección de roya, mancha de hierro, broca y ojo de gallo).
- **DANE SIPSA:** Sistema de Información de Precios y Abastecimiento del Sector Agropecuario (boletines semanales de Popayán y Cali).

---

## 2. Requisitos de Entorno para Desarrollo

Para compilar y ejecutar el proyecto localmente se requiere:
- **.NET 9 SDK** (versión 9.0.100 o superior).
- **Node.js** (versión 18 o superior) y **npm** (para la compilación de estilos TailwindCSS).
- **MySQL 8.0** local o accesible por red, con una base de datos creada (por defecto `InnovaFemIA`).

---

## 3. Puesta en Marcha en Entorno Local

1. Clonar el repositorio y situarse en la rama `develop`:
   ```bash
   git checkout develop
   ```

2. Configurar la conexión y credenciales en `src/InnovaFemIA.Web/appsettings.json`:
   ```json
   {
     "Logging": {
       "LogLevel": {
         "Default": "Information",
         "Microsoft.AspNetCore": "Warning",
         "Microsoft.EntityFrameworkCore": "Warning"
       }
     },
     "AllowedHosts": "*",
     "PATH_BASE": "/innovafemia",
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Port=3306;Database=InnovaFemIA;User=root;Password=TU_CONTRASENA_AQUI;SslMode=None;Allow User Variables=true;"
     },
     "Sesion": {
       "TiempoExpiracionMinutos": 60,
       "NombreCookieAdmin": "InnovaFemIA.Admin",
       "NombreCookieFamilia": "InnovaFemIA.Familia"
     },
     "Scraping": {
       "IntervalHoras": 5,
       "DelayInicialMinutos": 1,
       "UrlDane": "https://www.dane.gov.co/index.php/estadisticas-por-tema/agropecuario/sistema-de-informacion-de-precios-sipsa/mayoristas-boletin-semanal-1/boletin-mayorista-semanal-2026"
     },
     "Admin": {
       "UsuarioPorDefecto": "admin",
       "ContrasenaPorDefecto": "CambiarEstaContrasena123!",
       "EmailPorDefecto": "admin@innovafemia.local"
     },
     "Roboflow": {
       "WorkflowUrl": "https://serverless.roboflow.com/iot-proyecto/workflows/hojas-de-cafe-enfermedades-obje",
       "ApiKey": "TU_API_KEY_ROBOFLOW",
       "Workspace": "iot-proyecto",
       "WorkflowId": "hojas-de-cafe-enfermedades-obje"
     }
   }
   ```

3. Compilar los estilos TailwindCSS:
   ```bash
   cd src/InnovaFemIA.Web
   npm install
   npm run tw:build
   cd ../..
   ```

4. Compilar y ejecutar la aplicación:
   ```bash
   dotnet run --project src/InnovaFemIA.Web/InnovaFemIA.Web.csproj
   ```

Al iniciar por primera vez, el servicio `DatabaseInitializer` ejecuta automáticamente las migraciones pendientes en MySQL, crea las tablas del sistema, provisiona el usuario administrador por defecto e inicializa el catálogo de cultivos.

---

## 4. Generación del Paquete de Entrega para Producción

Para entregar o desplegar la plataforma en servidores de producción sin exponer el código fuente (`.cs`), el repositorio incluye el script automatizado `generar_paquete_entrega.ps1`.

### Cómo ejecutar el script
Desde PowerShell en la raíz del proyecto:
```powershell
powershell -ExecutionPolicy Bypass -File .\generar_paquete_entrega.ps1
```

### Qué realiza el script:
1. **Compilación de estilos:** Ejecuta `npm run tw:build` en el proyecto web para generar el archivo CSS final minificado (`app.css`).
2. **Compilación a binarios cerrados:** Ejecuta `dotnet publish -c Release /p:UseAppHost=false` generando exclusivamente librerías precompiladas (`.dll`) y vistas precompiladas en `dist/InnovaFemIA-Entrega/app/`. No se incluye ningún archivo `.cs`, resguardando la propiedad intelectual del software.
3. **Limpieza e higiene de carpetas:** Inicializa los directorios vacíos de `media_cache` y `uploads` para evitar arrastrar residuos de pruebas locales.
4. **Generación de la infraestructura Docker:**
   - **`Dockerfile`:** Contenedor optimizado de ejecución basado en `mcr.microsoft.com/dotnet/aspnet:9.0-bookworm-slim`, configurado bajo el usuario de seguridad no privilegiado `app`, con zona horaria `America/Bogota` y sondeo de salud (`HEALTHCHECK`) sobre `/health`.
   - **`docker-compose.yml`:** Orquestación completa de los dos servicios requeridos: `innovafemia-web` (aplicación web) e `innovafemia-db` (motor MySQL 8.0 con volumen persistente).
   - **`docker/mysql/my.cnf`:** Configuración de MySQL con parámetros optimizados para operación estable (`innodb_buffer_pool_size = 512M`, `max_allowed_packet = 512M`, timeouts de 28800 segundos).
   - **`.env`:** Archivo base de variables de entorno (puertos, contraseñas y sub-ruta `PATH_BASE`).
   - **`.dockerignore`:** Reglas de exclusión para optimizar el contexto de construcción.
   - **`manage.sh`:** Script utilitario para administración en servidores Linux (arranque, detención, respaldos `mysqldump` y reconstrucción sin pérdida de datos).
   - **`INSTRUCCIONES_DESPLIEGUE.md`:** Manual operativo detallado paso a paso para actualizar servidores existentes conservando intacta la base de datos MySQL o para desplegar instalaciones nuevas.
5. **Empaquetado final:** Comprime todo el contenido en el archivo estándar `dist/InnovaFemIA-Despliegue.zip` (aproximadamente 7 a 9 MB), listo para su transferencia al servidor.

---

## 5. Estructura de Ramas y Convenciones de Trabajo

El repositorio sigue un esquema de ramificación estructurado:

### Ramas Principales
- **`main`:** Rama protegida de producción que refleja la versión estable desplegada.
- **`develop`:** Rama activa de integración continua donde converge el desarrollo de los tres módulos y la administración.

### Ramas de Registro de Software ante la Dirección Nacional de Derecho de Autor
Para cumplir con los requerimientos formales de registro de soporte lógico ante la Dirección Nacional de Derecho de Autor, se mantienen tres ramas aisladas cuyo código fuente corresponde exclusivamente a cada dominio, conservando únicamente el núcleo común indispensable para su compilación y operación autónoma:
- **`registro-software/modulo-mapa-lunar`:** Cartografía productiva, georreferenciación, saberes ancestrales lunares, indicadores DANE y administración territorial.
- **`registro-software/modulo-chatbot`:** Asistente conversacional Asalia, biblioteca multimedia, modo lección y administración pedagógica.
- **`registro-software/modulo-biocomercio`:** Portal de productoras campesinas, Mi Stock, calculadoras agronómicas, diagnóstico foliar con IA y administración de cosechas.

### Convención de Ramas de Trabajo
- Las ramas temporales de desarrollo se derivan desde `develop` utilizando el prefijo `feature/`:
  - `feature/modulo-precios-dane`
  - `feature/deteccion-enfermedades-biocomercio`

### Convención de Mensajes de Commit
Los commits del proyecto deben redactarse de manera formal, técnica y descriptiva, iniciando con el prefijo `feature|` seguido del detalle del cambio:
```bash
git commit -m "feature|Se incorpora soporte para... y se optimiza la persistencia relacional"
```

---

## 6. Documentación Arquitectónica Adicional

Para consultar el análisis en profundidad de patrones de diseño, detalles de implementación de CQRS con MediatR, persistencia con EF Core y componentes de seguridad, consulte el documento:
- [ARQUITECTURA.md](ARQUITECTURA.md): Especificación técnica y diseño arquitectónico detallado.
