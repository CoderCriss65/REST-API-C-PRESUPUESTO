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
    public class GastoencabezadoController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public GastoencabezadoController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(_connectionString))
            {
                throw new ArgumentNullException(nameof(_connectionString), "Connection string is missing in configuration");
            }
        }

        // GET: api/gasto_encabezado
        [HttpGet]
        public ActionResult<IEnumerable<Gastoencabezado>> Get()
        {
            List<Gastoencabezado> gastos = new List<Gastoencabezado>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = @"
                    SELECT GE.id, GE.fecha, GE.fondo_id, GE.observaciones, 
                           GE.nombre_comercio, GE.tipo_documento, GE.total,
                           F.NroCuenta, F.NombreFondo
                    FROM gasto_encabezado GE
                    INNER JOIN Fondo F ON GE.fondo_id = F.Id";

                connection.Open();

                using (SqlCommand command = new SqlCommand(query, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        gastos.Add(new Gastoencabezado
                        {
                            Id = reader.GetInt32(0),
                            Fecha = reader.GetDateTime(1),
                            FondoId = reader.GetInt32(2),
                            Observaciones = reader.IsDBNull(3) ? null : reader.GetString(3),
                            NombreComercio = reader.GetString(4),
                            TipoDocumento = reader.GetString(5),
                            Total = reader.GetDecimal(6),
                           
                        });
                    }
                }
            }

            return Ok(gastos);
        }

        // GET: api/gasto_encabezado/5
        [HttpGet("{id}")]
        public ActionResult<Gastoencabezado> Get(int id)
        {
            Gastoencabezado gasto = null;

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = @"
                    SELECT GE.id, GE.fecha, GE.fondo_id, GE.observaciones, 
                           GE.nombre_comercio, GE.tipo_documento, GE.total,
                           F.NroCuenta, F.NombreFondo
                    FROM gasto_encabezado GE
                    INNER JOIN Fondo F ON GE.fondo_id = F.Id
                    WHERE GE.id = @Id";

                connection.Open();

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            gasto = new Gastoencabezado
                            {
                                Id = reader.GetInt32(0),
                                Fecha = reader.GetDateTime(1),
                                FondoId = reader.GetInt32(2),
                                Observaciones = reader.IsDBNull(3) ? null : reader.GetString(3),
                                NombreComercio = reader.GetString(4),
                                TipoDocumento = reader.GetString(5),
                                Total = reader.GetDecimal(6),
                                
                            };
                        }
                    }
                }
            }

            if (gasto == null)
                return NotFound();

            return Ok(gasto);
        }

        // POST: api/gasto_encabezado
        [HttpPost]
        public ActionResult<Gastoencabezado> Post([FromBody] Gastoencabezado gasto)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    string query = @"
                        INSERT INTO gasto_encabezado 
                        (fecha, fondo_id, observaciones, nombre_comercio, tipo_documento, total) 
                        VALUES (@Fecha, @FondoId, @Observaciones, @NombreComercio, @TipoDocumento, @Total);
                        SELECT SCOPE_IDENTITY();";

                    connection.Open();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Fecha", gasto.Fecha);
                        command.Parameters.AddWithValue("@FondoId", gasto.FondoId);
                        command.Parameters.AddWithValue("@Observaciones", (object)gasto.Observaciones ?? DBNull.Value);
                        command.Parameters.AddWithValue("@NombreComercio", gasto.NombreComercio);
                        command.Parameters.AddWithValue("@TipoDocumento", gasto.TipoDocumento);
                        command.Parameters.AddWithValue("@Total", gasto.Total);

                        int newId = Convert.ToInt32(command.ExecuteScalar());
                        gasto.Id = newId;
                    }
                }

                return CreatedAtAction(nameof(Get), new { id = gasto.Id }, gasto);
            }
            catch (SqlException ex)
            {
                if (ex.Number == 547) // FK violation
                {
                    return BadRequest("El fondo_id especificado no existe");
                }
                return StatusCode(500, $"Error de base de datos: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        // PUT: api/gasto_encabezado/5
        [HttpPut("{id}")]
        public IActionResult Put(int id, [FromBody] Gastoencabezado gasto)
        {
            if (id != gasto.Id)
                return BadRequest("ID en URL no coincide con ID en objeto");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    string query = @"
                        UPDATE gasto_encabezado 
                        SET fecha = @Fecha, 
                            fondo_id = @FondoId, 
                            observaciones = @Observaciones, 
                            nombre_comercio = @NombreComercio, 
                            tipo_documento = @TipoDocumento, 
                            total = @Total
                        WHERE id = @Id";

                    connection.Open();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        command.Parameters.AddWithValue("@Fecha", gasto.Fecha);
                        command.Parameters.AddWithValue("@FondoId", gasto.FondoId);
                        command.Parameters.AddWithValue("@Observaciones", (object)gasto.Observaciones ?? DBNull.Value);
                        command.Parameters.AddWithValue("@NombreComercio", gasto.NombreComercio);
                        command.Parameters.AddWithValue("@TipoDocumento", gasto.TipoDocumento);
                        command.Parameters.AddWithValue("@Total", gasto.Total);

                        int rowsAffected = command.ExecuteNonQuery();
                        if (rowsAffected == 0)
                            return NotFound();
                    }
                }

                return NoContent();
            }
            catch (SqlException ex)
            {
                if (ex.Number == 547)
                {
                    return BadRequest("El fondo_id especificado no existe");
                }
                return StatusCode(500, $"Error de base de datos: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        // DELETE: api/gasto_encabezado/5
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    string query = "DELETE FROM gasto_encabezado WHERE id = @Id";
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
                if (ex.Number == 547)
                {
                    return BadRequest("No se puede eliminar porque existen registros dependientes");
                }
                return StatusCode(500, $"Error de base de datos: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }
    }

    // Modelo en el mismo archivo (o puede moverse a carpeta Models)
   
}