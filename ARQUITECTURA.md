# Arquitectura de Software - InnovaFemIA

Este documento expone los fundamentos de diseño, la organización estructural y los patrones técnicos implementados en la plataforma web InnovaFemIA, desarrollada para la Asociación Municipal de Mujeres Campesinas de Caldono (ASCAMPESINA), Cauca, dentro del Proyecto Orquídeas.

---

## 1. Principio Fundamental: Vertical Slice Architecture

El sistema adopta el patrón de Arquitectura por Tramos Verticales (Vertical Slice Architecture) apoyado en principios de Arquitectura Limpia y segregación de responsabilidades CQRS (Command Query Responsibility Segregation) mediante la librería MediatR.

A diferencia del esquema tradicional en N-capas horizontales (donde los controladores, servicios y repositorios se agrupan en capas separadas), la solución organiza el código alrededor de características de negocio (Features).

### Beneficios del diseño vertical:
- **Alta cohesión:** Cada caso de uso (comando o consulta) agrupa en una misma carpeta su definición, su manejador (Handler) y sus objetos de transferencia de datos (DTOs).
- **Bajo acoplamiento:** Los cambios en un módulo no repercuten en los demás. Cada módulo puede evolucionar, probarse y aislarse de forma autónoma.
- **Trazabilidad:** Facilita la correspondencia directa entre los requerimientos funcionales y los artefactos de código fuente.
- **Aislamiento para registro de software:** Permite extraer ramas independientes con el código fuente exclusivo de cada módulo para trámites de registro ante la Dirección Nacional de Derecho de Autor.

---

## 2. Topología de la Solución

```text
InnovaFemIA/
|-- docker/                                     # Configuraciones de infraestructura y base de datos
|   `-- mysql/
|       `-- my.cnf                              # Parametrización del motor MySQL (paquetes de 512 MB, timeouts)
|
|-- src/
|   `-- InnovaFemIA.Web/                        # Proyecto principal ASP.NET Core 9
|       |-- BackgroundServices/                 # Tareas asíncronas de ejecución continua (IHostedService)
|       |   `-- ScrapingPreciosService.cs       # Extracción automatizada de boletines mayoristas DANE SIPSA
|       |
|       |-- Controllers/                        # Controladores MVC de nivel superior y enrutamiento base
|       |   |-- BiocomercioController.cs        # Vistas y APIs del dominio de Biocomercio
|       |   `-- HomeController.cs               # Controlador de redirección y portada inicial
|       |
|       |-- Features/                           # Tramos verticales organizados por dominio
|       |   |-- Admin/                          # Panel de administración integral
|       |   |   |-- AdminDashboard/             # Caso de uso: Consulta de métricas consolidadas
|       |   |   |-- AdminLogin/                 # Caso de uso: Autenticación administrativa
|       |   |   |-- Controllers/                # Controladores del panel administrativo (divididos por partials)
|       |   |   |-- GestionCuentasFamilias/     # Caso de uso: Administración de accesos a Mi Stock
|       |   |   |-- GestionCultivos/            # Caso de uso: Catálogo municipal de especies agrícolas
|       |   |   |-- GestionFamilias/            # Caso de uso: Padrón de productoras asociadas
|       |   |   |-- GestionRegistros/           # Caso de uso: Visor y filtro de auditoría técnica
|       |   |   |-- GestionStockConsolidado/    # Caso de uso: Monitoreo de disponibilidad agropecuaria
|       |   |   `-- GestionTiemposCosecha/      # Caso de uso: Parámetros de floración y cosechas
|       |   |
|       |   |-- Biocomercio/                    # Dominio de soberanía económica campesina
|       |   |   |-- Auth/                       # Autenticación descentralizada de familias
|       |   |   |-- Calculadoras/               # Estimaciones agronómicas y de rentabilidad
|       |   |   |-- DeteccionEnfermedades/      # Diagnóstico fitosanitario con IA (Roboflow Serverless)
|       |   |   |-- ObtenerPrecios/             # Consultas de precios de mercado y boletines DANE
|       |   |   `-- Stock/                      # Registro y actualización de cosechas por predio
|       |   |
|       |   |-- Chatbot/                        # Dominio de asistente educativo y lecciones
|       |   |   |-- Controllers/                # Endpoints de contenidos y evaluación
|       |   |   |-- ObtenerContenidos/          # Catálogo pedagógico y streaming de medios
|       |   |   `-- ObtenerLecciones/           # Modo lección, trivias y preguntas formativas
|       |   |
|       |   |-- Mapa/                           # Dominio cartográfico y saberes ancestrales
|       |   |   |-- Controllers/                # Endpoints públicos de navegación y APIs cartográficas
|       |   |   |-- ObtenerActores/             # Directorio de actores territoriales
|       |   |   |-- ObtenerCultivos/            # Filtros por especies agrícolas
|       |   |   |-- ObtenerFamilias/            # Georreferenciación de productoras y predios
|       |   |   |-- ObtenerLunar/               # Cálculo astronómico de fases lunares y recomendaciones
|       |   |   `-- ObtenerPrecios/             # Cotizaciones de referencia
|       |   |
|       |   `-- Shared/                         # Infraestructura transversal compartida
|       |       |-- Attributes/                 # Atributos de metadatos (PublicApiAttribute)
|       |       |-- Common/                     # Mónadas funcionales Result<T> y PagedResult<T>
|       |       |-- Entities/                   # Modelo relacional de entidades (EF Core)
|       |       |-- Middleware/                 # Componentes del pipeline HTTP (ApiKey, CaseInsensitive, etc.)
|       |       |-- Persistence/                # AppDbContext y DatabaseInitializer
|       |       `-- Services/                   # Caché de medios y cálculos especializados
|       |
|       |-- Migrations/                         # Historial de migraciones de Entity Framework Core
|       |-- Models/                             # Modelos de presentación general (ErrorViewModel)
|       |-- Properties/                         # Perfiles de ejecución y publicación
|       |-- Views/                              # Vistas Razor (.cshtml) agrupadas por contexto
|       `-- wwwroot/                            # Recursos estáticos: CSS minificado, JS modular, imágenes
|
|-- Dockerfile                                  # Definición de imagen de ejecución de producción
|-- docker-compose.yml                          # Orquestación del stack completo (Web + MySQL 8.0)
|-- generar_paquete_entrega.ps1                 # Script de compilación a binarios cerrados y empaquetado
`-- manage.sh                                   # Utilidad de administración para servidores Linux
```

---

## 3. Delimitación de Módulos de Negocio

### Módulo 1: Cartografía Productiva, Saberes Ancestrales e Indicadores DANE
- **Directorio de Productoras:** Registro georreferenciado con coordenadas GPS, vereda, predio, líneas productivas asociadas y fotografía de la titular.
- **Mapa Interactivo:** Visualización cartográfica basada en Leaflet con filtrado dinámico por cultivo y sector territorial, permitiendo ubicar la producción de ASCAMPESINA en el municipio de Caldono.
- **Calendario Lunar Agrícola:** Algoritmos astronómicos de Jean Meeus provistos por `SquareWidget.Astronomy.Core`, combinados con reglas de conocimiento ancestral campesino para guiar siembras, podas, cosechas y labores del suelo. Incluye módulo interactivo de saberes tradicionales.
- **Indicadores DANE SIPSA:** Monitoreo semanal de precios mayoristas de alimentos procedentes de las centrales de abastecimiento de Popayán y Cali, actualizados vía scraping automático.

### Módulo 2: Asistente Educativo y Modo Lección (Chatbot Asalia)
- **Asistente Conversacional:** Orientador pedagógico especializado en temáticas agroecológicas, conservación de semillas, soberanía alimentaria y enfoque de género.
- **Biblioteca Pedagógica:** Módulo de consulta multimedia con soporte para fichas técnicas, cartillas en formato PDF, grabaciones sonoras y cápsulas de video, transmitidas mediante streaming directo con soporte de rangos HTTP (`206 Partial Content`).
- **Modo Lección:** Sistema de evaluación formativa con preguntas de selección múltiple, explicación contextual de respuestas y seguimiento de progreso interactivo.

### Módulo 3: Biocomercio Campesino y Diagnóstico Fitosanitario con IA
- **Portal de Acceso para Productoras:** Autenticación individual de productoras campesinas para la gestión autónoma de su inventario.
- **Mi Stock:** Panel descentralizado donde cada socia registra, actualiza o retira lotes de cosecha disponibles para comercialización, con georreferenciación del predio.
- **Calculadoras Agronómicas:** Herramientas de cálculo técnico:
  - Estimación de fechas de floración y cosecha basadas en parámetros técnicos de Cenicafé y Agrosavia.
  - Calculadora financiera de costos, ingresos proyectados y margen de rentabilidad por cultivo.
  - Calculadora de factor de rendimiento en café pergamino seco.
- **Detección Fitosanitaria con Inteligencia Artificial:** Integración con modelo de visión artificial alojado en la infraestructura serverless de Roboflow, capaz de clasificar anomalías foliares en tiempo real desde cámaras de dispositivos móviles o fotografías adjuntas (roya, mancha de hierro, broca y ojo de gallo).

### Módulo de Administración Integral (Admin)
- **Control Central:** Panel administrativo unificado que presenta métricas clave de operación (productoras vinculadas, lotes en stock, recursos educativos, cultivos registrados).
- **Mantenimiento Territorial:** Formularios modales para creación, edición y eliminación de productoras, actores, cultivos, reglas lunares y credenciales de acceso.
- **Auditoría Técnica:** Registro centralizado (`RegistrosSistema`) que captura eventos relevantes, cambios de estado, excepciones técnicas y ejecuciones de scraping.

---

## 4. Persistencia y Acceso a Datos

La persistencia de información se implementa mediante **Entity Framework Core 9** con el proveedor **Pomelo.EntityFrameworkCore.MySql** sobre un motor **MySQL 8.0**.

### Características del modelo de persistencia:
1. **Configuración Fluida (Fluent API):** Todas las relaciones relacionales (muchos a muchos, uno a muchos), índices únicos y restricciones de borrado se definen en el método `OnModelCreating` de `AppDbContext.cs`, conservando las clases del modelo de entidades puras y limpias.
2. **Inicialización Idempotente (`DatabaseInitializer`):**
   - Al arrancar la aplicación, se ejecuta `await db.Database.MigrateAsync()`, aplicando de forma ordenada y transaccional las migraciones pendientes sin requerir ejecución manual de scripts SQL.
   - Aplica sentencias `CREATE TABLE IF NOT EXISTS` para tablas de soporte especial (como los históricos de precios SIPSA).
   - Verifica y aprovisiona el usuario administrador inicial si la base de datos se encuentra vacía.
3. **Optimización para Multimedia:** La base de datos y la conexión se encuentran parametrizadas para soportar transferencias de paquetes masivos de hasta 512 MB (`max_allowed_packet = 536870912`), permitiendo almacenar o canalizar material de difusión institucional.

---

## 5. Seguridad y Pipeline HTTP

### Autenticación Híbrida
- **Sesiones de Usuario y Administrador:** Implementadas mediante cookies seguras HTTP-only (`CookieAuthenticationDefaults.AuthenticationScheme`) con protección SameSite Strict y expiración parametrizable.
- **Hash de Contraseñas:** Las credenciales de productoras y administradores se protegen mediante algoritmos criptográficos de una sola vía basados en salt dinámico (PBKDF2 / BCrypt provisto por `BCrypt.Net-Next`).

### Exposición Controlada de APIs (`ApiKeyMiddleware`)
- Las rutas bajo el prefijo `/api/` están protegidas por defecto contra accesos externos no autorizados (retornando HTTP 403 Forbidden).
- Aquellos endpoints que requieren consumo público o integración con terceros se decoran explícitamente con el atributo `[PublicApi]`.
- El middleware valida la clave enviada (en la cabecera `X-Api-Key` o el parámetro `?apikey=`) comparando su hash SHA-256 contra la tabla de `ApiKeys` en MySQL.
- Peticiones originadas desde el propio frontend de la aplicación (mismo origen comprobado por cabeceras `Referer` y `Sec-Fetch-Site`) se permiten de forma transparente sin requerir clave estática.

### Compatibilidad con Contenedores Linux (`CaseInsensitiveStaticFilesMiddleware`)
Dado que los sistemas operativos basados en Linux diferencian mayúsculas de minúsculas en nombres de archivo (case-sensitive), el middleware intercepta las peticiones de recursos estáticos en `wwwroot` y resuelve coincidencias independientemente de la combinación de mayúsculas y minúsculas utilizada en la URL.

---

## 6. Servicios en Segundo Plano

La recolección periódica de precios de referencia se realiza mediante `ScrapingPreciosService`, implementado sobre `BackgroundService` (`IHostedService`).

- Se ejecuta en un hilo asíncrono independiente, garantizando que tareas de extracción de datos externos no degraden el tiempo de respuesta del servidor web Kestrel.
- Incorpora un retardo inicial configurable para permitir que la base de datos complete su ciclo de arranque antes del primer sondeo.
- Cuenta con manejo de excepciones y reintentos: en caso de que el sitio oficial del DANE no se encuentre disponible, registra el evento en auditoría y mantiene las cotizaciones vigentes sin interrumpir la operación del portal.
