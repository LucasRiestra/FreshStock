using FreshStock.API.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.

// Configurar DbContext con PostgreSQL
builder.Services.AddDbContext<ApplicationDbContext>(options =>
    options.UseNpgsql(builder.Configuration.GetConnectionString("DefaultConnection")));

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

builder.Services.AddControllers();
// Learn more about configuring Swagger/OpenAPI at https://aka.ms/aspnetcore/swashbuckle
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

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
