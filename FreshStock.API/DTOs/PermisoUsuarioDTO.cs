namespace FreshStock.API.DTOs
{
    public class PermisoUsuarioDTO
    {
        public int UsuarioId { get; set; }
        public bool PuedeCrearRestaurantes { get; set; }
        public List<PermisoRestauranteDTO> Restaurantes { get; set; } = new List<PermisoRestauranteDTO>();
    }

    public class PermisoRestauranteDTO
    {
        public int RestauranteId { get; set; }
        public string NombreRestaurante { get; set; } = string.Empty;
        public int Rol { get; set; }
        public bool PuedeCrearUsuarios { get; set; }
        public bool PuedeCrearCategorias { get; set; }
        public bool PuedeCrearProveedores { get; set; }
        public bool PuedeGestionarInventario { get; set; }
    }
}
