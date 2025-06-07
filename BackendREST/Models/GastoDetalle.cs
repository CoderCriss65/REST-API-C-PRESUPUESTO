namespace BackendREST.Models
{
    public class GastoDetalle
    {
        public int Id { get; set; }
        public int GastoEncabezadoId { get; set; }
        public int TipoGastoId { get; set; }
        public decimal Monto { get; set; }
    }
}