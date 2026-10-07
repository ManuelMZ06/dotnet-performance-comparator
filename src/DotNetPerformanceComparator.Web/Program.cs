using DotNetPerformanceComparator.Application;
using DotNetPerformanceComparator.Infrastructure;
using DotNetPerformanceComparator.Web.Components;

var builder = WebApplication.CreateBuilder(args);

// Punto de composición: registro de capas mediante inyección de dependencias.
builder.Services.AddApplication();
builder.Services.AddInfrastructure(builder.Configuration.GetConnectionString("DefaultConnection"));

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
