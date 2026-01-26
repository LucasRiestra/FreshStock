using FreshStock.API.Data;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;
using DotNetEnv;

var builder = WebApplication.CreateBuilder(args);

// Cargar variables de entorno desde .env
Env.Load();

// Add services to the container.

// Configurar MongoDB
var mongoConnectionString = Environment.GetEnvironmentVariable("MONGODB_CONNECTION_STRING")
    ?? builder.Configuration.GetConnectionString("MongoDB");
var mongoDatabaseName = Environment.GetEnvironmentVariable("MONGODB_DATABASE_NAME")
    ?? "FreshStockDB";

builder.Services.AddSingleton<MongoDbContext>(sp =>
    new MongoDbContext(mongoConnectionString!, mongoDatabaseName));

// Configurar AutoMapper
builder.Services.AddAutoMapper(typeof(Program));

// Configurar JWT Authentication
var jwtSettings = builder.Configuration.GetSection("JwtSettings");
var secretKey = jwtSettings["SecretKey"];

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuer = true,
        ValidateAudience = true,
        ValidateLifetime = true,
        ValidateIssuerSigningKey = true,
        ValidIssuer = jwtSettings["Issuer"],
        ValidAudience = jwtSettings["Audience"],
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey))
    };
});

// Registrar Services
builder.Services.AddScoped<FreshStock.API.Interfaces.IAuthService, FreshStock.API.Services.AuthService>();
builder.Services.AddScoped<FreshStock.API.Interfaces.IRestauranteService, FreshStock.API.Services.RestauranteService>();
builder.Services.AddScoped<FreshStock.API.Interfaces.IUsuarioService, FreshStock.API.Services.UsuarioService>();
builder.Services.AddScoped<FreshStock.API.Interfaces.ICategoriaService, FreshStock.API.Services.CategoriaService>();
builder.Services.AddScoped<FreshStock.API.Interfaces.IProveedorService, FreshStock.API.Services.ProveedorService>();
builder.Services.AddScoped<FreshStock.API.Interfaces.IProductoService, FreshStock.API.Services.ProductoService>();
builder.Services.AddScoped<FreshStock.API.Interfaces.IStockLocalService, FreshStock.API.Services.StockLocalService>();
builder.Services.AddScoped<FreshStock.API.Interfaces.IMovimientoInventarioService, FreshStock.API.Services.MovimientoInventarioService>();

// Nuevos servicios para relaciones N:M
builder.Services.AddScoped<FreshStock.API.Interfaces.IUsuarioRestauranteService, FreshStock.API.Services.UsuarioRestauranteService>();
builder.Services.AddScoped<FreshStock.API.Interfaces.IRestauranteProveedorService, FreshStock.API.Services.RestauranteProveedorService>();
builder.Services.AddScoped<FreshStock.API.Interfaces.IRestauranteCategoriaService, FreshStock.API.Services.RestauranteCategoriaService>();

// Servicio de permisos y autorización
builder.Services.AddScoped<FreshStock.API.Interfaces.IPermisoService, FreshStock.API.Services.PermisoService>();

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen(options =>
{
    options.AddSecurityDefinition("Bearer", new Microsoft.OpenApi.Models.OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = Microsoft.OpenApi.Models.SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = Microsoft.OpenApi.Models.ParameterLocation.Header,
        Description = "Ingresa tu token JWT. Ejemplo: eyJhbGciOiJIUzI1NiIs..."
    });
    options.AddSecurityRequirement(new Microsoft.OpenApi.Models.OpenApiSecurityRequirement
    {
        {
            new Microsoft.OpenApi.Models.OpenApiSecurityScheme
            {
                Reference = new Microsoft.OpenApi.Models.OpenApiReference
                {
                    Type = Microsoft.OpenApi.Models.ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// Configure the HTTP request pipeline.
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();

app.Run();
