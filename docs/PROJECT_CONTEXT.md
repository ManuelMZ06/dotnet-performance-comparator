# Contexto del proyecto — DotNetPerformanceComparator

> Este documento es la **fuente de verdad** funcional y arquitectónica del proyecto. Las reglas operativas resumidas están en [`AGENTS.md`](../AGENTS.md).

## 1. Contexto general

Proyecto de Graduación titulado:

**"Análisis de Rendimiento y Consumo de Recursos Computacionales Entre .NET 8 y .NET 10"**

El producto es una herramienta web orientada a evaluar y comparar el rendimiento y el consumo de recursos de una misma aplicación web ejecutada sobre .NET 8 y una versión experimental migrada a .NET 10.

El proyecto utiliza **RUP**, por lo que el software debe conservar trazabilidad con:

- requerimientos funcionales;
- requerimientos no funcionales;
- casos de uso;
- arquitectura;
- modelo de diseño;
- modelo de datos;
- mockups.

No se deben inventar funcionalidades que no estén contempladas en estos artefactos.

## 2. Usuario del sistema

El sistema tiene un único actor funcional: **Usuario**.

Puede representar estudiantes de Ingeniería en Sistemas, desarrolladores, testers u otros profesionales tecnológicos.

Actualmente **no existen**:

- autenticación;
- roles;
- permisos;
- administrador;
- gestión de usuarios.

No se implementan estas funcionalidades salvo instrucción futura explícita.

## 3. Casos de uso oficiales

### CU-01 — Incorporar aplicación .NET 8
Permitir al Usuario seleccionar e incorporar una aplicación web desarrollada en .NET 8 que será utilizada como aplicación base del proceso de evaluación.
El sistema debe validar que la información/proyecto seleccionado sea adecuado antes de registrarlo.

### CU-02 — Migrar aplicación experimentalmente a .NET 10
Permitir realizar una migración experimental de la aplicación incorporada desde .NET 8 hacia .NET 10.
La aplicación original en .NET 8 debe conservarse para permitir la comparación posterior.
El sistema debe informar si la migración finaliza correctamente o falla.

### CU-03 — Configurar evaluación
Permitir configurar una evaluación indicando:

- pruebas a ejecutar;
- métricas a recopilar;
- parámetros;
- número de ejecuciones.

La configuración debe validarse antes de ser utilizada.

### CU-04 — Ejecutar prueba de rendimiento
Permitir ejecutar una evaluación previamente configurada.
Durante las ejecuciones deben recopilarse las métricas configuradas y asociarse correctamente con cada ejecución y versión de .NET.
Una ejecución incompleta o fallida no debe registrarse como resultado válido.

### CU-05 — Consultar resultados
Permitir consultar evaluaciones previamente realizadas y visualizar sus resultados.

### CU-06 — Filtrar resultados por métrica
Permitir seleccionar una o más métricas y limitar la información mostrada de acuerdo con esos filtros.
Debe ser posible retirar los filtros y recuperar la vista completa.

### CU-07 — Comparar resultados entre .NET 8 y .NET 10
Permitir seleccionar resultados compatibles correspondientes a la misma aplicación evaluada en .NET 8 y .NET 10.
El sistema debe mostrar las métricas de manera comparativa.
Antes de comparar debe comprobarse que los resultados seleccionados sean compatibles.

### CU-08 — Visualizar resultados
Permitir visualizar resultados mediante:

- tablas;
- gráficos cuando la naturaleza de la métrica lo permita.

La presentación debe conservar nombres, unidades y estructura consistente.

### CU-09 — Generar reporte en PDF
Permitir generar un reporte PDF correspondiente a una evaluación o comparación seleccionada y permitir su descarga.

## 4. Requerimientos funcionales

| ID | Nombre | Descripción |
|----|--------|-------------|
| RF-01 | Incorporación de aplicación .NET 8 | El sistema deberá permitir al usuario incorporar una aplicación desarrollada en .NET 8 mediante la selección de los archivos o proyecto necesarios para realizar posteriormente la evaluación. |
| RF-02 | Migración experimental a .NET 10 | El sistema deberá permitir realizar una migración experimental desde .NET 8 hacia .NET 10, conservando la aplicación original. |
| RF-03 | Configuración de evaluación | El sistema deberá permitir indicar pruebas, métricas, parámetros y número de ejecuciones. |
| RF-04 | Ejecución de pruebas | El sistema deberá permitir iniciar una evaluación previamente configurada. |
| RF-05 | Recolección de métricas | El sistema deberá recopilar durante cada ejecución las métricas seleccionadas. |
| RF-06 | Registro de resultados | El sistema deberá registrar los resultados asociados con aplicación, versión .NET, métricas y condiciones de evaluación. |
| RF-07 | Consulta de resultados | El sistema deberá permitir consultar evaluaciones previamente ejecutadas. |
| RF-08 | Filtrado de resultados | El sistema deberá permitir filtrar resultados mediante una o más métricas. |
| RF-09 | Comparación de resultados | El sistema deberá comparar resultados de una misma aplicación correspondientes a .NET 8 y .NET 10. |
| RF-10 | Visualización tabular | El sistema deberá representar los resultados mediante tablas. |
| RF-11 | Visualización gráfica | El sistema deberá representar mediante gráficos aquellas métricas para las que resulte aplicable. |
| RF-12 | Generación de reporte | El sistema deberá generar un reporte PDF de una evaluación o comparación. |
| RF-13 | Descarga de reporte | El sistema deberá permitir descargar el PDF generado. |
| RF-14 | Gestión de errores | El sistema deberá informar al usuario cuando una operación de incorporación, migración, configuración, ejecución o generación no pueda completarse, indicando el estado y la información disponible sobre la causa. |

### Trazabilidad CU → RF

```
CU-01 → RF-01, RF-14
CU-02 → RF-02, RF-14
CU-03 → RF-03, RF-14
CU-04 → RF-04, RF-05, RF-06, RF-14
CU-05 → RF-07
CU-06 → RF-08
CU-07 → RF-09
CU-08 → RF-10, RF-11
CU-09 → RF-12, RF-13
```

## 5. Requerimientos no funcionales

1. **Usabilidad:** las funciones principales deben presentarse de manera clara y consistente.
2. **Navegación intuitiva:** el Usuario debe identificar fácilmente las funciones principales.
3. **Claridad de resultados:** las métricas deben mostrar nombre, valor y unidad cuando corresponda.
4. **Rendimiento:** la propia herramienta debe evitar interferir innecesariamente en las evaluaciones.
5. **Fiabilidad:** resultados válidos previamente almacenados no deben perderse ante fallos posteriores.
6. **Compatibilidad:** el sistema se desarrolla para el alcance definido de .NET 8 y .NET 10.
7. **Mantenibilidad:** utilizar estructura modular y bajo acoplamiento.
8. **Consistencia:** conservar nomenclatura, unidades y presentación coherentes.
9. **Seguridad operacional:** evitar modificaciones no intencionadas y validar operaciones sobre archivos/procesos.
10. **Trazabilidad:** una evaluación debe poder relacionarse con aplicación, versión, configuración, ejecuciones, condiciones y resultados.

No se deben inventar objetivos numéricos de rendimiento que no hayan sido definidos.

## 6. Stack tecnológico

```
Lenguaje:
C#

Aplicación:
ASP.NET Core / .NET 8

Presentación:
Blazor con interactividad Server

Persistencia:
Entity Framework Core + SQLite

Pruebas de rendimiento:
NBomber

Métricas:
dotnet-counters o Microsoft.Diagnostics.NETCore.Client

Migración:
herramientas del ecosistema .NET para migración experimental .NET 8 → .NET 10

Reportes:
QuestPDF

Sistema objetivo principal:
Windows 11
```

Docker solamente se considera opcional y no debe introducirse salvo decisión explícita posterior.

## 7. Arquitectura

Separación por responsabilidades:

```
DotNetPerformanceComparator.Web
    Presentación Blazor

DotNetPerformanceComparator.Application
    Casos de uso
    Orquestación
    Interfaces

DotNetPerformanceComparator.Domain
    Entidades
    Reglas del dominio

DotNetPerformanceComparator.Infrastructure
    EF Core
    SQLite
    filesystem
    procesos
    herramientas .NET
    métricas
    pruebas
    PDF

DotNetPerformanceComparator.Tests
    pruebas automatizadas
```

### Dependencias

```
Domain
   ↑
Application
   ↑
Infrastructure

Web → Application
Web → Infrastructure únicamente como composition root / DI.
```

`Domain` nunca debe depender de:

- Infrastructure;
- Web;
- EF Core;
- NBomber;
- QuestPDF;
- herramientas externas.

`Application` tampoco debe depender directamente de Infrastructure.

## 8. Modelo de datos

### Entidades persistentes previstas

```
ProyectoDotNet
Migracion
TipoPrueba
ConfiguracionEvaluacion
ConfiguracionPrueba
ParametroEvaluacion
Metrica
ConfiguracionMetrica
Ejecucion
CondicionEjecucion
ResultadoMetrica
```

### Relaciones principales

```
ProyectoDotNet 1 → N Migracion

ProyectoDotNet 1 → N ConfiguracionEvaluacion

ConfiguracionEvaluacion N ↔ N TipoPrueba
mediante ConfiguracionPrueba

ConfiguracionEvaluacion N ↔ N Metrica
mediante ConfiguracionMetrica

ConfiguracionEvaluacion 1 → N ParametroEvaluacion

ConfiguracionEvaluacion 1 → N Ejecucion

Ejecucion 1 → N CondicionEjecucion

Ejecucion 1 → N ResultadoMetrica

Metrica 1 → N ResultadoMetrica
```

`Comparacion` y `Reporte` se consideran inicialmente objetos transitorios/generados bajo demanda y **no** entidades persistentes, salvo que una decisión posterior cambie explícitamente esto.

## 9. Métricas previstas

```
CPU
Memoria RAM
Tiempo de respuesta
Latencia
Throughput
Tamaño de la aplicación
```

No se asumen todavía detalles técnicos adicionales sobre cómo se obtendrá cada una hasta implementar CU-04.

## 10. Tipos de prueba previstos

```
Carga
Estrés
Concurrencia
```

## 11. Flujo funcional general

```
Incorporar aplicación .NET 8
            ↓
Migrar experimentalmente a .NET 10
            ↓
Configurar evaluación
            ↓
Ejecutar pruebas
            ↓
Recolectar métricas
            ↓
Registrar resultados
            ↓
Consultar / filtrar
            ↓
Comparar .NET 8 vs .NET 10
            ↓
Visualizar tablas y gráficos
            ↓
Generar PDF
```

## 12. Interfaz (mockups)

```
M-01 Inicio / Panel principal
M-02 Incorporación de aplicación .NET 8
M-03 Migración experimental a .NET 10
M-04 Configuración de evaluación
M-05 Ejecución de evaluación
M-06 Consulta de resultados
M-07 Comparación y visualización
M-08 Generación de reporte PDF
```

La interfaz tendrá navegación consistente y estará orientada a un único Usuario.
No se diseñan pantallas de login, administración o gestión de usuarios.

## 13. Reglas importantes para el desarrollo

1. No agregar funcionalidades que no estén solicitadas.
2. No cambiar el alcance de los CU sin aprobación.
3. No instalar librerías innecesariamente.
4. Implementar un caso de uso por vez.
5. Mantener trazabilidad con CU/RF.
6. Usar inyección de dependencias.
7. Mantener Domain independiente de infraestructura.
8. Las páginas Blazor no deben ejecutar directamente procesos del sistema ni consultas complejas de persistencia.
9. Las integraciones externas deben estar detrás de interfaces.
10. No almacenar secretos o rutas personales en el repositorio.
11. Mantener compatibilidad con Windows 11 como entorno principal.
12. Después de cada tarea ejecutar `dotnet restore`, `dotnet build` y `dotnet test`.
13. No avanzar automáticamente hacia el siguiente CU.
14. Si una decisión no está definida en este contexto y puede modificar arquitectura, datos o alcance, detenerse e informarla antes de implementarla.

## 14. Decisiones técnicas registradas

Decisiones tomadas durante el desarrollo hasta la fecha:

| # | Decisión | Motivo |
|---|----------|--------|
| D-01 | Solución en formato `.sln` (`DotNetPerformanceComparator.sln`) con carpetas `src/` y `tests/`. | Compatibilidad amplia con Visual Studio y el SDK 8. |
| D-02 | Todos los proyectos de la herramienta usan `TargetFramework` `net8.0`. | La herramienta se desarrolla sobre .NET 8 según el stack definido. |
| D-03 | `global.json` fija el SDK 8.0.425 con `rollForward: latestFeature` y `allowPrerelease: false`. | Usar siempre un SDK 8.0.x aunque el SDK 10 esté instalado. No afecta a las aplicaciones evaluadas en CU-02. |
| D-04 | La plantilla Blazor (generada por el SDK 10) se adaptó a APIs de .NET 8: `UseStaticFiles` en lugar de `MapStaticAssets`, rutas directas en lugar de `@Assets[...]`, bloque `<NotFound>` en el `Router`, sin `ReconnectModal`. | Las plantillas del SDK 10 no ofrecen `net8.0`. |
| D-05 | Inyección de dependencias por capa mediante `AddApplication()` y `AddInfrastructure()`, invocados en `Web/Program.cs`. | Bajo acoplamiento; Web es el único punto de composición. |
| D-06 | Application e Infrastructure referencian solo `Microsoft.Extensions.DependencyInjection.Abstractions` 8.0.2. | Exponer el registro de servicios sin depender de ASP.NET Core. |
| D-07 | Pruebas con xUnit, incluidas pruebas de arquitectura que validan las dependencias entre capas. | Proteger la regla de dependencias de forma automatizada. |
| D-08 | EF Core en la línea **8.0.31**: `Microsoft.EntityFrameworkCore.Sqlite` en Infrastructure; `Microsoft.EntityFrameworkCore.Design` en Web con `PrivateAssets=all`. | Las herramientas EF exigen que el *startup project* (Web) referencie `Design`; con `PrivateAssets=all` no se propaga como dependencia transitiva ni llega a Infrastructure/Application/Domain. Las migraciones se generan con EF Core 8.0.31 (`ProductVersion` 8.0.31) aunque la herramienta global `dotnet-ef` instalada sea 10.0.10. |
| D-09 | Esquema gestionado exclusivamente con **migraciones EF Core** aplicadas de forma explícita (`dotnet ef database update`). Web no usa `EnsureCreated()` ni aplica migraciones al iniciar. `EnsureCreated()` solo se usa en pruebas con SQLite `:memory:`. | Esquema reproducible y versionado; la creación/actualización de la base es una acción explícita. |
| D-10 | Todas las claves foráneas usan `DeleteBehavior.Restrict` (sin cascadas). | Opción más conservadora: ningún borrado de proyecto, configuración, ejecución o catálogo (`TipoPrueba`, `Metrica`) puede eliminar en cascada resultados históricos (RNF Fiabilidad y Trazabilidad). Cualquier eliminación futura deberá ser explícita. |
| D-11 | `ResultadoMetrica.Valor` se mantiene como `decimal` en el dominio; el proveedor SQLite lo almacena como `TEXT`. | Conserva la precisión exacta sin alterar el modelo. Limitación conocida: SQLite no tiene tipo decimal nativo, por lo que ordenamientos/agregaciones sobre `Valor` en SQL pueden no traducirse; si CU-05/CU-07 los necesitan, se evaluará entonces (p. ej. calcular en memoria) sin cambiar el tipo de dominio. |
| D-12 | Cadena de conexión `DefaultConnection` = `Data Source=dotnet-performance.db` (ruta relativa) en `appsettings.json` de Web, pasada a `AddInfrastructure(string? connectionString)`. Sin longitudes máximas en columnas de texto. Archivos `*.db`, `*.db-shm`, `*.db-wal` excluidos en `.gitignore`. | Sin rutas personales en el repositorio y sin dependencia adicional de abstracciones de configuración en Infrastructure. La ruta relativa se resuelve contra el directorio de trabajo: con `dotnet ef` y `dotnet run --project` es `src/DotNetPerformanceComparator.Web/`. SQLite no aplica longitudes en `TEXT`, por lo que no se definen límites que pudieran truncar rutas de Windows. |
| D-13 | `dotnet-ef` **8.0.31** fijado como herramienta local en `.config/dotnet-tools.json` (`dotnet tool restore`). Sustituye el uso del `dotnet-ef` global mencionado en D-08. | Tooling reproducible y alineado con los paquetes EF Core 8.0.31; dentro del repositorio `dotnet ef` resuelve la herramienta local con independencia de las herramientas globales instaladas. |
| D-14 | El proyecto de pruebas referencia a `Web` para probar lógica de presentación en C# puro (p. ej. `BarChartLayout`) y verificar que ningún componente Blazor inyecte tipos de `Infrastructure`. No se usan frameworks de prueba de UI (bUnit, Playwright, Selenium). | Cubrir la lógica de interfaz y la regla "las páginas no acceden a infraestructura" sin dependencias adicionales. |
| D-15 | `AppDbContext` se registra con **`AddDbContextFactory`** (sustituye a `AddDbContext` de D-12). Los repositorios de Infrastructure crean un contexto de vida corta por operación (`CreateDbContextAsync` + `await using`). | En Blazor Server un servicio *scoped* vive todo el circuito; un `DbContext` compartido durante el circuito no es seguro ni adecuado. `dotnet ef` y las migraciones siguen funcionando sin cambios. |
| D-16 | Las fechas generadas por la aplicación se guardan en **UTC**, obtenidas de `TimeProvider` (BCL de .NET 8, registrado como `TimeProvider.System` en `AddApplication`). | Fechas consistentes e independientes de la zona horaria; testeable sin dependencias externas. |
| D-17 | Valores persistidos como texto definidos como constantes de Domain: `VersionesDotNet.Net8 = ".NET 8"` (`ProyectoDotNet.VersionBase`) y `EstadosValidacionProyecto.Valido = "Valido"` (`EstadoValidacion`). Sin tabla de estados. | Representación única y consistente sin cambiar el modelo de datos. El TFM (`net8.0`) es información transitoria de la inspección. |
| D-18 | **Criterios de inspección de CU-01:** ruta absoluta a un archivo `.csproj` existente (se rechazan enlaces simbólicos); lectura del XML en solo lectura con DTD prohibido y sin resolver recursos externos; **no se evalúa MSBuild** (no se consideran `Directory.Build.props`, importaciones ni condiciones); proyecto web = SDK `Microsoft.NET.Sdk.Web` (no se infiere por `FrameworkReference`); compatible = `net8.0` presente en `TargetFramework`/`TargetFrameworks`. Al registrar, Application vuelve a inspeccionar la ruta y solo persiste si sigue siendo válida. No se ejecuta restore/build/run del proyecto objetivo. | Inspección segura y predecible, sin ejecutar lógica del proyecto objetivo (RNF Seguridad operacional). Una inspección válida solo indica cumplimiento estructural. |
| D-19 | Application referencia `Microsoft.Extensions.Logging.Abstractions` 8.0.3 para registrar errores técnicos (p. ej. fallos de persistencia) mientras el Usuario recibe un mensaje comprensible. | RF-14: informar sin exponer trazas, conservando el diagnóstico en el log. |
| D-20 | **Selector nativo de `.csproj` específico de Windows (M-02):** abstracción `IProjectFilePicker` en Application; implementación `WindowsProjectFilePicker` aislada en `Infrastructure/Platform/Windows/`, usando el diálogo nativo Windows Shell `IFileOpenDialog` por interoperabilidad COM/P/Invoke de .NET (sin WinForms/WPF, sin `UseWindowsForms`/`UseWPF`, sin paquetes externos ni procesos externos). El diálogo se abre en un hilo STA dedicado que termina al cerrarse, con un único diálogo abierto a la vez y una ventana propietaria oculta *topmost* para que aparezca al frente. Fuera de Windows devuelve `Unsupported`. El ingreso manual de la ruta permanece disponible y todos los proyectos siguen en `net8.0`. El selector solo obtiene la ruta; la validación sigue siendo CU-01. | El navegador no expone rutas absolutas locales; la herramienta se ejecuta localmente en Windows 11 (proceso servidor en el mismo equipo que el Usuario). |
