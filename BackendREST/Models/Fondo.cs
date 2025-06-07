namespace BackendREST.Models
{
    public class Fondo
    {
        public int Id { get; set; }
        public string NroCuenta { get; set; }
        public string NombreFondo { get; set; }
        public int TipoFondoId { get; set; }

        // Solo para lectura (no se usa en POST/PUT)
        public string TipoFondoNombre { get; set; }

        public decimal Saldo { get; set; }
        public string Descripcion { get; set; }
        public bool Activo { get; set; }
    }
}
