# Claude Code

Instrucciones iniciales para trabajar en **DotNetPerformanceComparator**. Este archivo es breve y **apunta a las fuentes de verdad**; no las reemplaza.

## Antes de cualquier tarea

1. Leer [AGENTS.md](AGENTS.md) (reglas operativas y de arquitectura).
2. Leer [docs/PROJECT_CONTEXT.md](docs/PROJECT_CONTEXT.md) (fuente de verdad técnica/funcional: CU, RF, arquitectura, modelo de datos, decisiones D-xx).
3. Leer [docs/UI_SPEC.md](docs/UI_SPEC.md) cuando la tarea afecte la interfaz.
4. Inspeccionar el código existente antes de modificar.

## Reglas clave

5. No avanzar automáticamente al siguiente caso de uso.
6. Al terminar, ejecutar `dotnet restore`, `dotnet build` y `dotnet test`.
7. No alterar migraciones EF Core históricas.
8. No agregar funcionalidades fuera del alcance solicitado.
9. No modificar los proyectos .NET evaluados originales ni el `global.json` del repositorio.
10. Usar mensajes de commit en español cuando se solicite un commit.

## Comandos útiles

```bash
dotnet tool restore
dotnet restore
dotnet build
dotnet test
dotnet run --project src/DotNetPerformanceComparator.Web
dotnet ef migrations list --project src/DotNetPerformanceComparator.Infrastructure --startup-project src/DotNetPerformanceComparator.Web
dotnet ef migrations has-pending-model-changes --project src/DotNetPerformanceComparator.Infrastructure --startup-project src/DotNetPerformanceComparator.Web
```

Estado: CU-01 a CU-04 completos; CU-05 a CU-09 pendientes. Detalle en `docs/PROJECT_CONTEXT.md`.
