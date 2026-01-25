using FreshStock.API.Entities;
using Microsoft.EntityFrameworkCore;

namespace FreshStock.API.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
        {
        }

        public DbSet<Restaurante> Restaurantes { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Proveedor> Proveedores { get; set; }
        public DbSet<Producto> Productos { get; set; }
        public DbSet<StockLocal> StockLocal { get; set; }
        public DbSet<MovimientoInventario> MovimientosInventario { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configuración de Restaurante
            modelBuilder.Entity<Restaurante>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Direccion).HasMaxLength(200);
                entity.Property(e => e.Telefono).HasMaxLength(20);
                entity.Property(e => e.Activo).HasDefaultValue(true);
            });

            // Configuración de Usuario
            modelBuilder.Entity<Usuario>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Email).IsRequired().HasMaxLength(100);
                entity.Property(e => e.PasswordHash).IsRequired().HasMaxLength(255);
                entity.Property(e => e.Rol).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Activo).HasDefaultValue(true);
                entity.Property(e => e.RefreshToken).HasMaxLength(500);
                entity.Property(e => e.RefreshTokenExpiry);

                // Relación con Restaurante
                entity.HasOne<Restaurante>()
                    .WithMany()
                    .HasForeignKey(e => e.RestauranteId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Índice único para email
                entity.HasIndex(e => e.Email).IsUnique();
            });

            // Configuración de Categoria
            modelBuilder.Entity<Categoria>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(50);
            });

            // Configuración de Proveedor
            modelBuilder.Entity<Proveedor>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
                entity.Property(e => e.Telefono).HasMaxLength(20);
                entity.Property(e => e.Email).HasMaxLength(100);
                entity.Property(e => e.Contacto).HasMaxLength(100);
                entity.Property(e => e.Activo).HasDefaultValue(true);
            });

            // Configuración de Producto
            modelBuilder.Entity<Producto>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Nombre).IsRequired().HasMaxLength(100);
                entity.Property(e => e.UnidadMedida).IsRequired().HasMaxLength(20);
                entity.Property(e => e.StockMinimo).HasPrecision(10, 2);
                entity.Property(e => e.Activo).HasDefaultValue(true);

                // Relación con Proveedor
                entity.HasOne<Proveedor>()
                    .WithMany()
                    .HasForeignKey(e => e.ProveedorId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Relación con Categoria
                entity.HasOne<Categoria>()
                    .WithMany()
                    .HasForeignKey(e => e.CategoriaId)
                    .OnDelete(DeleteBehavior.Restrict);
            });

            // Configuración de StockLocal
            modelBuilder.Entity<StockLocal>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Lote).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Cantidad).HasPrecision(10, 2);
                entity.Property(e => e.CostoUnitario).HasPrecision(10, 2);
                entity.Property(e => e.FechaEntrada).HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Relación con Producto
                entity.HasOne<Producto>()
                    .WithMany()
                    .HasForeignKey(e => e.ProductoId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Relación con Restaurante
                entity.HasOne<Restaurante>()
                    .WithMany()
                    .HasForeignKey(e => e.RestauranteId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Índice único compuesto (ProductoId, RestauranteId, Lote)
                entity.HasIndex(e => new { e.ProductoId, e.RestauranteId, e.Lote })
                    .IsUnique();
            });

            // Configuración de MovimientoInventario
            modelBuilder.Entity<MovimientoInventario>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.Tipo).IsRequired().HasMaxLength(20);
                entity.Property(e => e.Cantidad).HasPrecision(10, 2);
                entity.Property(e => e.Lote).IsRequired().HasMaxLength(50);
                entity.Property(e => e.Motivo).IsRequired().HasMaxLength(100);
                entity.Property(e => e.CostoUnitario).HasPrecision(10, 2);
                entity.Property(e => e.Fecha).HasDefaultValueSql("CURRENT_TIMESTAMP");

                // Relación con Producto
                entity.HasOne<Producto>()
                    .WithMany()
                    .HasForeignKey(e => e.ProductoId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Relación con Restaurante (origen)
                entity.HasOne<Restaurante>()
                    .WithMany()
                    .HasForeignKey(e => e.RestauranteId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Relación con Usuario
                entity.HasOne<Usuario>()
                    .WithMany()
                    .HasForeignKey(e => e.UsuarioId)
                    .OnDelete(DeleteBehavior.Restrict);

                // Nota: RestauranteDestinoId es opcional, no se configura relación obligatoria
            });
        }
    }
}
