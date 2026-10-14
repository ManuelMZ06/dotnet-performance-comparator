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

> Estado vigente tras C-06.2. La entidad `ConfiguracionPrueba` del diseño original se eliminó (ver D-34): una `ConfiguracionEvaluacion` corresponde a un único `TipoPrueba`.

### Entidades persistentes

```
ProyectoDotNet
Migracion
TipoPrueba
ConfiguracionEvaluacion
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

Migracion 1 → N ConfiguracionEvaluacion            (una migración .NET 10 exitosa concreta; ver D-27)

TipoPrueba 1 → N ConfiguracionEvaluacion            (un único tipo por configuración; ver D-34)

ConfiguracionEvaluacion N ↔ N Metrica
mediante ConfiguracionMetrica

ConfiguracionEvaluacion 1 → N ParametroEvaluacion   (parámetros del tipo configurado; ver D-31)

ConfiguracionEvaluacion 1 → N Ejecucion

Ejecucion 1 → N CondicionEjecucion

Ejecucion 1 → N ResultadoMetrica

Metrica 1 → N ResultadoMetrica
```

Campos añadidos durante Construcción a `ConfiguracionEvaluacion`: `Nombre`, `MigracionId` (D-27), `TipoPruebaId` (D-34), `RutaObjetivo` y `Observaciones` (D-33, D-31), con `NumeroEjecuciones` restringido a 1..3 (D-30).

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
| D-21 | **Estrategia de migración experimental (CU-02) controlada por la herramienta**, sin Upgrade Assistant, Copilot, Visual Studio, shells ni herramientas de terceros: (1) revalidar el proyecto base con la inspección de CU-01, (2) copiarlo a un directorio nuevo o vacío, (3) cambiar `net8.0` por `net10.0` **solo en el `.csproj` de la copia** (preservando el resto del XML, otros TFMs, paquetes y propiedades), (4) `dotnet restore` y (5) `dotnet build --no-restore --framework net10.0` con el SDK .NET 10. No se corrige código ni se actualizan paquetes automáticamente: si restore o build fallan, la migración queda **Fallida** y la copia se conserva para diagnóstico. El proyecto original es de solo lectura. `Migracion` se registra (EnProceso, `EstadosMigracion` en Domain) solo cuando todas las precondiciones se cumplen; termina en Exitosa o Fallida con `FechaFin` UTC, `RutaMigrada` (desde que la copia existe) y un `Detalle` comprensible (incluye el SDK; sin trazas). La bitácora completa es transitoria. | Comparación justa: la copia .NET 10 difiere del original solo en el framework objetivo; los fallos reales de compatibilidad se informan en lugar de ocultarse (RNF Seguridad operacional, Trazabilidad). |
| D-22 | **Selección del SDK .NET 10:** se usa la mayor versión **estable** 10.x informada por `dotnet --list-sdks` (las versiones preview/rc se ignoran; si solo hay preview o ninguna, la operación se detiene antes de copiar). La herramienta no descarga ni instala SDKs. | Reproducibilidad sin depender de una versión exacta. |
| D-23 | **Ejecución aislada de `dotnet`:** `IDotNetCommandRunner` (Application) / `DotNetCommandRunner` (Infrastructure) usa `Process` con `UseShellExecute=false`, sin ventana, argumentos por `ArgumentList` (nunca concatenados ni vía shell) y captura de stdout/stderr/código de salida. Cada comando se ejecuta en un directorio temporal con un `global.json` temporal que fija el SDK 10 seleccionado (`rollForward: disable`); antes de migrar se comprueba con `dotnet --version` que el host resuelve exactamente esa versión. Se eliminan del entorno heredado las variables `MSBuild*` y `DOTNET_HOST_PATH`, y se usa `--disable-build-servers`. El directorio temporal se elimina al terminar; la cancelación termina el árbol de procesos. El `global.json` (.NET 8) de la herramienta no se modifica ni afecta a la migración, y no se escribe ningún `global.json` en el proyecto original ni en la copia. | El SDK de la herramienta (.NET 8) y el de la aplicación migrada (.NET 10) quedan aislados de forma verificable. |
| D-24 | **Política de copia:** el destino debe ser absoluto, nuevo (con carpeta contenedora existente) o existente vacío; no puede ser el origen, estar dentro de él ni contenerlo (comparación de rutas normalizadas sin distinguir mayúsculas). Se excluyen `bin`, `obj`, `.vs` y `.git` en cualquier nivel; no se siguen enlaces simbólicos/uniones (se omiten y se informan); no se sobrescriben archivos. Si la copia falla, se elimina solo lo creado por la operación (nunca un directorio preexistente). | Evitar sobrescrituras, copias recursivas y pérdida de datos. |
| D-25 | **`ProjectReference` externos:** si el `.csproj` referencia proyectos fuera de su directorio (o rutas con propiedades MSBuild que no pueden resolverse sin evaluar MSBuild), la migración se detiene antes de copiar. Limitación conocida (coherente con D-18): archivos heredados de directorios superiores (`Directory.Build.props`, importaciones) no se copian. | No copiar ni resolver silenciosamente archivos arbitrarios fuera del proyecto incorporado. |
| D-26 | Los selectores nativos (archivo `.csproj` en M-02 y carpeta en M-03, `IDirectoryPicker`/`WindowsDirectoryPicker`) comparten `WindowsShellDialog` (hilo STA, ventana propietaria *topmost*, un único diálogo abierto a la vez en toda la herramienta). Amplía D-20. | Evitar duplicar la interoperabilidad COM y diálogos simultáneos. |
| D-27 | **`ConfiguracionEvaluacion` evoluciona (CU-03):** se agregan de forma permanente `Nombre` (requerido, sin unicidad —dos evaluaciones pueden llamarse igual— ni longitud máxima) y `MigracionId` (FK **requerida** a `Migracion`, con `DeleteBehavior.Restrict`). Cada configuración queda asociada explícitamente a un `ProyectoDotNet` **y** a una `Migracion` concreta **Exitosa** que le pertenece; no se usa "la última migración" (una aplicación puede tener varias migraciones y la configuración debe ser reproducible). Application valida la pertenencia proyecto↔migración y el estado Exitosa, sin confiar en los Ids enviados por Web. Migración EF `AddEvaluationNameAndMigrationReference` (2.ª migración, aditiva; la tabla estaba vacía, así que no requirió backfill). Estado inicial `EstadosConfiguracionEvaluacion.Configurada`; los estados de ejecución se definirán en CU-04. | Trazabilidad experimental exacta entre la aplicación .NET 8, su migración .NET 10 y la evaluación. |
| D-28 | **Parámetros específicos (CU-03):** Warm-up, Usuarios virtuales, Duración, Ramp-up, Intervalo y Observaciones se persisten como filas `ParametroEvaluacion` (clave/valor), no como columnas nuevas. Las claves son constantes internas estables en Domain (`ClavesParametroEvaluacion`), distintas del texto de UI (sin espacios ni acentos). Solo se crea fila para los parámetros con valor; los numéricos se validan en Application (Warm-up/Ramp-up ≥ 0; Usuarios/Duración/Intervalo > 0; enteros) y Observaciones es texto libre opcional (se recorta el espacio exterior). `NumeroEjecuciones > 0` se valida en Application (regla aplazada desde el modelo de datos). No se definen valores máximos ni por defecto. | Esquema estable y flexible; Application es la autoridad de validación. La forma en que cada parámetro se usará es parte de CU-04. |
| D-29 | **Guardado como agregado (CU-03):** `IConfiguracionEvaluacionRepository.AddAsync` guarda `ConfiguracionEvaluacion` con sus `ConfiguracionPrueba`, `ConfiguracionMetrica` y `ParametroEvaluacion` en un único `SaveChanges` (contexto de vida corta vía `IDbContextFactory`). Una configuración incompleta no queda parcialmente persistida. El catálogo (proyectos, migraciones exitosas, tipos y métricas) se consulta con `IEvaluationCatalogRepository` (solo lectura, `AsNoTracking`, ordenado por Id para conservar el orden del catálogo). | Integridad del agregado; Web nunca accede a `AppDbContext`. |
| D-30 | **Número de ejecuciones 1..3 (C-06.1; significado actualizado en C-06.2):** `NumeroEjecuciones` solo admite 1, 2 o 3 (derivado del alcance de la investigación), validado en Application y reforzado con un `CHECK` en la base (`CK_ConfiguracionEvaluacion_NumeroEjecuciones`). **Significado:** número de repeticiones del **único** tipo de prueba configurado para **cada** versión evaluada (.NET 8 y .NET 10). Ej.: Carga con 3 ejecuciones → CU-04 ejecutará Carga ×3 en .NET 8 y ×3 en .NET 10. | Alcance experimental acotado y reproducible; fuente de verdad para CU-04. |
| D-31 | **Parámetros tipados por tipo de prueba (C-06.1; relación simplificada en C-06.2):** los tres tipos usan modelos de entrada **tipados** en Application (`ParametrosCargaInput`/`EstresInput`/`ConcurrenciaInput`), no listas clave/valor arbitrarias; Web no puede inventar claves. Se valida y persiste **solo** el bloque del tipo configurado, con todos sus parámetros; incluir bloques de otros tipos se rechaza por inconsistencia. `ParametroEvaluacion` pertenece a `ConfiguracionEvaluacion` por `ConfiguracionId` (sin `TipoPruebaId`); el tipo se deduce del padre (ver D-34, que sustituyó la FK compuesta a `ConfiguracionPrueba` de C-06.1). `Observaciones` es `ConfiguracionEvaluacion.Observaciones` (nullable); no es un parámetro. Claves en `ClavesParametroCarga`/`Estres`/`Concurrencia`. | Entrada tipada y segura; el tipo de cada parámetro queda determinado por la configuración. |
| D-32 | **Semántica de los tipos de prueba (C-06.1; fuente de verdad para CU-04; aún no ejecutada).** **Carga** — carga sostenida: warm-up → ramp-up progresivo hasta `UsuariosVirtuales`, mantenido durante `DuracionSegundos`; parámetros `UsuariosVirtuales`(>0), `DuracionSegundos`(>0), `WarmUpSegundos`(≥0), `RampUpSegundos`(≥0), `IntervaloSolicitudesMs`(≥0, pausa entre solicitudes de un usuario virtual). **Estrés** — presión creciente: desde `UsuariosIniciales` sumando `IncrementoUsuarios` por nivel (cada nivel dura `DuracionNivelSegundos`) hasta `UsuariosMaximos`; parámetros `UsuariosIniciales`(>0), `IncrementoUsuarios`(>0), `UsuariosMaximos`(>Iniciales), `DuracionNivelSegundos`(>0), `WarmUpSegundos`(≥0), `IntervaloSolicitudesMs`(≥0). La diferencia máximo−inicial no tiene que ser divisible por el incremento; CU-04 podrá limitar el último nivel al máximo. **Concurrencia** — ráfagas simultáneas (distinta de carga sostenida): `UsuariosConcurrentes` solicitudes a la vez por ronda, `Rondas` rondas separadas por `IntervaloRondasMs`; parámetros `UsuariosConcurrentes`(>0), `Rondas`(>0), `WarmUpSegundos`(≥0), `IntervaloRondasMs`(≥0); **sin Ramp-up**. | CU-04 dependerá de estas definiciones; quedan fijadas antes de ejecutar. |
| D-33 | **Objetivo HTTP (C-06.1):** cada evaluación define `ConfiguracionEvaluacion.RutaObjetivo`, una ruta **relativa** (requerida, comienza con `/`, no es URL absoluta ni contiene esquema `http(s)://`). CU-04 construirá `http://127.0.0.1:<puerto-asignado>` + `RutaObjetivo` para cada versión, de modo que .NET 8 y .NET 10 reciban la misma solicitud lógica. **Primera versión: únicamente método `GET`** (sin POST/PUT/PATCH/DELETE, cuerpos, autenticación, headers, cookies ni escenarios multi-request), por lo que no se agrega columna `MetodoHttp`; M-04 muestra "Método HTTP: GET" como dato de solo lectura. La comprobación de que el endpoint responde corresponde a CU-04. | Comparación idéntica entre versiones y alcance HTTP acotado para la primera versión. |
| D-35 | **Protocolo de ejecución (CU-04, C-07.1):** una `ConfiguracionEvaluacion` se ejecuta **una sola vez** (solo si `Estado = Configurada` y no tiene `Ejecucion`; para repetir, se crea otra configuración — sin clonación automática). **Solo una evaluación activa** a la vez en la herramienta (`IEvaluationExecutionGate`, semáforo singleton; una segunda se rechaza, no se encola). **.NET 8 y .NET 10 se ejecutan secuencialmente, nunca en paralelo**, en **orden alternado** por repetición (impar .NET 8→.NET 10; par .NET 10→.NET 8) para que ninguna versión tenga sistemáticamente la ventaja de ir primero. **Cada `Ejecucion` usa un proceso objetivo nuevo** (iniciar → readiness → fases → detener), reduciendo contaminación entre repeticiones. Todo el motor acepta `CancellationToken` (mata el árbol de procesos, libera gate/workspace). La ejecución **pertenece al motor de Application y no depende del navegador**: M-05 **no** ata un `CancellationToken` al ciclo de vida del circuito Blazor, por lo que una desconexión/reconexión del circuito no cancela ni reinicia la evaluación en curso. La app objetivo se lanza con `DOTNET_ENVIRONMENT`/`ASPNETCORE_ENVIRONMENT` = **Production** forzados (no hereda el entorno del evaluador), y ese entorno queda registrado como `CondicionEjecucion` (`Environment`). | Validez experimental reproducible; fuente de verdad para C-07.2+. |
| D-36 | **Preparación y fases de Carga (C-07.1):** ambas versiones se preparan en un **workspace temporal** (`%TEMP%/DotNetPerformanceComparator/<guid>/{net8,net10}`) mediante copia segura (excluye bin/obj/.vs/.git, sin seguir enlaces) + `dotnet restore` + `dotnet publish -c Release -f <tfm> --no-self-contained` (framework-dependent), con **SDK explícito por major** (8.0.x / 10.0.x estable, aislado del `global.json` del repo). Los proyectos originales (RutaOriginal, RutaMigrada) **nunca se modifican**; el workspace se elimina al terminar (si falla la limpieza, solo warning). El entry assembly se resuelve desde el `*.runtimeconfig.json` del publish. La app objetivo se inicia con `dotnet <entry>.dll` y `ASPNETCORE_URLS=http://127.0.0.1:<puerto-libre>`, sin tocar appsettings/launchSettings. Protocolo de Carga: **readiness** (GET a RutaObjetivo hasta 2xx) → **warm-up** (1 usuario) → **ramp-up** (hasta UsuariosVirtuales) → **medición estable** (KeepConstant durante DuracionSegundos). restore/publish, warm-up y ramp-up **no** generan métricas oficiales. | Comparación justa y aislada; solo la ventana estable se mide. |
| D-37 | **Métricas del motor (C-07.1):** solo **Carga** y solo **CPU, Memoria RAM y Tiempo de respuesta** (otras métricas o tipos → ejecución rechazada con mensaje claro). Durante la ventana estable se muestrea cada **1000 ms**: **CPU** = promedio de ΔTotalProcessorTime / (ΔWallClock · nºProcesadores) · 100 del proceso objetivo (se ignoran intervalos físicamente inválidos: ΔWall ≤ 0, ΔCPU < 0 o excesos grandes sobre el 100 %; solo las pequeñas desviaciones por granularidad del contador —hasta 105 %— se ajustan a 100, sin ocultar errores significativos con un clamp); **RAM** = promedio de `WorkingSet64` → MB; **Tiempo de respuesta** = media de latencia de las solicitudes HTTP exitosas del paso "request" de NBomber (solo GET; el `IntervaloSolicitudesMs` se aplica fuera del paso y no entra en la latencia). Si no hay solicitudes exitosas o falta una métrica, la `Ejecucion` queda **Fallida** (sin resultados parciales). NBomber vive **solo en Infrastructure**; Application usa DTOs propios. Las muestras son transitorias (no se persisten). | Métricas acotadas y bien definidas antes de ampliar en C-07.2+. **Ampliado por D-39 y D-40 (C-07.2): el motor de Carga soporta las seis métricas del catálogo. Ampliado por D-41–D-43 (C-07.3): el motor habilita también el tipo Estrés. Ampliado por D-44–D-46 (C-07.4): el motor habilita Concurrencia; los tres tipos del catálogo están soportados.** |
| D-39 | **Definiciones operacionales definitivas de métricas HTTP (C-07.2), medidas exclusivamente en la ventana `KeepConstant` estable (nunca warm-up ni ramp-up, que usan runners NBomber independientes cuyo resultado se descarta) y solo sobre las solicitudes exitosas (`StepStats.Ok`) del paso "request":** **Tiempo de respuesta** = Mean de `StepStats.Ok.Latency` (`MeanMs`), en ms. **Latencia** = **P95** de `StepStats.Ok.Latency` (`Percent95`), en ms (tomado directamente de NBomber, no se calcula a mano; no es P99 ni latencia de fallidas). **Throughput** = `StepStats.Ok.Request.RPS` (solicitudes exitosas por segundo), en req/s (no se reconstruye como exitosas/duración, no incluye fallidas). El pacing `IntervaloSolicitudesMs` ocurre fuera del paso y no entra en Mean/P95; sí afecta naturalmente al RPS. Antes de persistir se valida que cada valor sea finito y semánticamente válido (Mean/P95 ≥ 0; RPS > 0 cuando hay exitosas); un valor NaN/Infinity/incoherente marca la `Ejecucion` como **Fallida** sin resultados parciales (no se sustituye por 0/-1). `Tiempo de respuesta` (Mean) y `Latencia` (P95) son estadísticas distintas de la misma distribución; en UI se rotulan "Tiempo de respuesta promedio" y "Latencia P95". Fuente de verdad para CU-05/CU-07. | Definiciones estables de las seis métricas; NBomber sigue aislado en Infrastructure (Application solo recibe el DTO `LoadPhaseResult`). |
| D-40 | **Tamaño de aplicación (C-07.2):** suma recursiva, en bytes, de **todos** los archivos del directorio producido por `dotnet publish -c Release` framework-dependent (DLL, deps.json, runtimeconfig.json, PDB si lo genera, wwwroot, dependencias, etc.; se excluye fuente/obj/bin externo/.git/.vs). Se calcula **una sola vez por versión, inmediatamente después de un publish correcto y ANTES de iniciar cualquier proceso objetivo**, y se **congela** en `PreparedVersion.PublishedSizeBytes` (los archivos de runtime —logs, SQLite, cachés— no deben alterarlo). Conversión MB = bytes / 1024². No se cambia el comando de publish (sin Trimmed/ReadyToRun/SingleFile/SelfContained ni `DebugType=None`); se mide el publish real bajo las mismas condiciones para ambas versiones. **.NET 8 y .NET 10 pueden diferir**, pero todas las `Ejecucion` de una misma versión reciben el **mismo** valor (redundancia intencional, compatible con `ResultadoMetrica` sin nueva entidad; el promedio no se altera). Solo se persiste como `ResultadoMetrica` si la métrica fue seleccionada. | Tamaño reproducible e independiente del runtime; una sola fuente de verdad por versión. |
| D-41 | **Prueba de Estrés escalonada (C-07.3):** el Estrés de la herramienta es **escalonado (step stress)**: un **warm-up único por `Ejecucion`** (1 usuario, runner NBomber independiente, resultado descartado) seguido de una serie de **niveles**, cada uno ejecutado con `Simulation.KeepConstant(usuariosObjetivo, DuracionNivelSegundos)` en un runner NBomber independiente. **No hay ramp-up** entre niveles (no se usa `RampingConstant`; sería inventar una rampa sin parámetro de transición). Los niveles se generan con una función pura (`StressLevelPlan`): desde `UsuariosIniciales` sumando `IncrementoUsuarios`, con **último nivel exactamente `UsuariosMaximos`** (nunca se sobrepasa, sin duplicados, sin overflow). **Todos los niveles de una misma `Ejecucion` se ejecutan sobre el mismo proceso objetivo** (no se reinicia entre niveles; el estrés se acumula); la siguiente `Ejecucion` usa un proceso nuevo. El orden alternado .NET 8/.NET 10 y la preparación (publish Release una vez por versión, `PublishedSizeBytes` congelado) se reutilizan sin cambios. El intervalo entre solicitudes se mantiene fuera del Step (no entra en Mean/P95). Orquestado por `IStressExecutionRunner` para no inflar `EjecutarEvaluacionService`. | Observar el comportamiento progresivo bajo presión creciente de forma reproducible. |
| D-42 | **Resultados de Estrés por nivel y resumen (C-07.3):** cada nivel se persiste **incrementalmente** como `NivelEstres` (estado EnProceso→Completado/Saturado/Fallido/Cancelado) con sus `ResultadoMetricaNivelEstres` de las métricas **variables bajo presión seleccionadas** (CPU, RAM, Tiempo de respuesta=Mean, Latencia=P95, Throughput=RPS; **Tamaño nunca por nivel**, no depende de los usuarios). Monitor CPU/RAM **reiniciado por nivel** (muestras de niveles distintos nunca se promedian juntas). Para una `Ejecucion` que alcanza `UsuariosMaximos`, los `ResultadoMetrica` de resumen son **exactamente los del nivel máximo** (no un promedio de niveles) más el Tamaño (`PublishedSize`). Índices únicos `NivelEstres(EjecucionId,NumeroNivel)` y `ResultadoMetricaNivelEstres(NivelEstresId,MetricaId)`, `DeleteBehavior.Restrict`. Migración EF `AddStressLevelResults` (6.ª, aditiva). Los múltiples `SaveChanges` por nivel son intencionales: si un nivel posterior falla, los anteriores ya completados se conservan. | Preservar la curva progresiva; resumen compatible con el modelo general sin perder el detalle por nivel. |
| D-43 | **Saturación vs. fallo técnico en Estrés (C-07.3):** **saturación** = comportamiento observado de la aplicación bajo estrés, regla **conservadora**: un nivel que completa su ventana con **0 solicitudes exitosas** (con fallidas), o el **proceso objetivo muere durante el nivel**. En ese caso el nivel queda **Saturado**, **no se intentan niveles superiores**, y la `Ejecucion` queda **`CompletadaConSaturacion`** (nuevo estado) **sin `ResultadoMetrica` de resumen** (no se copia un nivel menor ni se inventan Mean/P95/RPS=0; en saturación por 0 exitosas pueden persistirse CPU/RAM confiables del nivel). La saturación **no detiene la evaluación**: continúa con la siguiente versión/repetición, y la `ConfiguracionEvaluacion` puede quedar **Finalizada** si todas sus ejecuciones terminaron Completada o CompletadaConSaturacion. En cambio, un **fallo técnico** del evaluador (NBomber no inicia, repositorio/monitor falla, o nunca se alcanza readiness) marca el nivel **Fallido**, la `Ejecucion` **Fallida** y la configuración **Fallida**, deteniendo todo. No se definen umbrales arbitrarios (% errores, P95/CPU/RAM) como saturación en C-07.3. | Distinguir capacidad de la aplicación de errores del banco de pruebas; base para la comparación de CU-07. |
| D-44 | **Prueba de Concurrencia por rondas sincronizadas (C-07.4):** "UsuariosConcurrentes = N" significa que los **N usuarios virtuales deben estar preparados antes de iniciar una ronda**: ninguno envía su GET hasta que los N llegan a una **barrera de inicio** (`RoundCoordinator`, pura y asíncrona, con `TaskCompletionSource`/`Interlocked`/`CancellationToken`, sin bloqueos síncronos), que los libera a todos de golpe; una **barrera de fin** espera a que las N respuestas terminen antes de cerrar la ronda. La simultaneidad se operacionaliza como "liberación desde una misma barrera tras estar listos los N", **no** como simultaneidad física perfecta. Cada usuario ejecuta **exactamente 1 GET por ronda** mediante el modelo cerrado `Simulation.IterationsForConstant(copies = N, iterations = N·Rondas)` con `WithRestartIterationOnFail(false)` (sin reintentos ocultos; un HTTP 500 cuenta como esa solicitud) y `MaxFailCount` alto (los fallos son evidencia, no abortan). Warm-up único por `Ejecucion` (1 usuario, descartado); **sin ramp-up**. La ronda siguiente no comienza hasta que la anterior terminó sus N respuestas y transcurrió `IntervaloRondasMs` (el intervalo ocurre tras la barrera de fin y antes de liberar la siguiente). Un `HttpClient` único con `MaxConnectionsPerServer ≥ N` evita serializar del lado del generador. Barrera con **timeout técnico interno** (no es parámetro del Usuario); si no llegan los N → fallo técnico. NBomber + barrera en Infrastructure; Application recibe `ConcurrencyPhaseResult`. Coordinado por `IConcurrencyExecutionRunner` para no inflar `EjecutarEvaluacionService`. | Medir el comportamiento bajo ráfagas realmente coordinadas, de forma reproducible. |
| D-45 | **Estadísticas y diagnóstico de Concurrencia (C-07.4):** **una sola ejecución NBomber** para todas las rondas, de modo que Mean (`StepStats.Ok.Latency.MeanMs`), **P95** (`Percent95`) y RPS (`Request.RPS`) se calculan sobre las **N·R solicitudes individuales** (P95 **global**, nunca promedio de percentiles por ronda); solo solicitudes OK. El Step "request" mide solo el envío/recepción HTTP (la espera en barrera, el intervalo y la preparación del request quedan fuera). La **ventana de CPU/RAM** abarca desde la liberación de la primera ronda hasta la última respuesta, incluyendo los intervalos; por eso el **Throughput refleja el ritmo efectivo de toda la prueba (incluye los intervalos)** y no se resta el intervalo del denominador. Para pruebas muy cortas (<1 s) se toma una **muestra de recursos inicial y otra final** además del muestreo periódico de 1000 ms, de modo que CPU/RAM siempre tengan un valor válido **sin alterar** la semántica histórica de Carga/Estrés. El **desfase de inicio** por ronda (`max−min` de `Stopwatch.GetTimestamp()` captado justo antes del envío) es **diagnóstico** (se guardan `DesfaseInicioMaximoMs`/`DesfaseInicioPromedioMs` en `CondicionEjecucion`), **no** una métrica ni un umbral de éxito. La validez estructural de una ronda es: N participantes en la barrera, liberados juntos, N requests ejecutados. | Estadísticas correctas y diagnóstico honesto del scheduler sin umbrales artificiales. |
| D-46 | **Persistencia y estados de Concurrencia (C-07.4):** las rondas son **repeticiones internas de la misma condición** (mismos N, endpoint y versión), a diferencia de los niveles de Estrés; por eso **no hay entidad por ronda ni migración** (no se requiere cambio de esquema). Los `ResultadoMetrica` de la `Ejecucion` (CPU, RAM, Mean, P95, RPS, Tamaño según selección) representan **todas las rondas en conjunto**. Debe cumplirse exactamente `SolicitudesExitosas + SolicitudesFallidas = N·Rondas`; si no, la `Ejecucion` es **Fallida** por inconsistencia técnica. Con OK>0 y algún Fail → **Completada** (Mean/P95/RPS solo OK). Con **0 OK** → **Fallida** sin resultados (no se inventan 0). Concurrencia **no usa `CompletadaConSaturacion`**: si el proceso objetivo muere, la sincronización falla (timeout de barrera) o el conteo no cuadra → `Ejecucion` Fallida y `ConfiguracionEvaluacion` Fallida (detiene la evaluación). El Tamaño sigue siendo el `PublishedSize` congelado. | Modelo simple y honesto: las rondas no son niveles de presión. |
| D-47 | **La sesión de ejecución pertenece al host Web, no al circuito Blazor (C-07.5):** un `EvaluationExecutionSessionManager` singleton (capa Web) arranca la evaluación en su propio scope (`IServiceScopeFactory`, sin capturar dependencias scoped), conserva y observa la `Task` (nunca `async void`; captura excepciones → estado Fallida, sin `UnobservedTaskException`) y es **dueño del `CancellationTokenSource`**. Cerrar/recargar (F5)/desconectar el navegador **no cancela, no reinicia ni altera** la evaluación. El `EvaluationExecutionGate` se conserva como garantía interna del motor (una sola evaluación simultánea); el manager es la capa de experiencia/lifetime del host. El progreso se recibe mediante un `IProgress` propio (`ExecutionProgressSink`) que solo actualiza un estado thread-safe; Application no conoce la UI ni invoca `StateHasChanged`. | Robustez: la evaluación es una operación del servidor, no del circuito. |
| D-48 | **Estado transitorio y reconexión de M-05 (C-07.5):** el manager mantiene un **snapshot inmutable** (estado de sesión, progreso, fase, CPU/RAM en vivo, logs recortados y serie de CPU) de la sesión activa, más opcionalmente el último snapshot finalizado; se descarta al iniciar otra evaluación. Nada de esto se persiste en SQLite (sin tablas de sesión/log/muestras; sin migración). M-05 al cargarse pregunta al manager por la sesión activa de esa configuración y, si existe, reconstruye la UI y hace **polling** (`PeriodicTimer` ~1 s, propiedad del componente; al `Dispose` se cancela solo el timer, nunca la evaluación). Si no hay sesión activa, reconstruye el estado **persistido** (ejecuciones, resultados, niveles) mediante `IExecutionStatusQueryService` (Application; la capa Web no usa `DbContext`). La serie temporal de CPU solo existe durante la sesión activa (no se reconstruye tras recarga si el snapshot se descartó). | Continuidad visual ante F5/reconexión sin duplicar evaluaciones ni fabricar datos. |
| D-49 | **Detener evaluación y LineChart (C-07.5, responsividad afinada en C-07.5.1):** el botón "Detener evaluación" solicita `RequestCancellation` al manager (idempotente); `Cancel()` del CTS del manager, nunca mata procesos desde Web. El token se propaga hasta Infrastructure para una **cancelación responsiva** de las fases NBomber: en cada invocación del escenario, si el token está cancelado se llama `IScenarioContext.StopCurrentTest(...)` (termina la fase cuanto antes en vez de esperar la duración restante); la solicitud HTTP usa un token **enlazado** (`CreateLinkedTokenSource(token, context.ScenarioCancellationToken)`) para abortar la petición en vuelo; y los `Task.Delay` de pacing/intervalo reciben el token. En Concurrencia, además, el `RoundCoordinator` libera las barreras al cancelar (sin deadlock). Verificado empíricamente: una Carga de 35 s se detuvo a ~18 s. Pase lo que pase con las estadísticas de una fase cancelada, se **descartan**: tras la fase se comprueba el token y la **unidad activa** (Ejecucion de Carga, nivel de Estrés, Ejecucion de Concurrencia) queda **Cancelada/Cancelado sin resultados**; las ejecuciones/niveles previamente **Completados se conservan** y **no** se crea resumen parcial. Nunca termina en Fallida por cancelación. Carrera cancelar-vs-finalizar: una vez persistida Finalizada, `RequestCancellation` responde que no hay ejecución activa (no se reescribe Finalizada como Cancelada). El **LineChart de CPU** es un componente SVG propio (sin librerías), serie **transitoria** de la Ejecucion actual que se reinicia al cambiar versión/repetición (no por nivel/ronda); no se persiste ni recalcula el valor oficial. | Cierra B-13 (cancelación responsiva) y B-14; cancelación coherente con el modelo experimental. |
| D-38 | **Endurecimiento de persistencia de ejecución (C-07.1):** índice ÚNICO en `Ejecucion (ConfiguracionId, VersionDotNet, NumeroEjecucion)` y en `ResultadoMetrica (EjecucionId, MetricaId)`; una `Ejecucion` con sus `CondicionEjecucion` y `ResultadoMetrica` se guarda en un único `SaveChanges`; no se persisten resultados de ejecuciones fallidas/canceladas. Estados: `ConfiguracionEvaluacion` → EnEjecucion/Finalizada/Fallida/Cancelada; `Ejecucion` → EnProceso/Completada/Fallida/Cancelada. Migración EF `HardenExecutionPersistence` (5.ª). | Integridad y unicidad de los resultados experimentales. |
| D-34 | **Una evaluación = un único tipo de prueba (C-06.2):** `ConfiguracionEvaluacion` corresponde exactamente a un `TipoPrueba` (`TipoPruebaId` requerido, FK a `TipoPrueba` con `Restrict`, `TipoPrueba 1 — 0..* ConfiguracionEvaluacion`). Para ejecutar otro tipo, el Usuario crea otra configuración (mismo proyecto, migración y endpoint si quiere). Se **eliminó la entidad/tabla `ConfiguracionPrueba`** (la unión N↔N de C-06.1 ya no aplica) y `ParametroEvaluacion` volvió a `(Id, ConfiguracionId, Clave, Valor)` con FK directa a la configuración (`Restrict`). M-04 usa selección única de tipo. Migración EF `SimplifyEvaluationToSingleTestType` (4.ª). Las métricas siguen siendo múltiples por evaluación. | Regla funcional definitiva previa a CU-04; el modelo refleja 1 configuración = 1 tipo sin ambigüedad. |
| D-50 | **Motor comparativo .NET 8 vs .NET 10 (CU-07, C-10.2; motor sin UI):** el cálculo vive en Application (`IComparacionResultadosService`), es **puro/determinista** y consume el detalle de lectura de CU-05 (`EvaluacionDetalleConsulta`) **sin nueva consulta EF, sin Infrastructure y sin persistir** (la `Comparacion` sigue siendo transitoria, §8). Reglas durables: (1) la comparación es **intra-evaluación**: las dos versiones de **una misma** `ConfiguracionEvaluacion`; (2) **.NET 8 es la base/referencia** y .NET 10 la experimental; (3) **pares experimentales completos por métrica y repetición** (emparejados por `NumeroEjecucion`): una repetición N aporta a una métrica solo si **ambas** versiones tienen `ResultadoMetrica` de esa `MetricaId` en N — nunca se promedian conjuntos desiguales; (4) **DiferenciaAbsoluta = PromedioNet10 − PromedioNet8**; **VariacionPorcentual = (PromedioNet10 − PromedioNet8) / PromedioNet8 × 100**, y si `PromedioNet8 == 0` la variación es **null** (no infinito) sin volver la métrica NoComparable si ambos valores existen; (5) **orientación por `MetricaId` estable** (no por texto, sin columna en BD): CPU, RAM, Tiempo de respuesta, Latencia y **Tamaño** = *menor es mejor* (Tamaño = menor huella, no mejor rendimiento), **Throughput = mayor es mejor**; la interpretación {Mejora, Empeora, Igual, NoComparable} se deriva de la orientación, separada del signo; (6) **igualdad exacta sobre `decimal`** (sin tolerancia ni redondeo antes de calcular; el redondeo es solo de presentación); (7) estados finales respetados: **Fallida/Cancelada no son fuente**, la ausencia **nunca** es cero; (8) **Tamaño** (MetricaId 6) se trata como **valor constante por versión** (medido una vez en preparación, D-40): valores repetidos idénticos = una constante (`ParesUtilizados` conceptual = 1), valores distintos en una misma versión = **inconsistencia → NoComparable** (no se promedia ni se elige); (9) **EsComparable** es true si **al menos una** métrica tiene comparación válida (comparación parcial permitida). **Estrés** (modelo separado `ComparacionEstres`): el **resumen** solo compara repeticiones donde **ambas** versiones alcanzaron el nivel máximo (estado `Completada`); una `CompletadaConSaturacion` no tiene resumen y **no** se sustituye por un nivel menor; además se calcula **comparación por carga equivalente** emparejando `NumeroEjecucion` + `UsuariosObjetivo` + `MetricaId` (nunca por número de nivel), exigiendo nivel **Completado en ambas versiones** de la misma repetición (un nivel Saturado nunca se compara contra uno Completado); **Tamaño no participa por nivel**; la **saturación** se expresa como `ContextoSaturacion` por versión, terminología del experimento concreto, **no** una 7.ª métrica ni dato persistido: `Saturo` (saturó en ≥1 repetición), `NivelSaturacionMinObservado`/`NivelSaturacionMaxObservado` (rango de niveles donde se observó saturación entre repeticiones — Min==Max = saturación consistente; si difieren, la variabilidad **no** se colapsa en un número arbitrario, C-10.2.1) y `MaxUsuariosCompletados` (= máximo nivel **completado observado en al menos una repetición**, no "completado siempre"). Para la comparación **por nivel**, `NivelComparable` es true si **alguna** métrica del nivel logró ≥1 par completo (regla por métrica, no basta con que el nivel se "completara"); cada métrica conserva su propio `ParesUtilizados`/`Interpretacion` (C-10.2.1). El servicio es **defensivo** ante datos inconsistentes (duplicados por versión/repetición → se omite el par, no se elige arbitrariamente) y **no** declara ganador global ni significancia estadística (reservado a C-10.3). El motor es independiente de CU-06: calcula todas las métricas configuradas y Web decide el filtrado de presentación. **Sin entidades nuevas ni migración (siguen 6).** | Comparación metodológicamente correcta, reproducible y testeable, derivada de lo persistido sin contaminar el modelo de datos ni la UI (RF-09; diseño cerrado en C-10.1). |
