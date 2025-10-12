namespace FreshStock.API.Entities
{
    public class Producto : BaseEntity
    {
        public int ProveedorId { get; set; }
        public int CategoriaId { get; set; }
        public string Nombre { get; set; }
        public string UnidadMedida { get; set; }
        public decimal StockMinimo { get; set; }
        public decimal CostoUnitario { get; set; }
        public bool Activo { get; set; }
    }
}