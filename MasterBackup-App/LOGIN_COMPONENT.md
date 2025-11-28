# Componente de Login - Documentación

## Descripción

Componente de login con autenticación de 3 pasos que incluye:
1. **Selección de cuenta guardada** o ingreso de email
2. **Validación de contraseña**
3. **Verificación 2FA** (si está habilitado)

### Características Principales

✅ **Login de 3 pasos progresivo**
- Paso 1: Validación de email y subdomain
- Paso 2: Validación de contraseña
- Paso 3: Código 2FA (solo si está habilitado)

✅ **Cuentas guardadas**
- Al cerrar sesión, la cuenta se guarda en localStorage
- Próximo login: selección rápida de cuenta guardada
- Opción para eliminar cuentas guardadas
- Muestra avatar con iniciales del usuario

✅ **Interfaz moderna con Metronic**
- Diseño responsivo
- Animaciones suaves entre pasos
- Validación en tiempo real
- Estados de carga

## Archivos Creados

### 1. Modelos (`src/app/models/auth.models.ts`)
- `LoginDto`: DTO para login
- `ValidateEmailDto`: DTO para validar email
- `Verify2FADto`: DTO para verificar 2FA
- `AuthResponse`: Respuesta de autenticación
- `EmailValidationResponse`: Respuesta de validación de email
- `SavedAccount`: Modelo de cuenta guardada

### 2. Servicios

#### `StorageService` (`src/app/services/storage.service.ts`)
Gestiona el almacenamiento local:
- `getSavedAccounts()`: Obtiene cuentas guardadas
- `saveAccount()`: Guarda una cuenta
- `removeAccount()`: Elimina una cuenta
- `setAuthToken()`: Guarda token JWT
- `getAuthToken()`: Obtiene token JWT
- `clearSession()`: Limpia sesión completa

#### `AuthService` (`src/app/services/auth.service.ts`)
Gestiona la autenticación:
- `validateEmail()`: Valida si el email existe
- `login()`: Login con email y contraseña
- `verify2FA()`: Verifica código 2FA
- `logout()`: Cierra sesión
- `getSavedAccounts()`: Obtiene cuentas guardadas
- `removeSavedAccount()`: Elimina cuenta guardada
- `isAuthenticated()`: Verifica si está autenticado

### 3. Componente

#### `LoginComponent` (`src/app/components/login/`)
- **TypeScript**: Lógica de 3 pasos y gestión de estado
- **HTML**: Template con Metronic
- **CSS**: Estilos personalizados y animaciones

### 4. Configuración

#### Environment (`src/environments/`)
- `environment.ts`: Desarrollo
- `environment.prod.ts`: Producción

#### Rutas (`src/app/app.routes.ts`)
- Ruta `/login` configurada
- Redirección por defecto a login

#### App Config (`src/app/app.config.ts`)
- HttpClient configurado

## Endpoints Requeridos en el Backend

### 1. Validar Email
```http
POST /api/auth/validate-email
Content-Type: application/json

{
  "email": "usuario@ejemplo.com",
  "subdomain": "app"
}
```

**Respuesta:**
```json
{
  "exists": true,
  "twoFactorEnabled": true,
  "firstName": "Juan",
  "lastName": "Pérez"
}
```

### 2. Login
```http
POST /api/auth/login
Content-Type: application/json

{
  "email": "usuario@ejemplo.com",
  "password": "contraseña123",
  "subdomain": "app"
}
```

**Respuesta (sin 2FA):**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "userId": "guid",
  "email": "usuario@ejemplo.com",
  "firstName": "Juan",
  "lastName": "Pérez",
  "role": "Admin",
  "tenantId": "guid",
  "twoFactorRequired": false
}
```

**Respuesta (con 2FA):**
```json
{
  "twoFactorRequired": true
}
```

### 3. Verificar 2FA
```http
POST /api/auth/verify-2fa
Content-Type: application/json

{
  "email": "usuario@ejemplo.com",
  "password": "contraseña123",
  "twoFactorCode": "123456",
  "subdomain": "app"
}
```

**Respuesta:**
```json
{
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "userId": "guid",
  "email": "usuario@ejemplo.com",
  "firstName": "Juan",
  "lastName": "Pérez",
  "role": "Admin",
  "tenantId": "guid",
  "twoFactorRequired": false
}
```

## Implementación en el Backend

### 1. Crear ValidateEmailCommand

```csharp
// Application/Features/Auth/Commands/ValidateEmailCommand.cs
public record ValidateEmailCommand(ValidateEmailDto Dto) : IRequest<EmailValidationResponse>;

public class ValidateEmailCommandHandler : IRequestHandler<ValidateEmailCommand, EmailValidationResponse>
{
    private readonly ITenantService _tenantService;

    public async Task<EmailValidationResponse> Handle(ValidateEmailCommand request, CancellationToken cancellationToken)
    {
        // 1. Obtener conexión del tenant
        var connectionString = await _tenantService.GetTenantConnectionStringAsync(request.Dto.Subdomain);
        
        // 2. Crear contexto del tenant
        var optionsBuilder = new DbContextOptionsBuilder<TenantDbContext>();
        optionsBuilder.UseNpgsql(connectionString);
        using var context = new TenantDbContext(optionsBuilder.Options);
        
        // 3. Buscar usuario
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email == request.Dto.Email);
        
        if (user == null)
        {
            return new EmailValidationResponse { Exists = false };
        }
        
        return new EmailValidationResponse
        {
            Exists = true,
            TwoFactorEnabled = user.TwoFactorEnabled,
            FirstName = user.FirstName,
            LastName = user.LastName
        };
    }
}
```

### 2. Actualizar LoginCommand

Asegúrate de que el `LoginCommand` retorne `twoFactorRequired` cuando el usuario tiene 2FA habilitado pero aún no ha verificado el código.

### 3. Crear Verify2FACommand

```csharp
// Application/Features/Auth/Commands/Verify2FACommand.cs
public record Verify2FACommand(Verify2FADto Dto) : IRequest<AuthResponseDto>;

public class Verify2FACommandHandler : IRequestHandler<Verify2FACommand, AuthResponseDto>
{
    // Implementar verificación del código 2FA
}
```

### 4. Actualizar AuthController

```csharp
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    [HttpPost("validate-email")]
    public async Task<IActionResult> ValidateEmail([FromBody] ValidateEmailDto dto)
    {
        var result = await _mediator.Send(new ValidateEmailCommand(dto));
        return Ok(result);
    }

    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginDto dto)
    {
        var result = await _mediator.Send(new LoginCommand(dto));
        return Ok(result);
    }

    [HttpPost("verify-2fa")]
    public async Task<IActionResult> Verify2FA([FromBody] Verify2FADto dto)
    {
        var result = await _mediator.Send(new Verify2FACommand(dto));
        return Ok(result);
    }
}
```

## Uso del Componente

### 1. Instalar dependencias
```bash
cd MasterBackup-App
npm install
```

### 2. Ejecutar aplicación
```bash
ng serve
```

### 3. Acceder al login
```
http://localhost:4200/login
```

## Flujo de Usuario

### Primera vez (sin cuentas guardadas)
1. Usuario ingresa email y subdomain
2. Sistema valida que el email existe
3. Usuario ingresa contraseña
4. Si tiene 2FA: ingresa código de 6 dígitos
5. Login exitoso → Cuenta guardada en localStorage

### Siguientes veces (con cuentas guardadas)
1. Usuario ve lista de cuentas guardadas
2. Usuario selecciona su cuenta
3. Usuario ingresa contraseña
4. Si tiene 2FA: ingresa código de 6 dígitos
5. Login exitoso → Fecha de último login actualizada

### Cerrar sesión
1. Usuario cierra sesión
2. Token y datos de usuario eliminados
3. Cuenta permanece guardada en la lista
4. Próxima vez: puede seleccionar la cuenta directamente

## Personalización

### Cambiar subdomain por defecto
En `login.component.ts`:
```typescript
defaultSubdomain = 'tu-subdomain';
```

### Cambiar ruta después del login
En `login.component.ts`:
```typescript
private handleLoginSuccess(): void {
  this.router.navigate(['/tu-ruta']);
}
```

### Deshabilitar cuentas guardadas
En `login.component.ts`, método `ngOnInit`:
```typescript
ngOnInit(): void {
  this.currentStep = LoginStep.EMAIL; // Siempre empezar en email
}
```

## Seguridad

✅ **Token JWT**: Almacenado en localStorage
✅ **Contraseñas**: Nunca se almacenan, solo se envían al backend
✅ **Validación**: Formularios reactivos con validación en tiempo real
✅ **HTTPS**: Usar siempre en producción
✅ **CORS**: Configurar correctamente en el backend

## Próximos Pasos

1. Implementar los endpoints faltantes en el backend
2. Crear componente de registro
3. Crear componente de recuperación de contraseña
4. Implementar interceptor HTTP para agregar token JWT
5. Implementar guard de autenticación para rutas protegidas
6. Agregar tests unitarios

## Soporte

Para más información sobre la arquitectura del proyecto, consulta:
- `COPILOT_INSTRUCTIONS.md`
- `CLEAN_ARCHITECTURE.md`
- `MIGRATION_GUIDE.md`
