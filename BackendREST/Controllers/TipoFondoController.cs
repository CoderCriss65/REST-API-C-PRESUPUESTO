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
   // [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class TipoFondoController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public TipoFondoController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(_connectionString))
            {
                throw new ArgumentNullException(nameof(_connectionString), "Connection string is missing in configuration");
            }
        }

        // GET: api/tipofondo
        [HttpGet]
        public ActionResult<IEnumerable<TipoFondo>> Get()
        {
            List<TipoFondo> tipos = new List<TipoFondo>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = "SELECT Id, Nombre FROM TipoFondo";
                connection.Open();

                using (SqlCommand command = new SqlCommand(query, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        tipos.Add(new TipoFondo
                        {
                            Id = reader.GetInt32(0),
                            Nombre = reader.GetString(1)
                        });
                    }
                }
            }

            return Ok(tipos);
        }

        // GET: api/tipofondo/5
        [HttpGet("{id}")]
        public ActionResult<TipoFondo> Get(int id)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = "SELECT Id, Nombre FROM TipoFondo WHERE Id = @Id";
                connection.Open();

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id); //un escalar que tome el valor del parametro

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return Ok(new TipoFondo
                            {
                                Id = reader.GetInt32(0),
                                Nombre = reader.GetString(1)
                            });
                        }
                    }
                }
            }
            return NotFound();
        }

        // POST: api/tipofondo
        [HttpPost]
        public ActionResult<TipoFondo> Post([FromBody] TipoFondo tipo)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    string query = @"INSERT INTO TipoFondo (Nombre) 
                                 VALUES (@Nombre);
                                 SELECT SCOPE_IDENTITY();";

                    connection.Open();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Nombre", tipo.Nombre);

                        int newId = Convert.ToInt32(command.ExecuteScalar());
                        tipo.Id = newId;
                    }
                }

                return CreatedAtAction(nameof(Get), new { id = tipo.Id }, tipo);
            }
            catch (SqlException ex)
            {
                // Detectar violación de clave única (Nombre duplicado)
                if (ex.Number == 2627)
                {
                    return Conflict("El nombre del tipo de fondo ya existe");
                }
                return StatusCode(500, $"Error de base de datos: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        // PUT: api/tipofondo/5
        [HttpPut("{id}")]
        public IActionResult Put(int id, [FromBody] TipoFondo tipo)
        {
            if (id != tipo.Id)
                return BadRequest("ID en URL no coincide con ID en objeto");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    string query = @"UPDATE TipoFondo 
                                 SET Nombre = @Nombre
                                 WHERE Id = @Id";

                    connection.Open();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        command.Parameters.AddWithValue("@Nombre", tipo.Nombre);

                        int rowsAffected = command.ExecuteNonQuery();
                        if (rowsAffected == 0)
                            return NotFound();
                    }
                }

                return NoContent();
            }
            catch (SqlException ex)
            {
                // Detectar violación de clave única (Nombre duplicado)
                if (ex.Number == 2627)
                {
                    return Conflict("El nombre del tipo de fondo ya existe");
                }
                return StatusCode(500, $"Error de base de datos: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        // DELETE: api/tipofondo/5
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    // Verificar si hay fondos asociados antes de eliminar
                    string checkQuery = "SELECT COUNT(*) FROM Fondo WHERE TipoFondoId = @Id";
                    string deleteQuery = "DELETE FROM TipoFondo WHERE Id = @Id";

                    connection.Open();

                    // Verificar dependencias
                    using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@Id", id);
                        int count = (int)checkCommand.ExecuteScalar();

                        if (count > 0)
                        {
                            return Conflict("No se puede eliminar el tipo porque tiene fondos asociados");
                        }
                    }

                    // Eliminar si no hay dependencias
                    using (SqlCommand command = new SqlCommand(deleteQuery, connection))
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