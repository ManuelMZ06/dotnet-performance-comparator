using DotNetPerformanceComparator.Application;
using DotNetPerformanceComparator.Infrastructure;
using DotNetPerformanceComparator.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Punto de composición: registro de capas mediante inyección de dependencias.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration.GetConnectionString("DefaultConnection"));

// CU-04 (C-07.5): la sesión de ejecución vive en el host Web, no en el circuito Blazor.
builder.Services.AddSingleton<DotNetPerformanceComparator.Web.Execution.IEvaluationExecutionSessionManager,
    DotNetPerformanceComparator.Web.Execution.EvaluationExecutionSessionManager>();

builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseAntiforgery();

app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
