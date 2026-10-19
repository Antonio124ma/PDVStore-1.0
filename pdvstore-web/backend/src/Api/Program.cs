using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using PDVStore.Application.Services;
using PDVStore.Domain.Interfaces;
using PDVStore.Infrastructure.Data;
using PDVStore.Infrastructure.Storage;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// ============ SERVIÇOS ============
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// ============ BANCO DE DADOS (PostgreSQL) ============
builder.Services.AddDbContext<PDVContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

// ============ SERVIÇOS DE APLICAÇÃO ============
builder.Services.AddTransient<IVendaService, VendaService>();
builder.Services.AddTransient<IProdutoService, ProdutoService>();
builder.Services.AddTransient<ICaixaService, CaixaService>();
builder.Services.AddTransient<IUsuarioService, UsuarioService>();
builder.Services.AddTransient<IPagamentoIntegrator, PagamentoIntegrator>();
builder.Services.AddTransient<ITokenService, TokenService>();
builder.Services.AddTransient<IClienteService, ClienteService>();

// ============ AUTENTICAÇÃO JWT ============
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"] ?? throw new InvalidOperationException("JwtSettings:SecretKey não configurado");
var key = Encoding.UTF8.GetBytes(secretKey);

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtSettings["Issuer"] ?? "PDVStore",
            ValidAudience = jwtSettings["Audience"] ?? "PDVStore",
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.Zero
        };
    });

builder.Services.AddAuthorization(options =>
{
    options.AddPolicy("ApenasAdministrador", p => p.RequireRole("Administrador"));
    options.AddPolicy("OperadorOuAdministrador", p => p.RequireRole("Operador", "Administrador"));
    options.AddPolicy("EstoquistaOuAdministrador", p => p.RequireRole("Estoquista", "Administrador"));
});

var app = builder.Build();

// ============ MIDDLEWARE ============
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

// ============ HEALTH CHECK ============
app.MapGet("/api/health", () => Results.Ok(new { status = "healthy", timestamp = DateTime.UtcNow }));
app.MapGet("/api/health/ready", async (PDVContext db) =>
{
    try
    {
        await db.Database.CanConnectAsync();
        return Results.Ok(new { status = "ready", database = "connected" });
    }
    catch
    {
        return Results.Problem(statusCode: 503, detail: "Database not reachable");
    }
});

app.Run();