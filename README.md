# DotNetPerformanceComparator

Herramienta web del Proyecto de Graduación **"Análisis de Rendimiento y Consumo de Recursos Computacionales Entre .NET 8 y .NET 10"**.

Permitirá incorporar una aplicación web .NET 8, migrarla experimentalmente a .NET 10, ejecutar evaluaciones de rendimiento, recopilar métricas, comparar resultados, visualizarlos y generar reportes PDF.

> Estado actual: estructura base, modelo de dominio y persistencia (EF Core + SQLite). Aún no se implementa ningún caso de uso.

## Stack

- C# / ASP.NET Core — la herramienta se ejecuta sobre **.NET 8** (`net8.0`)
- Blazor Server (componentes Razor con interactividad de servidor)
- Entity Framework Core 8 + SQLite (migraciones en `Infrastructure/Persistence/Migrations`)
- NBomber, dotnet-counters / Microsoft.Diagnostics.NETCore.Client, QuestPDF *(pendiente)*
- Windows 11 como entorno principal

## Estructura

```
DotNetPerformanceComparator.sln
src/
  DotNetPerformanceComparator.Web             Presentación (Blazor Server) y punto de composición (DI)
  DotNetPerformanceComparator.Application     Aplicación / Orquestación de casos de uso
  DotNetPerformanceComparator.Domain          Dominio (entidades y reglas de negocio)
  DotNetPerformanceComparator.Infrastructure  Infraestructura (SQLite, herramientas externas, PDF)
tests/
  DotNetPerformanceComparator.Tests           Pruebas (xUnit)
```

### Dependencias entre capas

```
Web ──────────────► Application ──► Domain
 │                      ▲
 └─(solo DI)──► Infrastructure
```

- **Domain** no depende de ningún otro proyecto.
- **Application** depende únicamente de Domain y define las abstracciones (interfaces) que necesita.
- **Infrastructure** depende de Application (y Domain) e implementa esas abstracciones.
- **Web** depende de Application. Referencia a Infrastructure **solo** para registrar implementaciones en `Program.cs` (punto de composición).
- Los componentes Blazor no ejecutan herramientas externas, no acceden a SQLite ni contienen lógica de negocio: consumen servicios de Application.

### Inyección de dependencias

Cada capa expone un método de extensión para registrar sus servicios:

- `services.AddApplication()` — `src/DotNetPerformanceComparator.Application/DependencyInjection.cs`
- `services.AddInfrastructure(connectionString)` — `src/DotNetPerformanceComparator.Infrastructure/DependencyInjection.cs`

Ambos se invocan desde `src/DotNetPerformanceComparator.Web/Program.cs`.

## Casos de uso

| ID | Caso de uso |
|----|-------------|
| CU-01 | Incorporar aplicación .NET 8 |
| CU-02 | Migrar aplicación experimentalmente a .NET 10 |
| CU-03 | Configurar evaluación |
| CU-04 | Ejecutar prueba de rendimiento |
| CU-05 | Consultar resultados |
| CU-06 | Filtrar resultados por métrica |
| CU-07 | Comparar resultados entre .NET 8 y .NET 10 |
| CU-08 | Visualizar resultados |
| CU-09 | Generar reporte en PDF |

## Requisitos

- **.NET SDK 8** (8.0.425 o una versión 8.0.x posterior). El archivo `global.json` fija el SDK 8 con `rollForward: latestFeature`, por lo que nunca se usa el SDK 10 aunque esté instalado.
- **Runtime ASP.NET Core 8** para ejecutar la herramienta y las pruebas.

## Comandos

```bash
dotnet restore
dotnet build
dotnet test
dotnet run --project src/DotNetPerformanceComparator.Web
```
