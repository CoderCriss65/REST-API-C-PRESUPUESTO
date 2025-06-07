namespace BackendREST.Models
{
    public class Deposito           
    {
        public int IdDeposito { get; set; }
        public DateTime FechaDeposito { get; set; }
        public int IdFondo { get; set; }
        public decimal Monto { get; set; }
    }
}
