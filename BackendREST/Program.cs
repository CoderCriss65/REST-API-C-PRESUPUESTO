using Microsoft.AspNetCore.Authentication.JwtBearer;  // Namespace correcto
using Microsoft.IdentityModel.Tokens;
using System.Text;
using BackendREST.Services;  // Asegúrate que coincida con tu namespace

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();



// Register services
builder.Services.AddScoped<IAuthService, AuthService>();  // Usa IAuthService, no TAuthService

// Configure JWT authentication - VERSIÓN CORREGIDA
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)  // Clase correcta
    .AddJwtBearer(options =>  // Método correcto
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(builder.Configuration["Jwt:Key"]))
        };
    });

var app = builder.Build();

// Configure the HTTP request pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();

// IMPORTANTE: El orden es crucial aquí
app.UseAuthentication();  // Primero
app.UseAuthorization();   // Después

app.MapControllers();

app.Run();