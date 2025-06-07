namespace BackendREST.Models
{
    public class Presupuesto
    {
        public int Id { get; set; }
        public byte Mes { get; set; }
        public int TipoGastoId { get; set; }
        public int Anio { get; set; }
        public decimal Monto { get; set; }

        // Nueva propiedad para el nombre del tipo de gasto
        public string TipoGastoNombre { get; set; }

        public string NombreMes
        {
            get
            {
                return Mes switch
                {
                    1 => "Enero",
                    2 => "Febrero",
                    3 => "Marzo",
                    4 => "Abril",
                    5 => "Mayo",
                    6 => "Junio",
                    7 => "Julio",
                    8 => "Agosto",
                    9 => "Septiembre",
                    10 => "Octubre",
                    11 => "Noviembre",
                    12 => "Diciembre",
                    _ => "Mes inválido"
                };
            }
        }
    }
}