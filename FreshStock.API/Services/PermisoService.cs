using FreshStock.API.Data;
using FreshStock.API.Enums;
using FreshStock.API.Interfaces;
using MongoDB.Driver;

namespace FreshStock.API.Services
{
    public class PermisoService : IPermisoService
    {
        private readonly MongoDbContext _context;

        public PermisoService(MongoDbContext context)
        {
            _context = context;
        }

        public async Task<bool> TieneAccesoARestauranteAsync(int usuarioId, int restauranteId)
        {
            var asignacion = await _context.UsuarioRestaurantes
                .Find(ur => ur.UsuarioId == usuarioId && ur.RestauranteId == restauranteId && ur.Activo)
                .FirstOrDefaultAsync();

            return asignacion != null;
        }

        public async Task<RolUsuario?> GetRolEnRestauranteAsync(int usuarioId, int restauranteId)
        {
            var asignacion = await _context.UsuarioRestaurantes
                .Find(ur => ur.UsuarioId == usuarioId && ur.RestauranteId == restauranteId && ur.Activo)
                .FirstOrDefaultAsync();

            return asignacion?.Rol;
        }

        public async Task<bool> TieneRolMinimoEnRestauranteAsync(int usuarioId, int restauranteId, RolUsuario rolMinimo)
        {
            var rol = await GetRolEnRestauranteAsync(usuarioId, restauranteId);

            if (rol == null)
                return false;

            // Los roles son jerárquicos: Admin (1) > Gerente (2) > Empleado (3)
            // Un número menor significa más permisos
            return (int)rol.Value <= (int)rolMinimo;
        }

        public async Task<bool> EsAdminOGerenteEnAlgunRestauranteAsync(int usuarioId)
        {
            var asignacion = await _context.UsuarioRestaurantes
                .Find(ur => ur.UsuarioId == usuarioId &&
                           (ur.Rol == RolUsuario.Admin || ur.Rol == RolUsuario.Gerente) &&
                           ur.Activo)
                .FirstOrDefaultAsync();

            return asignacion != null;
        }

        public async Task<bool> EsAdminEnAlgunRestauranteAsync(int usuarioId)
        {
            var asignacion = await _context.UsuarioRestaurantes
                .Find(ur => ur.UsuarioId == usuarioId && ur.Rol == RolUsuario.Admin && ur.Activo)
                .FirstOrDefaultAsync();

            return asignacion != null;
        }

        // =====================
        // PERMISOS ESPECÍFICOS
        // =====================

        // Admin y Gerente pueden crear usuarios
        public async Task<bool> PuedeCrearUsuariosAsync(int usuarioId, int restauranteId)
        {
            return await TieneRolMinimoEnRestauranteAsync(usuarioId, restauranteId, RolUsuario.Gerente);
        }

        // Admin y Gerente pueden crear categorías
        public async Task<bool> PuedeCrearCategoriasAsync(int usuarioId, int restauranteId)
        {
            return await TieneRolMinimoEnRestauranteAsync(usuarioId, restauranteId, RolUsuario.Gerente);
        }

        // Admin y Gerente pueden crear proveedores
        public async Task<bool> PuedeCrearProveedoresAsync(int usuarioId, int restauranteId)
        {
            return await TieneRolMinimoEnRestauranteAsync(usuarioId, restauranteId, RolUsuario.Gerente);
        }

        // Todos (Admin, Gerente, Empleado) pueden gestionar inventario
        public async Task<bool> PuedeGestionarInventarioAsync(int usuarioId, int restauranteId)
        {
            return await TieneAccesoARestauranteAsync(usuarioId, restauranteId);
        }

        // Admin y Gerente pueden gestionar stock ideal
        public async Task<bool> PuedeGestionarStockIdealAsync(int usuarioId, int restauranteId)
        {
            // Verificar si es SuperAdmin
            if (await EsSuperAdminAsync(usuarioId))
                return true;

            return await TieneRolMinimoEnRestauranteAsync(usuarioId, restauranteId, RolUsuario.Gerente);
        }

        // Verificar si es SuperAdmin del sistema
        public async Task<bool> EsSuperAdminAsync(int usuarioId)
        {
            var usuario = await _context.Usuarios
                .Find(u => u.Id == usuarioId)
                .FirstOrDefaultAsync();

            return usuario?.EsSuperAdmin == true;
        }

        // Solo Admin o SuperAdmin puede crear nuevos restaurantes
        public async Task<bool> PuedeCrearRestaurantesAsync(int usuarioId)
        {
            // Verificar si es SuperAdmin del sistema
            var usuario = await _context.Usuarios
                .Find(u => u.Id == usuarioId)
                .FirstOrDefaultAsync();

            if (usuario?.EsSuperAdmin == true)
                return true;

            return await EsAdminEnAlgunRestauranteAsync(usuarioId);
        }

        // =====================
        // INFO COMPLETA PARA FRONTEND
        // =====================

        public async Task<PermisoUsuarioDTO> GetPermisosUsuarioAsync(int usuarioId)
        {
            // Verificar si es SuperAdmin del sistema
            var usuario = await _context.Usuarios
                .Find(u => u.Id == usuarioId)
                .FirstOrDefaultAsync();

            var esSuperAdmin = usuario?.EsSuperAdmin == true;

            var permisos = new PermisoUsuarioDTO
            {
                UsuarioId = usuarioId,
                PuedeCrearRestaurantes = esSuperAdmin || await EsAdminEnAlgunRestauranteAsync(usuarioId)
            };

            // Obtener todas las asignaciones del usuario
            var asignaciones = await _context.UsuarioRestaurantes
                .Find(ur => ur.UsuarioId == usuarioId && ur.Activo)
                .ToListAsync();

            foreach (var asignacion in asignaciones)
            {
                // Obtener nombre del restaurante
                var restaurante = await _context.Restaurantes
                    .Find(r => r.Id == asignacion.RestauranteId)
                    .FirstOrDefaultAsync();

                var permisoRestaurante = new PermisoRestauranteDTO
                {
                    RestauranteId = asignacion.RestauranteId,
                    NombreRestaurante = restaurante?.Nombre ?? "Desconocido",
                    Rol = asignacion.Rol,
                    // Admin y Gerente pueden crear usuarios, categorías, proveedores
                    PuedeCrearUsuarios = asignacion.Rol <= RolUsuario.Gerente,
                    PuedeCrearCategorias = asignacion.Rol <= RolUsuario.Gerente,
                    PuedeCrearProveedores = asignacion.Rol <= RolUsuario.Gerente,
                    // Todos pueden gestionar inventario
                    PuedeGestionarInventario = true
                };

                permisos.Restaurantes.Add(permisoRestaurante);
            }

            return permisos;
        }
    }
}
