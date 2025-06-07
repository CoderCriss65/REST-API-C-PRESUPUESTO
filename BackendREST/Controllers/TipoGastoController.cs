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

    public class TipoGastoController : ControllerBase
    {
        private readonly IConfiguration _configuration;
        private readonly string _connectionString;

        public TipoGastoController(IConfiguration configuration)
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
        public ActionResult<IEnumerable<TipoGasto>> Get()
        {
            List<TipoGasto> tipos = new List<TipoGasto>();

            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string query = "SELECT Id,Codigo,Nombre,Descripcion from TipoGasto;";
                connection.Open();

                using (SqlCommand command = new SqlCommand(query, connection))
                using (SqlDataReader reader = command.ExecuteReader())
                {
                    while (reader.Read())
                    {
                        tipos.Add(new TipoGasto
                        {
                            Id = reader.GetInt32(0),//posiciones array de las columnas de la base de datos
                            Codigo = reader.GetString(1),
                            Nombre = reader.GetString(2),
                            Descripcion=reader.GetString(3)
                        });
                    }
                }
            }

            return Ok(tipos);
        }
        ////
        // GET: api/tipoGasto/id
        [HttpGet("{id}")]
        public ActionResult<TipoGasto> Get(int id)
        {
            using (SqlConnection connection = new SqlConnection(_connectionString))
            {
                string consulta = "SELECT Id,Codigo,Nombre,Descripcion FROM TipoGasto  WHERE Id = @Id";
                connection.Open();

                using (SqlCommand comando = new SqlCommand(consulta, connection))
                {
                    comando.Parameters.AddWithValue("@Id", id); //un escalar que tome el valor del parametro REFERENCIO EL VALOR

                    using (SqlDataReader reader = comando.ExecuteReader())
                    {
                        if (reader.Read())
                        {
                            return Ok(new TipoGasto
                            {
                                Id = reader.GetInt32(0),
                                Codigo = reader.GetString(1),
                                Nombre = reader.GetString(2),
                                Descripcion = reader.GetString(3)
                            });
                        }
                    }
                }
            }
            return NotFound();
        }///

        
        ///
        [HttpPost]
        public ActionResult<TipoGasto> Post([FromBody] TipoGastoInputModel input)
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
                        // 1. Generar nuevo código automático
                        string nuevoCodigo = GenerarNuevoCodigo(connection, transaction);

                        // 2. Insertar con el código generado
                        string query = @"INSERT INTO TipoGasto (Codigo, Nombre, Descripcion) 
                             VALUES (@Codigo, @Nombre, @Descripcion);
                             SELECT SCOPE_IDENTITY();";

                        using (SqlCommand command = new SqlCommand(query, connection, transaction))
                        {
                            command.Parameters.AddWithValue("@Codigo", nuevoCodigo);
                            command.Parameters.AddWithValue("@Nombre", input.Nombre);
                            command.Parameters.AddWithValue("@Descripcion", input.Descripcion);

                                //string.IsNullOrEmpty(input.Descripcion) ? (object)DBNull.Value : input.Descripcion);

                            int newId = Convert.ToInt32(command.ExecuteScalar());

                            // Crear objeto de respuesta
                            var tipoGasto = new TipoGasto
                            {
                                Id = newId,
                                Codigo = nuevoCodigo,
                                Nombre = input.Nombre,
                                Descripcion = input.Descripcion
                            };

                            transaction.Commit();
                            return CreatedAtAction(nameof(Get), new { id = newId }, tipoGasto);
                        }
                    }
                    catch (SqlException ex)
                    {
                        transaction.Rollback();
                        if (ex.Number == 2627) // Violación de constraint única
                        {
                            return Conflict("El nombre del tipo de gasto ya existe");
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

        // Modelo de entrada específico (sin Codigo) // que no recibe codigo, solo nombre y descripcion
        public class TipoGastoInputModel
        {
            [Required(ErrorMessage = "El nombre es obligatorio")]
            [StringLength(100, ErrorMessage = "Máximo 100 caracteres")]
            public string Nombre { get; set; }

            [StringLength(255, ErrorMessage = "Máximo 255 caracteres")]
            public string Descripcion { get; set; }
        }

        private string GenerarNuevoCodigo(SqlConnection connection, SqlTransaction transaction)
        {
            // Bloquear tabla para evitar concurrencia
            string query = "SELECT MAX(Codigo) FROM TipoGasto WITH (UPDLOCK, HOLDLOCK)";

            using (SqlCommand command = new SqlCommand(query, connection, transaction))
            {
                object result = command.ExecuteScalar();
                string ultimoCodigo = (result == DBNull.Value || result == null) ? null : (string)result;

                if (string.IsNullOrEmpty(ultimoCodigo))
                {
                    return "TG001";
                }

                // Extraer número y calcular siguiente (TG001 -> 001 -> 1)
                string numeroStr = ultimoCodigo.Substring(2);
                if (int.TryParse(numeroStr, out int ultimoNumero))
                {
                    return $"TG{(ultimoNumero + 1):000}";
                }

                // Si el formato es inválido, empezar desde 1
                return "TG001";
            }
        }//



        // PUT: api/tipoGasto/id
        [HttpPut("{id}")]
        public IActionResult Put(int id, [FromBody] TipoGastoUpdateModel input)
        {
            if (!ModelState.IsValid)
                return BadRequest(ModelState);

            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    // Consulta SQL corregida
                    string query = @"UPDATE TipoGasto 
                             SET Nombre = @Nombre,
                                 Descripcion = @Descripcion
                             WHERE Id = @Id";

                    connection.Open();

                    using (SqlCommand command = new SqlCommand(query, connection))
                    {
                        command.Parameters.AddWithValue("@Id", id);
                        command.Parameters.AddWithValue("@Nombre", input.Nombre);
                        command.Parameters.AddWithValue("@Descripcion",
                            string.IsNullOrEmpty(input.Descripcion) ? (object)DBNull.Value : input.Descripcion);

                        int rowsAffected = command.ExecuteNonQuery();

                        if (rowsAffected == 0)
                            return NotFound($"No se encontró el tipo de gasto con ID {id}");
                    }
                }

                return NoContent(); // 204 No Content
            }
            catch (SqlException ex)
            {
                // Detectar violación de clave única (Nombre duplicado)
                if (ex.Number == 2627)
                {
                    return Conflict("El nombre del tipo de gasto ya existe");
                }
                return StatusCode(500, $"Error de base de datos: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }

        // Modelo específico para actualización /auxiliar
        public class TipoGastoUpdateModel
        {
            [Required(ErrorMessage = "El nombre es obligatorio")]
            [StringLength(100, ErrorMessage = "Máximo 100 caracteres")]
            public string Nombre { get; set; }

            [StringLength(255, ErrorMessage = "Máximo 255 caracteres")]
            public string Descripcion { get; set; }
        }




        // DELETE: api/tipofondo/5
        [HttpDelete("{id}")]
        public IActionResult Delete(int id)
        {
            try
            {
                using (SqlConnection connection = new SqlConnection(_connectionString))
                {
                    connection.Open();

                    // 1. Primero verificar si existe el registro
                    string checkQuery = "SELECT COUNT(*) FROM TipoGasto WHERE Id = @Id";
                    using (SqlCommand checkCommand = new SqlCommand(checkQuery, connection))
                    {
                        checkCommand.Parameters.AddWithValue("@Id", id);
                        int exists = (int)checkCommand.ExecuteScalar();
                        if (exists == 0)
                            return NotFound($"No se encontró el tipo de gasto con ID {id}");
                    }

                    // 2. Intentar eliminar el registro
                    string deleteQuery = "DELETE FROM TipoGasto WHERE Id = @Id";
                    using (SqlCommand deleteCommand = new SqlCommand(deleteQuery, connection))
                    {
                        deleteCommand.Parameters.AddWithValue("@Id", id);
                        int rowsAffected = deleteCommand.ExecuteNonQuery();

                        if (rowsAffected > 0)
                            return NoContent(); // 204 No Content

                        return StatusCode(500, "No se pudo eliminar el registro");
                    }
                }
            }
            catch (SqlException ex)
            {
                // Manejar error de integridad referencial (registros dependientes)
                if (ex.Number == 547) // FK constraint violation n de restricción de clave foránea (FOREIGN KEY constraint violation). 
                {
                    return Conflict("No se puede eliminar porque tiene registros asociados en otras tablas es llave foranea");
                }
                return StatusCode(500, $"Error de base de datos: {ex.Message}");
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Error interno del servidor: {ex.Message}");
            }
        }






    }/////////
}
