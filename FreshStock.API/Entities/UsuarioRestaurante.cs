using FreshStock.API.Enums;

namespace FreshStock.API.Entities
{
    public class UsuarioRestaurante : BaseEntity
    {
        public int UsuarioId { get; set; }
        public int RestauranteId { get; set; }
        public RolUsuario Rol { get; set; }
        public bool Activo { get; set; }
    }
}
