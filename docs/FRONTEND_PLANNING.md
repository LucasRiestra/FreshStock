# FreshStock - Planificación Frontend Angular

## Resumen del Proyecto
Sistema de gestión de inventario para restaurantes con autenticación JWT.

**API Base URL:** `http://localhost:5140/api`

---

## 1. Estructura del Proyecto Angular

```
src/
├── app/
│   ├── core/                    # Servicios singleton, guards, interceptors
│   │   ├── services/
│   │   │   ├── auth.service.ts
│   │   │   ├── storage.service.ts
│   │   │   └── api.service.ts
│   │   ├── guards/
│   │   │   ├── auth.guard.ts
│   │   │   └── no-auth.guard.ts
│   │   ├── interceptors/
│   │   │   ├── auth.interceptor.ts
│   │   │   └── error.interceptor.ts
│   │   └── core.module.ts
│   │
│   ├── shared/                  # Componentes, pipes, directivas reutilizables
│   │   ├── components/
│   │   │   ├── navbar/
│   │   │   ├── sidebar/
│   │   │   ├── loading/
│   │   │   ├── confirm-dialog/
│   │   │   └── data-table/
│   │   ├── pipes/
│   │   └── shared.module.ts
│   │
│   ├── features/                # Módulos de funcionalidades
│   │   ├── auth/
│   │   │   ├── login/
│   │   │   ├── register/
│   │   │   └── auth.module.ts
│   │   │
│   │   ├── dashboard/
│   │   │   └── dashboard.module.ts
│   │   │
│   │   ├── restaurantes/
│   │   │   ├── restaurante-list/
│   │   │   ├── restaurante-form/
│   │   │   ├── restaurante.service.ts
│   │   │   └── restaurantes.module.ts
│   │   │
│   │   ├── categorias/
│   │   │   ├── categoria-list/
│   │   │   ├── categoria-form/
│   │   │   ├── categoria.service.ts
│   │   │   └── categorias.module.ts
│   │   │
│   │   ├── proveedores/
│   │   │   ├── proveedor-list/
│   │   │   ├── proveedor-form/
│   │   │   ├── proveedor.service.ts
│   │   │   └── proveedores.module.ts
│   │   │
│   │   ├── productos/
│   │   │   ├── producto-list/
│   │   │   ├── producto-form/
│   │   │   ├── producto.service.ts
│   │   │   └── productos.module.ts
│   │   │
│   │   ├── inventario/
│   │   │   ├── stock-list/
│   │   │   ├── movimiento-list/
│   │   │   ├── entrada-form/
│   │   │   ├── salida-form/
│   │   │   ├── merma-form/
│   │   │   ├── inventario.service.ts
│   │   │   └── inventario.module.ts
│   │   │
│   │   └── usuarios/
│   │       ├── usuario-list/
│   │       ├── usuario-form/
│   │       ├── usuario.service.ts
│   │       └── usuarios.module.ts
│   │
│   ├── models/                  # Interfaces/tipos
│   │   ├── usuario.model.ts
│   │   ├── restaurante.model.ts
│   │   ├── categoria.model.ts
│   │   ├── proveedor.model.ts
│   │   ├── producto.model.ts
│   │   ├── stock.model.ts
│   │   ├── movimiento.model.ts
│   │   └── auth.model.ts
│   │
│   ├── app.component.ts
│   ├── app.module.ts
│   └── app-routing.module.ts
│
├── environments/
│   ├── environment.ts
│   └── environment.prod.ts
│
└── styles/                      # Estilos globales
```

---

## 2. Modelos (Interfaces TypeScript)

### auth.model.ts
```typescript
export interface LoginRequest {
  email: string;
  password: string;
}

export interface RegisterRequest {
  nombre: string;
  email: string;
  password: string;
  rol: 'Admin' | 'Gerente' | 'Empleado';
}

export interface LoginResponse {
  accessToken: string;
  refreshToken: string;
  expiration: Date;
  usuario: Usuario;
}
```

### usuario.model.ts
```typescript
export interface Usuario {
  id: number;
  restauranteId: number | null;
  nombre: string;
  email: string;
  rol: string;
  activo: boolean;
}
```

### restaurante.model.ts
```typescript
export interface Restaurante {
  id: number;
  nombre: string;
  direccion: string;
  telefono?: string;
  activo: boolean;
}

export interface CreateRestaurante {
  nombre: string;
  direccion: string;
  telefono?: string;
}
```

### categoria.model.ts
```typescript
export interface Categoria {
  id: number;
  nombre: string;
}

export interface CreateCategoria {
  nombre: string;
}
```

### proveedor.model.ts
```typescript
export interface Proveedor {
  id: number;
  nombre: string;
  telefono: string;
  email: string;
  contacto: string;
  activo: boolean;
}

export interface CreateProveedor {
  nombre: string;
  telefono: string;
  email: string;
  contacto: string;
}
```

### producto.model.ts
```typescript
export interface Producto {
  id: number;
  proveedorId: number;
  categoriaId: number;
  nombre: string;
  unidadMedida: string;
  stockMinimo: number;
  costoUnitario: number;
  activo: boolean;
}

export interface CreateProducto {
  proveedorId: number;
  categoriaId: number;
  nombre: string;
  unidadMedida: string;
  stockMinimo: number;
  costoUnitario: number;
}
```

### stock.model.ts
```typescript
export interface StockLocal {
  id: number;
  productoId: number;
  restauranteId: number;
  lote: string;
  cantidad: number;
  costoUnitario: number;
  fechaEntrada: Date;
  fechaCaducidad?: Date;
}
```

### movimiento.model.ts
```typescript
export interface MovimientoInventario {
  id: number;
  tipo: 'Entrada' | 'Salida';
  productoId: number;
  restauranteId: number;
  cantidad: number;
  lote: string;
  motivo: string;
  costoUnitario?: number;
  fecha: Date;
  usuarioId: number;
  restauranteDestinoId?: number;
}

export interface CreateMovimiento {
  tipo: 'Entrada' | 'Salida';
  productoId: number;
  restauranteId: number;
  cantidad: number;
  lote: string;
  motivo: string;
  usuarioId: number;
  restauranteDestinoId?: number;
}

export interface CreateMerma {
  productoId: number;
  restauranteId: number;
  cantidad: number;
  lote: string;
  tipoMerma: string;
  usuarioId: number;
}
```

---

## 3. Rutas de la Aplicación

```typescript
const routes: Routes = [
  // Rutas públicas
  { path: 'login', component: LoginComponent, canActivate: [NoAuthGuard] },
  { path: 'register', component: RegisterComponent, canActivate: [NoAuthGuard] },

  // Rutas protegidas
  {
    path: '',
    canActivate: [AuthGuard],
    children: [
      { path: '', redirectTo: 'dashboard', pathMatch: 'full' },
      { path: 'dashboard', loadChildren: () => import('./features/dashboard/dashboard.module') },
      { path: 'restaurantes', loadChildren: () => import('./features/restaurantes/restaurantes.module') },
      { path: 'categorias', loadChildren: () => import('./features/categorias/categorias.module') },
      { path: 'proveedores', loadChildren: () => import('./features/proveedores/proveedores.module') },
      { path: 'productos', loadChildren: () => import('./features/productos/productos.module') },
      { path: 'inventario', loadChildren: () => import('./features/inventario/inventario.module') },
      { path: 'usuarios', loadChildren: () => import('./features/usuarios/usuarios.module') },
    ]
  },

  { path: '**', redirectTo: 'dashboard' }
];
```

---

## 4. Endpoints de la API

| Módulo | Método | Endpoint | Descripción |
|--------|--------|----------|-------------|
| **Auth** | POST | `/Auth/register` | Registrar usuario |
| | POST | `/Auth/login` | Iniciar sesión |
| | POST | `/Auth/refresh` | Renovar token |
| | POST | `/Auth/revoke` | Cerrar sesión |
| **Restaurante** | GET | `/Restaurante` | Listar restaurantes |
| | GET | `/Restaurante/{id}` | Obtener por ID |
| | POST | `/Restaurante` | Crear restaurante |
| | PUT | `/Restaurante/{id}` | Actualizar |
| | DELETE | `/Restaurante/{id}` | Eliminar (soft) |
| **Categoria** | GET | `/Categoria` | Listar categorías |
| | GET | `/Categoria/{id}` | Obtener por ID |
| | POST | `/Categoria` | Crear categoría |
| | DELETE | `/Categoria/{id}` | Eliminar |
| **Proveedor** | GET | `/Proveedor` | Listar proveedores |
| | GET | `/Proveedor/{id}` | Obtener por ID |
| | POST | `/Proveedor` | Crear proveedor |
| | PUT | `/Proveedor/{id}` | Actualizar |
| | DELETE | `/Proveedor/{id}` | Eliminar (soft) |
| **Producto** | GET | `/Producto` | Listar productos |
| | GET | `/Producto/{id}` | Obtener por ID |
| | GET | `/Producto/categoria/{id}` | Por categoría |
| | GET | `/Producto/proveedor/{id}` | Por proveedor |
| | POST | `/Producto` | Crear producto |
| | POST | `/Producto/bulk` | Crear varios |
| | PUT | `/Producto/{id}` | Actualizar |
| | DELETE | `/Producto/{id}` | Eliminar (soft) |
| **StockLocal** | GET | `/StockLocal` | Listar stock |
| | GET | `/StockLocal/restaurante/{id}` | Por restaurante |
| | GET | `/StockLocal/producto/{id}` | Por producto |
| | GET | `/StockLocal/lote` | Por lote |
| | POST | `/StockLocal` | Crear stock |
| | PUT | `/StockLocal/{id}` | Actualizar |
| | DELETE | `/StockLocal/{id}` | Eliminar |
| **Movimiento** | GET | `/MovimientoInventario` | Listar movimientos |
| | GET | `/MovimientoInventario/restaurante/{id}` | Por restaurante |
| | GET | `/MovimientoInventario/producto/{id}` | Por producto |
| | POST | `/MovimientoInventario` | Crear movimiento |
| | POST | `/MovimientoInventario/merma` | Registrar merma |
| | POST | `/MovimientoInventario/{id}/revertir` | Revertir |
| **Usuario** | GET | `/Usuario` | Listar usuarios |
| | GET | `/Usuario/{id}` | Obtener por ID |
| | GET | `/Usuario/restaurante/{id}` | Por restaurante |
| | POST | `/Usuario` | Crear usuario |
| | PUT | `/Usuario/{id}` | Actualizar |
| | DELETE | `/Usuario/{id}` | Eliminar (soft) |

---

## 5. Pantallas a Desarrollar

### 5.1 Autenticación
- [ ] **Login** - Formulario de inicio de sesión
- [ ] **Register** - Formulario de registro

### 5.2 Dashboard
- [ ] **Dashboard Principal** - Resumen general, alertas de stock bajo

### 5.3 Restaurantes
- [ ] **Lista de Restaurantes** - Tabla con acciones CRUD
- [ ] **Formulario Restaurante** - Crear/Editar

### 5.4 Categorías
- [ ] **Lista de Categorías** - Tabla simple
- [ ] **Formulario Categoría** - Modal o página

### 5.5 Proveedores
- [ ] **Lista de Proveedores** - Tabla con filtros
- [ ] **Formulario Proveedor** - Crear/Editar
- [ ] **Detalle Proveedor** - Ver productos asociados

### 5.6 Productos
- [ ] **Lista de Productos** - Tabla con filtros por categoría/proveedor
- [ ] **Formulario Producto** - Crear/Editar con selects
- [ ] **Carga Masiva** - Importar varios productos

### 5.7 Inventario
- [ ] **Stock Actual** - Vista de stock por restaurante
- [ ] **Entrada de Inventario** - Formulario de entrada
- [ ] **Salida de Inventario** - Formulario de salida
- [ ] **Registro de Merma** - Formulario de merma
- [ ] **Historial de Movimientos** - Tabla con filtros
- [ ] **Alertas Stock Bajo** - Lista de productos bajo mínimo

### 5.8 Usuarios
- [ ] **Lista de Usuarios** - Tabla con roles
- [ ] **Formulario Usuario** - Crear/Editar
- [ ] **Perfil** - Editar datos propios

---

## 6. Orden de Implementación Sugerido

### Fase 1: Core y Auth (Semana 1)
1. Configurar proyecto Angular
2. Crear estructura de carpetas
3. Configurar environments
4. Implementar AuthService
5. Implementar AuthInterceptor
6. Implementar AuthGuard
7. Crear Login y Register

### Fase 2: Layout y Navegación (Semana 1)
1. Crear Navbar
2. Crear Sidebar
3. Crear Layout principal
4. Configurar rutas base

### Fase 3: Catálogos Básicos (Semana 2)
1. Módulo Categorías (CRUD completo)
2. Módulo Proveedores (CRUD completo)
3. Módulo Restaurantes (CRUD completo)

### Fase 4: Productos (Semana 2)
1. Lista de productos con filtros
2. Formulario de producto
3. Carga masiva (opcional)

### Fase 5: Inventario (Semana 3)
1. Vista de Stock actual
2. Formulario de entrada
3. Formulario de salida
4. Registro de merma
5. Historial de movimientos

### Fase 6: Dashboard y Reportes (Semana 3)
1. Dashboard con métricas
2. Alertas de stock bajo
3. Gráficos (opcional)

### Fase 7: Usuarios y Permisos (Semana 4)
1. Gestión de usuarios
2. Perfil de usuario
3. Control de permisos por rol

---

## 7. Dependencias Recomendadas

```bash
# Angular Material (UI Components)
ng add @angular/material

# Iconos
npm install @fortawesome/fontawesome-free

# Gráficos (opcional)
npm install chart.js ng2-charts

# Notificaciones
npm install ngx-toastr

# Tablas avanzadas (opcional)
npm install @angular/cdk
```

---

## 8. Variables de Entorno

```typescript
// environment.ts
export const environment = {
  production: false,
  apiUrl: 'http://localhost:5140/api',
  tokenKey: 'freshstock_token',
  refreshTokenKey: 'freshstock_refresh_token',
  userKey: 'freshstock_user'
};
```

---

## 9. Notas Importantes

1. **Autenticación**: Todos los endpoints excepto login/register requieren token JWT
2. **Token**: Se envía en header `Authorization: Bearer <token>`
3. **Refresh Token**: Implementar renovación automática antes de expiración
4. **Soft Delete**: Restaurantes, proveedores, productos y usuarios usan soft delete
5. **IDs**: Todos los IDs son numéricos (int)
6. **Fechas**: Formato ISO 8601 (UTC)

---

## 10. Checklist Pre-Desarrollo

- [ ] Node.js instalado (v18+)
- [ ] Angular CLI instalado (`npm install -g @angular/cli`)
- [ ] API backend corriendo en localhost:5140
- [ ] Usuario de prueba creado en la API
- [ ] Postman/Swagger probado con todos los endpoints
