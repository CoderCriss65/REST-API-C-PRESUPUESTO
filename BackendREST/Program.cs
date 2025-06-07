using Microsoft.AspNetCore.Authentication.JwtBearer;  // Namespace correcto
using Microsoft.IdentityModel.Tokens;
using System.Text;
using BackendREST.Services;  // Aseg�rate que coincida con tu namespace

var builder = WebApplication.CreateBuilder(args);

// Add services to the container
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();



// Register services
builder.Services.AddScoped<IAuthService, AuthService>();  // Usa IAuthService, no TAuthService

// Configure JWT authentication - VERSI�N CORREGIDA
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)  // Clase correcta
    .AddJwtBearer(options =>  // M�todo correcto
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

// IMPORTANTE: El orden es crucial aqu�
app.UseAuthentication();  // Primero
app.UseAuthorization();   // Despu�s

app.MapControllers();

app.Run();