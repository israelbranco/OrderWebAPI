using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi;
using OrderWebAPI.Application.Interfaces;
using OrderWebAPI.Application.Services;
using OrderWebAPI.Infrastructure.Persistence;
using OrderWebAPI.Infrastructure.Repositories;
using System.Text;


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
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "OrderWebAPI",
        Version = "v1",
        Description = "API REST com autenticação JWT"
    });

    options.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        Description = "Insira o token JWT: {token} (Não escreva `Bearer` e não coloque `aspas` no token."
    });

    // CORREÇÃO: Usa o delegate 'document =>' e uma coleção List<string> (através do atalho [])
    options.AddSecurityRequirement(document => new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecuritySchemeReference("Bearer", document),
            [] // <-- Uma lista vazia compatível com System.Collections.Generic.List<string>
        }
    });

});

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