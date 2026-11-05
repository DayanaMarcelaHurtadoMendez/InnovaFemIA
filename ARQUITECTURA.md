# Arquitectura de Software - InnovaFemIA

Este documento expone los fundamentos de diseno, la organizacion estructural y los patrones tecnicos implementados en la plataforma web InnovaFemIA, desarrollada para la Asociacion Municipal de Mujeres Campesinas de Caldono (ASCAMPESINA), Cauca, dentro del Proyecto Orquideas.

---

## 1. Principio Fundamental: Vertical Slice Architecture

El sistema adopta el patron de Arquitectura por Tramos Verticales (Vertical Slice Architecture) apoyado en principios de Arquitectura Limpia y segregacion de responsabilidades CQRS (Command Query Responsibility Segregation) mediante la libreria MediatR.

A diferencia del esquema tradicional en N-capas horizontales (donde los controladores, servicios y repositorios se agrupan en capas separadas), la solucion organiza el codigo alrededor de caracteristicas de negocio (Features).

### Beneficios del diseno vertical:
- **Alta cohesion:** Cada caso de uso (comando o consulta) agrupa en una misma carpeta su definicion, su manejador (Handler) y sus objetos de transferencia de datos (DTOs).
- **Bajo acoplamiento:** Los cambios en un modulo no repercuten en los demas. Cada modulo puede evolucionar, probarse y aislarse de forma autonoma.
- **Trazabilidad:** Facilita la correspondencia directa entre los requerimientos funcionales y los artefactos de codigo fuente.
- **Aislamiento para registro de software:** Permite extraer ramas independientes con el codigo fuente exclusivo de cada modulo para tramites de registro ante la Direccion Nacional de Derecho de Autor (DNDA).

---

## 2. Topologia de la Solucion

```text
InnovaFemIA/
|-- docker/                                     # Configuraciones de infraestructura y base de datos
|   `-- mysql/
|       `-- my.cnf                              # Parametrizacion del motor MySQL (paquetes de 512 MB, timeouts)
|
|-- src/
|   `-- InnovaFemIA.Web/                        # Proyecto principal ASP.NET Core 9
|       |-- BackgroundServices/                 # Tareas asincronas de ejecucion continua (IHostedService)
|       |   `-- ScrapingPreciosService.cs       # Extraccion automatizada de boletines mayoristas DANE SIPSA
|       |
|       |-- Controllers/                        # Controladores MVC de nivel superior y enrutamiento base
|       |   |-- BiocomercioController.cs        # Vistas y APIs del dominio de Biocomercio
|       |   `-- HomeController.cs               # Controlador de redireccion y portada inicial
|       |
|       |-- Features/                           # Tramos verticales organizados por dominio
|       |   |-- Admin/                          # Panel de administracion integral
|       |   |   |-- AdminDashboard/             # Caso de uso: Consulta de metricas consolidadas
|       |   |   |-- AdminLogin/                 # Caso de uso: Autenticacion administrativa
|       |   |   |-- Controllers/                # Controladores del panel administrativo (divididos por partials)
|       |   |   |-- GestionCuentasFamilias/     # Caso de uso: Administracion de accesos a Mi Stock
|       |   |   |-- GestionCultivos/            # Caso de uso: Catalogo municipal de especies agricolas
|       |   |   |-- GestionFamilias/            # Caso de uso: Padron de productoras asociadas
|       |   |   |-- GestionRegistros/           # Caso de uso: Visor y filtro de auditoria tecnica
|       |   |   |-- GestionStockConsolidado/    # Caso de uso: Monitoreo de disponibilidad agropecuaria
|       |   |   `-- GestionTiemposCosecha/      # Caso de uso: Parametros de floracion y cosechas
|       |   |
|       |   |-- Biocomercio/                    # Dominio de soberania economica campesina
|       |   |   |-- Auth/                       # Autenticacion descentralizada de familias
|       |   |   |-- Calculadoras/               # Estimaciones agronomicas y de rentabilidad
|       |   |   |-- DeteccionEnfermedades/      # Diagnostico fitosanitario con IA (Roboflow Serverless)
|       |   |   |-- ObtenerPrecios/             # Consultas de precios de mercado y boletines DANE
|       |   |   `-- Stock/                      # Registro y actualizacion de cosechas por predio
|       |   |
|       |   |-- Chatbot/                        # Dominio de asistente educativo y lecciones
|       |   |   |-- Controllers/                # Endpoints de contenidos y evaluacion
|       |   |   |-- ObtenerContenidos/          # Catalogo pedagogico y streaming de medios
|       |   |   `-- ObtenerLecciones/           # Modo leccion, trivias y preguntas formativas
|       |   |
|       |   |-- Mapa/                           # Dominio cartografico y saberes ancestrales
|       |   |   |-- Controllers/                # Endpoints publicos de navegacion y APIs cartograficas
|       |   |   |-- ObtenerActores/             # Directorio de actores territoriales
|       |   |   |-- ObtenerCultivos/            # Filtros por especies agricolas
|       |   |   |-- ObtenerFamilias/            # Georreferenciacion de productoras y predios
|       |   |   |-- ObtenerLunar/               # Calculo astronomico de fases lunares y recomendaciones
|       |   |   `-- ObtenerPrecios/             # Cotizaciones de referencia
|       |   |
|       |   `-- Shared/                         # Infraestructura transversal compartida
|       |       |-- Attributes/                 # Atributos de metadatos (PublicApiAttribute)
|       |       |-- Common/                     # Monadas funcionales Result<T> y PagedResult<T>
|       |       |-- Entities/                   # Modelo relacional de entidades (EF Core)
|       |       |-- Middleware/                 # Componentes del pipeline HTTP (ApiKey, CaseInsensitive, etc.)
|       |       |-- Persistence/                # AppDbContext y DatabaseInitializer
|       |       `-- Services/                   # Cache de medios y calculos especializados
|       |
|       |-- Migrations/                         # Historial de migraciones de Entity Framework Core
|       |-- Models/                             # Modelos de presentacion general (ErrorViewModel)
|       |-- Properties/                         # Perfiles de ejecucion y publicacion
|       |-- Views/                              # Vistas Razor (.cshtml) agrupadas por contexto
|       `-- wwwroot/                            # Recursos estaticos: CSS minificado, JS modular, imagenes
|
|-- Dockerfile                                  # Definicion de imagen de ejecucion de produccion
|-- docker-compose.yml                          # Orquestacion del stack completo (Web + MySQL 8.0)
|-- generar_paquete_entrega.ps1                 # Script de compilacion a binarios cerrados y empaquetado
`-- manage.sh                                   # Utilidad de administracion para servidores Linux
```

---

## 3. Delimitacion de Modulos de Negocio

### Modulo 1: Cartografia Productiva, Saberes Ancestrales e Indicadores DANE
- **Directorio de Productoras:** Registro georreferenciado con coordenadas GPS, vereda, predio, lineas productivas asociadas y fotografia de la titular.
- **Mapa Interactivo:** Visualizacion cartografica basada en Leaflet con filtrado dinámico por cultivo y sector territorial, permitiendo ubicar la produccion de ASCAMPESINA en el municipio de Caldono.
- **Calendario Lunar Agricola:** Algoritmos astronomicos de Jean Meeus provistos por `SquareWidget.Astronomy.Core`, combinados con reglas de conocimiento ancestral campesino para guiar siembras, podas, cosechas y labores del suelo. Incluye modulo interactivo de saberes tradicionales.
- **Indicadores DANE SIPSA:** Monitoreo semanal de precios mayoristas de alimentos procedentes de las centrales de abastecimiento de Popayan y Cali, actualizados via scraping automatico.

### Modulo 2: Asistente Educativo y Modo Leccion (Chatbot Asalia)
- **Asistente Conversacional:** Orientador pedagogico especializado en tematicas agroecologicas, conservacion de semillas, soberania alimentaria y enfoque de genero.
- **Biblioteca Pedagogica:** Modulo de consulta multimedia con soporte para fichas tecnicas, cartillas en formato PDF, grabaciones sonoras y capsulas de video, transmitidas mediante streaming directo con soporte de rangos HTTP (`206 Partial Content`).
- **Modo Leccion:** Sistema de evaluacion formativa con preguntas de seleccion multiple, explicacion contextual de respuestas y seguimiento de progreso interactivo.

### Modulo 3: Biocomercio Campesino y Diagnostico Fitosanitario con IA
- **Portal de Acceso para Productoras:** Autenticacion individual de productoras campesinas para la gestion autonoma de su inventario.
- **Mi Stock:** Panel descentralizado donde cada socia registra, actualiza o retira lotes de cosecha disponibles para comercializacion, con georreferenciacion del predio.
- **Calculadoras Agronomicas:** Herramientas de calculo tecnico:
  - Estimacion de fechas de floracion y cosecha basadas en parametros tecnicos de Cenicafe y Agrosavia.
  - Calculadora financiera de costos, ingresos proyectados y margen de rentabilidad por cultivo.
  - Calculadora de factor de rendimiento en cafe pergamino seco.
- **Deteccion Fitosanitaria con Inteligencia Artificial:** Integracion con modelo de vision artificial alojado en la infraestructura serverless de Roboflow, capaz de clasificar anomalias foliares en tiempo real desde camaras de dispositivos moviles o fotografias adjuntas (roya, mancha de hierro, broca y ojo de gallo).

### Modulo de Administracion Integral (Admin)
- **Control Central:** Panel administrativo unificado que presenta metricas clave de operacion (productoras vinculadas, lotes en stock, recursos educativos, cultivos registrados).
- **Mantenimiento Territorial:** Formularios modales para creacion, edicion y eliminacion de productoras, actores, cultivos, reglas lunares y credenciales de acceso.
- **Auditoria Tecnica:** Registro centralizado (`RegistrosSistema`) que captura eventos relevantes, cambios de estado, excepciones tecnicas y ejecuciones de scraping.

---

## 4. Persistencia y Acceso a Datos

La persistencia de informacion se implementa mediante **Entity Framework Core 9** con el proveedor **Pomelo.EntityFrameworkCore.MySql** sobre un motor **MySQL 8.0**.

### Caracteristicas del modelo de persistencia:
1. **Configuracion Fluida (Fluent API):** Todas las relaciones relacionales (muchos a muchos, uno a muchos), indices unicos y restricciones de borrado se definen en el metodo `OnModelCreating` de `AppDbContext.cs`, conservando las clases del modelo de entidades puras y limpias.
2. **Inicializacion Idempotente (`DatabaseInitializer`):**
   - Al arrancar la aplicacion, se ejecuta `await db.Database.MigrateAsync()`, aplicando de forma ordenada y transaccional las migraciones pendientes sin requerir ejecucion manual de scripts SQL.
   - Aplica sentencias `CREATE TABLE IF NOT EXISTS` para tablas de soporte especial (como los historicos de precios SIPSA).
   - Verifica y aprovisiona el usuario administrador inicial si la base de datos se encuentra vacia.
3. **Optimizacion para Multimedia:** La base de datos y la conexion se encuentran parametrizadas para soportar transferencias de paquetes masivos de hasta 512 MB (`max_allowed_packet = 536870912`), permitiendo almacenar o canalizar material de difusion institucional.

---

## 5. Seguridad y Pipeline HTTP

### Autenticacion Hibrida
- **Sesiones de Usuario y Administrador:** Implementadas mediante cookies seguras HTTP-only (`CookieAuthenticationDefaults.AuthenticationScheme`) con proteccion SameSite Strict y expiracion parametrizable.
- **Hash de Contrasenas:** Las credenciales de productoras y administradores se protegen mediante algoritmos criptograficos de una sola via basados en salt dinamico (PBKDF2 / BCrypt provisto por `BCrypt.Net-Next`).

### Exposicion Controlada de APIs (`ApiKeyMiddleware`)
- Las rutas bajo el prefijo `/api/` estan protegidas por defecto contra accesos externos no autorizados (retornando HTTP 403 Forbidden).
- Aquellos endpoints que requieren consumo publico o integracion con terceros se decoran explicitamente con el atributo `[PublicApi]`.
- El middleware valida la clave enviada (en la cabecera `X-Api-Key` o el parametro `?apikey=`) comparando su hash SHA-256 contra la tabla de `ApiKeys` en MySQL.
- Peticiones originadas desde el propio frontend de la aplicacion (mismo origen comprobado por cabeceras `Referer` y `Sec-Fetch-Site`) se permiten de forma transparente sin requerir clave estatica.

### Compatibilidad con Contenedores Linux (`CaseInsensitiveStaticFilesMiddleware`)
Dado que los sistemas operativos basados en Linux diferencian mayusculas de minusculas en nombres de archivo (case-sensitive), el middleware intercepta las peticiones de recursos estaticos en `wwwroot` y resuelve coincidencias independientemente de la combinacion de mayusculas y minusculas utilizada en la URL.

---

## 6. Servicios en Segundo Plano

La recoleccion periodica de precios de referencia se realiza mediante `ScrapingPreciosService`, implementado sobre `BackgroundService` (`IHostedService`).

- Se ejecuta en un hilo asincrono independiente, garantizando que tareas de extraccion de datos externos no degraden el tiempo de respuesta del servidor web Kestrel.
- Incorpora un retardo inicial configurable para permitir que la base de datos complete su ciclo de arranque antes del primer sondeo.
- Cuenta con manejo de excepciones y reintentos: en caso de que el sitio oficial del DANE no se encuentre disponible, registra el evento en auditoria y mantiene las cotizaciones vigentes sin interrumpir la operacion del portal.
