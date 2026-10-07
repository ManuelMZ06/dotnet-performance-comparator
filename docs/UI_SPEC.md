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
| `OptionToggleGroup` | Grupo de botones seleccionables (selección múltiple). | M-04 (tipos de prueba) |
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

### M-02 — Incorporar proyecto .NET 8 (CU-01)
- **Panel "Datos del proyecto":**
  - Campos: Nombre del proyecto · Tipo de aplicación · Ruta del proyecto (ruta a `.csproj`, ancho completo).
  - Botón secundario "Examinar carpeta".
  - Campos de solo lectura/detectados: Versión detectada · Framework objetivo · Estado.
  - Sub-panel "Validaciones realizadas": `StatusList` con:
    - Se localizó el archivo .csproj
    - El proyecto corresponde a una aplicación web ASP.NET
    - La versión base es compatible con el proceso de evaluación
    - La estructura del proyecto permite continuar con la migración experimental
- **Panel "Vista previa del proyecto":** `KeyValueList` — Nombre, Framework, Carpetas clave, Archivos detectados.
- **Panel "Acciones":** "Guardar proyecto" (primario), "Cancelar" (secundario) y texto: "Al guardar, el sistema registrará la aplicación y la dejará disponible para el proceso de migración experimental y configuración de evaluaciones."

### M-03 — Migración experimental a .NET 10 (CU-02)
- **Panel "Selección y parámetros de migración":** Proyecto base · Versión origen (.NET 8) · Versión destino (.NET 10) · Directorio de salida + botón "Examinar" · botón primario "Iniciar migración experimental".
- **Panel "Bitácora de ejecución":** `LogConsole` con los mensajes del proceso.
- **Panel "Progreso de migración":** `StatusList` con pasos y estado (Correcto / En proceso / Pendiente):
  1. Validación del proyecto
  2. Respaldo de versión original
  3. Ejecución de Upgrade Assistant
  4. Actualización de dependencias
  5. Build preliminar (.NET 10)
- Botón secundario "Ver detalle".

### M-04 — Configuración de evaluación (CU-03)
- **Panel "Datos generales de la evaluación":**
  - Campos: Nombre de la evaluación · Proyecto · Ejecuciones · Warm-up.
  - "Tipo de pruebas": `OptionToggleGroup` con Carga · Estrés · Concurrencia (selección múltiple).
  - "Métricas a recopilar": `CheckboxGroup` 3×2 con CPU · Memoria RAM · Tiempo de respuesta · Latencia · Throughput · Tamaño de la aplicación.
  - Sub-panel "Parámetros específicos": Usuarios virtuales · Duración (s) · Ramp-up (s) · Intervalo · Observaciones.
- **Acciones:** "Guardar" (primario), "Iniciar evaluación" (verde) → M-05.

### M-05 — Ejecución de evaluación (CU-04)
- **Panel "Estado general":** `KeyValueList` (Proyecto, Prueba, Versión evaluada, Ejecución actual "n de N", Estado) + `ProgressBar` con porcentaje.
- **4 `MetricTile`:** CPU (%), RAM (MB), Latencia (ms), Throughput (req/s), cada una con su color.
- **Panel "Evolución de consumo de CPU":** `LineChart`.
- **Panel "Bitácora de ejecución":** `LogConsole`.
- **Acción:** botón rojo "Detener evaluación".

### M-06 — Consulta de resultados (CU-05, CU-06, CU-08)
- **Panel "Filtros":** Proyecto · Métrica · Tipo de prueba · Rango de fecha; botones "Aplicar" (primario) y "Limpiar" (secundario). Nota: "También pueden visualizarse resultados individuales o promedios por evaluación."
- **Panel "Resultados encontrados":** `DataTable` con Fecha · Evaluación · Versión · Métrica · Valor (valor **con unidad**). Fila seleccionable.
- **Sub-panel "Detalle del resultado seleccionado":** Evaluación, Tipo, Métrica; Promedio .NET 8 / .NET 10; Diferencia y Variación porcentual.
- **Acción:** "Enviar a comparación" (primario) → M-07.

### M-07 — Comparación y visualización (CU-07, CU-08)
- **Panel "Resumen comparativo":**
  - `DataTable`: Métrica · .NET 8 · .NET 10 · Diferencia · Variación %.
  - `InfoBox` "Conclusión automática" con texto interpretativo.
  - Sub-panel "Gráfico comparativo por métricas": `BarChart` agrupado (CPU, RAM, Resp., Lat., Thr.) con leyenda .NET 8 / .NET 10.
  - Sub-panel "Ejecuciones individuales": `DataTable` Ejecución · .NET 8 · .NET 10.
- **Acción:** "Generar reporte comparativo" (primario) → M-08.

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

---

## 8. Brechas y decisiones pendientes

Elementos de los mockups que **no están definidos** en `PROJECT_CONTEXT.md` o en el modelo de datos actual. Ninguno se implementará por suposición; se resolverán al llegar a la tarea correspondiente.

### 8.1 Layout (tarea de interfaz base / M-01) — RESUELTAS
| # | Brecha | Decisión |
|---|---|---|
| B-01 | Nombre personal ("Manuel Zúñiga") en el encabezado. | **Resuelta.** No se muestra ningún nombre personal ni información que sugiera una sesión de usuario. Se reemplaza por el texto estático **"Proyecto de Graduación"**, únicamente institucional/informativo; no representa al actor `Usuario`. |
| B-02 | Logo institucional. | **Resuelta.** Se usa exclusivamente el logo embebido en `docs/mockups/Mockups-Interfaces.drawio`, extraído y guardado como recurso estático en `src/DotNetPerformanceComparator.Web/wwwroot/images/` (p. ej. `logo-umg.webp`). No se descarga ningún logo de Internet ni se mantiene como base64 en componentes Razor o CSS. |
| B-03 | Gráficos (M-01, M-05, M-07). | **Resuelta.** Componentes Blazor propios basados en SVG, sin librerías NuGet, npm ni JavaScript. Reutilizables, con datos por parámetros y limitados a lo que exigen los mockups. `BarChart` con M-01; `LineChart` solo cuando llegue M-05. |

### 8.2 M-01 Inicio — PENDIENTES
M-01 usa estados vacíos (ver sección 5, M-01) hasta que estas brechas se conecten a información real conforme se implementen los CU correspondientes.

| # | Brecha | Comentario |
|---|---|---|
| B-04 | "Proyecto activo". | El modelo no tiene concepto de proyecto activo. Opción: el último `ProyectoDotNet` registrado. |
| B-05 | "Último reporte" y filas "Reporte" en "Actividad reciente". | `Reporte` es transitorio y **no se persiste** (decisión de diseño). No hay fuente para estos datos sin cambiar el modelo. |
| B-06 | "Actividad reciente". | No existe entidad de bitácora/actividad. Podría derivarse de `ProyectoDotNet.FechaRegistro`, `Migracion` y `Ejecucion`, salvo los reportes (B-05). |
| B-07 | Mensajes de error (RF-14). | Ningún mockup los muestra; se propondrá un estilo coherente con los tokens. Los estados vacíos de M-01 ya quedaron definidos (sección 5). |

### 8.3 Pantallas de casos de uso — PENDIENTES
| # | Mockup | Brecha | Comentario |
|---|---|---|---|
| B-08 | M-02 | "Tipo de aplicación", "Framework objetivo", "Carpetas clave", "Archivos detectados" no existen en `ProyectoDotNet`. | Pueden ser datos detectados solo para mostrar (no persistidos) o requerir cambio de modelo. Decidir en CU-01. |
| B-09 | M-02, M-03 | Botones "Examinar carpeta" / "Examinar". | Blazor Server se ejecuta en el navegador; no dispone de un selector nativo de carpetas del servidor. Decidir mecanismo en CU-01 (p. ej. ingreso de ruta + validación). |
| B-10 | M-03 | Paso "Ejecución de Upgrade Assistant". | La herramienta concreta de migración no está definida en el contexto ("herramientas del ecosistema .NET"). Decidir en CU-02. |
| B-11 | M-04 | "Nombre de la evaluación" no existe en `ConfiguracionEvaluacion`; M-06 y M-08 lo muestran ("Eval. 01"). | Requiere agregar el campo o definir cómo se identifica una evaluación. Decidir en CU-03. |
| B-12 | M-04 | Warm-up, Usuarios virtuales, Duración, Ramp-up, Intervalo, Observaciones. | Encajan como `ParametroEvaluacion` (clave/valor); confirmar en CU-03. |
| B-13 | M-05 | Botón "Detener evaluación". | CU-04 no describe la detención; coherente con "ejecución incompleta no se registra como válida". Confirmar alcance en CU-04. |
| B-14 | M-05 | Valores en vivo y gráfico de evolución de CPU. | Implica actualización en tiempo real durante la ejecución; detalle técnico se define en CU-04. |
| B-15 | M-06 | Filtros por Proyecto, Tipo de prueba y Rango de fecha. | CU-06/RF-08 solo exigen filtrar por métrica. Confirmar si los demás filtros forman parte del alcance (CU-05). |
| B-16 | M-06, M-07 | Promedios, diferencia y variación porcentual. | Fórmulas no definidas en el contexto (p. ej. variación % = (NET10 − NET8) / NET8 × 100). Definir en CU-07. |
| B-17 | M-07 | "Conclusión automática". | Requiere reglas de interpretación (qué dirección es "mejor" por métrica) no definidas. Definir en CU-07. |
| B-18 | M-08 | Vista previa del reporte. | Definir si es una representación HTML aproximada o una previsualización del PDF real. Decidir en CU-09. |
