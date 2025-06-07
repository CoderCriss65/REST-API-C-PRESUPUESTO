using Microsoft.AspNetCore.Mvc;

using Microsoft.Data.SqlClient;
using System.Security.Cryptography;
using System.Text;
using BackendREST.Models;
using BackendREST.Services;

namespace BackendREST.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly IAuthService _authService;
        private readonly IConfiguration _configuration;

        public AuthController(IAuthService authService, IConfiguration configuration)
        {
            _authService = authService;
            _configuration = configuration;
        }

        [HttpPost("login")]
        public IActionResult Login([FromBody] LoginRequest request)
        {
            try
            {
                var usuario = ValidateUser(request.NombreUsuario, request.Contrasena);

                if (usuario == null)
                    return Unauthorized("Credenciales inválidas");

                var token = _authService.GenerateJwtToken(usuario);

                return Ok(new LoginResponse
                {
                    Token = token,
                    Expiracion = DateTime.UtcNow.AddMinutes(
                        Convert.ToDouble(_configuration["Jwt:ExpireMinutes"])),
                    Nombre = usuario.Nombre,
                    Rol = usuario.Rol
                });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno: {ex.Message}");
            }
        }

        private Usuario? ValidateUser(string username, string password)
        {
            using (var connection = new SqlConnection(
                _configuration.GetConnectionString("DefaultConnection")))
            {
                string query = @"SELECT UsuarioId, NombreUsuario, 
                                        Contrasena, Nombre, Rol 
                                 FROM Usuario 
                                 WHERE NombreUsuario = @Username 
                                 AND Activo = 1";

                connection.Open();

                using (var command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Username", username);

                    using (var reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            var storedHash = reader["Contrasena"].ToString();

                            // Verificación de contraseña
                            using (var sha256 = SHA256.Create())
                            {
                                var passwordBytes = Encoding.UTF8.GetBytes(password);
                                var hashBytes = sha256.ComputeHash(passwordBytes);
                                var inputHash = BitConverter.ToString(hashBytes)
                                    .Replace("-", "")
                                    .ToLower();

                                if (inputHash == storedHash?.ToLower())
                                {
                                    return new Usuario
                                    {
                                        UsuarioId = (int)reader["UsuarioId"],
                                        NombreUsuario = reader["NombreUsuario"].ToString(),
                                        Nombre = reader["Nombre"].ToString(),
                                        Rol = reader["Rol"].ToString()
                                    };
                                }
                            }
                        }
                    }
                }
            }
            return null;
        }
    }
}