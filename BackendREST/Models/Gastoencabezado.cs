namespace BackendREST.Models
{
    public class Gastoencabezado
    {
        public int Id { get; set; }
        public DateTime Fecha { get; set; }
        public int FondoId { get; set; }
        public string Observaciones { get; set; }
        public string NombreComercio { get; set; } = string.Empty;
        public string TipoDocumento { get; set; } = string.Empty;
        public decimal Total { get; set; }
    }
}