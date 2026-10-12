# Desarrollo

Gobierno ligero del repositorio **DotNetPerformanceComparator**. Las reglas técnicas y de arquitectura están en [AGENTS.md](../AGENTS.md) y la fuente de verdad en [docs/PROJECT_CONTEXT.md](PROJECT_CONTEXT.md).

## Rama principal

`main` debe mantenerse siempre **compilable y con todas las pruebas correctas**.

## Flujo de trabajo

```
crear rama feature/fix/docs → implementar → restore/build/test → commit → PR → CI → merge a main
```

Como proyecto individual, no se exige revisión de una segunda persona, pero sí que **CI esté en verde** antes de integrar a `main`.

## Convención de ramas

| Prefijo | Uso | Ejemplo |
|---------|-----|---------|
| `feature/<descripción>` | Desarrollo relevante (típicamente un CU) | `feature/cu05-consulta-resultados` |
| `fix/<descripción>` | Correcciones puntuales | `fix/calculo-latencia` |
| `docs/<descripción>` | Documentación | `docs/gobierno-repositorio` |

No se usan `develop`, `release/*` ni `hotfix/*` salvo que exista una necesidad real futura.

## Convención de commits

Formato: `<tipo>: <descripción en español>`.

Tipos admitidos: `feat`, `fix`, `docs`, `test`, `refactor`, `chore`. No se exigen *scopes* y no se reescribe el historial.

```
feat: implementar consulta de resultados
fix: corregir cálculo de latencia
docs: actualizar documentación de arquitectura
test: agregar pruebas de comparación
refactor: simplificar orquestación de ejecución
chore: actualizar configuración del repositorio
```

## Política de merge

- `feature/*` → Pull Request → CI correcto → merge a `main`.
- **Merge normal** cuando la rama tenga pocos commits limpios y bien delimitados.
- **Squash** cuando la rama acumule muchos commits intermedios/WIP.
- No se impone una única estrategia todavía; se decide conscientemente por rama.

## Validación local

Antes de hacer commit/PR, confirmar localmente lo mismo que ejecuta CI:

```bash
dotnet tool restore
dotnet restore
dotnet build --configuration Release
dotnet test --configuration Release
```

Opcional (estado de migraciones EF Core):

```bash
dotnet ef migrations list --project src/DotNetPerformanceComparator.Infrastructure --startup-project src/DotNetPerformanceComparator.Web
dotnet ef migrations has-pending-model-changes --project src/DotNetPerformanceComparator.Infrastructure --startup-project src/DotNetPerformanceComparator.Web
```

## Integración continua (CI)

Workflow: [.github/workflows/ci.yml](../.github/workflows/ci.yml).

- **Runner:** `windows-latest` (Windows es el entorno principal soportado; hay infraestructura específica de Windows).
- **SDK:** .NET **8.0.425** (coincide con `global.json`, para un CI reproducible).
- **Disparadores:** `push` y `pull_request` hacia `main`, más `workflow_dispatch` (ejecución manual).
- **Permisos:** `contents: read` (mínimo privilegio).
- **Pasos:** checkout → setup .NET 8 → `dotnet --info` → `dotnet tool restore` → `dotnet restore` → `dotnet build -c Release --no-restore` → `dotnet test -c Release --no-build`.
- **No** instala .NET 10, **no** ejecuta `dotnet ef database update`, **no** ejecuta la herramienta ni pruebas de carga reales: la suite automatizada usa *fakes* y SQLite en memoria, por lo que no depende de SDK 10, de una migración real ni de una base local.

> El *badge* de CI y las reglas de protección de rama se añadirán cuando exista el repositorio remoto.

## Versionado (hitos, SemVer pre-1.0)

| Versión | Hito |
|---------|------|
| `v0.4.0` | CU-01 a CU-04 completos |
| `v0.5.0` | CU-05 completo |
| `v0.6.0` | CU-06 completo |
| `v0.7.0` | CU-07 completo |
| `v0.8.0` | CU-08 completo |
| `v0.9.0` | CU-09 completo funcionalmente |
| `v1.0.0` | Versión final validada del Proyecto de Graduación |

> El primer *tag* (`v0.4.0`) se creará únicamente **después** de publicar el remoto y de una ejecución correcta de CI.
