using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;
using BackendREST.Models;

namespace BackendREST.Controllers
{
    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class FondoController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public FondoController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(_connectionString))
            {
                throw new ArgumentNullException(nameof(_connectionString), "Connection string is missing in configuration");
            }
        }

        // GET: api/fondo
        [HttpGet]
        public ActionResult<IEnumerable<Fondo>> Get()
        {
            List<Fondo> fondos = new List<Fondo>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                // Incluye JOIN con TipoFondo para obtener el nombre del tipo
                string query = @"
                    SELECT F.Id, F.NroCuenta, F.NombreFondo, F.TipoFondoId, 
                           TF.Nombre AS TipoFondoNombre, F.Saldo, F.Descripcion, F.Activo
                    FROM Fondo F
                    INNER JOIN TipoFondo TF ON F.TipoFondoId = TF.Id";

                connection.Open();

                using (SqlCommand command = new SqlCommand(query, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        fondos.Add(new Fondo
                        {
                            Id = reader.GetInt32(0),
                            NroCuenta = reader.GetString(1),
                            NombreFondo = reader.GetString(2),
                            TipoFondoId = reader.GetInt32(3),
                            TipoFondoNombre = reader.GetString(4),
                            Saldo = reader.GetDecimal(5),
                            Descripcion = reader.IsDBNull(6) ? null : reader.GetString(6),
                            Activo = reader.GetBoolean(7)
                        });
                    }
                }
            }

            return Ok(fondos);
        }///


        // GET: api/fondo/activos
        [HttpGet("activos")]
        public ActionResult<IEnumerable<Fondo>> GetActivos()
        {
            List<Fondo> fondos = new List<Fondo>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                // Consulta modificada con WHERE Activo = 1
                string query = @"
            SELECT F.Id, F.NroCuenta, F.NombreFondo, F.TipoFondoId, 
                   TF.Nombre AS TipoFondoNombre, F.Saldo, F.Descripcion, F.Activo
            FROM Fondo F
            INNER JOIN TipoFondo TF ON F.TipoFondoId = TF.Id
            WHERE F.Activo = 1";  // Filtro clave aquí

                connection.Open();

                using (SqlCommand command = new SqlCommand(query, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        fondos.Add(new Fondo
                        {
                            Id = reader.GetInt32(0),
                            NroCuenta = reader.GetString(1),
                            NombreFondo = reader.GetString(2),
                            TipoFondoId = reader.GetInt32(3),
                            TipoFondoNombre = reader.GetString(4),
                            Saldo = reader.GetDecimal(5),
                            Descripcion = reader.IsDBNull(6) ? null : reader.GetString(6),
                            Activo = reader.GetBoolean(7)
                        });
                    }
                }
            }

            return Ok(fondos);
        }





        // GET: api/fondo/5
        [HttpGet("{id}")]
        public ActionResult<Fondo> Get(int id)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                // Incluye JOIN con TipoFondo para obtener el nombre del tipo
                string query = @"
                    SELECT F.Id, F.NroCuenta, F.NombreFondo, F.TipoFondoId, 
                           TF.Nombre AS TipoFondoNombre, F.Saldo, F.Descripcion, F.Activo
                    FROM Fondo F
                    INNER JOIN TipoFondo TF ON F.TipoFondoId = TF.Id
                    WHERE F.Id = @Id";

                connection.Open();

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return Ok(new Fondo
                            {
                                Id = reader.GetInt32(0),
                                NroCuenta = reader.GetString(1),
                                NombreFondo = reader.GetString(2),
                                TipoFondoId = reader.GetInt32(3),
                                TipoFondoNombre = reader.GetString(4),
                                Saldo = reader.GetDecimal(5),
                                Descripcion = reader.IsDBNull(6) ? null : reader.GetString(6),
                                Activo = reader.GetBoolean(7)
                            });
                        }
                    }
                }
            }
            return NotFound();
        }


        // POST: api/fondo inserccion de nuevo fondo monetario
        [HttpPost]
        public ActionResult<Fondo> Post([FromBody] Fondo fondo)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    string query = @"
                        INSERT INTO Fondo (NroCuenta, NombreFondo, TipoFondoId, Saldo, Descripcion, Activo) 
                        VALUES (@NroCuenta, @NombreFondo, @TipoFondoId, @Saldo, @Descripcion, @Activo);
                        SELECT SCOPE_IDENTITY();";

                    connection.Open();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@NroCuenta", fondo.NroCuenta);
                        command.Parameters.AddWithValue("@NombreFondo", fondo.NombreFondo);
                        command.Parameters.AddWithValue("@TipoFondoId", fondo.TipoFondoId);
                        command.Parameters.AddWithValue("@Saldo", fondo.Saldo);
                        command.Parameters.AddWithValue("@Descripcion", (object)fondo.Descripcion ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Activo", fondo.Activo);

                        int newId = Convert.ToInt32(command.ExecuteScalar());
                        fondo.Id = newId;
                    }
                }

                return CreatedAtAction(nameof(Get), new { id = fondo.Id }, fondo);
            }
            catch (SqlException ex)
            {
                // Detectar violación de clave única (NroCuenta duplicado)
                if (ex.Number == 2627) // SQL Server error number for unique constraint violation
                {
                    return Conflict("El número de cuenta ya existe");
                }
                return StatusCode(500, $"Error de base de datos: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        // PUT: api/fondo/5
        [HttpPut("{id}")]
        public IActionResult Put(int id, [FromBody] Fondo fondo)
        {
            if (id != fondo.Id)
                return BadRequest("ID en URL no coincide con ID en objeto");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    string query = @"
                        UPDATE Fondo 
                        SET NroCuenta = @NroCuenta, 
                            NombreFondo = @NombreFondo, 
                            TipoFondoId = @TipoFondoId, 
                            Saldo = @Saldo, 
                            Descripcion = @Descripcion, 
                            Activo = @Activo 
                        WHERE Id = @Id";

                    connection.Open();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        command.Parameters.AddWithValue("@NroCuenta", fondo.NroCuenta);
                        command.Parameters.AddWithValue("@NombreFondo", fondo.NombreFondo);
                        command.Parameters.AddWithValue("@TipoFondoId", fondo.TipoFondoId);
                        command.Parameters.AddWithValue("@Saldo", fondo.Saldo);
                        command.Parameters.AddWithValue("@Descripcion", (object)fondo.Descripcion ?? DBNull.Value);
                        command.Parameters.AddWithValue("@Activo", fondo.Activo);

                        int rowsAffected = command.ExecuteNonQuery();
                        if (rowsAffected == 0)
                            return NotFound();
                    }
                }

                return NoContent();
            }
            catch (SqlException ex)
            {
                // Detectar violación de clave única (NroCuenta duplicado)
                if (ex.Number == 2627)
                {
                    return Conflict("El número de cuenta ya existe");
                }
                return StatusCode(500, $"Error de base de datos: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        // DELETE: api/fondo/5
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    // Eliminación lógica (marcar como inactivo)
                    string query = @"
                        UPDATE Fondo 
                        SET Activo = 0 
                        WHERE Id = @Id";

                    connection.Open();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        int rowsAffected = command.ExecuteNonQuery();
                        if (rowsAffected == 0)
                            return NotFound();
                    }
                }

                return NoContent();
            }
            catch (SqlException ex)
            {
                return StatusCode(500, $"Error de base de datos: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }
    }
}
