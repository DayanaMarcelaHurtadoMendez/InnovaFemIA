# InnovaFemIA

Plataforma tecnologica integral para la gestion productiva, pedagogica y comercial desarrollada para la Asociacion Municipal de Mujeres Campesinas de Caldono (ASCAMPESINA), Cauca, en el marco del Proyecto Orquideas.

La plataforma articula tres modulos verticales sobre una misma infraestructura:
1. **Cartografia Productiva y Calendario Lunar:** Georreferenciacion de predios campesinos, red de actores territoriales, saberes ancestrales de siembra segun fases lunares e indicadores de precios mayoristas DANE SIPSA.
2. **Asistente Educativo y Modo Leccion (Chatbot Asalia):** Asistente conversacional agroecologico, catalogo de recursos formativos multimedia y modulo interactivo de evaluacion campesina.
3. **Biocomercio y Diagnostico Fitosanitario con IA:** Monitoreo descentralizado de inventario de cosechas (Mi Stock), calculadoras agronomicas/financieras y diagnostico foliar de enfermedades de cafe mediante vision artificial.

---

## 1. Stack Tecnologico y Dependencias

### Nucleo de la Aplicacion
- **Lenguaje y Plataforma:** C# 13 sobre **.NET 9** (`net9.0`).
- **Framework Web:** **ASP.NET Core 9** (modelo hibrido: vistas MVC renderizadas con Razor y controladores de API REST).
- **Servidor Web:** Kestrel integrado bajo proxy inverso (Nginx / Docker Swarm).

### Base de Datos y Persistencia
- **Motor Relacional:** **MySQL 8.0** oficial.
  - Conjunto de caracteres: `utf8mb4`.
  - Intercalacion: `utf8mb4_unicode_ci`.
  - Autenticacion: `mysql_native_password`.
  - Soporte de paquetes de red: 512 MB (`max_allowed_packet = 536870912`) para streaming y almacenamiento de medios.
- **ORM / Mapeo Objeto-Relacional:** **Entity Framework Core 9** con enfoque Code-First.

### Paquetes NuGet (Backend)
- `MediatR` (version 12.5.0): Implementacion del patron Mediador y segregacion CQRS (comandos y consultas desacopladas).
- `Microsoft.EntityFrameworkCore.Design` (version 9.0.7): Herramientas en tiempo de diseno para generacion y control de migraciones.
- `Pomelo.EntityFrameworkCore.MySql` (version 9.0.0-preview.2): Proveedor optimizado de Entity Framework Core para MySQL.
- `BCrypt.Net-Next` (version 4.2.0): Funciones de hash criptografico con sal dinamica para resguardo de contrasenas.
- `HtmlAgilityPack` (version 1.12.4): Analizador sintactico de HTML para el servicio de extraccion de boletines del DANE.
- `ExcelDataReader` (version 3.9.0) y `ExcelDataReader.DataSet` (version 3.9.0): Lectura y conversion estructurada de boletines mayoristas en formato Excel.
- `SquareWidget.Astronomy.Core` (version 1.0.0): Motor astronomico basado en los algoritmos de Jean Meeus para calculo de posicion, iluminacion y fase lunar.

### Frontend y Diseno
- **Framework CSS:** **TailwindCSS** (version 3.4.0), compilado mediante Node.js / npm.
- **JavaScript:** Vanilla JavaScript modular organizado por dominios (`wwwroot/js/`), sin dependencias de frameworks cliente pesados (cero JS inline en vistas).
- **Cartografia Web:** Leaflet 1.9.4 para visualizacion de mapas interactivos y capas GeoJSON.

### Servicios Externos
- **Roboflow Serverless API:** Modelo de vision artificial para clasificacion de imagenes foliares de cafe (deteccion de roya, mancha de hierro, broca y ojo de gallo).
- **DANE SIPSA:** Sistema de Informacion de Precios y Abastecimiento del Sector Agropecuario (boletines semanales de Popayan y Cali).

---

## 2. Requisitos de Entorno para Desarrollo

Para compilar y ejecutar el proyecto localmente se requiere:
- **.NET 9 SDK** (version 9.0.100 o superior).
- **Node.js** (version 18 o superior) y **npm** (para la compilacion de estilos TailwindCSS).
- **MySQL 8.0** local o accesible por red, con una base de datos creada (por defecto `InnovaFemIA`).

---

## 3. Puesta en Marcha en Entorno Local

1. Clonar el repositorio y situarse en la rama `develop`:
   ```bash
   git checkout develop
   ```

2. Configurar la conexion y credenciales en `src/InnovaFemIA.Web/appsettings.json`:
   ```json
   {
     "ConnectionStrings": {
       "DefaultConnection": "Server=localhost;Port=3306;Database=InnovaFemIA;User=root;Password=TuPassword;Allow User Variables=true;"
     },
     "Admin": {
       "UsuarioPorDefecto": "admin",
       "ContrasenaPorDefecto": "InnovaFemIA2026!",
       "EmailPorDefecto": "admin@innovafemia.local"
     },
     "Roboflow": {
       "WorkflowUrl": "https://serverless.roboflow.com/iot-proyecto/workflows/hojas-de-cafe-enfermedades-obje",
       "ApiKey": "TU_API_KEY",
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

4. Compilar y ejecutar la aplicacion:
   ```bash
   dotnet run --project src/InnovaFemIA.Web/InnovaFemIA.Web.csproj
   ```

Al iniciar por primera vez, el servicio `DatabaseInitializer` ejecuta automaticamente las migraciones pendientes en MySQL, crea las tablas del sistema, provisiona el usuario administrador por defecto e inicializa el catalogo de cultivos.

---

## 4. Generacion del Paquete de Entrega para Produccion

Para entregar o desplegar la plataforma en servidores de produccion sin exponer el codigo fuente (`.cs`), el repositorio incluye el script automatizado `generar_paquete_entrega.ps1`.

### Como ejecutar el script
Desde PowerShell en la raiz del proyecto:
```powershell
powershell -ExecutionPolicy Bypass -File .\generar_paquete_entrega.ps1
```

### Que realiza el script:
1. **Compilacion de estilos:** Ejecuta `npm run tw:build` en el proyecto web para generar el archivo CSS final minificado (`app.css`).
2. **Compilacion a binarios cerrados:** Ejecuta `dotnet publish -c Release /p:UseAppHost=false` generando exclusivamente librerias precompiladas (`.dll`) y vistas precompiladas en `dist/InnovaFemIA-Entrega/app/`. No se incluye ningun archivo `.cs`, resguardando la propiedad intelectual del software.
3. **Limpieza e higiene de carpetas:** Inicializa los directorios vacios de `media_cache` y `uploads` para evitar arrastrar residuos de pruebas locales.
4. **Generacion de la infraestructura Docker:**
   - **`Dockerfile`:** Contenedor optimizado de ejecucion basado en `mcr.microsoft.com/dotnet/aspnet:9.0-bookworm-slim`, configurado bajo el usuario de seguridad no privilegiado `app`, con zona horaria `America/Bogota` y sondeo de salud (`HEALTHCHECK`) sobre `/health`.
   - **`docker-compose.yml`:** Orquestacion completa de los dos servicios requeridos: `innovafemia-web` (aplicacion web) e `innovafemia-db` (motor MySQL 8.0 con volumen persistente).
   - **`docker/mysql/my.cnf`:** Configuracion de MySQL con parametros optimizados para operacion estable (`innodb_buffer_pool_size = 512M`, `max_allowed_packet = 512M`, timeouts de 28800 segundos).
   - **`.env`:** Archivo base de variables de entorno (puertos, contrasenas y sub-ruta `PATH_BASE`).
   - **`.dockerignore`:** Reglas de exclusion para optimizar el contexto de construccion.
   - **`manage.sh`:** Script utilitario para administracion en servidores Linux (arranque, detencion, respaldos `mysqldump` y reconstruccion sin perdida de datos).
   - **`INSTRUCCIONES_DESPLIEGUE.md`:** Manual operativo detallado paso a paso para actualizar servidores existentes conservando intacta la base de datos MySQL o para desplegar instalaciones nuevas.
5. **Empaquetado final:** Comprime todo el contenido en el archivo estandar `dist/InnovaFemIA-Despliegue.zip` (aproximadamente 7 a 9 MB), listo para su transferencia al servidor.

---

## 5. Estructura de Ramas y Convenciones de Trabajo

El repositorio sigue un esquema de ramificacion estructurado:

### Ramas Principales
- **`main`:** Rama protegida de produccion que refleja la version estable desplegada.
- **`develop`:** Rama activa de integracion continua donde converge el desarrollo de los tres modulos y la administracion.

### Ramas de Registro de Software (DNDA)
Para cumplir con los requerimientos formales de registro de soporte logico ante la Direccion Nacional de Derecho de Autor, se mantienen tres ramas aisladas cuyo codigo fuente corresponde exclusivamente a cada dominio, conservando unicamente el nucleo comun indispensable para su compilacion y operacion autonoma:
- **`registro-software/modulo-mapa-lunar`:** Cartografia productiva, georreferenciacion, saberes ancestrales lunares, indicadores DANE y administracion territorial.
- **`registro-software/modulo-chatbot`:** Asistente conversacional Asalia, biblioteca multimedia, modo leccion y administracion pedagogica.
- **`registro-software/modulo-biocomercio`:** Portal de productoras campesinas, Mi Stock, calculadoras agronomicas, diagnostico foliar con IA y administracion de cosechas.

### Convencion de Ramas de Trabajo
- Las ramas temporales de desarrollo se derivan desde `develop` utilizando el prefijo `feature/`:
  - `feature/modulo-precios-dane`
  - `feature/deteccion-enfermedades-biocomercio`

### Convencion de Mensajes de Commit
Los commits del proyecto deben redactarse de manera formal, tecnica y descriptiva, iniciando con el prefijo `feature|` seguido del detalle del cambio:
```bash
git commit -m "feature|Se incorpora soporte para... y se optimiza la persistencia relacional"
```

---

## 6. Documentacion Arquitectonica Adicional

Para consultar el analisis en profundidad de patrones de diseno, tics de implementacion de CQRS con MediatR, persistencia con EF Core y componentes de seguridad, consulte el documento:
- [ARQUITECTURA.md](ARQUITECTURA.md): Especificacion tecnica y diseno arquitectonico detallado.
