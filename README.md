# 🍽️ FreshStock - Sistema de Gestión de Inventario para Restaurantes

[![.NET](https://img.shields.io/badge/.NET-8.0-purple)](https://dotnet.microsoft.com/)
[![PostgreSQL](https://img.shields.io/badge/PostgreSQL-Supabase-blue)](https://supabase.com/)
[![License](https://img.shields.io/badge/License-MIT-green.svg)](LICENSE)

**FreshStock** es una API REST completa para la gestión de inventario de productos perecederos en cadenas de restaurantes, desarrollada con **ASP.NET Core 8** y **PostgreSQL**.

---

## 📋 Tabla de Contenidos

- [Características](#-características)
- [Arquitectura](#-arquitectura)
- [Tecnologías](#-tecnologías)
- [Modelo de Datos](#-modelo-de-datos)
- [Instalación](#-instalación)
- [Configuración](#-configuración)
- [Uso](#-uso)
- [Endpoints API](#-endpoints-api)
- [Funcionalidades Destacadas](#-funcionalidades-destacadas)
- [Autor](#-autor)

---

## ✨ Características

- ✅ **Gestión Multi-Restaurante**: Administra inventario de múltiples locales desde una única API
- ✅ **Control de Stock en Tiempo Real**: Actualización automática de inventario con cada movimiento
- ✅ **Trazabilidad Completa**: Sistema de lotes para seguimiento de productos perecederos
- ✅ **Gestión de Mermas**: Registro detallado de pérdidas por caducidad, daño o robo
- ✅ **Transferencias entre Restaurantes**: Movimiento de stock entre locales
- ✅ **Reversión de Movimientos**: Sistema de corrección de errores sin perder trazabilidad
- ✅ **Creación Masiva de Productos**: Endpoint bulk para alta rápida de catálogos
- ✅ **Validaciones de Negocio**: Control de stock disponible, lotes únicos y consistencia de datos
- ✅ **Soft Delete**: Desactivación lógica de registros manteniendo historial

---

## 🏗️ Arquitectura

El proyecto sigue una **arquitectura en capas** con separación de responsabilidades:

```
FreshStock.API/
├── Controllers/          # API REST Controllers
├── Services/            # Lógica de negocio
├── Interfaces/          # Contratos de servicios
├── DTOs/                # Data Transfer Objects
├── Entities/            # Modelos de dominio
├── Data/                # DbContext y configuración EF Core
├── Mappings/            # Perfiles de AutoMapper
└── Migrations/          # Migraciones de base de datos
```

### Flujo de Datos

```
Cliente → Controller → Service → Repository (DbContext) → PostgreSQL
                  ↓
              AutoMapper
                  ↓
                 DTO
```

---

## 🛠️ Tecnologías

### Backend
- **ASP.NET Core 8** - Framework web
- **Entity Framework Core 9** - ORM
- **AutoMapper 12** - Mapeo objeto-objeto
- **PostgreSQL** - Base de datos relacional
- **Npgsql** - Driver para PostgreSQL

### Infraestructura
- **Supabase** - Hosting de PostgreSQL
- **Swagger/OpenAPI** - Documentación interactiva de la API

### Patrones y Prácticas
- ✅ Repository Pattern
- ✅ Dependency Injection
- ✅ DTOs para separación de capas
- ✅ Async/Await para operaciones I/O
- ✅ Transacciones para integridad de datos
- ✅ Data Annotations para validación

---

## 📊 Modelo de Datos

### Entidades Principales

**Restaurante**
- Gestión de múltiples locales
- Estado activo/inactivo

**Usuario**
- Vinculado a un restaurante
- Roles: Admin, Gerente, Empleado
- Registro de quién realiza cada operación

**Categoría**
- Clasificación de productos (Bebidas, Carnes, Lácteos, etc.)

**Proveedor**
- Información de contacto
- Asociación con productos

**Producto**
- Vinculado a proveedor y categoría
- Costo unitario centralizado
- Stock mínimo configurable
- Unidad de medida (Unidad, Kg, L, etc.)

**StockLocal**
- Inventario por restaurante
- Control por lotes
- Fecha de caducidad
- Costo y fecha de entrada
- **Índice único**: (ProductoId, RestauranteId, Lote)

**MovimientoInventario**
- Registro de todas las operaciones
- Tipos: Entrada, Salida
- Motivos: Compra, Venta, Ajuste, Merma, Transferencia
- Trazabilidad completa con usuario y fecha
- Sistema de reversión sin eliminación

### Relaciones

```
Restaurante 1 ─── N Usuario
Proveedor   1 ─── N Producto
Categoria   1 ─── N Producto
Producto    1 ─── N StockLocal
Producto    1 ─── N MovimientoInventario
Restaurante 1 ─── N StockLocal
Restaurante 1 ─── N MovimientoInventario
Usuario     1 ─── N MovimientoInventario
```

---

## 🚀 Instalación

### Requisitos Previos

- [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
- [PostgreSQL](https://www.postgresql.org/download/) o cuenta en [Supabase](https://supabase.com/)
- [Git](https://git-scm.com/downloads)

### Pasos

1. **Clonar el repositorio**

```bash
git clone https://github.com/LucasRiestra/FreshStock.git
cd FreshStock
```

2. **Instalar dependencias**

```bash
cd FreshStock.API
dotnet restore
```

3. **Configurar la base de datos** (ver sección [Configuración](#-configuración))

4. **Aplicar migraciones**

```bash
dotnet ef database update
```

5. **Ejecutar la aplicación**

```bash
dotnet run
```

La API estará disponible en: `http://localhost:5140`

Swagger UI: `http://localhost:5140/swagger`

---

## ⚙️ Configuración

### Connection String

Edita `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=YOUR_HOST;Port=5432;Database=postgres;Username=YOUR_USER;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=true"
  }
}
```

### Configuración de Supabase

Para usar Supabase (recomendado para producción):

1. Crea una cuenta en [Supabase](https://supabase.com/)
2. Crea un nuevo proyecto
3. Ve a **Settings → Database → Connection String**
4. Selecciona **Session Pooler** (puerto 5432)
5. Copia el connection string y actualiza `appsettings.json`

Ejemplo para Supabase:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Host=aws-1-eu-north-1.pooler.supabase.com;Port=5432;Database=postgres;Username=postgres.xxxxx;Password=YOUR_PASSWORD;SSL Mode=Require;Trust Server Certificate=true"
  }
}
```

---

## 💻 Uso

### Ejemplo de Flujo Completo

#### 1. Crear Restaurante

```bash
POST /api/restaurante
Content-Type: application/json

{
  "nombre": "Restaurante Central",
  "direccion": "Calle Mayor 123, Madrid",
  "telefono": "+34 912 345 678"
}
```

**Respuesta:**
```json
{
  "id": 1,
  "nombre": "Restaurante Central",
  "direccion": "Calle Mayor 123, Madrid",
  "telefono": "+34 912 345 678",
  "activo": true
}
```

#### 2. Crear Categoría

```bash
POST /api/categoria
Content-Type: application/json

{
  "nombre": "BEBIDAS"
}
```

#### 3. Crear Proveedor

```bash
POST /api/proveedor
Content-Type: application/json

{
  "nombre": "Coca-Cola",
  "telefono": "+34 900 123 456",
  "email": "pedidos@cocacola.es",
  "contacto": "Juan García"
}
```

#### 4. Crear Productos en Masa

```bash
POST /api/producto/bulk
Content-Type: application/json

{
  "proveedorId": 1,
  "categoriaId": 1,
  "productos": [
    {
      "nombre": "Coca-Cola Original 330ml",
      "unidadMedida": "Unidad",
      "stockMinimo": 50,
      "costoUnitario": 0.85
    },
    {
      "nombre": "Coca-Cola Zero 330ml",
      "unidadMedida": "Unidad",
      "stockMinimo": 40,
      "costoUnitario": 0.85
    }
  ]
}
```

#### 5. Registrar Entrada de Stock

```bash
POST /api/stocklocal
Content-Type: application/json

{
  "productoId": 1,
  "restauranteId": 1,
  "lote": "LOTE-2025-001",
  "cantidad": 100,
  "fechaCaducidad": "2026-12-31"
}
```

#### 6. Registrar Venta (Salida)

```bash
POST /api/movimientoinventario
Content-Type: application/json

{
  "tipo": "Salida",
  "productoId": 1,
  "restauranteId": 1,
  "cantidad": 10,
  "lote": "LOTE-2025-001",
  "motivo": "Venta",
  "usuarioId": 1
}
```

#### 7. Registrar Merma

```bash
POST /api/movimientoinventario/merma
Content-Type: application/json

{
  "productoId": 1,
  "restauranteId": 1,
  "lote": "LOTE-2025-001",
  "cantidad": 3,
  "tipoMerma": "Caducidad",
  "usuarioId": 1
}
```

---

## 📡 Endpoints API

### Restaurantes
- `GET    /api/restaurante` - Listar todos
- `GET    /api/restaurante/{id}` - Obtener por ID
- `POST   /api/restaurante` - Crear nuevo
- `PUT    /api/restaurante/{id}` - Actualizar
- `DELETE /api/restaurante/{id}` - Eliminar (soft delete)

### Usuarios
- `GET    /api/usuario` - Listar todos
- `GET    /api/usuario/{id}` - Obtener por ID
- `GET    /api/usuario/restaurante/{restauranteId}` - Por restaurante
- `POST   /api/usuario` - Crear nuevo
- `PUT    /api/usuario/{id}` - Actualizar
- `DELETE /api/usuario/{id}` - Eliminar (soft delete)

### Categorías
- `GET    /api/categoria` - Listar todas
- `GET    /api/categoria/{id}` - Obtener por ID
- `POST   /api/categoria` - Crear nueva
- `DELETE /api/categoria/{id}` - Eliminar

### Proveedores
- `GET    /api/proveedor` - Listar todos
- `GET    /api/proveedor/{id}` - Obtener por ID
- `POST   /api/proveedor` - Crear nuevo
- `PUT    /api/proveedor/{id}` - Actualizar
- `DELETE /api/proveedor/{id}` - Eliminar (soft delete)

### Productos
- `GET    /api/producto` - Listar todos
- `GET    /api/producto/{id}` - Obtener por ID
- `GET    /api/producto/categoria/{categoriaId}` - Por categoría
- `GET    /api/producto/proveedor/{proveedorId}` - Por proveedor
- `POST   /api/producto` - Crear nuevo
- `POST   /api/producto/bulk` - **Creación masiva**
- `PUT    /api/producto/{id}` - Actualizar
- `DELETE /api/producto/{id}` - Eliminar (soft delete)

### Stock Local
- `GET    /api/stocklocal` - Listar todo el stock
- `GET    /api/stocklocal/{id}` - Obtener por ID
- `GET    /api/stocklocal/restaurante/{restauranteId}` - Por restaurante
- `GET    /api/stocklocal/producto/{productoId}` - Por producto
- `GET    /api/stocklocal/lote?productoId={id}&restauranteId={id}&lote={lote}` - Por lote específico
- `POST   /api/stocklocal` - Registrar entrada
- `PUT    /api/stocklocal/{id}` - Actualizar cantidad/fecha caducidad
- `DELETE /api/stocklocal/{id}` - Eliminar

### Movimientos de Inventario
- `GET    /api/movimientoinventario` - Listar todos
- `GET    /api/movimientoinventario/{id}` - Obtener por ID
- `GET    /api/movimientoinventario/restaurante/{restauranteId}` - Por restaurante
- `GET    /api/movimientoinventario/producto/{productoId}` - Por producto
- `GET    /api/movimientoinventario/usuario/{usuarioId}` - Por usuario
- `POST   /api/movimientoinventario` - Crear movimiento
- `POST   /api/movimientoinventario/merma` - **Registrar merma**
- `POST   /api/movimientoinventario/{id}/revertir` - **Revertir movimiento**

---

## 🎯 Funcionalidades Destacadas

### 1. Creación Masiva de Productos

Permite registrar catálogos completos de proveedores en una sola operación:

```json
POST /api/producto/bulk
{
  "proveedorId": 1,
  "categoriaId": 1,
  "productos": [
    { "nombre": "Producto 1", "unidadMedida": "Unidad", "stockMinimo": 50, "costoUnitario": 0.85 },
    { "nombre": "Producto 2", "unidadMedida": "Kg", "stockMinimo": 30, "costoUnitario": 1.20 }
  ]
}
```

### 2. Sistema de Mermas

Registro simplificado de pérdidas sin necesidad de especificar costos:

```json
POST /api/movimientoinventario/merma
{
  "productoId": 1,
  "restauranteId": 1,
  "lote": "LOTE-001",
  "cantidad": 5,
  "tipoMerma": "Caducidad",  // Opciones: Caducidad, Daño, Robo, Error
  "usuarioId": 1
}
```

**El sistema automáticamente:**
- ✅ Obtiene el costo del producto
- ✅ Valida stock disponible
- ✅ Crea movimiento de salida
- ✅ Actualiza inventario
- ✅ Registra el costo de la pérdida

### 3. Reversión de Movimientos

Corrección de errores sin eliminar historial:

```json
POST /api/movimientoinventario/5/revertir
{
  "usuarioId": 1,
  "motivo": "Error en cantidad registrada"
}
```

**El sistema crea automáticamente:**
- Movimiento inverso (Entrada ↔ Salida)
- Restaura el stock afectado
- Mantiene trazabilidad completa
- Registra el motivo de la reversión

### 4. Gestión Automática de Costos

El costo se define **una sola vez en el Producto** y se propaga automáticamente a:
- Entradas de stock
- Movimientos de inventario
- Registro de mermas
- Cálculo de pérdidas

### 5. Transferencias entre Restaurantes

```json
POST /api/movimientoinventario
{
  "tipo": "Salida",
  "productoId": 1,
  "restauranteId": 1,           // Restaurante origen
  "restauranteDestinoId": 2,    // Restaurante destino
  "cantidad": 20,
  "lote": "LOTE-001",
  "motivo": "Transferencia",
  "usuarioId": 1
}
```

**El sistema automáticamente:**
- ✅ Descuenta del restaurante origen
- ✅ Incrementa en el restaurante destino
- ✅ Mantiene el mismo lote
- ✅ Registra ambos movimientos

### 6. Validaciones de Negocio

- ✅ **Stock insuficiente**: No permite salidas mayores al disponible
- ✅ **Lotes únicos**: Un lote no puede repetirse para el mismo producto y restaurante
- ✅ **Productos activos**: Solo se pueden usar productos no eliminados
- ✅ **Relaciones válidas**: Valida existencia de productos, restaurantes y usuarios
- ✅ **Transacciones**: Rollback automático si falla cualquier operación

---

## 🗄️ Migraciones

### Crear nueva migración

```bash
dotnet ef migrations add NombreMigracion
```

### Aplicar migraciones

```bash
dotnet ef database update
```

### Revertir última migración

```bash
dotnet ef migrations remove
```

### Ver historial de migraciones

```bash
dotnet ef migrations list
```

---

## 🧪 Testing

### Colección de Postman

Importa la colección de pruebas desde `postman/FreshStock.postman_collection.json` para probar todos los endpoints.

### Swagger UI

Accede a la documentación interactiva en:
```
http://localhost:5140/swagger
```

---

## 📈 Roadmap

### Próximas Funcionalidades

- [ ] **Autenticación JWT**: Sistema de login y permisos por rol
- [ ] **Alertas de Stock Bajo**: Notificaciones cuando el stock está bajo mínimo
- [ ] **Alertas de Caducidad**: Avisos de productos próximos a caducar
- [ ] **Reportes y Analytics**: Dashboard de consumo y pérdidas
- [ ] **Gestión de Compras**: Órdenes de compra a proveedores
- [ ] **API de Integración**: Webhooks para sistemas externos
- [ ] **Auditoría Avanzada**: Logs detallados de todas las operaciones
- [ ] **Multi-tenant**: Soporte para múltiples empresas
- [ ] **Exportación de Datos**: CSV, Excel, PDF
- [ ] **Recetas**: Gestión de preparaciones y sus ingredientes

---

## 👨‍💻 Autor

**Lucas Riestra**

- GitHub: [@LucasRiestra](https://github.com/LucasRiestra)
- LinkedIn: [Lucas Riestra](https://linkedin.com/in/lucasriestra)
- Email: lucas.riestra@example.com

---

## 📝 Licencia

Este proyecto está bajo la Licencia MIT. Ver el archivo `LICENSE` para más detalles.

---

## 🙏 Agradecimientos

- **ASP.NET Core Team** - Por el excelente framework
- **Supabase** - Por el hosting de PostgreSQL
- **AutoMapper** - Por simplificar el mapeo de objetos
- **Entity Framework Core** - Por el poderoso ORM

---

## 📞 Contacto

¿Preguntas? ¿Sugerencias? ¿Oportunidades laborales?

📧 Contáctame en [tu-email@example.com]

---

<div align="center">

**⭐ Si este proyecto te resultó útil, considera darle una estrella ⭐**

Hecho con ❤️ por Lucas Riestra

</div>
