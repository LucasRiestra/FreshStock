using FreshStock.API.Enums;

namespace FreshStock.API.Interfaces
{
    public interface IPermisoService
    {
        // Verificar acceso a restaurante
        Task<bool> TieneAccesoARestauranteAsync(int usuarioId, int restauranteId);

        // Obtener rol del usuario en un restaurante
        Task<RolUsuario?> GetRolEnRestauranteAsync(int usuarioId, int restauranteId);

        // Verificar si tiene rol mínimo en un restaurante
        Task<bool> TieneRolMinimoEnRestauranteAsync(int usuarioId, int restauranteId, RolUsuario rolMinimo);

        // Verificar si es Admin o Gerente en al menos un restaurante
        Task<bool> EsAdminOGerenteEnAlgunRestauranteAsync(int usuarioId);

        // Verificar si es Admin en al menos un restaurante
        Task<bool> EsAdminEnAlgunRestauranteAsync(int usuarioId);

        // Permisos específicos por acción
        Task<bool> PuedeCrearUsuariosAsync(int usuarioId, int restauranteId);
        Task<bool> PuedeCrearCategoriasAsync(int usuarioId, int restauranteId);
        Task<bool> PuedeCrearProveedoresAsync(int usuarioId, int restauranteId);
        Task<bool> PuedeGestionarInventarioAsync(int usuarioId, int restauranteId);
        Task<bool> PuedeCrearRestaurantesAsync(int usuarioId);

        // Obtener información de permisos del usuario (para enviar al frontend)
        Task<PermisoUsuarioDTO> GetPermisosUsuarioAsync(int usuarioId);
    }

    // DTO para enviar al frontend con toda la info de permisos
    public class PermisoUsuarioDTO
    {
        public int UsuarioId { get; set; }
        public bool PuedeCrearRestaurantes { get; set; }
        public List<PermisoRestauranteDTO> Restaurantes { get; set; } = new();
    }

    public class PermisoRestauranteDTO
    {
        public int RestauranteId { get; set; }
        public string NombreRestaurante { get; set; } = string.Empty;
        public RolUsuario Rol { get; set; }
        public bool PuedeCrearUsuarios { get; set; }
        public bool PuedeCrearCategorias { get; set; }
        public bool PuedeCrearProveedores { get; set; }
        public bool PuedeGestionarInventario { get; set; }
    }
}
