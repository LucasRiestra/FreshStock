# Guía de Migración Frontend - Sistema Multi-Tenant con Roles

## Resumen de Cambios Críticos

El backend ha sido completamente reestructurado para soportar un sistema multi-tenant donde:
- **Un usuario puede pertenecer a múltiples restaurantes**
- **Un usuario puede tener diferentes roles en cada restaurante**
- **Los datos se filtran automáticamente según los permisos del usuario**

---

## 1. CAMBIOS EN EL MODELO DE DATOS

### Usuario - ANTES
```typescript
interface Usuario {
  id: number;
  nombre: string;
  email: string;
  rol: string;           // ❌ YA NO EXISTE
  restauranteId: number; // ❌ YA NO EXISTE
  activo: boolean;
}
```

### Usuario - AHORA
```typescript
interface Usuario {
  id: number;
  nombre: string;
  email: string;
  activo: boolean;
  // El rol y restaurante ahora están en UsuarioRestaurante
}
```

### Nueva Entidad: UsuarioRestaurante
```typescript
interface UsuarioRestaurante {
  id: number;
  usuarioId: number;
  restauranteId: number;
  rol: RolUsuario;  // 1=Admin, 2=Gerente, 3=Empleado
  activo: boolean;
  // Datos adicionales
  nombreUsuario?: string;
  nombreRestaurante?: string;
}

enum RolUsuario {
  Admin = 1,
  Gerente = 2,
  Empleado = 3
}
```

---

## 2. AUTENTICACIÓN - CAMBIOS IMPORTANTES

### Login Request (SIN CAMBIOS)
```typescript
POST /api/auth/login
{
  "email": "usuario@ejemplo.com",
  "password": "contraseña123"
}
```

### Login Response (CAMBIÓ)
```typescript
{
  "accessToken": "eyJhbG...",
  "refreshToken": "abc123...",
  "expiration": "2026-01-27T12:00:00Z",
  "usuario": {
    "id": 1,
    "nombre": "Juan Pérez",
    "email": "juan@ejemplo.com",
    "activo": true
    // ⚠️ YA NO INCLUYE rol NI restauranteId
  }
}
```

### Obtener Permisos del Usuario (NUEVO ENDPOINT OBLIGATORIO)
**Después del login, DEBES llamar a este endpoint para conocer los permisos:**

```typescript
GET /api/permiso/mis-permisos
Authorization: Bearer {accessToken}

// Respuesta:
{
  "usuarioId": 1,
  "puedeCrearRestaurantes": true,
  "restaurantes": [
    {
      "restauranteId": 1,
      "nombreRestaurante": "Restaurante Centro",
      "rol": 1,  // Admin
      "puedeCrearUsuarios": true,
      "puedeCrearCategorias": true,
      "puedeCrearProveedores": true,
      "puedeGestionarInventario": true
    },
    {
      "restauranteId": 2,
      "nombreRestaurante": "Restaurante Norte",
      "rol": 3,  // Empleado
      "puedeCrearUsuarios": false,
      "puedeCrearCategorias": false,
      "puedeCrearProveedores": false,
      "puedeGestionarInventario": true
    }
  ]
}
```

### Registro Público - ⚠️ ELIMINADO DEL FLUJO NORMAL

El endpoint `/api/auth/register` **ya NO debe usarse para registro público**.
- Los usuarios ahora son creados por Admin/Gerente
- Después de crear el usuario, se les asigna a restaurantes con un rol específico

---

## 3. NUEVO FLUJO DE TRABAJO

### Flujo de Login (Actualizado)
```
1. Usuario hace login → POST /api/auth/login
2. Guardar tokens (accessToken, refreshToken)
3. Llamar GET /api/permiso/mis-permisos
4. Guardar permisos en estado global
5. Si usuario tiene múltiples restaurantes → mostrar selector de restaurante
6. Redirigir según el rol en el restaurante seleccionado
```

### Flujo de Creación de Usuarios (Nuevo)
```
1. Admin/Gerente va a gestión de usuarios
2. Crea usuario → POST /api/usuario (nombre, email, password)
3. Asigna usuario a restaurante → POST /api/usuariorestaurante
   {
     "usuarioId": nuevoUsuarioId,
     "restauranteId": restauranteActual,
     "rol": 2  // Gerente, por ejemplo
   }
```

---

## 4. ENDPOINTS ACTUALIZADOS

### Restaurantes
| Endpoint | Método | Permisos | Notas |
|----------|--------|----------|-------|
| `/api/restaurante` | GET | Público | Lista todos los restaurantes |
| `/api/restaurante/mis-restaurantes` | GET | Autenticado | Solo los restaurantes del usuario |
| `/api/restaurante/{id}` | GET | Autenticado | Detalles de un restaurante |
| `/api/restaurante` | POST | Admin | Crea restaurante (se asigna como Admin automáticamente) |
| `/api/restaurante/{id}` | PUT | Admin del restaurante | Actualizar restaurante |
| `/api/restaurante/{id}` | DELETE | Admin del restaurante | Eliminar restaurante |

### Usuarios
| Endpoint | Método | Permisos | Notas |
|----------|--------|----------|-------|
| `/api/usuario` | GET | Admin/Gerente | Lista todos los usuarios |
| `/api/usuario/{id}` | GET | Propio perfil o Admin/Gerente | Ver usuario |
| `/api/usuario/restaurante/{restauranteId}` | GET | Acceso al restaurante | Usuarios de un restaurante |
| `/api/usuario` | POST | Admin/Gerente | Crear usuario |
| `/api/usuario/{id}` | PUT | Propio perfil o Admin/Gerente | Actualizar usuario |
| `/api/usuario/{id}` | DELETE | Admin/Gerente | Eliminar usuario |

### Asignación Usuario-Restaurante (NUEVO)
| Endpoint | Método | Permisos | Notas |
|----------|--------|----------|-------|
| `/api/usuariorestaurante` | GET | Admin/Gerente | Todas las asignaciones |
| `/api/usuariorestaurante/{id}` | GET | Autenticado | Una asignación específica |
| `/api/usuariorestaurante/usuario/{usuarioId}` | GET | Propio o Admin/Gerente | Restaurantes de un usuario |
| `/api/usuariorestaurante/restaurante/{restauranteId}` | GET | Acceso al restaurante | Usuarios de un restaurante |
| `/api/usuariorestaurante` | POST | Admin/Gerente del restaurante | Asignar usuario a restaurante |
| `/api/usuariorestaurante/{id}` | PUT | Admin/Gerente del restaurante | Cambiar rol/estado |
| `/api/usuariorestaurante/{id}` | DELETE | Admin/Gerente del restaurante | Quitar usuario de restaurante |

### Categorías
| Endpoint | Método | Permisos | Notas |
|----------|--------|----------|-------|
| `/api/categoria` | GET | Autenticado | **Filtrado automático por usuario** |
| `/api/categoria/restaurante/{restauranteId}` | GET | Acceso al restaurante | Categorías de un restaurante |
| `/api/categoria/{id}` | GET | Autenticado | Una categoría |
| `/api/categoria` | POST | Admin/Gerente | Crear categoría global |
| `/api/categoria/crear-y-asignar/{restauranteId}` | POST | Admin/Gerente del restaurante | Crear y asignar a restaurante |
| `/api/categoria/{id}` | DELETE | Admin/Gerente | Eliminar categoría |

### Proveedores
| Endpoint | Método | Permisos | Notas |
|----------|--------|----------|-------|
| `/api/proveedor` | GET | Autenticado | **Filtrado automático por usuario** |
| `/api/proveedor/restaurante/{restauranteId}` | GET | Acceso al restaurante | Proveedores de un restaurante |
| `/api/proveedor/{id}` | GET | Autenticado | Un proveedor |
| `/api/proveedor` | POST | Admin/Gerente | Crear proveedor global |
| `/api/proveedor/crear-y-asignar/{restauranteId}` | POST | Admin/Gerente del restaurante | Crear y asignar a restaurante |
| `/api/proveedor/{id}` | PUT | Admin/Gerente | Actualizar proveedor |
| `/api/proveedor/{id}` | DELETE | Admin/Gerente | Eliminar proveedor |

### Productos
| Endpoint | Método | Permisos | Notas |
|----------|--------|----------|-------|
| `/api/producto` | GET | Autenticado | **Filtrado automático por usuario** |
| `/api/producto/{id}` | GET | Autenticado | Un producto |
| `/api/producto/categoria/{categoriaId}` | GET | Autenticado | Productos por categoría |
| `/api/producto/proveedor/{proveedorId}` | GET | Autenticado | Productos por proveedor |
| `/api/producto/restaurante/{restauranteId}` | GET | Acceso al restaurante | Productos disponibles para restaurante |
| `/api/producto` | POST | Admin/Gerente | Crear producto |
| `/api/producto/bulk` | POST | Admin/Gerente | Crear múltiples productos |
| `/api/producto/{id}` | PUT | Admin/Gerente | Actualizar producto |
| `/api/producto/{id}` | DELETE | Admin/Gerente | Eliminar producto |

### Stock Local
| Endpoint | Método | Permisos | Notas |
|----------|--------|----------|-------|
| `/api/stocklocal` | GET | Autenticado | **Filtrado automático por usuario** |
| `/api/stocklocal/{id}` | GET | Acceso al restaurante del stock | Un registro de stock |
| `/api/stocklocal/restaurante/{restauranteId}` | GET | Acceso al restaurante | Stock de un restaurante |
| `/api/stocklocal/producto/{productoId}` | GET | Autenticado | Stock de un producto (filtrado) |
| `/api/stocklocal/lote` | GET | Acceso al restaurante | Buscar por lote |
| `/api/stocklocal` | POST | Cualquier rol con acceso | Crear stock |
| `/api/stocklocal/{id}` | PUT | Cualquier rol con acceso | Actualizar stock |
| `/api/stocklocal/{id}` | DELETE | Admin/Gerente | Eliminar stock |

### Movimientos de Inventario
| Endpoint | Método | Permisos | Notas |
|----------|--------|----------|-------|
| `/api/movimientoinventario` | GET | Autenticado | **Filtrado automático por usuario** |
| `/api/movimientoinventario/{id}` | GET | Acceso al restaurante | Un movimiento |
| `/api/movimientoinventario/restaurante/{restauranteId}` | GET | Acceso al restaurante | Movimientos de un restaurante |
| `/api/movimientoinventario/producto/{productoId}` | GET | Autenticado | Movimientos de un producto (filtrado) |
| `/api/movimientoinventario/usuario/{usuarioId}` | GET | Propio o Admin/Gerente | Movimientos de un usuario |
| `/api/movimientoinventario` | POST | Cualquier rol con acceso | Crear movimiento |
| `/api/movimientoinventario/merma` | POST | Cualquier rol con acceso | Registrar merma |
| `/api/movimientoinventario/{id}/revertir` | POST | Admin/Gerente | Revertir movimiento |

### Permisos (NUEVO)
| Endpoint | Método | Permisos | Notas |
|----------|--------|----------|-------|
| `/api/permiso/mis-permisos` | GET | Autenticado | Obtener permisos del usuario actual |
| `/api/permiso/usuario/{usuarioId}` | GET | Admin | Permisos de otro usuario |
| `/api/permiso/verificar/restaurante/{restauranteId}` | GET | Autenticado | Verificar acceso a restaurante |
| `/api/permiso/puede-crear-restaurante` | GET | Autenticado | Verificar si puede crear restaurantes |

---

## 5. FILTRADO AUTOMÁTICO - IMPORTANTE

Los endpoints `GET /api/xxx` ahora filtran automáticamente los datos:

- **Si el usuario es Admin**: Ve TODOS los datos del sistema
- **Si el usuario NO es Admin**: Solo ve datos de sus restaurantes asignados

**Esto aplica a:**
- `/api/categoria` - Solo categorías de sus restaurantes
- `/api/proveedor` - Solo proveedores de sus restaurantes
- `/api/producto` - Solo productos de proveedores de sus restaurantes
- `/api/stocklocal` - Solo stock de sus restaurantes
- `/api/movimientoinventario` - Solo movimientos de sus restaurantes

---

## 6. CAMBIOS EN LA UI REQUERIDOS

### Pantalla de Login
1. Eliminar enlace/botón de registro público
2. Después del login exitoso, llamar a `/api/permiso/mis-permisos`
3. Si el usuario tiene múltiples restaurantes, mostrar un selector

### Selector de Restaurante (NUEVO)
```typescript
// Después del login, si tiene múltiples restaurantes
if (permisos.restaurantes.length > 1) {
  mostrarSelectorRestaurante(permisos.restaurantes);
} else if (permisos.restaurantes.length === 1) {
  seleccionarRestaurante(permisos.restaurantes[0].restauranteId);
} else {
  mostrarError("No tiene restaurantes asignados");
}
```

### Navegación Basada en Rol
```typescript
// Mostrar/ocultar opciones según permisos del restaurante seleccionado
const restauranteActual = permisos.restaurantes.find(r => r.restauranteId === selectedRestauranteId);

// Menú de navegación
{
  restauranteActual.puedeCrearUsuarios && <MenuItem>Gestión de Usuarios</MenuItem>
}
{
  restauranteActual.puedeCrearCategorias && <MenuItem>Gestión de Categorías</MenuItem>
}
{
  restauranteActual.puedeCrearProveedores && <MenuItem>Gestión de Proveedores</MenuItem>
}
{
  restauranteActual.puedeGestionarInventario && <MenuItem>Inventario</MenuItem>
}
```

### Gestión de Usuarios (Solo Admin/Gerente)
1. Lista de usuarios del restaurante: `GET /api/usuario/restaurante/{restauranteId}`
2. Crear usuario:
   - Paso 1: `POST /api/usuario` con { nombre, email, password }
   - Paso 2: `POST /api/usuariorestaurante` con { usuarioId, restauranteId, rol }
3. Cambiar rol de usuario: `PUT /api/usuariorestaurante/{id}` con { id, rol, activo }
4. Quitar usuario del restaurante: `DELETE /api/usuariorestaurante/{id}`

### Gestión de Categorías (Solo Admin/Gerente)
1. Usar endpoint combinado: `POST /api/categoria/crear-y-asignar/{restauranteId}`
2. Listar solo las del restaurante: `GET /api/categoria/restaurante/{restauranteId}`

### Gestión de Proveedores (Solo Admin/Gerente)
1. Usar endpoint combinado: `POST /api/proveedor/crear-y-asignar/{restauranteId}`
2. Listar solo los del restaurante: `GET /api/proveedor/restaurante/{restauranteId}`

### Inventario (Todos los roles)
1. Usar siempre el restaurante seleccionado:
   - `GET /api/stocklocal/restaurante/{restauranteId}`
   - `GET /api/movimientoinventario/restaurante/{restauranteId}`

---

## 7. MANEJO DE ERRORES ACTUALIZADO

### Códigos de Error
- `401 Unauthorized`: Token inválido o expirado
- `403 Forbidden`: No tiene permisos para esta acción
- `404 Not Found`: Recurso no encontrado
- `400 Bad Request`: Datos inválidos

### Ejemplo de Respuesta de Error
```typescript
{
  "message": "No tiene permisos para acceder a este restaurante"
}
```

---

## 8. EJEMPLO DE IMPLEMENTACIÓN - STORE DE AUTH

```typescript
// authStore.ts
interface AuthState {
  usuario: Usuario | null;
  permisos: PermisoUsuario | null;
  restauranteSeleccionado: number | null;
  accessToken: string | null;
  refreshToken: string | null;
}

interface PermisoUsuario {
  usuarioId: number;
  puedeCrearRestaurantes: boolean;
  restaurantes: PermisoRestaurante[];
}

interface PermisoRestaurante {
  restauranteId: number;
  nombreRestaurante: string;
  rol: number; // 1=Admin, 2=Gerente, 3=Empleado
  puedeCrearUsuarios: boolean;
  puedeCrearCategorias: boolean;
  puedeCrearProveedores: boolean;
  puedeGestionarInventario: boolean;
}

// Acciones
async function login(email: string, password: string) {
  // 1. Login
  const loginResponse = await api.post('/auth/login', { email, password });
  setTokens(loginResponse.accessToken, loginResponse.refreshToken);
  setUsuario(loginResponse.usuario);

  // 2. Obtener permisos
  const permisos = await api.get('/permiso/mis-permisos');
  setPermisos(permisos);

  // 3. Seleccionar restaurante
  if (permisos.restaurantes.length === 1) {
    setRestauranteSeleccionado(permisos.restaurantes[0].restauranteId);
  }
  // Si tiene múltiples, el UI debe mostrar selector
}

function getPermisosRestauranteActual(): PermisoRestaurante | null {
  if (!permisos || !restauranteSeleccionado) return null;
  return permisos.restaurantes.find(r => r.restauranteId === restauranteSeleccionado);
}

function esAdmin(): boolean {
  const permisoActual = getPermisosRestauranteActual();
  return permisoActual?.rol === 1;
}

function esGerente(): boolean {
  const permisoActual = getPermisosRestauranteActual();
  return permisoActual?.rol === 2;
}

function esEmpleado(): boolean {
  const permisoActual = getPermisosRestauranteActual();
  return permisoActual?.rol === 3;
}
```

---

## 9. CHECKLIST DE MIGRACIÓN

### Autenticación
- [ ] Eliminar registro público
- [ ] Actualizar flujo de login para llamar a `/api/permiso/mis-permisos`
- [ ] Actualizar modelo de Usuario (sin rol ni restauranteId)
- [ ] Crear store/estado para permisos
- [ ] Implementar selector de restaurante

### Navegación y UI
- [ ] Condicionar menú según permisos del restaurante seleccionado
- [ ] Agregar indicador de restaurante actual en header
- [ ] Permitir cambiar de restaurante si tiene múltiples

### Gestión de Usuarios
- [ ] Crear vista para Admin/Gerente
- [ ] Implementar crear usuario + asignar a restaurante
- [ ] Implementar cambiar rol de usuario
- [ ] Implementar quitar usuario de restaurante

### Categorías y Proveedores
- [ ] Usar endpoints `/restaurante/{id}` para listar
- [ ] Usar endpoints `/crear-y-asignar/{restauranteId}` para crear

### Inventario
- [ ] Siempre pasar restauranteId en las peticiones
- [ ] Actualizar DTOs de creación de stock/movimientos

### Manejo de Errores
- [ ] Agregar manejo de 403 Forbidden
- [ ] Redirigir a selector de restaurante si no tiene acceso

---

## 10. DTOs DE REFERENCIA

### CreateUsuarioDTO
```typescript
{
  "nombre": "string (requerido, max 100)",
  "email": "string (requerido, email válido, max 100)",
  "password": "string (requerido, min 6, max 255)"
}
```

### CreateUsuarioRestauranteDTO
```typescript
{
  "usuarioId": "number (requerido)",
  "restauranteId": "number (requerido)",
  "rol": "number (requerido, 1=Admin, 2=Gerente, 3=Empleado)"
}
```

### UpdateUsuarioRestauranteDTO
```typescript
{
  "id": "number (requerido)",
  "rol": "number (requerido, 1=Admin, 2=Gerente, 3=Empleado)",
  "activo": "boolean (requerido)"
}
```

### CreateStockLocalDTO
```typescript
{
  "productoId": "number (requerido)",
  "restauranteId": "number (requerido)",
  "cantidad": "number (requerido)",
  "lote": "string (requerido)",
  "fechaCaducidad": "date (opcional)"
}
```

### CreateMovimientoInventarioDTO
```typescript
{
  "tipo": "string (requerido, 'Entrada' o 'Salida')",
  "productoId": "number (requerido)",
  "restauranteId": "number (requerido)",
  "cantidad": "number (requerido)",
  "lote": "string (requerido)",
  "motivo": "string (opcional)",
  "usuarioId": "number (requerido)",
  "restauranteDestinoId": "number (opcional, para transferencias)"
}
```

---

## RESUMEN DE JERARQUÍA DE ROLES

| Acción | Admin | Gerente | Empleado |
|--------|-------|---------|----------|
| Crear restaurantes | ✅ | ❌ | ❌ |
| Crear usuarios | ✅ | ✅ | ❌ |
| Crear categorías | ✅ | ✅ | ❌ |
| Crear proveedores | ✅ | ✅ | ❌ |
| Crear productos | ✅ | ✅ | ❌ |
| Gestionar inventario | ✅ | ✅ | ✅ |
| Revertir movimientos | ✅ | ✅ | ❌ |
| Eliminar stock | ✅ | ✅ | ❌ |
| Ver todos los datos | ✅ | ❌ | ❌ |
