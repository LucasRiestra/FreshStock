using AutoMapper;
using FreshStock.API.DTOs;
using FreshStock.API.Entities;

namespace FreshStock.API.Mappings
{
    public class MappingProfile : Profile
    {
        public MappingProfile()
        {
            // Restaurante
            CreateMap<Restaurante, RestauranteResponseDTO>();
            CreateMap<CreateRestauranteDTO, Restaurante>();
            CreateMap<UpdateRestauranteDTO, Restaurante>();

            // Usuario
            CreateMap<Usuario, UsuarioResponseDTO>();
            CreateMap<CreateUsuarioDTO, Usuario>()
                .ForMember(dest => dest.PasswordHash, opt => opt.MapFrom(src => src.Password));
            CreateMap<UpdateUsuarioDTO, Usuario>();

            // Categoria
            CreateMap<Categoria, CategoriaResponseDTO>();
            CreateMap<CreateCategoriaDTO, Categoria>();

            // Proveedor
            CreateMap<Proveedor, ProveedorResponseDTO>();
            CreateMap<CreateProveedorDTO, Proveedor>();
            CreateMap<UpdateProveedorDTO, Proveedor>();

            // Producto
            CreateMap<Producto, ProductoResponseDTO>();
            CreateMap<CreateProductoDTO, Producto>();
            CreateMap<UpdateProductoDTO, Producto>();

            // StockLocal
            CreateMap<StockLocal, StockLocalResponseDTO>();
            CreateMap<CreateStockLocalDTO, StockLocal>();
            CreateMap<UpdateStockLocalDTO, StockLocal>();

            // MovimientoInventario
            CreateMap<MovimientoInventario, MovimientoInventarioResponseDTO>();
            CreateMap<CreateMovimientoInventarioDTO, MovimientoInventario>();

            // UsuarioRestaurante
            CreateMap<UsuarioRestaurante, UsuarioRestauranteResponseDTO>();
            CreateMap<CreateUsuarioRestauranteDTO, UsuarioRestaurante>();
            CreateMap<UpdateUsuarioRestauranteDTO, UsuarioRestaurante>();

            // RestauranteProveedor
            CreateMap<RestauranteProveedor, RestauranteProveedorResponseDTO>();
            CreateMap<CreateRestauranteProveedorDTO, RestauranteProveedor>();
            CreateMap<UpdateRestauranteProveedorDTO, RestauranteProveedor>();

            // RestauranteCategoria
            CreateMap<RestauranteCategoria, RestauranteCategoriaResponseDTO>();
            CreateMap<CreateRestauranteCategoriaDTO, RestauranteCategoria>();
            CreateMap<UpdateRestauranteCategoriaDTO, RestauranteCategoria>();

            // StockIdealRestaurante
            CreateMap<StockIdealRestaurante, StockIdealRestauranteResponseDTO>();
            CreateMap<CreateStockIdealRestauranteDTO, StockIdealRestaurante>();
            CreateMap<UpdateStockIdealRestauranteDTO, StockIdealRestaurante>();

            // Inventario
            CreateMap<Inventario, InventarioResponseDTO>();
            CreateMap<CreateInventarioDTO, Inventario>();

            // InventarioDetalle
            CreateMap<InventarioDetalle, InventarioDetalleResponseDTO>();
            CreateMap<CreateInventarioDetalleDTO, InventarioDetalle>();

            // AlertaStock
            CreateMap<AlertaStock, AlertaStockResponseDTO>();
        }
    }
}
