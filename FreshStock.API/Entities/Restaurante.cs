namespace FreshStock.API.Entities
{
    public class Restaurante : BaseEntity
    {
        public string Nombre { get; set; }
        public string Direccion { get; set; }
        public string? Telefono { get; set; }
        public bool Activo { get; set; }
    }
}
