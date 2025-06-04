using PlantApp.Components;
using PlantApp.Services;
using MudBlazor.Services;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddRazorComponents()
    .AddInteractiveServerComponents();

builder.Services.AddMudServices();

// Add custom services
builder.Services.AddSingleton<PlantService>();
builder.Services.AddSingleton<QrCodeService>();
builder.Services.AddScoped<OdooService>();
builder.Services.AddScoped<ZebraPrinterService>();
builder.Services.AddScoped<ExcelService>();

builder.Services.AddHttpClient();

var app = builder.Build();

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error", createScopeForErrors: true);
}


app.UseAntiforgery();

app.MapStaticAssets();
app.MapRazorComponents<App>()
    .AddInteractiveServerRenderMode();

app.Run();
