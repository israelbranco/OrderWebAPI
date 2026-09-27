using System.Text;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using OrderWebAPI.Application.Interfaces;
using OrderWebAPI.Application.Services;
using OrderWebAPI.Infrastructure.Persistence;
using OrderWebAPI.Infrastructure.Repositories;
using Swashbuckle.AspNetCore.SwaggerGen;

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

    // Use reflection to access OpenApiSecurityScheme from Microsoft.OpenApi package
    var openApiAssembly = AppDomain.CurrentDomain.GetAssemblies()
        .FirstOrDefault(a => a.GetName().Name == "Microsoft.OpenApi");

    if (openApiAssembly != null)
    {
        var securitySchemeType = openApiAssembly.GetType("Microsoft.OpenApi.Models.OpenApiSecurityScheme");
        var openApiInfoType = openApiAssembly.GetType("Microsoft.OpenApi.Models.OpenApiInfo");
        var referenceType = openApiAssembly.GetType("Microsoft.OpenApi.Models.OpenApiReference");
        var securityReqType = openApiAssembly.GetType("Microsoft.OpenApi.Models.OpenApiSecurityRequirement");

        if (securitySchemeType != null && referenceType != null && securityReqType != null)
        {
            // Create OpenApiSecurityScheme
            var scheme = Activator.CreateInstance(securitySchemeType)!;
            securitySchemeType.GetProperty("Name")!.SetValue(scheme, "Authorization");

            var schemeTypeEnum = openApiAssembly.GetType("Microsoft.OpenApi.Models.SecuritySchemeType");
            securitySchemeType.GetProperty("Type")!.SetValue(scheme, Enum.GetValues(schemeTypeEnum!).GetValue(4)); // Http

            securitySchemeType.GetProperty("Scheme")!.SetValue(scheme, "bearer");
            securitySchemeType.GetProperty("BearerFormat")!.SetValue(scheme, "JWT");
            securitySchemeType.GetProperty("Description")!.SetValue(scheme, "Insira o token JWT");

            // Create OpenApiReference
            var reference = Activator.CreateInstance(referenceType)!;
            var refTypeEnum = openApiAssembly.GetType("Microsoft.OpenApi.Models.ReferenceType");
            referenceType.GetProperty("Type")!.SetValue(reference, Enum.GetValues(refTypeEnum!).GetValue(3)); // SecurityScheme
            referenceType.GetProperty("Id")!.SetValue(reference, "Bearer");

            securitySchemeType.GetProperty("Reference")!.SetValue(scheme, reference);

            // Add security definition
            var addSecDefMethod = c.GetType().GetMethod("AddSecurityDefinition", 
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
                null, new[] { typeof(string), securitySchemeType }, null);
            addSecDefMethod?.Invoke(c, new[] { "Bearer", scheme });

            // Create and add security requirement
            var secReq = Activator.CreateInstance(securityReqType)!;
            var addMethod = securityReqType.GetMethod("Add");
            addMethod?.Invoke(secReq, new[] { scheme, Array.Empty<string>() });

            var addSecReqMethod = c.GetType().GetMethod("AddSecurityRequirement",
                System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance,
                null, new[] { securityReqType }, null);
            addSecReqMethod?.Invoke(c, new[] { secReq });
        }
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