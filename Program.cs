using Microsoft.EntityFrameworkCore;
using Software_SARA.Repositories.Models;
using Software_SARA.Repositories;
using Software_SARA.Services;

var builder = WebApplication.CreateBuilder(args);

QuestPDF.Settings.License = QuestPDF.Infrastructure.LicenseType.Community;

// Configuración de controladores con la traducción de mensajes de enlace al español
builder.Services.AddControllersWithViews()
    .AddMvcOptions(options =>
    {
        options.ModelBindingMessageProvider.SetAttemptedValueIsInvalidAccessor(
            (x, y) => "El valor proporcionado no es válido.");
        options.ModelBindingMessageProvider.SetValueIsInvalidAccessor(
            (x) => "El valor ingresado no es válido.");
    });

// Usar el contexto del profesor
// Usar el contexto del profesor
builder.Services.AddDbContext<SarabdContext>(options =>
    options.UseSqlServer(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        sqlOptions => sqlOptions.EnableRetryOnFailure()
    ));

builder.Services.AddScoped<SedeRepository>();
builder.Services.AddScoped<SedesServices>();
builder.Services.AddScoped<FacProveedorRepository>();
builder.Services.AddScoped<FacProveedorService>();
builder.Services.AddScoped<FacServPubRepository>();
builder.Services.AddScoped<FacServPubService>();

// REGISTRAR REPOSITORIO Y SERVICIO DE RECORDATORIOS DE PEDIDOS
builder.Services.AddScoped<RecordatorioPedidoRepository>();
builder.Services.AddScoped<RecordatorioPedidoService>();

// REGISTRAR EL SERVICIO DE CORREO
builder.Services.AddScoped<Software_SARA.Services.EmailService>();

// REGISTRAR EL SERVICIO DE GERENTES
builder.Services.AddScoped<Software_SARA.Services.GerenteService>();

// REGISTRAR HTTP CONTEXT ACCESSOR
builder.Services.AddHttpContextAccessor();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

// Aquí se construye la aplicación UNA SOLA VEZ
var app = builder.Build();

// 1. ESTO CAPTURA LOS ERRORES INTERNOS (500)
app.UseExceptionHandler("/Error");

// 2. ESTO CAPTURA LOS ERRORES DE PÁGINA NO ENCONTRADA (404 - Host)
app.UseStatusCodePagesWithReExecute("/Error/StatusCode/{0}");

if (!app.Environment.IsDevelopment())
{
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseSession();
app.UseAuthorization();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Acceso}/{action=Login}/{id?}");

app.Run();