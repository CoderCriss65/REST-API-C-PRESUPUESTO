using System.ComponentModel.DataAnnotations;

namespace BackendREST.Models
{
    public class TipoGasto
    {
        public int Id { get; set; }
        public string? Codigo { get; set; }  // 
        public string Nombre { get; set; }
        public string Descripcion { get; set; }
    }
}
