using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OrderWebAPI.Application.Interfaces;
using OrderWebAPI.Application.Services;
using OrderWebAPI.Infrastructure.Persistence;
using OrderWebAPI.Infrastructure.Repositories;


var builder = WebApplication.CreateBuilder(args);

// Configurar Banco de Dados PostgreSQL
var connectionString = builder.Configuration.GetConnectionString("DefaultConnection")
                       ?? "Host=localhost;Port=5432;Database=orderdb;Username=postgres;Password=postgrespassword";

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseNpgsql(connectionString));

// Injeção de Dependências
builder.Services.AddScoped<IOrderRepository, OrderRepository>();
builder.Services.AddScoped<IProductRepository, ProductRepository>();
builder.Services.AddScoped<OrderService>();

// Configuração JWT
var jwtKey = builder.Configuration["Jwt:Key"] ?? "SuperSecretKeyForJwtAuthenticationMustBeLongEnough123!";
var jwtIssuer = builder.Configuration["Jwt:Issuer"] ?? "OrderWebAPI";
var jwtAudience = builder.Configuration["Jwt:Audience"] ?? "OrderWebAPIUsers";

builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = jwtIssuer,
            ValidAudience = jwtAudience,
            IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtKey))
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();

// Aplicar Migrations automaticamente na inicialização
using (var scope = app.Services.CreateScope())
{
    var dbContext = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    dbContext.Database.Migrate();
}

if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "OrderWebAPI v1");
        c.RoutePrefix = string.Empty; // <--- Isso faz o Swagger abrir direto na raiz (http://localhost:5260/)
    });
}

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();