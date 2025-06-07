using Microsoft.AspNetCore.Mvc;
using System;
using System.Collections.Generic;
using System.Data;
using System.Data.SqlClient;
using Microsoft.Extensions.Configuration;
using Microsoft.AspNetCore.Authorization;
using BackendREST.Models;
using System.ComponentModel.DataAnnotations;

namespace BackendREST.Controllers
{

    [Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class PresupuestoController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public PresupuestoController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(_connectionString))
            {
                throw new ArgumentNullException(nameof(_connectionString), "Connection string is missing in configuration");
            }
        }

        // GET: api/presupuesto
        [HttpGet]
        public ActionResult<IEnumerable<Presupuesto>> Get()
        {
            List<Presupuesto> presupuestos = new List<Presupuesto>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = @"
                    SELECT p.Id, p.TipoGastoId, p.Mes, p.Anio, p.Monto, 
                           t.Nombre AS TipoGastoNombre
                    FROM Presupuesto p

                    INNER JOIN TipoGasto t ON p.TipoGastoId = t.Id;
                ";
                connection.Open();

                using (SqlCommand command = new SqlCommand(query, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        presupuestos.Add(new Presupuesto
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            TipoGastoId = reader.GetInt32(reader.GetOrdinal("TipoGastoId")),
                            Mes = reader.GetByte(reader.GetOrdinal("Mes")),
                            Anio = reader.GetInt32(reader.GetOrdinal("Anio")),
                            Monto = reader.GetDecimal(reader.GetOrdinal("Monto")),
                            TipoGastoNombre = reader.GetString(reader.GetOrdinal("TipoGastoNombre"))
                        });
                    }
                }
            }

            return Ok(presupuestos);
        }

        // GET: api/presupuesto/5
        [HttpGet("{id}")]
        public ActionResult<Presupuesto> Get(int id)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = @"
                    SELECT p.Id, p.TipoGastoId, p.Mes, p.Anio, p.Monto, 
                           t.Nombre AS TipoGastoNombre
                    FROM Presupuesto p
                    INNER JOIN TipoGasto t ON p.TipoGastoId = t.Id
                    WHERE p.Id = @Id;
                ";
                connection.Open();

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return Ok(new Presupuesto
                            {
                                Id = reader.GetInt32(reader.GetOrdinal("Id")),
                                TipoGastoId = reader.GetInt32(reader.GetOrdinal("TipoGastoId")),
                                Mes = reader.GetByte(reader.GetOrdinal("Mes")),
                                Anio = reader.GetInt32(reader.GetOrdinal("Anio")),
                                Monto = reader.GetDecimal(reader.GetOrdinal("Monto")),
                                TipoGastoNombre = reader.GetString(reader.GetOrdinal("TipoGastoNombre"))
                            });
                        }
                    }
                }
            }
            return NotFound();
        }

        // POST: api/presupuesto
        [HttpPost]
        public ActionResult<Presupuesto> Post([FromBody] PresupuestoInputModel input)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();

                    try
                    {
                        // Validar unicidad (TipoGastoId + Mes + Anio)
                        if (PresupuestoExists(input.TipoGastoId, input.Mes, input.Anio, connection, transaction))
                        {
                            transaction.Rollback();
                            return Conflict("Ya existe un presupuesto para este tipo de gasto en el mismo mes y año");
                        }

                        // Insertar nuevo presupuesto
                        string query = @"
                            INSERT INTO Presupuesto (TipoGastoId, Mes, Anio, Monto)
                            OUTPUT INSERTED.Id
                            VALUES (@TipoGastoId, @Mes, @Anio, @Monto);
                        ";

                        using (SqlCommand command = new SqlCommand(query, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@TipoGastoId", input.TipoGastoId);
                            command.Parameters.AddWithValue("@Mes", input.Mes);
                            command.Parameters.AddWithValue("@Anio", input.Anio);
                            command.Parameters.AddWithValue("@Monto", input.Monto);

                            int newId = Convert.ToInt32(command.ExecuteScalar());

                            // Obtener el presupuesto creado con el nombre del tipo de gasto
                            var presupuesto = GetPresupuestoById(newId, connection, transaction);

                            if (presupuesto == null)
                            {
                                transaction.Rollback();
                                return StatusCode(500, "Error al obtener el presupuesto creado");
                            }

                            transaction.Commit();
                            return CreatedAtAction(nameof(Get), new { id = newId }, presupuesto);
                        }
                    }
                    catch (SqlException ex)
                    {
                        transaction.Rollback();
                        return StatusCode(500, $"Error de base de datos: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return StatusCode(500, $"Error interno: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error de conexión: {ex.Message}");
            }
        }




        // PUT: api/presupuesto/5
        [HttpPut("{id}")]
        public IActionResult Put(int id, [FromBody] PresupuestoUpdateModel input)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();

                    try
                    {
                        // Validar existencia del presupuesto
                        if (!PresupuestoExistsById(id, connection, transaction))
                        {
                            transaction.Rollback();
                            return NotFound($"No se encontró el presupuesto con ID {id}");
                        }

                        // Validar unicidad excluyendo el registro actual
                        if (PresupuestoExists(input.TipoGastoId, input.Mes, input.Anio, connection, transaction, id))
                        {
                            transaction.Rollback();
                            return Conflict("Ya existe otro presupuesto para este tipo de gasto en el mismo mes y año");
                        }

                        // Actualizar presupuesto
                        string query = @"
                            UPDATE Presupuesto 
                            SET TipoGastoId = @TipoGastoId,
                                Mes = @Mes,
                                Anio = @Anio,
                                Monto = @Monto
                            WHERE Id = @Id;
                        ";

                        using (SqlCommand command = new SqlCommand(query, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@Id", id);
                            command.Parameters.AddWithValue("@TipoGastoId", input.TipoGastoId);
                            command.Parameters.AddWithValue("@Mes", input.Mes);
                            command.Parameters.AddWithValue("@Anio", input.Anio);
                            command.Parameters.AddWithValue("@Monto", input.Monto);

                            int rowsAffected = command.ExecuteNonQuery();

                            if (rowsAffected == 0)
                            {
                                transaction.Rollback();
                                return NotFound($"No se encontró el presupuesto con ID {id}");
                            }

                            // Obtener el presupuesto actualizado
                            var presupuesto = GetPresupuestoById(id, connection, transaction);

                            if (presupuesto == null)
                            {
                                transaction.Rollback();
                                return StatusCode(500, "Error al obtener el presupuesto actualizado");
                            }

                            transaction.Commit();
                            return Ok(presupuesto);
                        }
                    }
                    catch (SqlException ex)
                    {
                        transaction.Rollback();
                        return StatusCode(500, $"Error de base de datos: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return StatusCode(500, $"Error interno: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error de conexión: {ex.Message}");
            }
        }

        // DELETE: api/presupuesto/5
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    SqlTransaction transaction = connection.BeginTransaction();

                    try
                    {
                        // Verificar si existe el registro
                        if (!PresupuestoExistsById(id, connection, transaction))
                        {
                            transaction.Rollback();
                            return NotFound($"No se encontró el presupuesto con ID {id}");
                        }

                        // Eliminar el presupuesto
                        string query = "DELETE FROM Presupuesto WHERE Id = @Id";
                        using (SqlCommand command = new SqlCommand(query, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@Id", id);
                            int rowsAffected = command.ExecuteNonQuery();

                            if (rowsAffected == 0)
                            {
                                transaction.Rollback();
                                return NotFound($"No se encontró el presupuesto con ID {id}");
                            }

                            transaction.Commit();
                            return NoContent();
                        }
                    }
                    catch (SqlException ex)
                    {
                        transaction.Rollback();
                        if (ex.Number == 547) // FK constraint violation
                        {
                            return Conflict("No se puede eliminar porque tiene registros asociados en otras tablas");
                        }
                        return StatusCode(500, $"Error de base de datos: {ex.Message}");
                    }
                    catch (Exception ex)
                    {
                        transaction.Rollback();
                        return StatusCode(500, $"Error interno: {ex.Message}");
                    }
                }
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error de conexión: {ex.Message}");
            }
        }

        // Modelos de entrada
        public class PresupuestoInputModel
        {
            [Required(ErrorMessage = "El tipo de gasto es obligatorio")]
            public int TipoGastoId { get; set; }

            [Required(ErrorMessage = "El mes es obligatorio")]
            [Range(1, 12, ErrorMessage = "El mes debe ser un valor entre 1 y 12")]
            public byte Mes { get; set; }

            [Required(ErrorMessage = "El año es obligatorio")]
            [Range(2000, 2100, ErrorMessage = "El año debe ser un valor válido")]
            public int Anio { get; set; }

            [Required(ErrorMessage = "El monto es obligatorio")]
            [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor que cero")]
            public decimal Monto { get; set; }
        }

        public class PresupuestoUpdateModel
        {
            [Required(ErrorMessage = "El tipo de gasto es obligatorio")]
            public int TipoGastoId { get; set; }

            [Required(ErrorMessage = "El mes es obligatorio")]
            [Range(1, 12, ErrorMessage = "El mes debe ser un valor entre 1 y 12")]
            public byte Mes { get; set; }

            [Required(ErrorMessage = "El año es obligatorio")]
            [Range(2000, 2100, ErrorMessage = "El año debe ser un valor válido")]
            public int Anio { get; set; }

            [Required(ErrorMessage = "El monto es obligatorio")]
            [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor que cero")]
            public decimal Monto { get; set; }
        }

        // Métodos auxiliares
        private bool PresupuestoExists(int tipoGastoId, byte mes, int anio,
                                      SqlConnection connection, SqlTransaction transaction,
                                      int excludeId = 0)
        {
            string query = @"
                SELECT COUNT(*) 
                FROM Presupuesto 
                WHERE TipoGastoId = @TipoGastoId 
                    AND Mes = @Mes 
                    AND Anio = @Anio
                    AND Id <> @ExcludeId;
            ";

            using (SqlCommand command = new SqlCommand(query, connection, transaction))
            {
                command.Parameters.AddWithValue("@TipoGastoId", tipoGastoId);
                command.Parameters.AddWithValue("@Mes", mes);
                command.Parameters.AddWithValue("@Anio", anio);
                command.Parameters.AddWithValue("@ExcludeId", excludeId);

                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

        private bool PresupuestoExistsById(int id, SqlConnection connection, SqlTransaction transaction)
        {
            string query = "SELECT COUNT(*) FROM Presupuesto WHERE Id = @Id";
            using (SqlCommand command = new SqlCommand(query, connection, transaction))
            {
                command.Parameters.AddWithValue("@Id", id);
                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

        private Presupuesto GetPresupuestoById(int id, SqlConnection connection, SqlTransaction transaction)
        {
            string query = @"
                SELECT p.Id, p.TipoGastoId, p.Mes, p.Anio, p.Monto, 
                       t.Nombre AS TipoGastoNombre
                FROM Presupuesto p
                INNER JOIN TipoGasto t ON p.TipoGastoId = t.Id
                WHERE p.Id = @Id;
            ";

            using (SqlCommand command = new SqlCommand(query, connection, transaction))
            {
                command.Parameters.AddWithValue("@Id", id);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new Presupuesto
                        {
                            Id = reader.GetInt32(reader.GetOrdinal("Id")),
                            TipoGastoId = reader.GetInt32(reader.GetOrdinal("TipoGastoId")),
                            Mes = reader.GetByte(reader.GetOrdinal("Mes")),
                            Anio = reader.GetInt32(reader.GetOrdinal("Anio")),
                            Monto = reader.GetDecimal(reader.GetOrdinal("Monto")),
                            TipoGastoNombre = reader.GetString(reader.GetOrdinal("TipoGastoNombre"))
                        };
                    }
                }
            }
            return null;
        }
    }
}