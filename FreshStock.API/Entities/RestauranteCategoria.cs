namespace FreshStock.API.Entities
{
    public class RestauranteCategoria : BaseEntity
    {
        public int RestauranteId { get; set; }
        public int CategoriaId { get; set; }
        public bool Activo { get; set; }
    }
}
