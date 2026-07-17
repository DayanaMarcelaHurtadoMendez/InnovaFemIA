# Arquitectura InnovaFemIA - Proyecto Orquídeas

Este documento describe la arquitectura técnica de la plataforma web InnovaFemIA, construida para la asociación ASCAMPESINA. El diseño estructural prioriza la modularidad, la mantenibilidad a largo plazo y el rendimiento, implementando el patrón de Arquitectura por Tramos Verticales (Vertical Slice Architecture) apoyado en principios de Arquitectura Limpia.

## Estructura del Proyecto

A continuación, se presenta la representación topológica de la solución, ilustrando la separación de responsabilidades y la segmentación por dominios:

```text
InnovaFemIA
|-- src
|   |-- InnovaFemIA.Web
|       |
|       |-- BackgroundServices
|       |   |-- ScrapingPreciosService.cs
|       |
|       |-- Features
|       |   |-- Biocomercio         (Módulo 3: Comercialización)
|       |   |-- Chatbot             (Módulo 2: Asistente Educativo)
|       |   |-- Mapa                (Módulo 1: Mapa Interactivo)
|       |   |   |-- Controllers
|       |   |   |   |-- MapaApiController.cs
|       |   |   |   |-- MapaController.cs
|       |   |   |
|       |   |   |-- ObtenerFamilias
|       |   |       |-- FamiliaDto.cs
|       |   |       |-- ObtenerFamiliasHandler.cs
|       |   |       |-- ObtenerFamiliasQuery.cs
|       |   |
|       |   |-- Shared              (Dominio y Lógica Compartida)
|       |       |-- Attributes
|       |       |   |-- PublicApiAttribute.cs
|       |       |-- Common
|       |       |   |-- PagedResult.cs
|       |       |   |-- Result.cs
|       |       |-- Entities
|       |       |   |-- Cultivo.cs
|       |       |   |-- Familia.cs
|       |       |   |-- PrecioMercado.cs
|       |       |   |-- (y demás entidades del modelo)
|       |       |-- Middleware
|       |       |   |-- ApiKeyMiddleware.cs
|       |       |-- Persistence
|       |           |-- AppDbContext.cs
|       |
|       |-- Migrations              (Control de Versiones BD)
|       |-- Views                   (Interfaces Gráficas - Razor)
|       |-- wwwroot                 (Recursos Estáticos CSS/JS)
|       |
|       |-- appsettings.json        (Configuraciones y Cadenas de Conexión)
|       |-- package.json            (Dependencias Node para Compilación)
|       |-- Program.cs              (Punto de Entrada e Inyección de Dependencias)
|       |-- tailwind.config.js      (Sistema de Diseño Gráfico)
```

## Fundamentos Arquitectónicos

### Vertical Slice Architecture (Tramos Verticales)

El proyecto abandona la separación horizontal tradicional en N-capas (donde se agrupaban todos los controladores por un lado y los servicios por otro) en favor de una organización basada estrictamente en funcionalidades de negocio (Features). Mediante el uso del patrón CQRS (Command Query Responsibility Segregation) orquestado con MediatR, cada caso de uso encapsula todos los artefactos necesarios para su operación.

Beneficios estructurales:
- **Alta Cohesión:** La lógica de ejecución, los modelos de transferencia de datos (DTO) y las consultas operan juntos en una misma carpeta. Modificar un requerimiento solo afecta dicho directorio.
- **Bajo Acoplamiento:** Los módulos (Mapa, Chatbot, Biocomercio) operan de forma independiente. Un fallo o un refactor en un módulo no corrompe la funcionalidad de los demás.
- **Escalabilidad Sostenible:** Previene el antipatrón de "God Services" (servicios monolíticos sobrecargados), permitiendo que el proyecto escale orgánicamente a medida que ASCAMPESINA crezca.

### Persistencia de Datos (Entity Framework Core)

El modelo de acceso a datos se ha implementado mediante un enfoque Code-First sobre el motor MySQL. La infraestructura de base de datos se centraliza en el `AppDbContext`.

Beneficios estructurales:
- **Control de Esquema (Migrations):** Permite iterar el modelo de la base de datos de manera controlada y persistente a través del código fuente.
- **Integridad del Dominio:** La implementación de reglas de relación, índices y restricciones de base de datos se maneja a través de Fluent API en el `OnModelCreating`. Esto asegura que las clases del modelo (Entidades) permanezcan limpias, puras y agnósticas a las dependencias de persistencia, cumpliendo con la regla fundamental de la Arquitectura Limpia.

### Seguridad y Exposición Híbrida de Endpoints

El software tiene el doble requerimiento de servir vistas web (Razor) y proveer interfaces de programación (API) para integraciones. Para manejar este modelo híbrido, se estableció un control de seguridad a nivel de Middleware:

- **Endpoints Internos:** Las rutas destinadas para ser consumidas exclusivamente por el frontend no llevan decoradores especiales. El middleware restringe el acceso externo retornando una respuesta HTTP 403 (Forbidden), garantizando que procesos externos no puedan interactuar con funciones no autorizadas.
- **Endpoints Públicos:** Los recursos que deben ser expuestos son decorados explícitamente con el atributo `[PublicApi]`. El middleware requiere validación criptográfica comparando el token enviado por el cliente (X-Api-Key) contra hashes SHA-256 almacenados en la base de datos.

### Tareas en Segundo Plano (Background Services)

La recolección de información asíncrona externa (específicamente la automatización de web scraping para el monitoreo de precios agrícolas del DANE) se procesa a través de la interfaz `IHostedService`.

Beneficios estructurales:
- **Concurrencia y Estabilidad:** Permite ejecutar lógicas pesadas y de alta latencia en hilos independientes sin penalizar el rendimiento del servidor web (Kestrel).
- **Resiliencia:** El servicio está diseñado con mecanismos para atrapar fallos originados por indisponibilidad del servicio externo. En caso de error, emite un registro centralizado y marca la información como "desactualizada", manteniendo la continuidad operativa de la plataforma sin disrupción para las usuarias.
