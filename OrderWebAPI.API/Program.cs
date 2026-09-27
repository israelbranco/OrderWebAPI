using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
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
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new()
    {
        Title = "OrderWebAPI",
        Version = "v1",
        Description = "API REST de Gestão de Pedidos com autenticação JWT"
    });

    var bearerSchemeType = Type.GetType("Microsoft.OpenApi.Models.OpenApiSecurityScheme, Microsoft.OpenApi");
    var referenceType = Type.GetType("Microsoft.OpenApi.Models.OpenApiReference, Microsoft.OpenApi");
    var securityReqType = Type.GetType("Microsoft.OpenApi.Models.OpenApiSecurityRequirement, Microsoft.OpenApi");
    var refTypeEnum = Type.GetType("Microsoft.OpenApi.Models.ReferenceType, Microsoft.OpenApi");
    var secSchemeTypeEnum = Type.GetType("Microsoft.OpenApi.Models.SecuritySchemeType, Microsoft.OpenApi");
    var paramLocationEnum = Type.GetType("Microsoft.OpenApi.Models.ParameterLocation, Microsoft.OpenApi");

    if (bearerSchemeType != null && referenceType != null)
    {
        dynamic scheme = Activator.CreateInstance(bearerSchemeType)!;
        scheme.Name = "Authorization";
        scheme.Type = Enum.GetValues(secSchemeTypeEnum!).GetValue(4); // SecuritySchemeType.Http = 4
        scheme.Scheme = "bearer";
        scheme.BearerFormat = "JWT";
        scheme.Description = "Insira o token JWT";

        dynamic reference = Activator.CreateInstance(referenceType)!;
        reference.Type = Enum.GetValues(refTypeEnum!).GetValue(3); // ReferenceType.SecurityScheme = 3
        reference.Id = "Bearer";
        scheme.Reference = reference;

        c.GetType().GetMethod("AddSecurityDefinition")!.Invoke(c, new object?[] { "Bearer", scheme });

        dynamic securityRequirement = Activator.CreateInstance(securityReqType)!;
        securityRequirement.Add(scheme, Array.Empty<string>());
        c.GetType().GetMethod("AddSecurityRequirement")!.Invoke(c, new object?[] { securityRequirement });
    }
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