namespace FreshStock.API.Entities
{
    public class RestauranteProveedor : BaseEntity
    {
        public int RestauranteId { get; set; }
        public int ProveedorId { get; set; }
        public bool Activo { get; set; }
    }
}
