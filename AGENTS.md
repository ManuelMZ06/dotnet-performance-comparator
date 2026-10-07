# AGENTS.md — Reglas operativas

Reglas que todo agente o desarrollador debe consultar antes de cada tarea en **DotNetPerformanceComparator**.

El contexto completo del proyecto (casos de uso, requerimientos, arquitectura, modelo de datos y decisiones) está en [`docs/PROJECT_CONTEXT.md`](docs/PROJECT_CONTEXT.md) y es la **fuente de verdad**. Ante cualquier duda, consultarlo antes de implementar.

## Proyecto

- Proyecto de Graduación: **"Análisis de Rendimiento y Consumo de Recursos Computacionales Entre .NET 8 y .NET 10"**.
- Metodología: **RUP**. Todo cambio debe conservar trazabilidad con requerimientos funcionales, no funcionales, casos de uso, arquitectura, modelo de diseño, modelo de datos y mockups.
- Actor único: **Usuario**. No existen autenticación, roles, permisos, administrador ni gestión de usuarios.

## Alcance permitido

Únicamente los casos de uso oficiales:

| CU | Nombre | RF |
|----|--------|----|
| CU-01 | Incorporar aplicación .NET 8 | RF-01, RF-14 |
| CU-02 | Migrar aplicación experimentalmente a .NET 10 | RF-02, RF-14 |
| CU-03 | Configurar evaluación | RF-03, RF-14 |
| CU-04 | Ejecutar prueba de rendimiento | RF-04, RF-05, RF-06, RF-14 |
| CU-05 | Consultar resultados | RF-07 |
| CU-06 | Filtrar resultados por métrica | RF-08 |
| CU-07 | Comparar resultados entre .NET 8 y .NET 10 | RF-09 |
| CU-08 | Visualizar resultados | RF-10, RF-11 |
| CU-09 | Generar reporte en PDF | RF-12, RF-13 |

**Prohibido** (salvo instrucción futura explícita): autenticación, roles, permisos, administración o gestión de usuarios, pantallas de login, funcionalidades cloud, microservicios, Docker, o cualquier función fuera de estos CU.

## Reglas de desarrollo

1. No agregar funcionalidades que no estén solicitadas.
2. No cambiar el alcance de los CU sin aprobación.
3. No instalar librerías innecesariamente.
4. Implementar un caso de uso por vez.
5. Mantener trazabilidad con CU/RF.
6. Usar inyección de dependencias.
7. Mantener `Domain` independiente de infraestructura.
8. Las páginas Blazor no deben ejecutar directamente procesos del sistema ni consultas complejas de persistencia.
9. Las integraciones externas deben estar detrás de interfaces.
10. No almacenar secretos o rutas personales en el repositorio.
11. Mantener compatibilidad con Windows 11 como entorno principal.
12. Después de cada tarea ejecutar `dotnet restore`, `dotnet build` y `dotnet test`.
13. No avanzar automáticamente hacia el siguiente CU.
14. Si una decisión no está definida en el contexto y puede modificar arquitectura, datos o alcance, **detenerse e informarla antes de implementarla**.
15. No inventar objetivos numéricos de rendimiento que no hayan sido definidos.

## Reglas de arquitectura

```
Domain
   ↑
Application
   ↑
Infrastructure

Web → Application
Web → Infrastructure únicamente como composition root / DI (Program.cs).
```

- `Domain` **nunca** depende de Infrastructure, Web, EF Core, NBomber, QuestPDF ni herramientas externas.
- `Application` no depende directamente de Infrastructure; define interfaces que Infrastructure implementa.
- `Infrastructure` contiene EF Core, SQLite, filesystem, procesos, herramientas .NET, métricas, pruebas de carga y PDF.
- `Web` (Blazor Server) solo presenta y consume servicios de `Application`.
- Registro de servicios: `AddApplication()` y `AddInfrastructure()`, invocados desde `src/DotNetPerformanceComparator.Web/Program.cs`.
- Las pruebas de arquitectura en `tests/DotNetPerformanceComparator.Tests/ArchitectureTests.cs` deben seguir pasando.

## Forma de trabajo por tarea

1. Inspeccionar el código existente.
2. Explicar brevemente qué se modificará (qué archivos) antes de una modificación importante.
3. Realizar únicamente la tarea solicitada.
4. Compilar (`dotnet restore`, `dotnet build`).
5. Ejecutar pruebas (`dotnet test`).
6. Resumir los cambios.
7. Informar decisiones técnicas y problemas encontrados.
8. Detenerse y esperar instrucciones.

## Entorno

- Herramienta: `net8.0` (no modificar los `TargetFramework`).
- SDK fijado en `global.json` a 8.0.425 con `rollForward: latestFeature` (nunca usa el SDK 10).
- Sistema principal: Windows 11.
