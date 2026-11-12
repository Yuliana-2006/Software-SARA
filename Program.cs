using Microsoft.EntityFrameworkCore;
using Software_SARA.Repositories.Models; // <-- Namespace del SarabdContext
using Software_SARA.Repositories;
using Software_SARA.Services;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// Usar el contexto del profesor
builder.Services.AddDbContext<SarabdContext>(options =>
    options.UseSqlServer(builder.Configuration.GetConnectionString("DefaultConnection")));

builder.Services.AddScoped<SedeRepository>();
builder.Services.AddScoped<SedesServices>();
builder.Services.AddScoped<FacProveedorRepository>();
builder.Services.AddScoped<FacProveedorService>();
builder.Services.AddScoped<FacServPubRepository>();
builder.Services.AddScoped<FacServPubService>();

// ==========================================
// REGISTRAR REPOSITORIO Y SERVICIO DE RECORDATORIOS DE PEDIDOS
// ==========================================
builder.Services.AddScoped<RecordatorioPedidoRepository>();
builder.Services.AddScoped<RecordatorioPedidoService>();

// ==========================================
// REGISTRAR EL SERVICIO DE CORREO
// ==========================================
builder.Services.AddScoped<Software_SARA.Services.EmailService>();

// ==========================================
// REGISTRAR EL SERVICIO DE GERENTES
// ==========================================
builder.Services.AddScoped<Software_SARA.Services.GerenteService>();



// ==========================================
// REGISTRAR HTTP CONTEXT ACCESSOR
// ==========================================
builder.Services.AddHttpContextAccessor();

builder.Services.AddSession(options =>
{
    options.IdleTimeout = TimeSpan.FromMinutes(30);
    options.Cookie.HttpOnly = true;
    options.Cookie.IsEssential = true;
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
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