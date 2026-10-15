# Especificación de interfaz (UI_SPEC)

> Especificación técnica de interfaz derivada **estrictamente** de los mockups M-01 a M-08 (`docs/mockups/*.png` y `docs/mockups/Mockups-Interfaces.drawio`).
> Complementa a [`PROJECT_CONTEXT.md`](PROJECT_CONTEXT.md), que sigue siendo la fuente de verdad funcional. Cuando un mockup muestra algo que el contexto o el modelo de datos no cubren, se registra en la sección 8 (*Brechas y decisiones pendientes*) en lugar de asumirlo.
>
> Los valores que aparecen en los mockups (AplicaciónWebEjemplo, 42 %, 780 MB, fechas, etc.) son **datos ilustrativos** y no deben codificarse en la interfaz.

---

## 1. Estructura visual común

Todas las pantallas comparten la misma estructura de tres zonas:

```
┌──────────────────────────────────────────────────────────────────────────┐
│ ENCABEZADO (banda oscura)                                                │
│  Título de la herramienta         Proyecto de Graduación   [Logo]        │
│  Subtítulo = descripción de la página actual                             │
├───────────────┬──────────────────────────────────────────────────────────┤
│ BARRA LATERAL │ ÁREA DE CONTENIDO                                        │
│ "Navegación"  │  Título de página (h1)                                   │
│  [Inicio]     │  Descripción de página                                   │
│  [Incorporar] │                                                          │
│  [.NET 10...] │  ┌────────── Panel ─────────┐ ┌──── Panel ────┐          │
│  [...]        │  │ Título de panel          │ │               │          │
│               │  │ contenido                │ │               │          │
│               │  └──────────────────────────┘ └───────────────┘          │
│               │                                  [Acciones de página]    │
└───────────────┴──────────────────────────────────────────────────────────┘
```

### 1.1 Encabezado
| Elemento | Contenido | Observación |
|---|---|---|
| Título de la herramienta | "Herramienta de evaluación y comparación de rendimiento .NET" | Fijo en todas las pantallas. |
| Subtítulo | Igual a la descripción de la página actual (ej. "Registro y validación de la aplicación base que será evaluada") | **Varía por página.** |
| Identificación institucional | "Proyecto de Graduación" | Texto estático informativo que **reemplaza** el nombre personal del mockup. No representa al actor `Usuario` ni una sesión (decisión B-01). |
| Logo institucional | Escudo de la universidad sobre recuadro blanco | Recurso estático `wwwroot/images/logo-umg.webp`, extraído del `.drawio` (decisión B-02). |

### 1.2 Barra lateral de navegación
- Título "Navegación".
- 8 elementos, en este orden y con estas etiquetas exactas:

| # | Etiqueta | Mockup |
|---|---|---|
| 1 | Inicio | M-01 |
| 2 | Incorporar proyecto | M-02 |
| 3 | .NET 10 / Migración | M-03 |
| 4 | Configuración | M-04 |
| 5 | Ejecución | M-05 |
| 6 | Resultados | M-06 |
| 7 | Comparación | M-07 |
| 8 | Reportes | M-08 |

- Cada elemento es un recuadro blanco con borde gris; el elemento de la página actual se resalta (fondo azul claro, borde azul, texto azul).
- No existen elementos de login, perfil, administración ni configuración de usuario.

### 1.3 Área de contenido
- Encabezado de página: **título** (h1) + **descripción** (texto secundario).
- Contenido organizado en **paneles** (tarjetas blancas con borde gris claro y esquinas muy redondeadas), cada uno con un título propio.
- Disposición en rejilla de 1 a 4 columnas según la pantalla.
- Botones de acción principales ubicados abajo a la derecha del panel o del área de contenido.

---

## 2. Guía visual (tokens)

Valores extraídos del archivo `.drawio`. Se definirán como variables CSS en `wwwroot/app.css`.

### 2.1 Colores

| Token propuesto | Valor | Uso en mockups |
|---|---|---|
| `--color-header-bg` | `#0f172a` | Banda del encabezado, consola de bitácora, encabezado de vista previa de reporte |
| `--color-header-text` | `#ffffff` | Texto del encabezado |
| `--color-page-bg` | `#f8fafc` | Fondo del área de contenido |
| `--color-sidebar-bg` | `#e2e8f0` | Fondo de la barra lateral |
| `--color-surface` | `#ffffff` | Paneles, elementos de navegación, inputs |
| `--color-surface-muted` | `#f9fafb` | Sub-paneles, filas alternas, opciones de checkbox |
| `--color-border` | `#cbd5e1` / `#d1d5db` | Bordes de paneles, inputs y navegación |
| `--color-text` | `#111827` | Texto principal y títulos |
| `--color-text-secondary` | `#475569` / `#64748b` | Descripciones, etiquetas secundarias |
| `--color-primary` | `#2563eb` | Botón primario |
| `--color-primary-strong` | `#1d4ed8` | Texto de navegación activa, enlaces |
| `--color-primary-soft` | `#dbeafe` | Navegación activa, encabezado de tablas, opción seleccionada |
| `--color-primary-soft-2` | `#eff6ff` | Accesos rápidos, recuadros informativos |
| `--color-primary-border` | `#60a5fa` | Borde de navegación activa y tablas |
| `--color-primary-text-dark` | `#1e3a8a` | Texto en recuadros azules |
| `--color-secondary` | `#e2e8f0` (borde `#9ca3af`) | Botones secundarios (Cancelar, Limpiar, Examinar, Ver detalle) |
| `--color-success` | `#16a34a` | Botones "Iniciar evaluación" y "Descargar" |
| `--color-danger` | `#ef4444` | Botón "Detener evaluación" |
| `--color-status-ok` | `#22c55e` | Indicador "Correcto" / validación superada |
| `--color-status-progress` | `#f59e0b` | Indicador "En proceso" |
| `--color-status-pending` | `#cbd5e1` | Indicador "Pendiente" |
| `--color-net8` | `#60a5fa` (variante `#93c5fd`) | Serie .NET 8 en gráficos |
| `--color-net10` | `#34d399` (variante `#6ee7b7`) | Serie .NET 10 en gráficos |
| `--color-metric-cpu` | `#60a5fa` | Indicador de tarjeta CPU (M-05) |
| `--color-metric-ram` | `#34d399` | Indicador de tarjeta RAM (M-05) |
| `--color-metric-latency` | `#f59e0b` | Indicador de tarjeta Latencia (M-05) |
| `--color-metric-throughput` | `#a78bfa` | Indicador de tarjeta Throughput (M-05) |
| `--color-console-text` | `#cbd5e1` / azul claro | Texto de bitácora |

### 2.2 Tipografía
| Elemento | Tamaño aprox. | Peso |
|---|---|---|
| Título de la herramienta (encabezado) | 20 px | Negrita |
| Título de página (h1) | 24 px | Negrita |
| Título de panel | 16 px | Negrita |
| Valor destacado (tarjetas) | 18–20 px | Negrita |
| Etiquetas de campo | 12–13 px | Negrita |
| Texto general / tablas | 11–13 px | Normal |
| Descripción de página / subtítulos | 11–12 px | Normal |
| Bitácora | 12–13 px | Monoespaciada (`Courier New`) |

Fuente general: sans-serif del sistema (el `.drawio` no fija otra familia salvo `Courier New` para bitácoras).

### 2.3 Formas
- Paneles principales: esquinas muy redondeadas (radio grande).
- Inputs, botones, elementos de navegación, tarjetas pequeñas: esquinas levemente redondeadas.
- Tablas: celdas con borde; encabezado con fondo azul claro y texto centrado.

---

## 3. Elementos del `MainLayout`

Pertenecen al layout (comunes a todas las páginas):

1. **Encabezado** (`AppHeader`): título fijo, subtítulo dinámico, texto "Proyecto de Graduación", logo.
2. **Barra lateral** (`NavMenu`): título "Navegación" + 8 enlaces con estado activo.
3. **Contenedor del área de contenido** con fondo `--color-page-bg`.
4. **Zona de error no controlado de Blazor** (`#blazor-error-ui`), ya existente.

El **subtítulo del encabezado** depende de la página. Mecanismo propuesto: `SectionOutlet`/`SectionContent` de Blazor (.NET 8), de modo que cada página declare su descripción una sola vez a través de `PageHeader` y esta se proyecte tanto en el encabezado como en el área de contenido.

No pertenecen al layout: título/descripción de página (componente `PageHeader` usado por cada página) ni botones de acción (propios de cada página).

---

## 4. Componentes reutilizables

Identificados por repetición en dos o más mockups. Solo contienen presentación; los datos llegan por parámetros desde la página, que a su vez los obtiene de servicios de `Application`.

> **Implementación incremental:** este catálogo describe los componentes de toda la aplicación, pero **no se implementan por adelantado**. La tarea de interfaz base solo crea los necesarios para el layout y M-01; cada uno de los demás se crea al trabajar la primera pantalla que lo use.
>
> **Gráficos (decisión B-03):** `BarChart` y `LineChart` son componentes Blazor propios basados en SVG, sin librerías NuGet, npm ni JavaScript. Reciben sus datos por parámetros y solo cubren las capacidades que exigen los mockups. `BarChart` se implementa con M-01; `LineChart` se implementará con M-05.

| Componente | Descripción | Mockups |
|---|---|---|
| `PageHeader` | Título (h1) + descripción; además publica el subtítulo del encabezado. | Todos |
| `Panel` | Tarjeta blanca redondeada con título y contenido (`ChildContent`). | Todos |
| `FormField` | Etiqueta en negrita + control (input, select) debajo. | M-02, M-03, M-04, M-06, M-08 |
| `AppButton` | Botón con variantes: `Primary` (azul), `Secondary` (gris), `Success` (verde), `Danger` (rojo). | Todos excepto M-01 (solo primario) |
| `DataTable` | Tabla con encabezado azul claro, celdas con borde, filas alternas; permite selección de fila (M-06). | M-01, M-06, M-07 |
| `StatusList` | Lista de elementos con indicador cuadrado de color (correcto / en proceso / pendiente), texto y detalle opcional. | M-02 (validaciones), M-03 (progreso de migración) |
| `LogConsole` | Panel oscuro con líneas monoespaciadas precedidas por `>`. | M-03, M-05 |
| `KeyValueList` | Pares "**Clave:** valor" en líneas. | M-02 (vista previa), M-05 (estado general), M-06 (detalle), M-08 (vista previa) |
| `SummaryCard` | Tarjeta con etiqueta pequeña y valor destacado. | M-01 |
| `MetricTile` | Tarjeta con nombre de métrica, valor + unidad y barra indicadora de color. | M-05 |
| `SingleOptionToggleGroup` | Grupo de botones de **selección única** (radiogroup accesible). | M-04 (tipo de prueba) |
| `CheckboxGroup` | Opciones con casilla, en rejilla. | M-04 (métricas), M-08 (contenido a incluir) |
| `ProgressBar` | Barra de progreso con porcentaje. | M-05 |
| `BarChart` | Barras verticales; agrupadas por métrica con series .NET 8 / .NET 10 y leyenda. | M-01, M-07 |
| `LineChart` | Serie temporal en escalones/líneas. | M-05 |
| `QuickAccessCard` | Enlace en forma de tarjeta con título y texto secundario. | M-01 |
| `InfoBox` | Recuadro azul claro con título y texto. | M-07 (conclusión), M-08 (resultados principales en vista previa) |

**Componente no presente en los mockups pero requerido por RF-14:** `OperationMessage` (mensaje de estado/error de una operación). Los mockups no muestran estados de error; su estilo deberá derivarse de los tokens anteriores (ver 8.2).

Ubicación propuesta: `src/DotNetPerformanceComparator.Web/Components/Layout/` (layout, encabezado, navegación) y `src/DotNetPerformanceComparator.Web/Components/Shared/` (resto).

---

## 5. Especificación por pantalla

Rutas propuestas (en español, coherentes con la navegación):

| Mockup | Ruta | Título de página | Descripción / subtítulo |
|---|---|---|---|
| M-01 | `/` | Inicio / Panel principal | Vista general del sistema y acceso rápido a las funciones principales |
| M-02 | `/incorporar-proyecto` | Incorporar proyecto .NET 8 | Registro y validación de la aplicación base que será evaluada |
| M-03 | `/migracion` | Migración experimental a .NET 10 | Preparación de una versión experimental a partir de la aplicación base en .NET 8 |
| M-04 | `/configuracion` | Configuración de evaluación | Definición de pruebas, métricas, parámetros y número de ejecuciones |
| M-05 | `/ejecucion` | Ejecución de evaluación | Monitoreo de la prueba en curso y de las métricas recolectadas |
| M-06 | `/resultados` | Consulta de resultados | Historial de evaluaciones y detalle filtrado por criterio o métrica |
| M-07 | `/comparacion` | Comparación y visualización | Análisis comparativo entre resultados de .NET 8 y .NET 10 mediante tablas y gráficos |
| M-08 | `/reportes` | Generación de reporte PDF | Selección de contenido y emisión del reporte final en formato PDF |

### M-01 — Inicio / Panel principal
- **Fila de 4 `SummaryCard`:** Proyecto activo (nombre + versión), Última migración (resultado y destino), Última evaluación (tipo · nº de ejecuciones), Último reporte.
- **Panel "Accesos rápidos":** 4 `QuickAccessCard` en rejilla 2×2:
  - Incorporar proyecto — ".NET 8" → M-02
  - Migrar a .NET 10 — "Preparar versión experimental" → M-03
  - Configurar evaluación — "Pruebas, métricas y repeticiones" → M-04
  - Consultar resultados — "Historial y filtros" → M-06
- **Panel "Resumen comparativo reciente":** `BarChart` con barras .NET 8 / .NET 10 y etiquetas (ej. CPU %, RAM MB).
- **Panel "Actividad reciente":** `DataTable` con columnas Fecha · Acción · Detalle · Estado. Acciones mostradas: Incorporación, Migración, Evaluación, Reporte.
- **Acción:** botón primario "Crear nueva evaluación" (abajo a la derecha) → M-04.

**Estados vacíos (tarea de interfaz base).** Mientras no existan los CU que producen la información, M-01 no muestra valores ilustrativos ni usa servicios, entidades, tablas o lógica ficticia:

| Elemento | Texto en estado vacío |
|---|---|
| Proyecto activo | Sin proyecto registrado |
| Última migración | Sin migraciones |
| Última evaluación | Sin evaluaciones |
| Último reporte | No disponible |
| Resumen comparativo reciente | Estado vacío del `BarChart` (sin datos para mostrar) |
| Actividad reciente | Mensaje indicando que todavía no existe actividad disponible |

Estos elementos se conectarán a información real cuando se resuelvan B-04, B-05 y B-06.

> **Estado de implementación — "Resumen comparativo reciente" (CU-08, C-11.3, UI-26).** El `BarChart` de este panel deja de ser un placeholder vacío y se conecta a datos reales: muestra la **evaluación más reciente con una comparación de resumen válida** (recorre el listado de CU-05, ya ordenado `FechaCreacion` desc, `Id` desc, y toma la primera con resultados cuyo resumen tenga alguna métrica graficable; implicación: puede inspeccionar el detalle de unas pocas evaluaciones recientes, sin patrón N+1 masivo ni nueva consulta/Infrastructure). Se representa como **small multiples** (un `BarChart` por métrica comparable, `SharedScale=false`), reutilizando el mapeo `ComparacionChartMapper` compartido con M-07 (solo métricas comparables con ambos promedios; NoComparable omitida, nunca null→0; cero real visible; Tamaño como valor constante por versión). Contexto mínimo (evaluación · proyecto · tipo · fecha; .NET 8 base / .NET 10 experimental) y acción **"Ver comparación" → `/comparacion/{ConfiguracionId}`**. Solo **resumen**: Inicio nunca muestra niveles de Estrés ni saturación, y una evaluación de Estrés saturada sin resumen **no** fabrica gráfico (se omite y se busca la siguiente). Estados: cargando, sin datos comparables (`EmptyState` "Aún no hay resultados comparables para visualizar.", nunca un SVG vacío) y error (mensaje seguro, sin trazas). Lógica en el presentador testeable `InicioResumenModelo`. Las tarjetas de resumen y "Actividad reciente" siguen en estado vacío (B-04/B-05/B-06, fuera de CU-08).

### M-02 — Incorporar proyecto .NET 8 (CU-01)
- **Panel "Datos del proyecto":**
  - Campos: Nombre del proyecto · Tipo de aplicación · Ruta del proyecto (ruta a `.csproj`, ancho completo).
  - Botón secundario "Examinar carpeta" → **implementado como "Seleccionar archivo..."** (junto al campo, abre el selector nativo de Windows) **+ "Validar ruta"** (debajo, ejecuta la inspección) (decisión B-09, UI-09).
  - Campos de solo lectura/detectados: Versión detectada · Framework objetivo · Estado.
  - Sub-panel "Validaciones realizadas": `StatusList` con:
    - Se localizó el archivo .csproj
    - El proyecto corresponde a una aplicación web ASP.NET
    - La versión base es compatible con el proceso de evaluación
    - La estructura del proyecto permite continuar con la migración experimental
- **Panel "Vista previa del proyecto":** `KeyValueList` — Nombre, Framework, Carpetas clave, Archivos detectados.
- **Panel "Acciones":** "Guardar proyecto" (primario), "Cancelar" (secundario) y texto: "Al guardar, el sistema registrará la aplicación y la dejará disponible para el proceso de migración experimental y configuración de evaluaciones."

### M-03 — Migración experimental a .NET 10 (CU-02)
- **Panel "Selección y parámetros de migración":** Proyecto base (`select` con los proyectos registrados, por nombre) · Versión origen (.NET 8) · Versión destino (.NET 10) · Directorio de salida + botón "Examinar" → **implementado como "Seleccionar carpeta..."** (selector nativo de Windows, decisión B-09) · botón primario "Iniciar migración experimental".
- **Panel "Bitácora de ejecución":** `LogConsole` (panel oscuro con título) con los mensajes del proceso y la salida de `dotnet restore`/`dotnet build`.
- **Panel "Progreso de migración":** `StatusList` con pasos y estado (Pendiente / En proceso / Correcto / Error). **Pasos implementados (decisión B-10):**
  1. Validación del proyecto base
  2. Creación de copia experimental
  3. Actualización del framework objetivo a .NET 10
  4. Restauración de dependencias
  5. Build preliminar .NET 10
- Resultado (Proyecto, Ruta migrada, SDK .NET 10 utilizado, Estado) y botón secundario "Ver detalle", visibles solo cuando existe un resultado de migración (UI-10).

### M-04 — Configuración de evaluación (CU-03)
- **Panel "Datos generales de la evaluación":**
  - Campos: Nombre de la evaluación · Proyecto · **Migración .NET 10** (selector añadido, ver UI-12) · **Ruta / endpoint de prueba** (relativa, placeholder `/api/recurso`) · **Método HTTP** (solo lectura, `GET`) · Ejecuciones (**1–3**, con texto "Número de repeticiones que se realizarán para el tipo de prueba seleccionado en cada versión (.NET 8 y .NET 10)").
  - "Tipo de prueba": **selección única** (`SingleOptionToggleGroup`, radiogroup) entre Carga · Estrés · Concurrencia, con el texto "Seleccione un único tipo de prueba para esta configuración" (una configuración corresponde a un solo tipo; ver UI-16, D-34).
  - **Un único panel dinámico de parámetros**, según el tipo seleccionado: **Carga** (Usuarios virtuales · Duración (s) · Warm-up (s) · Ramp-up (s) · Intervalo entre solicitudes (ms)); **Estrés** (Usuarios iniciales · Incremento de usuarios · Usuarios máximos · Duración por nivel (s) · Warm-up (s) · Intervalo entre solicitudes (ms)); **Concurrencia** (Usuarios concurrentes · Rondas · Warm-up (s) · Intervalo entre rondas (ms); sin Ramp-up).
  - "Métricas a recopilar": `CheckboxGroup` 3×2 con las métricas del catálogo, al menos una (comunes a toda la evaluación).
  - "Observaciones": área de texto global y opcional al final.
- **Acciones:** "Guardar" (primario); "Iniciar evaluación" (verde) **habilitado** una vez guardada la configuración (C-07.1): navega a M-05 (`/ejecucion/{ConfiguracionId:int}`). Ver UI-17.
- El bloque genérico "Parámetros específicos" del mockup se eliminó; cada tipo de prueba tiene su propio conjunto de parámetros (C-06.1).

### M-05 — Ejecución de evaluación (CU-04)
- **Panel "Estado general":** `KeyValueList` (Proyecto, Prueba, Versión evaluada, Ejecución actual "n de N", Estado) + `ProgressBar` con porcentaje.
- **4 `MetricTile`:** CPU (%), RAM (MB), Latencia (ms), Throughput (req/s), cada una con su color.
- **Panel "Evolución de consumo de CPU":** `LineChart`.
- **Panel "Bitácora de ejecución":** `LogConsole`.
- **Acción:** botón rojo "Detener evaluación".

> **Estado de implementación — COMPLETA (C-07.1 → C-07.5, UI-17…UI-21).** M-05 cubre todos los elementos funcionales de CU-04 del mockup: panel de estado general (`KeyValueList` + badge + `ProgressBar`), CPU/RAM en vivo, **`LineChart` "Evolución de consumo de CPU"** (C-07.5), `LogConsole`, tabla de resultados por `Ejecucion` (seis métricas), tabla de niveles de Estrés, progreso por nivel/ronda, botones **"Iniciar evaluación"** y **"Detener evaluación"** (C-07.5), y reconstrucción tras recarga/reconexión. La evaluación es server-side (independiente del circuito Blazor). Detalle histórico por etapa:
> - **Implementado:** botón "Iniciar evaluación", panel de estado general (`KeyValueList` + badge de estado), `ProgressBar`, fase en curso, valores de CPU/RAM en vivo durante la fase de medición, `LogConsole` con la bitácora, y una **tabla de resultados** por `Ejecucion` al finalizar con las **seis** métricas seleccionadas: CPU (%), RAM (MB), Tiempo de respuesta promedio (ms), **Latencia P95 (ms)**, **Throughput (req/s)** y **Tamaño de aplicación (MB)** (C-07.2). Latencia/Throughput/Tamaño se muestran solo como valor **final** tras cada `Ejecucion` (no en vivo).
> - **Estrés (C-07.3, UI-19):** durante la ejecución M-05 muestra la fase **Nivel de estrés** con *Nivel N de M · K usuarios concurrentes* y CPU/RAM en vivo; al completar cada nivel se añade una fila a la **tabla "Niveles de estrés"** (Versión, Rep., Nivel, Usuarios, CPU, RAM, Tiempo resp. prom., Latencia P95, Throughput, OK/Fallidas, Estado). El estado del nivel se distingue visualmente (**Completado** verde, **Saturado** ámbar, **Fallido/Cancelado** rojo) y, si hay saturación, se muestra el aviso "⚠ Saturación detectada en N usuarios" — nunca "Error interno" para un resultado de estrés. No se repite Tamaño por nivel. No hay P95/Throughput en vivo (solo valor final del nivel).
> - **Concurrencia (C-07.4, UI-20):** durante la ejecución M-05 muestra la fase **Concurrencia** con *Ronda X de Y · N usuarios concurrentes* y CPU/RAM en vivo, y en la bitácora las líneas de sincronización ("Ronda 2 sincronizada; desfase de inicio 2.1 ms."). La cabecera deja claro *Concurrencia · N usuarios · R rondas* (no "N usuarios durante X s"). Al finalizar se muestran las seis métricas (Mean, P95, RPS, CPU, RAM, Tamaño, OK/Fail) en la misma tabla de resultados por Ejecucion que Carga; no hay tabla por ronda (las rondas son repeticiones de la misma condición).
> - **C-07.5 (UI-21):** ejecución **server-side** mediante `EvaluationExecutionSessionManager` (singleton): cerrar/recargar (F5)/desconectar el navegador **no cancela ni reinicia** la evaluación; M-05 recupera la sesión activa por *polling* de un snapshot (~1 s) o, si no hay sesión, reconstruye el estado persistido. Botón **"Detener evaluación"** (rojo) habilitado solo con evaluación activa; al pulsarlo muestra **"Cancelando..."** y la unidad activa queda Cancelada sin resultados parciales. **`LineChart` de CPU** (SVG propio, serie transitoria que se reinicia al cambiar versión/repetición). Tras recarga sin sesión activa, la serie temporal de CPU no se reconstruye (es transitoria). **M-05 queda COMPLETA; B-13 y B-14 resueltas.**

### M-06 — Consulta de resultados (CU-05, CU-06, CU-08)
- **Panel "Filtros":** Proyecto · Métrica · Tipo de prueba · Rango de fecha; botones "Aplicar" (primario) y "Limpiar" (secundario). Nota: "También pueden visualizarse resultados individuales o promedios por evaluación."
- **Panel "Resultados encontrados":** `DataTable` con Fecha · Evaluación · Versión · Métrica · Valor (valor **con unidad**). Fila seleccionable.
- **Sub-panel "Detalle del resultado seleccionado":** Evaluación, Tipo, Métrica; Promedio .NET 8 / .NET 10; Diferencia y Variación porcentual.
- **Acción:** "Enviar a comparación" (primario) → M-07.

> **Estado de implementación — CU-05 (C-08.2, UI-22).** Se implementa la **consulta** de M-06 (`/resultados`), **solo lectura** vía `IConsultaResultadosService`:
> - **Listado de evaluaciones** (`ObtenerEvaluacionesAsync`): columnas Evaluación · Proyecto · Tipo · Fecha · Estado · Resultados (badge "Disponibles"/"Sin resultados", con texto además de color) · acción "Ver". Orden fijado por Application/Infrastructure (FechaCreacion desc, Id desc). Estado vacío si no hay evaluaciones.
> - **Detalle** (`ObtenerDetalleAsync`): información general (evaluación, proyecto, migración, tipo, estado, fecha, ejecuciones configuradas, endpoint, observaciones), métricas seleccionadas (chips), y por cada **Ejecucion** (identificando .NET 8 / .NET 10 como registros independientes, sin comparar): resumen de métricas con unidad del catálogo, tabla de **niveles de estrés** (por `NumeroNivel`, con métricas por nivel) cuando existan, y condiciones de ejecución en sección secundaria expandible. Ausencia de resultados se muestra explícitamente ("Todavía no hay resultados registrados" / "No existe un resumen general para esta ejecución"), nunca con ceros ficticios; `CompletadaConSaturacion` conserva sus niveles.
> - **Fuera de alcance de C-08.2 (pendiente):** el **Panel "Filtros"** (CU-06), el cálculo de **Promedio/Diferencia/Variación .NET 8 vs .NET 10** y la acción "Enviar a comparación" (CU-07), y las gráficas (CU-08). El detalle muestra ambas versiones como registros, sin estadística comparativa.

> **Estado de implementación — CU-06 (C-09, UI-23).** Sobre el detalle ya consultado se añade un **filtro por métrica** (selector único "Filtrar por métrica" con opción por defecto "Todas las métricas") ubicado en el panel "Ejecuciones". Es **puramente de presentación**: no recalcula métricas, no consulta de nuevo la base de datos, no modifica los resultados persistidos ni los DTO/esquema, y no se persiste la selección.
> - **Origen de las opciones:** las métricas **seleccionadas en la evaluación actual** (`MetricasSeleccionadas`), no el catálogo global. El filtro compara por `MetricaId` estable, no por el texto de presentación (ver D-xx / sección 24).
> - **Selección única** (según mockup): una métrica a la vez; "Todas las métricas" restablece la vista completa de CU-05.
> - **Resumen y Estrés:** con filtro activo se restringen tanto el **resumen** de cada ejecución como las **métricas por nivel** de estrés a la métrica elegida. Las **ejecuciones y niveles permanecen visibles** aunque no tengan esa métrica (nota "No hay un resultado registrado para esta métrica" / "Sin resultado para esta métrica"), nunca un cero ficticio; `Tamaño de la aplicación` no aparece artificialmente por nivel.
> - **Casos vacíos distinguidos:** evaluación **sin resultados** (caso A, mensaje de CU-05) frente a **filtro sin coincidencias** (caso B, "La métrica seleccionada no tiene resultados registrados en esta evaluación").
> - **Reinicio del filtro:** vuelve a "Todas las métricas" al cambiar de evaluación y al cerrar el detalle; se conserva la garantía "última selección gana" (las respuestas tardías de selecciones previas se descartan).
> - **Fuera de alcance de C-09 (pendiente):** filtros por Proyecto/Tipo/Fecha (ver B-15), estadística comparativa .NET 8 vs .NET 10 y "Enviar a comparación" (CU-07), y gráficas (CU-08).

### M-07 — Comparación y visualización (CU-07, CU-08)
- **Panel "Resumen comparativo":**
  - `DataTable`: Métrica · .NET 8 · .NET 10 · Diferencia · Variación %.
  - `InfoBox` "Conclusión automática" con texto interpretativo.
  - Sub-panel "Gráfico comparativo por métricas": `BarChart` agrupado (CPU, RAM, Resp., Lat., Thr.) con leyenda .NET 8 / .NET 10.
  - Sub-panel "Ejecuciones individuales": `DataTable` Ejecución · .NET 8 · .NET 10.
- **Acción:** "Generar reporte comparativo" (primario) → M-08.

> **Estado de implementación — CU-07 (C-10.3, UI-24).** Se implementa **M-07** (`/comparacion/{ConfiguracionId:int}`), **solo lectura**, que obtiene el detalle con `IConsultaResultadosService` (CU-05) y delega **todo** el cálculo en `IComparacionResultadosService` (Application; Web no hace aritmética). Comparación **intra-evaluación** (.NET 8 base vs .NET 10 experimental de la **misma** `ConfiguracionEvaluacion`), nunca entre dos evaluaciones distintas.
> - **Acceso:** desde M-06, botón **"Enviar a comparación"** (visible solo con una evaluación seleccionada) que navega a M-07 con el `ConfiguracionId`; si CU-06 tiene una métrica enfocada, se propaga por query parameter estable `?metricaId=X`. En M-07 el selector "Enfocar métrica" (reutiliza la semántica de CU-06: sin enfoque = todas; con enfoque = una) afecta **solo la presentación**, sin recalcular; un `metricaId` que no pertenece a la evaluación se restablece a "Todas".
> - **Tabla comparativa:** Métrica · .NET 8 · .NET 10 · Diferencia (con unidad y **signo matemático conservado**) · Variación % (`0.## %`, o **"No calculable"** si la base es 0; nunca ∞/NaN/0 ficticio) · Interpretación (badge **con texto** Mejora/Empeora/Igual/No comparable, no solo color) · Pares (transparencia; Tamaño = "1 comparación (valor constante)"). Valores con formato `0.####`. Comparación **parcial** permitida: las métricas `NoComparable` se muestran con "—" sin invalidar las demás; aviso general solo si `EsComparable=false` (aun así se muestra la tabla).
> - **Conclusión automática:** `InfoBox` **descriptivo** con conteos (mejoras/empeoramientos/iguales/no comparables). **No** declara ganador global, porcentaje global de superioridad, ponderación ni significancia; redacción prudente ("en las métricas comparables del experimento"), y el Tamaño se cuenta como huella, no como rendimiento.
> - **Estrés:** sección de **resumen** (nivel máximo) o aviso "no existe comparación de resumen válida al nivel máximo" si hubo saturación (nunca se sustituye por un nivel menor); sección **"Comparación por nivel de carga"** emparejando por `UsuariosObjetivo` (no por número de nivel), con niveles parcialmente comparables visibles (C-10.2.1); y **"Contexto de saturación observado"** por versión (máximo nivel completado observado y rango Min–Max de saturación), separado de las seis métricas y rotulado como descripción del experimento (no capacidad universal ni 7.ª métrica).
> - **Ejecuciones individuales:** sub-panel secundario colapsable (versión, repetición, estado, resultados persistidos) para trazar los promedios; no recalcula ni duplica M-06.
> - **Fuera de alcance (pendiente):** **CU-08** (gráficas/`BarChart`/SVG — no se añadió ninguna) y **CU-09** (PDF; no se añadió el botón "Generar reporte comparativo"). La superficie de comparación queda **tabular completa**.

> **Estado de implementación — CU-08 en M-07 (C-11.2, UI-25).** Se añade a M-07 un panel **"Visualización comparativa"** que grafica la comparación ya calculada (no recalcula, no consulta BD). **Small multiples:** un `BarChart` SVG independiente **por métrica** (nunca un único gráfico multi-métrica con eje compartido entre unidades distintas); cada gráfico compara solo **.NET 8 vs .NET 10** para una unidad, con nota "cada gráfico usa una escala propia… comparar alturas solo dentro del mismo gráfico". Reutiliza `BarChart`/`BarChartLayout`/`BarChartGroup`; se añadió el modo **`SharedScale`** (default false preserva el escalado por grupo existente; true = máximo global entre grupos). Formato de etiquetas alineado con la tabla (`0.####`, invariante).
> - **Datos:** el presentador mapea `ComparacionMetrica`/`ComparacionNivelEstres` a `BarChartGroup`; solo métricas **comparables con ambos promedios** se grafican (NoComparable se **omite**, nunca null→0; un cero real sí se grafica con etiqueta "0"). Respeta el enfoque de métrica de CU-06 (sin segundo selector). Tamaño aparece en el resumen como valor constante por versión.
> - **Estrés:** resumen (nivel máximo) con los mismos small multiples cuando es comparable; si no, sin gráfico de resumen (sin fabricar). **Por nivel:** un gráfico por métrica (excepto Tamaño) con un grupo por `UsuariosObjetivo` (ordenados) y **`SharedScale=true`** (alturas comparables entre cargas de la misma métrica); un nivel/métrica no comparable **no genera grupo** (sin interpolación ni barra ficticia). Saturación permanece **textual** (sin gráfico).
> - **Accesibilidad/responsive:** `BarChart` conserva `role="img"`/`aria-labelledby`/`<title>` descriptivo, leyenda y etiquetas de valor; grid responsive (`auto-fill minmax`); la **tabla permanece** como fuente detallada (la gráfica no duplica Diferencia/Variación/Pares). Mensaje "No hay métricas comparables disponibles para visualizar" cuando no hay nada graficable.
> - **CU-08 completo (C-11.3):** auditadas todas las superficies — M-05 (`LineChart` CPU en vivo), M-06 (tablas/`DataTable`), M-07 (tablas + small multiples + gráficos por nivel de Estrés) y M-01 ("Resumen comparativo reciente" conectado, ver UI-26). CU-08 (RF-10 tablas + RF-11 gráficos) queda **cerrado**. CU-09 sigue fuera de alcance.

### M-08 — Generación de reporte PDF (CU-09)
- **Panel "Configuración del reporte":**
  - Campos: Título del reporte · Evaluación base.
  - "Contenido a incluir": `CheckboxGroup` (una columna) con Datos generales de la evaluación · Resultados por ejecución · Promedios y consolidado · Tabla comparativa · Gráficos · Conclusión / observaciones.
  - Campo "Observaciones finales".
  - Botones: "Generar PDF" (primario), "Descargar" (verde), "Cancelar" (secundario).
- **Panel "Vista previa del reporte":** simulación de página con encabezado oscuro (título del reporte en mayúsculas), Proyecto, Evaluación, "Resultados principales" (`InfoBox` con variaciones %) y la nota "Se incluirán tablas, gráficos y conclusión."

---

## 6. Trazabilidad Mockup → CU → RF

| Mockup | CU | RF | Observación |
|---|---|---|---|
| M-01 Inicio | — (panel general) | — (apoya RNF Usabilidad y Navegación intuitiva) | Resume información de CU-01, CU-02, CU-04, CU-07/CU-08 y CU-09. |
| M-02 Incorporar proyecto | CU-01 | RF-01, RF-14 | |
| M-03 Migración | CU-02 | RF-02, RF-14 | |
| M-04 Configuración | CU-03 | RF-03, RF-14 | |
| M-05 Ejecución | CU-04 (+ CU-08 por el gráfico) | RF-04, RF-05, RF-06, RF-14 (+ RF-11) | |
| M-06 Resultados | CU-05, CU-06 (+ CU-08 por la tabla) | RF-07, RF-08 (+ RF-10) | |
| M-07 Comparación | CU-07, CU-08 | RF-09, RF-10, RF-11 | |
| M-08 Reportes | CU-09 | RF-12, RF-13 | |

CU-08 (Visualizar resultados) no tiene pantalla propia: se materializa mediante los componentes `DataTable`, `BarChart` y `LineChart` en M-05, M-06 y M-07 (y M-01).

### Flujo de navegación entre pantallas (acciones en los mockups)

```
M-01 ──Accesos rápidos──► M-02, M-03, M-04, M-06
M-01 ──"Crear nueva evaluación"──► M-04
M-04 ──"Iniciar evaluación"──► M-05
M-06 ──"Enviar a comparación"──► M-07
M-07 ──"Generar reporte comparativo"──► M-08
(todas) ──barra lateral──► cualquier pantalla
```

---

## 7. Reglas de implementación de la interfaz

1. Las páginas Blazor solo presentan datos y delegan en servicios de `Application`; no ejecutan procesos, no acceden a SQLite ni contienen lógica de negocio (cálculo de diferencias, variaciones o conclusiones incluidos).
2. Las métricas se muestran siempre con **nombre, valor y unidad** tomados del catálogo `Metrica` (RNF Claridad y Consistencia).
3. Nombres de métricas y tipos de prueba: usar exactamente los del catálogo (CPU, Memoria RAM, Tiempo de respuesta, Latencia, Throughput, Tamaño de la aplicación; Carga, Estrés, Concurrencia). Las abreviaturas de los gráficos de M-07 (Resp., Lat., Thr.) y "RAM" en M-05/M-06 son solo de presentación compacta.
4. Etiquetas, títulos y textos de botones según los mockups.
5. Cada pantalla se implementa junto con su caso de uso; la tarea de interfaz base solo incluye layout, navegación, componentes base y M-01.
6. No agregar pantallas, menús ni elementos que no aparezcan en los mockups (salvo el mensaje de estado requerido por RF-14).

### 7.1 Decisiones de implementación de interfaz (C-03)

| # | Decisión | Motivo |
|---|---|---|
| UI-01 | **Pantallas aún no implementadas:** los elementos de navegación y los accesos rápidos se muestran con su apariencia final como `<a role="link" aria-disabled="true">` **sin `href`**; los botones usan el atributo `disabled`. No se crean páginas *placeholder*. `NavMenu` y `QuickAccessCard` reciben el destino como `Href` opcional: al implementar cada pantalla basta con asignar su ruta. | Evitar 404 y rutas ficticias, manteniendo semántica accesible (no enfocables, anunciados como no disponibles). |
| UI-02 | **Subtítulo dinámico:** `PageHeader` publica la descripción mediante `SectionContent` y `AppHeader` la muestra con `SectionOutlet` (nombre de sección en `LayoutSections.PageSubtitle`). `PageHeader` también fija `<PageTitle>`. | Cada página declara título y descripción una sola vez, sin servicios globales. |
| UI-03 | **Escala de `BarChart`:** cada grupo (categoría/métrica) se escala respecto a su propio valor máximo; los valores se muestran con su unidad sobre cada barra y el gráfico tiene leyenda textual .NET 8 / .NET 10. La geometría se calcula en `BarChartLayout` (C# puro). | Las métricas tienen unidades distintas (%, MB, ms…) y no son comparables en una escala común; la leyenda y las etiquetas evitan depender solo del color. |
| UI-04 | **Responsive sin JavaScript:** ≤ 1200 px tarjetas de resumen en 2 columnas y paneles en 1 columna; ≤ 900 px la barra lateral pasa a una fila superior de elementos con ajuste de línea; ≤ 600 px todo apilado y se oculta el texto "Proyecto de Graduación" (el logo se conserva). | Evitar desbordamiento horizontal manteniendo el diseño de escritorio de los mockups. |
| UI-05 | **`OperationMessage` (RF-14):** se ubica bajo `PageHeader`; tipos Info, Success, Warning y Error con borde de color **y** prefijo textual ("Información:", "Correcto:", "Advertencia:", "Error:"). Error usa `role="alert"`; los demás `role="status"`. Nunca muestra detalles técnicos ni trazas. (M-02) | Accesibilidad y no depender solo del color. |
| UI-06 | **`StatusList`:** cada elemento muestra indicador de color **y** el texto del estado (Pendiente / Correcto / Error) debajo de la descripción, como en M-03. | No depender solo del color; mismo formato en M-02 y M-03. |
| UI-07 | **Componentes creados en M-02:** `OperationMessage`, `StatusList`, `KeyValueList` y `FormField` (etiqueta asociada al control mediante `for`/`id`). Los botones usan las clases CSS `.btn-primary` / `.btn-secondary` de `app.css`; no se creó `AppButton`. | Solo lo necesario para la pantalla; evitar generalización prematura. |
| UI-08 | **M-02 — comportamiento:** la inspección solo se ejecuta con "Validar ruta"; si la ruta se modifica después de validar, la inspección se descarta (estado Pendiente) y "Guardar proyecto" vuelve a deshabilitarse. Una inspección válida se informa con mensaje Info (no Success), reservando Success para el registro. Tras guardar, el formulario se limpia para evitar registros duplicados accidentales. | Evitar guardar información que no corresponde a la ruta mostrada y no prometer más de lo validado. |
| UI-09 | **M-02 — "Seleccionar archivo..." (C-04.1):** botón secundario a la derecha del campo "Ruta del proyecto" (apilado debajo en ≤ 600 px); el campo sigue siendo editable. Seleccionar, Validar y Guardar son tres acciones distintas: seleccionar coloca la ruta absoluta, descarta cualquier inspección anterior y **no** valida automáticamente. Cancelar el selector no cambia nada ni muestra mensajes. El botón se deshabilita mientras el selector está abierto. Solo se usa `OperationMessage` si el selector no puede abrirse (Error) o la plataforma no es Windows (Warning). El estado de M-02 vive en `IncorporarProyectoFormulario` (C# puro) para poder probarlo sin frameworks de UI. | Usabilidad sin perder el ingreso manual ni la validación explícita de CU-01. |
| UI-10 | **M-03 — comportamiento (C-05):** "Seleccionar carpeta..." (a la derecha del campo, apilado en ≤ 600 px) solo rellena el directorio de salida, que sigue siendo editable; nunca inicia la migración. "Iniciar migración experimental" se habilita con proyecto seleccionado y directorio no vacío; las validaciones definitivas ocurren al iniciar. Durante la ejecución se deshabilitan el proyecto, la ruta, el selector y el botón de inicio (texto "Migración en curso..."). Las cinco etapas se actualizan en `StatusList` y la salida de los comandos se agrega a `LogConsole`. Al terminar se muestra `OperationMessage` (éxito o error), el resumen (Proyecto, Ruta migrada, SDK .NET 10 utilizado, Estado) y "Ver detalle"/"Ocultar detalle" (versiones, fechas locales y detalle final) solo si existe un registro de migración. Si no hay proyectos registrados, el `select` se deshabilita y se enlaza a "Incorporar proyecto". El mensaje de éxito no afirma mejoras de rendimiento. El estado vive en `MigracionFormulario` (C# puro, testeable). Al salir de la página se cancela la migración en curso. | Coherencia con M-02 (UI-09) y con el flujo del mockup sin prometer más de lo validado. |
| UI-11 | **Componentes de M-03:** `LogConsole` (bloque oscuro con título opcional, `role="log"`, monoespaciado, altura fija con scroll interno; las líneas se muestran como texto, nunca como HTML; `flex-direction: column-reverse` mantiene visible la última línea sin JavaScript) y `StatusList` ampliado con el estado **En proceso** (indicador ámbar + texto). Las clases de disposición de formularios compartidas por M-02 y M-03 (`.form-grid`, `.form-row`, `.form-help`, `.input-with-action`) pasan a `app.css`. | Reutilización sin duplicar estilos; accesibilidad. |
| UI-12 | **M-04 — selector de "Migración .NET 10" (C-06):** añadido respecto al mockup para trazabilidad; depende del proyecto, lista solo migraciones **Exitosas** con etiqueta legible ("Migración #Id — fecha local") y muestra la ruta de la copia como nota secundaria. | El mockup solo mostraba Proyecto; una configuración debe referirse a una migración concreta y reproducible. |
| UI-13 | **M-04 — comportamiento (C-06):** la validación definitiva la hace Application; M-04 solo gestiona estado de formulario. Al cambiar de proyecto se descarta la migración seleccionada y se recargan solo las migraciones exitosas de ese proyecto; si el proyecto no tiene ninguna, se muestra un aviso y se bloquea el guardado. Los campos pueden iniciar vacíos (sin valores por defecto del mockup). Tras un guardado correcto el formulario se conserva y "Guardar" se deshabilita hasta que el Usuario modifique algún campo (`EstaGuardado`/dirty); cada guardado crea una nueva `ConfiguracionEvaluacion` (no hay edición). Si no hay proyectos, todo el formulario se deshabilita. El estado vive en `ConfiguracionFormulario` (C# puro, testeable). | Coherencia con M-02/M-03; evitar guardados duplicados accidentales sin un sistema de tracking complejo. |
| UI-14 | **Componentes de M-04 (C-06; tipos de prueba actualizado en C-06.2):** `CheckboxGroup` (casillas reales en rejilla 3×2 → 2 → 1) para métricas (selección múltiple); el tipo de prueba usa `SingleOptionToggleGroup` (selección única, ver UI-16). "Iniciar evaluación" usa `.btn-success`. Nombres siempre desde el catálogo. | Accesibilidad; nombres desde el catálogo. |
| UI-15 | **M-04 — paneles dinámicos de parámetros (C-06.1):** al seleccionar un tipo de prueba se muestra su panel de parámetros; al desmarcarlo se oculta. Los valores pueden permanecer en el estado local durante la edición, pero **solo se persisten los de los tipos seleccionados al guardar**. Carga, Estrés y Concurrencia tienen conjuntos de campos distintos (Concurrencia no tiene Ramp-up). Se añaden "Ruta / endpoint de prueba" (relativa), "Método HTTP: GET" de solo lectura y `Ejecuciones` con `min=1 max=3`; "Observaciones" es un área de texto global. El estado vive en `ConfiguracionFormulario` con un sub-modelo por tipo (`ParametrosCargaForm`/`EstresForm`/`ConcurrenciaForm`), testeable sin framework UI. | Eliminar la ambigüedad de parámetros compartidos; cada parámetro pertenece a un tipo de prueba. |
| UI-26 | **M-01 — resumen comparativo reciente CU-08 (C-11.3):** el `BarChart` vacío de "Resumen comparativo reciente" se conecta a datos reales: la evaluación **más reciente con comparación de resumen válida** (recorre el listado de CU-05 ordenado `FechaCreacion`/`Id` desc y toma la primera con resultados y alguna métrica graficable), como **small multiples** (`SharedScale=false`) reutilizando `ComparacionChartMapper` (compartido con M-07). Contexto (evaluación·proyecto·tipo·fecha) y acción "Ver comparación" → `/comparacion/{id}`. Solo resumen (nunca niveles/saturación en Inicio; Estrés saturado sin resumen no fabrica gráfico). Estados cargando/sin datos (`EmptyState`, nunca SVG vacío)/error (mensaje seguro). Presentador testeable `InicioResumenModelo`. Con esto **CU-08 queda completo** (M-05/M-06/M-07/M-01). Las tarjetas de resumen y "Actividad reciente" siguen en estado vacío (B-04/B-05/B-06). | Cerrar CU-08 conectando la última superficie gráfica sin placeholder engañoso, reutilizando la metodología y el mapeo de M-07. |
| UI-25 | **M-07 — visualización comparativa CU-08 (C-11.2):** panel "Visualización comparativa" con **small multiples** (un `BarChart` SVG por métrica; nunca un eje único entre unidades distintas). Reutiliza `BarChart`/`BarChartLayout`/`BarChartGroup` + modo nuevo **`SharedScale`** (default false; true para Estrés por nivel = máximo global entre cargas). Solo grafica métricas comparables con ambos promedios (NoComparable omitida, nunca null→0; cero real con etiqueta "0"); respeta el enfoque de CU-06 sin segundo selector. Estrés: resumen (si comparable) + un gráfico por métrica (excepto Tamaño) con grupos por `UsuariosObjetivo` y escala compartida; huecos sin barra ficticia; saturación solo textual. Mapeo en `ComparacionResultadosModelo` (`GraficosResumen`, `GraficosNivelesEstres`, `GraficoNivelMetrica`); geometría en el layout; la tabla permanece y no se duplica. Formato `0.####` invariante. Mensaje vacío cuando no hay nada graficable. **CU-08 aún no cerrado:** falta auditar M-01 (C-11.3). | Visualizar fielmente la comparación de CU-07 sin mezclar unidades ni recalcular, reutilizando el gráfico existente con una extensión mínima. |
| UI-24 | **M-07 — comparación .NET 8 vs .NET 10 (CU-07, C-10.3):** página `/comparacion/{ConfiguracionId:int}` de **solo lectura** que reutiliza el detalle de CU-05 y delega el cálculo en `IComparacionResultadosService` (sin aritmética en Web). Comparación **intra-evaluación** (.NET 8 base vs .NET 10 experimental). Acceso vía botón "Enviar a comparación" en M-06 (con el `ConfiguracionId`; propaga `?metricaId=` si CU-06 tiene enfoque). Presentador testeable `ComparacionResultadosModelo` (carga/inexistente/error/sin resultados/parcial/no disponible, enfoque de métrica, formato). Tabla Métrica·.NET 8·.NET 10·Diferencia·Variación %·Interpretación·Pares: signo matemático conservado, "No calculable" para base cero, interpretación con texto (no solo color). Conclusión descriptiva por conteos **sin ganador global**. Estrés: resumen (o aviso por saturación), comparación por nivel (por `UsuariosObjetivo`) y contexto de saturación (máximo completado observado + rango Min–Max), separado de las métricas. Sub-panel de ejecuciones individuales para trazabilidad. **No** se implementan gráficas (CU-08) ni PDF (CU-09). | Presentar fielmente la comparación calculada en Application, con transparencia metodológica, sin recalcular en Web ni adelantar CU-08/CU-09. |
| UI-23 | **M-06 — filtro por métrica (CU-06, C-09):** sobre el detalle ya consultado se añade un selector único "Filtrar por métrica" (opción por defecto "Todas las métricas") en el panel "Ejecuciones". Es **puramente de presentación** (no recalcula, no reconsulta la base, no altera DTO/esquema ni persiste la selección). Las opciones son las **métricas seleccionadas en la evaluación** (no el catálogo global) y se compara por `MetricaId` estable, no por texto. Con filtro activo se restringen tanto el resumen de cada ejecución como las métricas por nivel de Estrés; las ejecuciones y niveles **permanecen visibles** aunque carezcan de la métrica (nota "No hay un resultado registrado para esta métrica" / "Sin resultado para esta métrica", nunca un cero ficticio) y `Tamaño` no aparece por nivel. Se distingue "evaluación sin resultados" (caso A) de "filtro sin coincidencias" (caso B, mensaje propio). El filtro se reinicia a "Todas" al cambiar de evaluación y al cerrar el detalle, conservando la garantía "última selección gana". Lógica en `ConsultaResultadosModelo` (`MetricaFiltroId`, `ResultadosVisibles`, `FiltroSinCoincidencias`). | Permitir enfocar la lectura en una métrica sin modificar lo persistido ni introducir comparación/estadística (reservada a CU-07). |
| UI-22 | **M-06 — consulta de resultados (CU-05, C-08.2):** página `/resultados` de **solo lectura** que consume `IConsultaResultadosService` (Web no accede a EF). Listado maestro (Evaluación/Proyecto/Tipo/Fecha/Estado/Resultados/Ver) con orden fijado en Application/Infrastructure (FechaCreacion desc, Id desc) y badge de disponibilidad con texto; detalle maestro-detalle con información general, métricas seleccionadas, ejecuciones .NET 8/.NET 10 como registros independientes (resumen con unidad del catálogo, niveles de estrés por `NumeroNivel`, condiciones en sección secundaria). Lógica en un presentador testeable `ConsultaResultadosModelo` (última selección gana, estados de carga/vacío/inexistente/sin resultados). Formato de valores: hasta 4 decimales sin relleno, cultura invariante, sin alterar el valor persistido. No se implementan filtros (CU-06), comparación/estadística .NET 8–10 (CU-07) ni gráficas (CU-08). | Consultar y mostrar fielmente lo persistido, sin recalcular ni comparar. |
| UI-21 | **M-05 — finalización de CU-04: ejecución server-side, Detener y LineChart (C-07.5):** la evaluación la gestiona un `EvaluationExecutionSessionManager` singleton del host Web (no el circuito); M-05 inicia/consulta/cancela/visualiza vía snapshots con *polling* de ~1 s y reconstruye la sesión activa tras F5/reconexión (o el estado persistido si no hay sesión). Botón **"Detener evaluación"** (`.btn-danger`) visible solo con evaluación activa; pasa a "Cancelando..." y la unidad activa queda Cancelada sin resultados. Nuevo panel **"Evolución de consumo de CPU"** con `LineChart` SVG propio (serie transitoria, se reinicia al cambiar versión/repetición, no por nivel/ronda; no se persiste). CPU/RAM en vivo se mantienen. | Completar M-05/CU-04 (B-13 y B-14) sin atar la evaluación al navegador ni usar librerías de gráficos. |
| UI-20 | **M-05 — ejecución de Concurrencia por rondas (C-07.4):** cuando el tipo es Concurrencia, la cabecera de estado muestra "Concurrencia · Ronda X de Y · N usuarios concurrentes" con CPU/RAM en vivo, y la bitácora registra la sincronización de cada ronda con su desfase de inicio (diagnóstico). Se evita la redacción "N usuarios durante X s" (Concurrencia no usa duración sostenida). Al finalizar se reutiliza la tabla de resultados por Ejecucion (seis métricas + OK/Fallidas); **no** hay tabla por ronda. Sin estadísticas P95/Throughput en vivo. | Reflejar la naturaleza por rondas sin inventar una entidad por ronda ni streaming de percentiles. |
| UI-19 | **M-05 — ejecución de Estrés por niveles (C-07.3):** cuando el tipo es Estrés, la cabecera de estado muestra la fase "Nivel de estrés · Nivel N de M · K usuarios concurrentes" y CPU/RAM en vivo; al terminar cada nivel se agrega una fila a un panel **"Niveles de estrés"** (Versión, Rep., Nivel, Usuarios, CPU, RAM, Tiempo resp. prom., Latencia P95, Throughput, OK/Fallidas, Estado). Los estados de nivel se colorean (Completado/Saturado/Fallido/Cancelado) y la saturación se anuncia con un aviso claro, distinto de un error técnico. El tamaño no se repite por nivel. Latencia/Throughput solo como valor final del nivel (sin streaming). El estado de la vista vive en `EjecucionFormulario`. | Mostrar la progresión y la saturación sin sacar conclusiones comparativas (reservadas a CU-07) ni añadir infraestructura de tiempo real. |
| UI-18 | **M-05 — visualización final de las seis métricas (C-07.2):** la tabla de resultados por `Ejecucion` añade columnas **Latencia P95 (ms)**, **Throughput (req/s)** y **Tamaño (MB)** junto a CPU/RAM/Tiempo de respuesta promedio y OK/Fallidas. Se rotula "Tiempo resp. prom." y "Latencia P95" para distinguir Mean de P95 (D-39). Latencia/Throughput/Tamaño se presentan solo como **valor final** por ejecución (sin estadísticas en vivo ni sinks de streaming); CPU/RAM siguen en vivo durante la medición. El redondeo es solo de presentación (`0.##`); los valores persistidos no se alteran. Las unidades provienen del catálogo `Metrica`. | Completar la presentación de las seis métricas sin introducir infraestructura de tiempo real (reservada a C-07.5). |
| UI-17 | **M-04→M-05 — implementación parcial de la ejecución (C-07.1):** en M-04, "Iniciar evaluación" (`.btn-success`) queda **habilitado tras guardar** y navega a M-05 (`/ejecucion/{ConfiguracionId:int}`). M-05 se implementa **parcialmente**: estado general (`KeyValueList` + badge), `ProgressBar`, fase en curso, CPU/RAM en vivo durante la medición, `LogConsole` y tabla de resultados por `Ejecucion`. Quedan **pendientes** `MetricTile` de Latencia/Throughput, `LineChart` de evolución de CPU (B-14) y el botón "Detener evaluación" en la UI (B-13); el estado de la vista vive en `EjecucionFormulario`, testeable sin framework UI. | El motor C-07.1 solo cubre Carga + CPU/RAM/Tiempo de respuesta (D-35..D-37); la UI acompaña exactamente ese alcance. |
| UI-16 | **M-04 — selección única de tipo de prueba (C-06.2):** el tipo de prueba pasa a **selección exclusiva** con `SingleOptionToggleGroup` (`role="radiogroup"`/`role="radio"`, `aria-checked`, marca ●/○, no solo color); elegir un tipo desactiva el anterior y vuelve a pulsarlo lo deselecciona. Solo hay **un panel de parámetros visible** a la vez. Los valores escritos para cada tipo pueden conservarse en el estado local al cambiar de tipo, pero al guardar solo se persisten los del tipo seleccionado. `TipoPruebaId` (int?, selección única) forma parte del estado dirty de `ConfiguracionFormulario`. Se reemplazó `OptionToggleGroup` por `SingleOptionToggleGroup` (sin consumidores de selección múltiple). | Una configuración corresponde a un único tipo de prueba (D-34). |

---

## 8. Brechas y decisiones pendientes

Elementos de los mockups que **no están definidos** en `PROJECT_CONTEXT.md` o en el modelo de datos actual. Ninguno se implementará por suposición; se resolverán al llegar a la tarea correspondiente.

### 8.1 Layout (tarea de interfaz base / M-01) — RESUELTAS
| # | Brecha | Decisión |
|---|---|---|
| B-01 | Nombre personal ("Manuel Zúñiga") en el encabezado. | **Resuelta.** No se muestra ningún nombre personal ni información que sugiera una sesión de usuario. Se reemplaza por el texto estático **"Proyecto de Graduación"**, únicamente institucional/informativo; no representa al actor `Usuario`. |
| B-02 | Logo institucional. | **Resuelta.** Se usa exclusivamente el logo embebido en `docs/mockups/Mockups-Interfaces.drawio`, extraído y guardado como recurso estático en `src/DotNetPerformanceComparator.Web/wwwroot/images/` (p. ej. `logo-umg.webp`). No se descarga ningún logo de Internet ni se mantiene como base64 en componentes Razor o CSS. |
| B-03 | Gráficos (M-01, M-05, M-07). | **Resuelta.** Componentes Blazor propios basados en SVG, sin librerías NuGet, npm ni JavaScript. Reutilizables, con datos por parámetros y limitados a lo que exigen los mockups. `BarChart` con M-01; `LineChart` solo cuando llegue M-05. |

### 8.2 M-01 Inicio — PENDIENTES (B-07 resuelta)
M-01 usa estados vacíos (ver sección 5, M-01) hasta que estas brechas se conecten a información real conforme se implementen los CU correspondientes.

| # | Brecha | Comentario |
|---|---|---|
| B-04 | "Proyecto activo". | El modelo no tiene concepto de proyecto activo. Opción: el último `ProyectoDotNet` registrado. |
| B-05 | "Último reporte" y filas "Reporte" en "Actividad reciente". | `Reporte` es transitorio y **no se persiste** (decisión de diseño). No hay fuente para estos datos sin cambiar el modelo. |
| B-06 | "Actividad reciente". | No existe entidad de bitácora/actividad. Podría derivarse de `ProyectoDotNet.FechaRegistro`, `Migracion` y `Ejecucion`, salvo los reportes (B-05). |
| B-07 | Mensajes de error (RF-14). | **Resuelta.** Los estados vacíos se implementaron con `EmptyState` (M-01, M-02) y los mensajes de estado/error con `OperationMessage` (UI-05). |

### 8.3 Pantallas de casos de uso — RESUELTAS
| # | Mockup | Brecha | Decisión |
|---|---|---|---|
| B-11 | M-04 | "Nombre de la evaluación" no existe en `ConfiguracionEvaluacion`. | **Resuelta (C-06).** `ConfiguracionEvaluacion.Nombre` se agrega de forma **permanente** (persistido, obligatorio, sin unicidad; se reutilizará en M-06 y M-08). Además M-04 incorpora un selector de **Migración .NET 10** como evolución del mockup para garantizar trazabilidad experimental: cada configuración queda asociada a una migración exitosa concreta (ver D-27, UI-12). |
| B-12 | M-04 | Warm-up, Usuarios virtuales, Duración, Ramp-up, Intervalo, Observaciones. | **Resuelta (C-06; actualizada en C-06.1 y C-06.2).** Los parámetros pertenecen a `ConfiguracionEvaluacion` (`ParametroEvaluacion` por `ConfiguracionId`); la asociación con el tipo es inequívoca porque **cada configuración posee exactamente un `TipoPrueba`** (ver D-34). **Cada tipo de prueba (Carga, Estrés, Concurrencia) tiene un conjunto de parámetros diferente** (ver D-32). `Observaciones` describe la evaluación completa y es `ConfiguracionEvaluacion.Observaciones` (no es un parámetro). Claves internas en `ClavesParametroCarga`/`Estres`/`Concurrencia`. |

| # | Mockup | Brecha | Decisión |
|---|---|---|---|
| B-08 | M-02 | "Tipo de aplicación", "Framework objetivo", "Carpetas clave", "Archivos detectados" no existen en `ProyectoDotNet`. | **Resuelta (C-04).** Son información **transitoria** del resultado de inspección (`ProjectInspectionResult` en Application), mostrada en M-02 y **no persistida**. `ProyectoDotNet` conserva solo Id, Nombre, RutaOriginal, VersionBase, EstadoValidacion y FechaRegistro; no se crean tablas nuevas. |
| B-09 | M-02 (M-03) | Botones "Examinar carpeta" / "Examinar". | **Resuelta para M-02 (C-04).** Por las restricciones de seguridad del navegador no se obtiene la ruta mediante `<input type="file">`, File System Access API, diálogos nativos ni librerías. El Usuario escribe la **ruta absoluta del `.csproj`** (la herramienta se ejecuta localmente en Windows 11) y el botón "Examinar carpeta" se sustituye por **"Validar ruta"**, que ejecuta la inspección. Evolución técnica justificada del mockup. **Ampliación (C-04.1):** la ruta puede introducirse manualmente o seleccionarse mediante el selector nativo de archivos de Windows ("Seleccionar archivo...") cuando la herramienta se ejecuta localmente en Windows 11. El navegador por sí solo no proporciona la ruta absoluta requerida. La validación continúa siendo una acción independiente. **M-03 (C-05):** el botón "Examinar" del directorio de salida se implementa como **"Seleccionar carpeta..."** con el selector nativo de carpetas de Windows (mismo enfoque); la ruta también puede escribirse manualmente y seleccionar una carpeta no inicia la migración. |
| B-10 | M-03 | Paso "Ejecución de Upgrade Assistant". | **Resuelta (C-05).** La herramienta realiza una migración experimental controlada (copia + cambio de framework + restore + build) sin Upgrade Assistant; el paso se sustituye por **"Actualización del framework objetivo a .NET 10"** y el progreso usa las cinco etapas de la sección 5 (M-03). La documentación histórica/comparativa sobre Upgrade Assistant no se modifica; la decisión solo afecta a la implementación de la herramienta (ver D-21). |

### 8.4 Pantallas de casos de uso — PENDIENTES
| # | Mockup | Brecha | Comentario |
|---|---|---|---|
| B-13 | M-05 | Botón "Detener evaluación". | **Resuelta (C-07.5; responsividad afinada en C-07.5.1).** Cancelación **server-side** independiente del circuito Blazor: el botón "Detener evaluación" solicita `RequestCancellation` al `EvaluationExecutionSessionManager` (idempotente), que cancela el `CancellationTokenSource` del que es dueño. La cancelación es **responsiva**: el token llega a Infrastructure y, dentro del escenario NBomber, `StopCurrentTest(...)` + un token enlazado al `SendAsync`/`GetAsync` interrumpen la fase sin esperar su duración restante (verificado: Carga de 35 s detenida a ~18 s). La unidad activa queda Cancelada sin resultados parciales; las previas Completadas se conservan; nunca Fallida por cancelación. Ver D-47, D-49, UI-21. |
| B-14 | M-05 | Valores en vivo y gráfico de evolución de CPU. | **Resuelta (C-07.5).** Durante la medición se muestran CPU/RAM en vivo y un **`LineChart`** SVG propio ("Evolución de consumo de CPU") con la serie **transitoria** de la Ejecucion actual (se reinicia al cambiar versión/repetición; no se persiste ni recalcula el valor oficial). Adaptado a Carga/Estrés/Concurrencia. Ver D-48, D-49, UI-21. |
| B-15 | M-06 | Filtros por Proyecto, Tipo de prueba y Rango de fecha. | **Parcialmente resuelta (C-09).** CU-06/RF-08 solo exigen **filtrar por métrica**, que ya se implementa (ver UI-23). Los filtros por Proyecto/Tipo/Fecha del mockup quedan **fuera de alcance** mientras no se confirmen como requisito. |
| B-16 | M-06, M-07 | Promedios, diferencia y variación porcentual. | Fórmulas no definidas en el contexto (p. ej. variación % = (NET10 − NET8) / NET8 × 100). Definir en CU-07. |
| B-17 | M-07 | "Conclusión automática". | Requiere reglas de interpretación (qué dirección es "mejor" por métrica) no definidas. Definir en CU-07. |
| B-18 | M-08 | Vista previa del reporte. | Definir si es una representación HTML aproximada o una previsualización del PDF real. Decidir en CU-09. |
