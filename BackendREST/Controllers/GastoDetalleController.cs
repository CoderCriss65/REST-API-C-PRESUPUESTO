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
    public class GastoDetalleController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public GastoDetalleController(IConfiguration configuration)
        {
            _configuration = configuration;
            _connectionString = _configuration.GetConnectionString("DefaultConnection");

            if (string.IsNullOrEmpty(_connectionString))
            {
                throw new ArgumentNullException(nameof(_connectionString), "Connection string is missing in configuration");
            }
        }

        // GET: api/gastodetalle
        [HttpGet]
        public ActionResult<IEnumerable<GastoDetalle>> Get()
        {
            List<GastoDetalle> detalles = new List<GastoDetalle>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = "SELECT id, gasto_encabezado_id, TipoGasto_id, monto FROM gasto_detalle";

                connection.Open();

                using (SqlCommand command = new SqlCommand(query, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        detalles.Add(new GastoDetalle
                        {
                            Id = reader.GetInt32(0),
                            GastoEncabezadoId = reader.GetInt32(1),
                            TipoGastoId = reader.GetInt32(2),
                            Monto = reader.GetDecimal(3)
                        });
                    }
                }
            }

            return Ok(detalles);
        }







        public class GastoDetalleView
        {
            public long Numero { get; set; }
            public string Fecha { get; set; } = string.Empty;
            public string Fondo { get; set; } = string.Empty;
            public string TipoGasto { get; set; } = string.Empty;
            public string Monto { get; set; } = "0.00";
        }


        ///  
        [HttpGet("datosDetalle")]
        public ActionResult<IEnumerable<GastoDetalleView>> GetDatosDetalle()
        {
            List<GastoDetalleView> resultados = new List<GastoDetalleView>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = @"
            SELECT 
                ROW_NUMBER() OVER (ORDER BY ge.fecha DESC) AS Numero,
                CONVERT(VARCHAR(10), ge.fecha, 120) AS Fecha,
                f.NombreFondo AS Fondo,
                tg.Nombre AS [TipoGasto],
                FORMAT(gd.monto, 'N2') AS Monto
            FROM gasto_detalle gd
            INNER JOIN gasto_encabezado ge ON gd.gasto_encabezado_id = ge.id
            INNER JOIN Fondo f ON ge.fondo_id = f.Id
            INNER JOIN TipoGasto tg ON gd.TipoGasto_id = tg.Id";

                connection.Open();

                using (SqlCommand command = new SqlCommand(query, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        resultados.Add(new GastoDetalleView
                        {
                            Numero = reader.GetInt64(reader.GetOrdinal("Numero")),
                            Fecha = reader.GetString(reader.GetOrdinal("Fecha")),
                            Fondo = reader.GetString(reader.GetOrdinal("Fondo")),
                            TipoGasto = reader.GetString(reader.GetOrdinal("TipoGasto")),
                            Monto = reader.GetString(reader.GetOrdinal("Monto"))
                        });
                    }
                }
            }

            return Ok(resultados);
        }


































        // GET: api/gastodetalle/5
        [HttpGet("{id}")]
        public ActionResult<GastoDetalle> Get(int id)
        {
            GastoDetalle detalle = null;

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = @"
                    SELECT id, gasto_encabezado_id, TipoGasto_id, monto 
                    FROM gasto_detalle 
                    WHERE id = @Id";

                connection.Open();

                using (SqlCommand command = new SqlCommand(query, connection))
                {
                    command.Parameters.AddWithValue("@Id", id);

                    using (SqlDataReader reader = command.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            detalle = new GastoDetalle
                            {
                                Id = reader.GetInt32(0),
                                GastoEncabezadoId = reader.GetInt32(1),
                                TipoGastoId = reader.GetInt32(2),
                                Monto = reader.GetDecimal(3)
                            };
                        }
                    }
                }
            }

            if (detalle == null)
                return NotFound();

            return Ok(detalle);
        }

        // POST: api/gastodetalle
        [HttpPost]
        public ActionResult<GastoDetalle> Post([FromBody] GastoDetalle detalle)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    string query = @"
                        INSERT INTO gasto_detalle 
                        (gasto_encabezado_id, TipoGasto_id, monto) 
                        VALUES (@GastoEncabezadoId, @TipoGastoId, @Monto);
                        SELECT SCOPE_IDENTITY();";

                    connection.Open();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@GastoEncabezadoId", detalle.GastoEncabezadoId);
                        command.Parameters.AddWithValue("@TipoGastoId", detalle.TipoGastoId);
                        command.Parameters.AddWithValue("@Monto", detalle.Monto);

                        int newId = Convert.ToInt32(command.ExecuteScalar());
                        detalle.Id = newId;
                    }
                }

                return CreatedAtAction(nameof(Get), new { id = detalle.Id }, detalle);
            }
            catch (SqlException ex)
            {
                if (ex.Number == 547) // FK violation
                {
                    return BadRequest("El ID de encabezado o tipo de gasto especificado no existe");
                }
                return StatusCode(500, $"Error de base de datos: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        ///
        // POST: api/gastodetalle/masivo - INSERCIÓN MASIVA
        [HttpPost("masivo")]
        public ActionResult<List<GastoDetalle>> PostBulk([FromBody] List<GastoDetalle> detalles)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            if (detalles == null || detalles.Count == 0)
                return BadRequest("La lista de detalles no puede estar vacía");

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();
                    using (SqlTransaction transaction = connection.BeginTransaction())
                    {
                        try
                        {
                            string query = @"
                                INSERT INTO gasto_detalle 
                                (gasto_encabezado_id, TipoGasto_id, monto) 
                                VALUES (@GastoEncabezadoId, @TipoGastoId, @Monto);
                                SELECT SCOPE_IDENTITY();";

                            foreach (var detalle in detalles)
                            {
                                using (SqlCommand command = new SqlCommand(query, connection, transaction))
                                {
                                    command.Parameters.AddWithValue("@GastoEncabezadoId", detalle.GastoEncabezadoId);
                                    command.Parameters.AddWithValue("@TipoGastoId", detalle.TipoGastoId);
                                    command.Parameters.AddWithValue("@Monto", detalle.Monto);

                                    int newId = Convert.ToInt32(command.ExecuteScalar());
                                    detalle.Id = newId;
                                }
                            }

                            transaction.Commit();
                            return StatusCode(201, detalles);
                        }
                        catch (Exception)
                        {
                            transaction.Rollback();
                            throw;
                        }
                    }
                }
            }
            catch (SqlException ex)
            {
                if (ex.Number == 547) // FK violation
                {
                    return BadRequest("Uno de los IDs de encabezado o tipo de gasto no existe");
                }
                return StatusCode(500, $"Error de base de datos: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }


        ///

        // PUT: api/gastodetalle/5
        [HttpPut("{id}")]
        public IActionResult Put(int id, [FromBody] GastoDetalle detalle)
        {
            if (id != detalle.Id)
                return BadRequest("ID en URL no coincide con ID en objeto");

            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    string query = @"
                        UPDATE gasto_detalle 
                        SET gasto_encabezado_id = @GastoEncabezadoId, 
                            TipoGasto_id = @TipoGastoId, 
                            monto = @Monto
                        WHERE id = @Id";

                    connection.Open();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        command.Parameters.AddWithValue("@GastoEncabezadoId", detalle.GastoEncabezadoId);
                        command.Parameters.AddWithValue("@TipoGastoId", detalle.TipoGastoId);
                        command.Parameters.AddWithValue("@Monto", detalle.Monto);

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
                    return BadRequest("El ID de encabezado o tipo de gasto especificado no existe");
                }
                return StatusCode(500, $"Error de base de datos: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        // DELETE: api/gastodetalle/5
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    string query = "DELETE FROM gasto_detalle WHERE id = @Id";
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