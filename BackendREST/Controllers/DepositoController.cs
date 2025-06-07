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
    //[Authorize]
    [Route("api/[controller]")]
    [ApiController]
    public class DepositoController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public DepositoController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(_connectionString))
            {
                throw new ArgumentNullException(nameof(_connectionString), "Connection string is missing in configuration");
            }
        }

        // GET: api/deposito
        [HttpGet]
        public ActionResult<IEnumerable<Deposito>> Get()
        {
            List<Deposito> depositos = new List<Deposito>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = "SELECT id_deposito, fechaDeposito, id_fondo, monto FROM Deposito";
                connection.Open();

                using (SqlCommand command = new SqlCommand(query, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        depositos.Add(new Deposito
                        {
                            IdDeposito = reader.GetInt32(reader.GetOrdinal("id_deposito")),
                            FechaDeposito = reader.GetDateTime(reader.GetOrdinal("fechaDeposito")),
                            IdFondo = reader.GetInt32(reader.GetOrdinal("id_fondo")),
                            Monto = reader.GetDecimal(reader.GetOrdinal("monto"))
                        });
                    }
                }
            }

            return Ok(depositos);
        }



        // GET: api/deposito/5
        [HttpGet("{id}")]
        public ActionResult<Deposito> Get(int id)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = "SELECT id_deposito, fechaDeposito, id_fondo, monto FROM Deposito WHERE id_deposito = @Id";
                connection.Open();

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return Ok(new Deposito
                            {
                                IdDeposito = reader.GetInt32(reader.GetOrdinal("id_deposito")),
                                FechaDeposito = reader.GetDateTime(reader.GetOrdinal("fechaDeposito")),
                                IdFondo = reader.GetInt32(reader.GetOrdinal("id_fondo")),
                                Monto = reader.GetDecimal(reader.GetOrdinal("monto"))
                            });
                        }
                    }
                }
            }
            return NotFound();
        }


        //
        // GET: api/deposito/DepositoDetalle
        [HttpGet("DepositoDetalle")]
        public ActionResult<IEnumerable<object>> GetDepositoDetalle()
        {
            List<object> depositos = new List<object>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = @"
            SELECT 
                d.id_deposito,
                d.fechaDeposito,
                f.NombreFondo,
                d.monto
            FROM Deposito d
            INNER JOIN Fondo f ON d.id_fondo = f.Id
            ORDER BY d.fechaDeposito DESC"; // traerme deposito con nombreFondo

                connection.Open();

                using (SqlCommand command = new SqlCommand(query, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        depositos.Add(new
                        {
                            IdDeposito = reader.GetInt32(reader.GetOrdinal("id_deposito")),
                            FechaDeposito = reader.GetDateTime(reader.GetOrdinal("fechaDeposito")),
                            NombreFondo = reader.GetString(reader.GetOrdinal("NombreFondo")),
                            Monto = reader.GetDecimal(reader.GetOrdinal("monto"))
                        });
                    }
                }
            }

            return Ok(depositos);
        }



        //



        // POST: api/deposito
        [HttpPost]
        public ActionResult<Deposito> Post([FromBody] DepositoInputModel input)
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
                        // 1. Insertar nuevo depósito
                        string insertDepositoQuery = @"
                    INSERT INTO Deposito (fechaDeposito, id_fondo, monto)
                    OUTPUT INSERTED.id_deposito
                    VALUES (@FechaDeposito, @IdFondo, @Monto);
                ";

                        int newId;
                        using (SqlCommand command = new SqlCommand(insertDepositoQuery, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@FechaDeposito", input.FechaDeposito);
                            command.Parameters.AddWithValue("@IdFondo", input.IdFondo);
                            command.Parameters.AddWithValue("@Monto", input.Monto);

                            newId = Convert.ToInt32(command.ExecuteScalar());
                        }

                        // 2. Actualizar saldo del fondo
                        string updateFondoQuery = @"
                    UPDATE Fondo 
                    SET saldo = saldo + @MontoDepositado 
                    WHERE id = @IdFondo;
                ";

                        using (SqlCommand command = new SqlCommand(updateFondoQuery, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@MontoDepositado", input.Monto);
                            command.Parameters.AddWithValue("@IdFondo", input.IdFondo);

                            int rowsAffected = command.ExecuteNonQuery();

                            if (rowsAffected == 0)
                            {
                                transaction.Rollback();
                                return BadRequest("No se pudo actualizar el saldo del fondo. El fondo especificado no existe.");
                            }
                        }

                        // 3. Obtener el depósito recién creado
                        var deposito = GetDepositoById(newId, connection, transaction);

                        if (deposito == null)
                        {
                            transaction.Rollback();
                            return StatusCode(500, "Error al obtener el depósito creado");
                        }

                        transaction.Commit();
                        return CreatedAtAction(nameof(Get), new { id = newId }, deposito);
                    }
                    catch (SqlException ex)
                    {
                        transaction.Rollback();

                        // FK constraint violation
                        if (ex.Number == 547)
                        {
                            return BadRequest("El fondo especificado no existe");
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
        //

        // PUT: api/deposito/5
        [HttpPut("{id}")]
        public IActionResult Put(int id, [FromBody] DepositoUpdateModel input)
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
                        // Verificar existencia del depósito
                        if (!DepositoExistsById(id, connection, transaction))
                        {
                            transaction.Rollback();
                            return NotFound($"No se encontró el depósito con ID {id}");
                        }

                        // Actualizar depósito
                        string query = @"
                            UPDATE Deposito 
                            SET fechaDeposito = @FechaDeposito,
                                id_fondo = @IdFondo,
                                monto = @Monto
                            WHERE id_deposito = @Id;
                        ";

                        using (SqlCommand command = new SqlCommand(query, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@Id", id);
                            command.Parameters.AddWithValue("@FechaDeposito", input.FechaDeposito);
                            command.Parameters.AddWithValue("@IdFondo", input.IdFondo);
                            command.Parameters.AddWithValue("@Monto", input.Monto);

                            int rowsAffected = command.ExecuteNonQuery();

                            if (rowsAffected == 0)
                            {
                                transaction.Rollback();
                                return NotFound($"No se encontró el depósito con ID {id}");
                            }
                        }

                        // Obtener el depósito actualizado
                        var deposito = GetDepositoById(id, connection, transaction);

                        if (deposito == null)
                        {
                            transaction.Rollback();
                            return StatusCode(500, "Error al obtener el depósito actualizado");
                        }

                        transaction.Commit();
                        return Ok(deposito);
                    }
                    catch (SqlException ex)
                    {
                        transaction.Rollback();
                        if (ex.Number == 547)
                        {
                            return BadRequest("El fondo especificado no existe");
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

        // DELETE: api/deposito/5
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
                        // Verificar existencia
                        if (!DepositoExistsById(id, connection, transaction))
                        {
                            transaction.Rollback();
                            return NotFound($"No se encontró el depósito con ID {id}");
                        }

                        string query = "DELETE FROM Deposito WHERE id_deposito = @Id";
                        using (SqlCommand command = new SqlCommand(query, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@Id", id);
                            int rowsAffected = command.ExecuteNonQuery();

                            if (rowsAffected == 0)
                            {
                                transaction.Rollback();
                                return NotFound($"No se encontró el depósito con ID {id}");
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
                            return Conflict("No se puede eliminar porque tiene registros asociados");
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
        public class DepositoInputModel
        {
            [Required(ErrorMessage = "La fecha de depósito es obligatoria")]
            public DateTime FechaDeposito { get; set; }

            [Required(ErrorMessage = "El ID del fondo es obligatorio")]
            [Range(1, int.MaxValue, ErrorMessage = "ID de fondo inválido")]
            public int IdFondo { get; set; }

            [Required(ErrorMessage = "El monto es obligatorio")]
            [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor que cero")]
            public decimal Monto { get; set; }
        }

        public class DepositoUpdateModel
        {
            [Required(ErrorMessage = "La fecha de depósito es obligatoria")]
            public DateTime FechaDeposito { get; set; }

            [Required(ErrorMessage = "El ID del fondo es obligatorio")]
            [Range(1, int.MaxValue, ErrorMessage = "ID de fondo inválido")]
            public int IdFondo { get; set; }

            [Required(ErrorMessage = "El monto es obligatorio")]
            [Range(0.01, double.MaxValue, ErrorMessage = "El monto debe ser mayor que cero")]
            public decimal Monto { get; set; }
        }

        // Métodos auxiliares
        private bool DepositoExistsById(int id, SqlConnection connection, SqlTransaction transaction)
        {
            string query = "SELECT COUNT(*) FROM Deposito WHERE id_deposito = @Id";
            using (SqlCommand command = new SqlCommand(query, connection, transaction))
            {
                command.Parameters.AddWithValue("@Id", id);
                return Convert.ToInt32(command.ExecuteScalar()) > 0;
            }
        }

        private Deposito GetDepositoById(int id, SqlConnection connection, SqlTransaction transaction)
        {
            string query = "SELECT id_deposito, fechaDeposito, id_fondo, monto FROM Deposito WHERE id_deposito = @Id";
            using (SqlCommand command = new SqlCommand(query, connection, transaction))
            {
                command.Parameters.AddWithValue("@Id", id);

                using (SqlDataReader reader = command.ExecuteReader())
                {
                    if (reader.Read())
                    {
                        return new Deposito
                        {
                            IdDeposito = reader.GetInt32(reader.GetOrdinal("id_deposito")),
                            FechaDeposito = reader.GetDateTime(reader.GetOrdinal("fechaDeposito")),
                            IdFondo = reader.GetInt32(reader.GetOrdinal("id_fondo")),
                            Monto = reader.GetDecimal(reader.GetOrdinal("monto"))
                        };
                    }
                }
            }
            return null;
        }
    }
}