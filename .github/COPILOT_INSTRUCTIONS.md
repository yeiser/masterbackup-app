# Instrucciones de GitHub Copilot - Proyecto MasterBackup

## Descripción del Proyecto

**MasterBackup** es un sistema SaaS multi-tenant para gestión de respaldos con:
- **Backend**: API REST en .NET 8 con Clean Architecture + CQRS
- **Frontend**: Angular 18.2 con tema Metronic
- **Base de Datos**: PostgreSQL con arquitectura database-per-tenant
- **Autenticación**: JWT + 2FA vía email

## Principios de Arquitectura

### Arquitectura del Backend (MasterBackup-API)

#### Capas de Clean Architecture

1. **Capa de Dominio** (`Domain/`)
   - Contiene las entidades de negocio y enumeraciones
   - NO tiene dependencias de otras capas
   - Solo lógica de negocio pura
   - Entidades: `ApplicationUser`, `Tenant`, `UserInvitation`, `Log`
   - Enums: `UserRole` (Admin, User)

2. **Capa de Aplicación** (`Application/`)
   - Depende ÚNICAMENTE de Domain
   - Contiene la orquestación de lógica de negocio
   - Usa patrón CQRS con MediatR
   - Estructura:
     - `Common/DTOs/` - Data Transfer Objects
     - `Common/Interfaces/` - Interfaces de servicios (`IEmailService`, `ITenantService`)
     - `Features/{Feature}/Commands/` - Operaciones de escritura
     - `Features/{Feature}/Queries/` - Operaciones de lectura
     - Cada Command/Query tiene su propio Handler

3. **Capa de Infraestructura** (`Infrastructure/`)
   - Depende de Application y Domain
   - Contiene implementaciones técnicas
   - Estructura:
     - `Persistence/` - DbContexts (`MasterDbContext`, `TenantDbContext`), Migraciones
     - `Services/` - Implementaciones de servicios (`EmailService`, `TenantService`, `AuthService`)
     - `Middleware/` - Middleware personalizado (`TenantMiddleware`, `RoleAuthorizationAttribute`)

4. **Capa de Presentación** (`Presentation/`)
   - Depende de Application
   - Contiene los controladores de la API
   - Los controladores deben ser ligeros - delegar a handlers de MediatR

#### Patrón CQRS

- **Commands**: Operaciones de escritura (Create, Update, Delete)
  - Ubicación: `Application/Features/{Feature}/Commands/`
  - Patrón: `{Action}Command.cs` + `{Action}CommandHandler.cs`
  - Ejemplo: `RegisterCommand.cs`, `RegisterCommandHandler.cs`
  
- **Queries**: Operaciones de lectura (Get, List, Search)
  - Ubicación: `Application/Features/{Feature}/Queries/`
  - Patrón: `{Action}Query.cs` + `{Action}QueryHandler.cs`

- **Handlers**: Implementan `IRequestHandler<TCommand, TResponse>`
  - Tipo de retorno: `Task<Result<T>>` o DTOs específicos
  - Usar FluentValidation para validación de entrada

#### Arquitectura Multi-Tenant

**IMPORTANTE - Arquitectura Centralizada de Usuarios:**

- **Database-per-Tenant**: Cada tenant tiene su propia base de datos PostgreSQL para datos de negocio
- **Base de Datos Master**: Almacena:
  - **Usuarios (ASP.NET Identity)**: Todos los usuarios en una sola base de datos
  - **Tenants**: Tabla con ApiKey único por tenant y ConnectionString
  - **Logs**: Logs centralizados de Serilog
  - **UserInvitations**: Invitaciones pendientes
  
- **NO usar Subdomain**: El concepto de subdomain fue completamente eliminado
- **Resolución de Tenant**: Se hace por:
  1. **JWT Token**: Contiene claim `TenantId`
  2. **API Key**: Header `X-API-Key` que mapea a `Tenant.ApiKey`
  
- **TenantMiddleware**: Middleware que resuelve el tenant actual y establece `ITenantContext`
- **Identity**: Registrado globalmente con `MasterDbContext` en `Program.cs`
- **UserManager**: Inyectado directamente en handlers desde DI (no crear manualmente)
- **Email Único Global**: Los emails son únicos en toda la plataforma, no por tenant

#### Tecnologías Clave

- **MediatR** 12.2.0 - Implementación de CQRS
- **FluentValidation** 11.9.0 - Validación de entrada (ÚNICA fuente de validación)
- **Serilog** - Logging estructurado con sink de PostgreSQL
- **ASP.NET Core Identity** - Gestión de usuarios en MasterDbContext
- **JWT Bearer Authentication** - Autenticación basada en tokens con claim TenantId
- **Maileroo** - Proveedor de servicio de email
- **PostgreSQL** - Base de datos con Npgsql provider
- **EF Core 8** - ORM para acceso a datos

### Arquitectura del Frontend (MasterBackup-App)

#### Estructura de Angular

- **Versión**: Angular 18.2
- **Arquitectura**: Componentes standalone (sin módulos)
- **Tema**: Metronic (assets en `public/assets/`)
- **Enrutamiento**: Configurado en `app.routes.ts`

#### Estado Actual

- Configuración básica completada
- Assets de Metronic integrados
- Componentes necesitan ser implementados

## Estándares de Código

### Backend (.NET)

#### Namespaces
```csharp
// Entidades de dominio
namespace MasterBackup_API.Domain.Entities;

// Características de aplicación
namespace MasterBackup_API.Application.Features.Auth.Commands;
namespace MasterBackup_API.Application.Common.DTOs;

// Infraestructura
namespace MasterBackup_API.Infrastructure.Services;
namespace MasterBackup_API.Infrastructure.Persistence;

// Presentación
namespace MasterBackup_API.Presentation.Controllers;
```

#### Ejemplo de Patrón Command
```csharp
// Command
public record RegisterCommand(RegisterDto Dto) : IRequest<AuthResponseDto>;

// Handler
public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponseDto>
{
    private readonly MasterDbContext _masterContext;
    private readonly UserManager<ApplicationUser> _userManager;
    private readonly ITenantService _tenantService;
    private readonly IEmailService _emailService;
    
    // IMPORTANTE: UserManager se inyecta desde DI, NO crear manualmente
    public RegisterCommandHandler(
        MasterDbContext masterContext,
        UserManager<ApplicationUser> userManager,
        ITenantService tenantService,
        IEmailService emailService,
        IConfiguration configuration,
        ILogger<RegisterCommandHandler> logger)
    {
        _masterContext = masterContext;
        _userManager = userManager;
        _tenantService = tenantService;
        _emailService = emailService;
    }
    
    public async Task<AuthResponseDto> Handle(RegisterCommand request, CancellationToken cancellationToken)
    {
        // 1. Validar email único globalmente
        var existingUser = await _userManager.FindByEmailAsync(request.Dto.Email);
        if (existingUser != null)
            return new AuthResponseDto { Success = false, Message = "Email already exists" };
        
        // 2. Crear tenant en master DB
        var tenant = new Tenant
        {
            Name = request.Dto.TenantName,
            ApiKey = GenerateApiKey(), // Generar API Key único
            IsActive = true
        };
        
        // 3. Crear base de datos del tenant
        var connectionString = await _tenantService.CreateTenantDatabaseAsync(tenant.Id, tenant.Name);
        tenant.ConnectionString = connectionString;
        
        _masterContext.Tenants.Add(tenant);
        await _masterContext.SaveChangesAsync(cancellationToken);
        
        // 4. Crear usuario en MasterDbContext
        var user = new ApplicationUser
        {
            UserName = request.Dto.Email,
            Email = request.Dto.Email,
            FirstName = request.Dto.FirstName,
            LastName = request.Dto.LastName,
            TenantId = tenant.Id,
            Role = UserRole.Admin,
            TwoFactorEnabled = request.Dto.EnableTwoFactor,
            IsActive = true
        };
        
        var result = await _userManager.CreateAsync(user, request.Dto.Password);
        if (!result.Succeeded)
            return new AuthResponseDto { Success = false, Message = string.Join(", ", result.Errors.Select(e => e.Description)) };
        
        // 5. Generar JWT con TenantId claim
        var token = GenerateJwtToken(user);
        
        return new AuthResponseDto
        {
            Success = true,
            Token = token,
            ApiKey = tenant.ApiKey // Devolver ApiKey para uso futuro
        };
    }
}
```

#### Controllers con FluentValidation
```csharp
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;
    private readonly IValidator<RegisterDto> _registerValidator;
    
    public AuthController(IMediator mediator, IValidator<RegisterDto> registerValidator)
    {
        _mediator = mediator;
        _registerValidator = registerValidator;
    }
    
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        // IMPORTANTE: Validar con FluentValidation antes de enviar comando
        var validationResult = await _registerValidator.ValidateAsync(dto);
        if (!validationResult.IsValid)
        {
            return BadRequest(new 
            { 
                errors = validationResult.Errors.Select(e => new 
                { 
                    field = e.PropertyName, 
                    message = e.ErrorMessage 
                }) 
            });
        }
        
        var result = await _mediator.Send(new RegisterCommand(dto));
        
        if (!result.Success)
            return BadRequest(new { message = result.Message });
            
        return Ok(result);
    }
}
```

#### Validators con FluentValidation
```csharp
public class RegisterDtoValidator : AbstractValidator<RegisterDto>
{
    public RegisterDtoValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email must be a valid email address")
            .MaximumLength(255).WithMessage("Email must not exceed 255 characters");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(8).WithMessage("Password must be at least 8 characters")
            .MaximumLength(100).WithMessage("Password must not exceed 100 characters")
            .Matches(@"[A-Z]").WithMessage("Password must contain at least one uppercase letter")
            .Matches(@"[a-z]").WithMessage("Password must contain at least one lowercase letter")
            .Matches(@"[0-9]").WithMessage("Password must contain at least one digit");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required")
            .MaximumLength(50).WithMessage("First name must not exceed 50 characters");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required")
            .MaximumLength(50).WithMessage("Last name must not exceed 50 characters");

        RuleFor(x => x.TenantName)
            .NotEmpty().WithMessage("Tenant name is required")
            .MaximumLength(100).WithMessage("Tenant name must not exceed 100 characters");
    }
}
```

### Frontend (Angular)

#### Estructura de Componentes
```typescript
@Component({
  selector: 'app-feature',
  standalone: true,
  imports: [CommonModule, ReactiveFormsModule, ...],
  templateUrl: './feature.component.html',
  styleUrl: './feature.component.css'
})
export class FeatureComponent {
  // Implementación
}
```

#### Patrón de Servicios
```typescript
@Injectable({ providedIn: 'root' })
export class AuthService {
  private apiUrl = environment.apiUrl;
  
  constructor(private http: HttpClient) {}
  
  login(credentials: LoginDto): Observable<AuthResponse> {
    return this.http.post<AuthResponse>(`${this.apiUrl}/auth/login`, credentials);
  }
}
```

#### Componentes Implementados de Workers y Database Connections

**Workers Component** (`src/app/features/workers/component/workers/`)
- Lista de workers con cards estilo Metronic
- Estados visuales: Active (badge-success) / Inactive (badge-secondary)
- Indicador de heartbeat: Online (<5 min) / Offline (≥5 min)
- Tags visualizados como badges
- Botones de acción: Detalles, Activar/Desactivar
- Navegación a detalle de worker

**Worker Detail Component** (`src/app/features/workers/component/worker-detail/`)
- Vista detallada de worker individual
- Estadísticas en tiempo real
- Información de último heartbeat
- Acciones: Activar/Desactivar, Editar

**Database Connections Component** (`src/app/features/databases/component/database-connections/`)
- Lista de conexiones con tabla estilo Metronic
- Columnas: Name, Type, Host:Port, Worker Assignment, Status, Last Test
- Indicadores de estado: Active (success) / Inactive (danger)
- Resultado de último test con timestamp
- Botones: Test Connection, Edit, Delete
- Modal de creación/edición de conexiones

**Database Connection Form (en modal):**
```typescript
// Campos del formulario
- name: string (required, max 100)
- databaseType: DatabaseType (required, select)
- host: string (required)
- port: number (required)
- databaseName: string (required)
- username: string (required)
- password: string (required en create, opcional en update)
- assignmentMode: WorkerAssignmentMode (required, radio buttons)
- assignedWorkerId: Guid? (required si mode=Dedicated)
- tags: string (required si mode=Automatic, JSON array)
- isActive: boolean (checkbox)

// Validación del formulario
- Password solo requerido en create
- Worker solo requerido si assignmentMode = Dedicated
- Tags solo requeridos si assignmentMode = Automatic
```

**SweetAlert2 para Confirmaciones:**
```typescript
// Instalación: npm install sweetalert2
// Importar: import Swal from 'sweetalert2';

// Ejemplo de uso para eliminar
Swal.fire({
  title: 'Are you sure?',
  text: 'This action cannot be undone',
  icon: 'warning',
  showCancelButton: true,
  confirmButtonColor: '#d33',
  cancelButtonColor: '#3085d6',
  confirmButtonText: 'Yes, delete it!'
}).then((result) => {
  if (result.isConfirmed) {
    // Ejecutar eliminación
  }
});
```

**RelativeTimePipe:**
```typescript
// src/app/core/pipes/relative-time.pipe.ts
@Pipe({ name: 'relativeTime', standalone: true })
export class RelativeTimePipe implements PipeTransform {
  transform(value: string | Date | null): string {
    // Convierte timestamp a formato relativo
    // "2 minutes ago", "5 hours ago", "3 days ago"
  }
}

// Uso en templates:
{{ worker.lastHeartbeat | relativeTime }}
```

**DatabaseConnectionService:**
```typescript
@Injectable({ providedIn: 'root' })
export class DatabaseConnectionService {
  // Métodos implementados:
  getAll(): Observable<DatabaseConnectionDto[]>
  getById(id: string): Observable<DatabaseConnectionDto>
  create(dto: CreateDatabaseConnectionDto): Observable<DatabaseConnectionDto>
  update(id: string, dto: UpdateDatabaseConnectionDto): Observable<void>
  delete(id: string): Observable<void>
  testConnection(id: string): Observable<TestConnectionResultDto>
}
```

**WorkerService:**
```typescript
@Injectable({ providedIn: 'root' })
export class WorkerService {
  // Métodos implementados:
  getAll(): Observable<WorkerDto[]>
  getById(id: string): Observable<WorkerDto>
  getStats(): Observable<WorkerStatsDto>
  activate(id: string): Observable<void>
  deactivate(id: string): Observable<void>
}
```

## Guías de Desarrollo

### Al Crear Nuevas Funcionalidades

1. **Comenzar con Domain**: Definir entidades y reglas de negocio
2. **Crear DTOs**: En `Application/Common/DTOs/`
3. **Implementar Commands/Queries**: En `Application/Features/{Feature}/`
4. **Crear Handlers**: Implementar lógica de negocio
5. **Agregar Validadores**: Usar FluentValidation
6. **Crear Controller**: Controlador ligero que delega a MediatR
7. **Actualizar Frontend**: Crear componentes/servicios de Angular

### Flujo de Autenticación Actualizado

**Registro (`POST /api/auth/register`):**
1. Validar DTO con FluentValidation
2. Verificar que email NO existe globalmente en MasterDbContext
3. Crear Tenant en master DB con ApiKey único generado
4. Crear database `tenant_{guid}` vía TenantService
5. Crear usuario en MasterDbContext con TenantId y Role=Admin
6. Retornar JWT + ApiKey del tenant

**Login (`POST /api/auth/login`):**
1. Validar DTO con FluentValidation
2. Buscar usuario por email en MasterDbContext (globalmente único)
3. Verificar contraseña con UserManager
4. Si 2FA habilitado:
   - Sin código: Generar y enviar código por email, retornar `twoFactorRequired: true`
   - Con código: Validar código y continuar
5. Generar JWT con claims: userId, email, role, **TenantId**
6. Retornar JWT

**Verificación 2FA (`POST /api/auth/verify-2fa`):**
1. Validar email, password y código 2FA
2. Buscar usuario en MasterDbContext
3. Verificar contraseña y código 2FA
4. Limpiar código de la base de datos
5. Generar JWT con TenantId
6. Retornar JWT

**Resolución de Tenant (TenantMiddleware):**
1. Extraer TenantId del JWT claim **O**
2. Buscar tenant por header `X-API-Key` en tabla Tenants
3. Establecer `ITenantContext.TenantId` y `ConnectionString`
4. Requests subsecuentes usan TenantDbContext con ConnectionString dinámico

**IMPORTANTE:**
- NO usar subdomain en ningún endpoint
- Email es único globalmente (no por tenant)
- Usuarios se buscan/crean SOLO en MasterDbContext
- UserManager inyectado desde DI (registrado con MasterDbContext en Program.cs)
- JWT siempre incluye claim TenantId

### Migraciones de Base de Datos

```bash
# Base de datos master
dotnet ef migrations add MigrationName --context MasterDbContext --output-dir Infrastructure/Persistence/Migrations/Master

# Plantilla de base de datos tenant
dotnet ef migrations add MigrationName --context TenantDbContext --output-dir Infrastructure/Persistence/Migrations/Tenant
```

### Ejecutar el Proyecto

**Backend:**
```bash
cd MasterBackup-API
dotnet run
```
API: https://localhost:7001

**Frontend:**
```bash
cd MasterBackup-App
npm install
ng serve
```
App: http://localhost:4200

## Patrones Comunes

### Manejo de Errores
- Usar try-catch en handlers
- Retornar mensajes de error significativos
- Registrar errores con Serilog
- Usar códigos de estado HTTP apropiados

### Validación

**IMPORTANTE: Usar ÚNICAMENTE FluentValidation, NO DataAnnotations**

- ✅ Crear validators en `Application/Common/Validators/`
- ✅ Registrar en DI: `builder.Services.AddValidatorsFromAssemblyContaining<Program>()`
- ✅ Inyectar `IValidator<TDto>` en controllers
- ✅ Validar ANTES de enviar comando a MediatR
- ✅ Formato de error estándar:
  ```json
  {
    "errors": [
      { "field": "Email", "message": "Email is required" },
      { "field": "Password", "message": "Password must be at least 8 characters" }
    ]
  }
  ```
- ❌ NO usar `[Required]`, `[EmailAddress]`, `[MinLength]` en DTOs
- ❌ NO usar `ModelState.IsValid` - usar FluentValidation

### Autorización
- Usar atributo `[Authorize]`
- Usar `RoleAuthorizationAttribute` personalizado para acceso basado en roles
- Verificar aislamiento de tenant en handlers

### Plantillas de Email
- Ubicadas en el servicio de email
- Usar plantillas HTML
- Incluir marca de la empresa
- Probar con Maileroo

## Configuración de Entorno

### Configuración con Variables de Entorno

**IMPORTANTE: Usar variables de entorno para conexiones sensibles**

#### Variables de Entorno (Prioritarias)
```bash
# PostgreSQL Master Database
MASTER_DATABASE_CONNECTION="Host=postgres.railway.app;Port=5432;Database=railway;Username=postgres;Password=***"

# PostgreSQL para Serilog (puede ser la misma)
SERILOG_DATABASE_CONNECTION="Host=postgres.railway.app;Port=5432;Database=railway;Username=postgres;Password=***"
```

#### appsettings.json (Fallback)
```json
{
  "ConnectionStrings": {
    "MasterDatabase": "Host=localhost;Database=master_saas;Username=postgres;Password=***",
    "SerilogDatabase": "Host=localhost;Database=master_saas;Username=postgres;Password=***"
  },
  "Jwt": {
    "Key": "your-secret-key-min-32-chars",
    "Issuer": "MasterBackup",
    "Audience": "MasterBackup",
    "ExpiryMinutes": 1440
  },
  "Maileroo": {
    "ApiKey": "your-maileroo-api-key",
    "SenderEmail": "noreply@masterbackup.com",
    "SenderName": "MasterBackup"
  },
  "Serilog": {
    "MinimumLevel": {
      "Default": "Information",
      "Override": {
        "Microsoft": "Warning",
        "Microsoft.AspNetCore": "Warning",
        "System": "Warning"
      }
    }
  }
}
```

#### Orden de Prioridad en Program.cs
```csharp
// 1. Intentar variable de entorno
var masterConnection = Environment.GetEnvironmentVariable("MASTER_DATABASE_CONNECTION");

// 2. Fallback a appsettings.json
if (string.IsNullOrEmpty(masterConnection))
{
    masterConnection = builder.Configuration.GetConnectionString("MasterDatabase");
}
```

### Entorno de Angular
```typescript
export const environment = {
  production: false,
  apiUrl: 'https://localhost:7001/api'
};
```

## Estrategia de Testing

### Backend
- Pruebas unitarias para handlers
- Pruebas de integración para controllers
- Probar aislamiento de tenants
- Probar flujos de autenticación

### Frontend
- Pruebas de componentes con Jasmine/Karma
- Pruebas E2E para flujos críticos
- Probar diseño responsivo

## Consideraciones de Seguridad

- Nunca registrar contraseñas o datos sensibles
- Siempre hacer hash de contraseñas con Identity
- Validar JWT en cada petición
- Implementar limitación de tasa (rate limiting)
- Usar HTTPS en producción
- Sanitizar entradas de usuarios
- Proteger contra inyección SQL (usar queries parametrizadas de EF Core)
- Implementar CORS apropiadamente

## Optimización de Rendimiento

- Usar async/await consistentemente
- Implementar caché donde sea apropiado
- Optimizar consultas de base de datos
- Usar lazy loading en Angular
- Minimizar llamadas a la API
- Implementar paginación para listas

## Despliegue

- Usar Docker (Dockerfile incluido)
- Configurar pipeline de CI/CD
- Usar variables de entorno para secretos
- Configurar respaldos de base de datos
- Monitorear logs con Serilog
- Implementar health checks

## Documentación

- Mantener README.md actualizado
- Documentar API con Swagger
- Usar comentarios XML en código C#
- Documentar lógica de negocio compleja
- Mantener CHANGELOG.md

## Objetivo y Alcance del Proyecto

**MasterBackup** es una aplicación SaaS multi-tenant para gestión automatizada de backups de bases de datos con las siguientes características principales:

### Funcionalidades Core

1. **Gestión de Conexiones de Bases de Datos**
   - Crear, activar, inactivar y editar conexiones
   - Soportar múltiples tipos de bases de datos (PostgreSQL prioritario)
   - Validación de conexiones antes de guardar

2. **Programación Flexible de Backups**
   - Usar Quartz.NET para scheduling
   - Interfaz amigable con expresiones CRON
   - Backups programados y backups instantáneos (on-demand)

3. **Ejecución de Backups**
   - Ejecutar backups inmediatos
   - Procesar backups programados automáticamente
   - Integración con RabbitMQ para comunicación con workers

4. **Gestión de Archivos de Backup**
   - Listar backups realizados con filtros y búsqueda
   - Descargar backups desde Azure Blob Storage
   - Eliminar backups obsoletos

5. **Workers Dockerizados**
   - Workers en red local del cliente con Docker
   - Precargados con `pg_dump` y `pg_restore`
   - Consumen mensajes de RabbitMQ por tenant
   - Actualizan estado via API
   - Envían notificaciones por email

### Arquitectura de Comunicación

#### Flujo de Backups Programados
1. Usuario programa backup → API REST
2. API registra job en Quartz.NET
3. Quartz dispara job → mensaje a RabbitMQ `{tenantId}.backup.queue`
4. Worker consume mensaje de su queue específica
5. Worker actualiza estado en DB via API
6. BackupStrategyFactory selecciona estrategia (PostgreSQL)
7. Worker ejecuta backup y sube a Azure Blob Storage
8. Worker actualiza estado final en DB
9. Worker envía notificaciones por email
10. Worker notifica finalización a API
11. API notifica a frontend via SignalR

#### Flujo de Backups Instantáneos
1. Usuario solicita backup inmediato → API REST
2. API envía mensaje directo a RabbitMQ `{tenantId}.backup.queue`
3. Worker consume mensaje
4. BackupStrategyFactory selecciona estrategia
5. Worker ejecuta backup y sube a Azure Blob Storage
6. Worker actualiza estado en DB via API
7. Worker envía notificaciones por email
8. Worker notifica finalización a API
9. API notifica a frontend via SignalR

### Tecnologías Clave Adicionales

- **Quartz.NET**: Scheduling de jobs de backup
- **RabbitMQ**: Message broker para comunicación API ↔ Workers
- **Azure Blob Storage**: Almacenamiento de archivos de backup
- **SignalR**: Notificaciones en tiempo real a frontend
- **Docker**: Containerización de workers con pg_dump/pg_restore
- **PostgreSQL Tools**: pg_dump y pg_restore para backups nativos

### Entidades Adicionales en Master Database

**Base de Datos Master extendida con:**
- `Users` - Usuarios (ya existe con Identity)
- `Roles` - Roles de usuarios (parte de Identity)
- `Tenants` - Tenants con ApiKey y ConnectionString
- `Logs` - Logs centralizados con Serilog
- `Notifications` - Notificaciones de la aplicación
- `SubscriptionPlans` - Planes de suscripción para tenants
- `Workers` - **✅ IMPLEMENTADO** - Registro de workers con TenantId, Name, ApiKey, Status, Tags, LastHeartbeat

**Base de Datos por Tenant incluirá:**
- `DatabaseConnections` - **✅ IMPLEMENTADO** - Conexiones a BD con AssignedWorkerId, AssignmentMode, Tags
- `BackupSchedules` - Programaciones de backups con CRON
- `BackupExecutions` - Historial de ejecuciones de backups
- `BackupFiles` - Referencias a archivos en Azure Blob Storage
- `UserNotificationPreferences` - Preferencias de notificación por usuario

### Arquitectura de Workers y Asignación (IMPLEMENTADO ✅)

#### Entidad Worker (Master Database)
```csharp
public class Worker
{
    public Guid Id { get; set; }
    public Guid TenantId { get; set; }
    public string Name { get; set; }
    public string? Description { get; set; }
    public string ApiKey { get; set; }  // Para autenticación del worker
    public WorkerStatus Status { get; set; }  // Active, Inactive
    public string? Tags { get; set; }  // JSON array de tags ["linux", "postgresql"]
    public DateTime? LastHeartbeat { get; set; }
    public DateTime CreatedAt { get; set; }
}

public enum WorkerStatus
{
    Inactive = 0,
    Active = 1
}
```

#### Entidad DatabaseConnection (Tenant Database)
```csharp
public class DatabaseConnection
{
    public Guid Id { get; set; }
    public string Name { get; set; }
    public DatabaseType DatabaseType { get; set; }  // PostgreSQL, MySQL, SQLServer, MongoDB
    public string Host { get; set; }
    public int Port { get; set; }
    public string DatabaseName { get; set; }
    public string Username { get; set; }
    public string EncryptedPassword { get; set; }  // Encriptado con EncryptionService
    public bool IsActive { get; set; }
    
    // Worker Assignment
    public Guid? AssignedWorkerId { get; set; }  // ID del worker en MasterDb
    public WorkerAssignmentMode AssignmentMode { get; set; }  // Automatic, Dedicated
    public string? Tags { get; set; }  // JSON array ["postgresql", "production"]
    
    // Connection Testing
    public DateTime? LastTestDate { get; set; }
    public string? LastTestStatus { get; set; }  // "Success", "Failed: ..."
    public DateTime CreatedAt { get; set; }
    public DateTime UpdatedAt { get; set; }
}

public enum WorkerAssignmentMode
{
    Automatic = 0,  // Asignación por tags
    Dedicated = 1   // Worker dedicado específico
}

public enum DatabaseType
{
    PostgreSQL = 0,
    MySQL = 1,
    SQLServer = 2,
    MongoDB = 3
}
```

#### Flujo de Asignación de Workers

**Modo Automático (por Tags):**
1. Usuario crea conexión y selecciona modo "Automatic"
2. Usuario ingresa tags (ej: ["postgresql", "production"])
3. Sistema NO valida worker en creación (AssignedWorkerId = null)
4. En tiempo de backup, sistema busca worker activo con tags coincidentes
5. Si no hay worker disponible, backup falla con error descriptivo

**Modo Dedicado:**
1. Usuario crea conexión y selecciona modo "Dedicated"
2. Usuario selecciona worker específico de dropdown
3. **Validación crítica**: Sistema valida que WorkerId existe en MasterDb.Workers
4. Sistema valida que worker pertenece al mismo TenantId
5. Sistema asigna AssignedWorkerId
6. En tiempo de backup, usa solo ese worker específico

#### Validación de Workers en Backend

**CreateDatabaseConnectionDtoValidator:**
```csharp
public class CreateDatabaseConnectionDtoValidator : AbstractValidator<CreateDatabaseConnectionDto>
{
    private readonly MasterDbContext _masterContext;
    private readonly IHttpContextAccessor _httpContextAccessor;
    
    public CreateDatabaseConnectionDtoValidator(
        MasterDbContext masterContext,
        IHttpContextAccessor httpContextAccessor)
    {
        _masterContext = masterContext;
        _httpContextAccessor = httpContextAccessor;
        
        // Validación condicional de AssignedWorkerId
        When(x => x.AssignmentMode == WorkerAssignmentMode.Dedicated, () =>
        {
            RuleFor(x => x.AssignedWorkerId)
                .NotNull()
                .WithMessage("Worker is required when assignment mode is Dedicated")
                .MustAsync(WorkerExistsForTenant)
                .WithMessage("Invalid worker ID or worker does not belong to your tenant");
        });
        
        When(x => x.AssignmentMode == WorkerAssignmentMode.Automatic, () =>
        {
            RuleFor(x => x.Tags)
                .NotEmpty()
                .WithMessage("Tags are required when assignment mode is Automatic");
        });
    }
    
    private async Task<bool> WorkerExistsForTenant(Guid? workerId, CancellationToken cancellationToken)
    {
        if (!workerId.HasValue) return false;
        
        var tenantIdClaim = _httpContextAccessor.HttpContext?.User
            .FindFirst("TenantId")?.Value;
        
        if (string.IsNullOrEmpty(tenantIdClaim)) return false;
        
        var tenantId = Guid.Parse(tenantIdClaim);
        
        return await _masterContext.Workers
            .AnyAsync(w => w.Id == workerId.Value && w.TenantId == tenantId, cancellationToken);
    }
}
```

#### Test Connection Flow (IMPLEMENTADO)

**Endpoint**: `POST /api/TestConnections/{connectionId}/test`

**Flujo Completo:**
1. Frontend llama endpoint con connectionId
2. Backend busca conexión en TenantDb
3. Backend desencripta password con EncryptionService
4. Backend crea mensaje TestConnectionMessage:
   ```json
   {
     "connectionId": "guid",
     "databaseType": "PostgreSQL",
     "host": "localhost",
     "port": 5432,
     "database": "mydb",
     "username": "user",
     "password": "decrypted_pass",
     "tenantId": "tenant_guid"
   }
   ```
5. Backend publica mensaje a RabbitMQ queue: `{tenantId}.test-connection.queue`
6. Worker consume mensaje de su queue
7. Worker ejecuta test de conexión nativo
8. Worker publica resultado a RabbitMQ response queue: `{tenantId}.test-connection.response`
9. Backend consume respuesta y actualiza LastTestDate + LastTestStatus
10. Frontend recibe respuesta inmediata

**Nota Importante sobre Workers y TenantDb:**
- ❌ La tabla Worker NO debe existir en TenantDb
- ✅ Workers solo existen en MasterDb (tabla global)
- ✅ DatabaseConnections en TenantDb solo almacena AssignedWorkerId (GUID)
- ✅ Para validar workers, siempre consultar MasterDb.Workers con TenantId
- ✅ Migración `RemoveWorkerTableFromTenant` elimina tabla Worker del tenant

#### Servicios Implementados

**EncryptionService:**
```csharp
public interface IEncryptionService
{
    string Encrypt(string plainText);
    string Decrypt(string cipherText);
}

// Usa AES con key de appsettings.json "Encryption:Key"
// Implementación con IV aleatorio por cada encriptación
```

**DatabaseConnectionTester:**
```csharp
public interface IDatabaseConnectionTester
{
    Task<TestConnectionResultDto> TestConnectionAsync(
        DatabaseConnection connection, 
        CancellationToken cancellationToken = default);
}

// Envía mensaje a RabbitMQ y espera respuesta
// Timeout configurable (default 30 segundos)
```

**RabbitMQService:**
```csharp
public interface IMessageQueueService
{
    Task PublishAsync<T>(string queueName, T message);
    Task<TResponse?> PublishAndWaitForResponseAsync<TRequest, TResponse>(
        string requestQueue, 
        string responseQueue, 
        TRequest message, 
        TimeSpan timeout);
}

// Naming convention: "{tenantId}.{operation}.queue"
// Ejemplos:
// - "abc-123.test-connection.queue"
// - "abc-123.test-connection.response"
// - "abc-123.backup.queue"
```

### Patrones de Diseño a Implementar

1. **Strategy Pattern**: `BackupStrategyFactory` para diferentes tipos de BD
   - `PostgreSQLBackupStrategy`
   - `MySQLBackupStrategy` (futuro)
   - `SQLServerBackupStrategy` (futuro)

2. **Repository Pattern**: Acceso a datos abstracto por entidad

3. **Unit of Work**: Transacciones coordinadas en handlers

4. **Factory Pattern**: Creación de estrategias de backup

### Consideraciones de Seguridad

- Resolución de tenant por JWT claim `TenantId` O header `X-API-Key`
- Workers autenticados con API Key del tenant
- Credenciales de BD encriptadas en DatabaseConnections
- Tokens de acceso a Azure Blob Storage con expiración
- RabbitMQ con queues aisladas por tenant: `{tenantId}.backup.queue`

## Estado Actual del Proyecto (Diciembre 2025)

✅ **Completado - Backend:**
- ✅ Estructura de Clean Architecture
- ✅ CQRS con MediatR 12.2.0
- ✅ Multi-tenant database-per-tenant
- ✅ **Usuarios centralizados en MasterDbContext**
- ✅ **Resolución de tenant por JWT/API Key (NO subdomain)**
- ✅ Autenticación completa (register, login, 2FA, reset password)
- ✅ FluentValidation en todos los endpoints (8 validators)
- ✅ TenantMiddleware funcional
- ✅ Servicio de email con Maileroo
- ✅ Logging con Serilog + PostgreSQL sink
- ✅ Variables de entorno para configuración sensible
- ✅ Migraciones aplicadas exitosamente
- ✅ **Código limpiado (sin deprecados)**
- ✅ Compilación exitosa sin errores ni warnings
- ✅ **Workers**: CRUD completo con Commands/Queries/Handlers
- ✅ **DatabaseConnections**: CRUD completo con asignación de workers
- ✅ **Test Connection**: Flujo completo con RabbitMQ y workers
- ✅ **Worker Assignment**: Modos Automático (tags) y Dedicado
- ✅ **EncryptionService**: Encriptación de credenciales de BD
- ✅ **DatabaseConnectionTester**: Servicio para test de conexiones
- ✅ **RabbitMQService**: Integración con message broker

✅ **Completado - Frontend:**
- ✅ Estructura con Angular 18.2 standalone components
- ✅ Tema Metronic integrado
- ✅ Componente de login multi-paso (email → password → 2FA)
- ✅ Componente de registro con validaciones
- ✅ Forgot password y reset password completos
- ✅ Saved accounts en localStorage
- ✅ AuthService con todos los endpoints
- ✅ Guards de autenticación (authGuard)
- ✅ Layout con header, sidebar y footer
- ✅ Dashboard inicial
- ✅ Rutas de un solo nivel (/dashboard, no /home/dashboard)
- ✅ Título dinámico de página con íconos Font Awesome
- ✅ StorageService para manejo consistente de localStorage
- ✅ **Workers UI**: Lista, detalle, estadísticas y estados (Active/Inactive)
- ✅ **DatabaseConnections UI**: CRUD completo con formulario de creación/edición
- ✅ **Worker Assignment UI**: Selector de modo y worker dedicado
- ✅ **SweetAlert2**: Diálogos de confirmación para eliminaciones
- ✅ **RelativeTimePipe**: Pipe para formato de tiempo relativo
- ✅ **DatabaseConnectionService**: Servicio completo con test connection

⏳ **Siguiente Fase - Programación y Ejecución de Backups:**
- ⏳ Entidades: DatabaseConnections, BackupSchedules, BackupExecutions, BackupFiles
- ⏳ Integración con Quartz.NET para scheduling
- ⏳ Integración con RabbitMQ para comunicación con workers
- ⏳ Integración con Azure Blob Storage
- ⏳ Workers dockerizados con pg_dump/pg_restore
- ⏳ BackupStrategyFactory con PostgreSQLBackupStrategy
- ⏳ Sistema de notificaciones por email
- ⏳ SignalR para notificaciones en tiempo real
- ⏳ CRUD de conexiones de bases de datos
- ⏳ Programación de backups con expresiones CRON amigables
- ⏳ Ejecución de backups instantáneos
- ⏳ Listado y gestión de archivos de backup
- ⏳ Dashboard con estadísticas de backups

⏳ **Pendiente - Funcionalidades Adicionales:**
- ⏳ Gestión de SubscriptionPlans
- ⏳ Gestión de Notifications
- ⏳ Gestión de usuarios por tenant
- ⏳ Reportes y analytics
- ⏳ Cobertura de pruebas unitarias e integración

## Arquitectura Limpia - Código Eliminado

**Archivos Eliminados (Deprecados):**
- ❌ `Infrastructure/Services/AuthService.cs` - Migrado a CQRS handlers
- ❌ `Infrastructure/Services/IAuthService.cs` - Interface obsoleta

**Métodos Eliminados:**
- ❌ `TenantService.GetTenantDbContextOptionsBySubdomainAsync()`
- ❌ `TenantService.GetTenantConnectionStringAsync()`

**Conceptos Eliminados:**
- ❌ Subdomain en toda la aplicación
- ❌ DataAnnotations en DTOs (usar solo FluentValidation)
- ❌ Creación manual de UserManager en handlers

## Referencia de Archivos Clave

### Backend Críticos
- `Program.cs` - DI, Identity, JWT, Middleware, Serilog, FluentValidation
- `MasterDbContext.cs` - **Hereda de IdentityDbContext<ApplicationUser>**, contiene Users, Tenants, Logs, UserInvitations, **Workers**
- `TenantDbContext.cs` - DbContext simple para datos de negocio del tenant (sin Identity, **sin Worker**)
- `ApplicationUser.cs` - Usuario con TenantId y Role
- `Tenant.cs` - **Sin Subdomain**, con ApiKey y ConnectionString
- `Worker.cs` - Worker con TenantId, Name, ApiKey, Status, Tags (en MasterDb)
- `DatabaseConnection.cs` - Conexión con AssignedWorkerId, AssignmentMode, Tags, EncryptedPassword
- `TenantMiddleware.cs` - Resolución de tenant por JWT claim o X-API-Key header
- `*CommandHandler.cs` - Todos usan MasterDbContext + UserManager inyectado
- `*Validator.cs` - 8+ validators para todos los DTOs
- `EncryptionService.cs` - Encriptación/desencriptación de passwords
- `DatabaseConnectionTester.cs` - Test de conexiones con RabbitMQ
- `RabbitMQService.cs` - Publicación/consumo de mensajes

### Migraciones Importantes
- `20251201145833_RemoveWorkerTableFromTenant` - Elimina Worker de TenantDb
- `RemoveWorkerTable_Manual.sql` - Script SQL manual para limpiar TenantDb
- `20251130191957_AddWorkersTable` - Crea Workers en MasterDb
- `20251130193758_AddWorkerAssignmentToDatabaseConnection` - Agrega campos de asignación

### Frontend Críticos
- `app.routes.ts` - Enrutamiento de la aplicación
- `app.config.ts` - Configuración de la aplicación
- `app.component.ts` - Componente raíz
- `workers.component.ts` - Lista de workers
- `worker-detail.component.ts` - Detalle de worker
- `database-connections.component.ts` - CRUD de conexiones
- `worker.service.ts` - Servicio de workers
- `database-connection.service.ts` - Servicio de conexiones
- `relative-time.pipe.ts` - Pipe para tiempo relativo

## Recursos de Soporte

### Documentación de Arquitectura
- Clean Architecture: `CLEAN_ARCHITECTURE.md`
- Guía de Migración: `MIGRATION_GUIDE.md`
- Implementación de Serilog: `SERILOG_IMPLEMENTATION.md`
- **Arquitectura de Workers**: `WORKER_ARCHITECTURE.md`
- **Test Connection Flow**: `TEST_CONNECTION_FLOW.md`
- **Workers UI Implementation**: `WORKERS_UI_IMPLEMENTATION.md`
- **Frontend Test Connection**: `FRONTEND_TEST_CONNECTION_IMPLEMENTATION.md`
- **Backup Management Plan**: `.github/BACKUP_MANAGEMENT_PLAN.md`

### Worker Service (MasterBackup-Worker)
- **README**: `MasterBackup-Worker/README.md`
- **PostgreSQL Tools**: `MasterBackup-Worker/POSTGRESQL_TOOLS.md`
- **API Key Verification**: `MasterBackup-Worker/VERIFY_APIKEY.sql`
- **Configuration Example**: `MasterBackup-Worker/appsettings.example.json`

### Documentación de API
- Swagger: `/swagger` (en desarrollo)
- `MasterBackup-API.http` - Colección de requests HTTP

## Notas para Asistentes de IA

### Reglas Críticas (NO VIOLAR)

1. **✅ Usuarios en MasterDbContext SIEMPRE**
   - ❌ NUNCA crear usuarios en TenantDbContext
   - ❌ NUNCA buscar usuarios en TenantDbContext
   - ✅ Todos los usuarios están en la base de datos master
   
2. **✅ UserManager Inyectado desde DI**
   - ❌ NUNCA crear UserManager manualmente con `new UserManager<>()`
   - ✅ Inyectar `UserManager<ApplicationUser>` en constructor
   - ✅ Está registrado en Program.cs con MasterDbContext

3. **✅ Email Único Globalmente**
   - ❌ NUNCA validar email único por tenant
   - ✅ Email es único en toda la plataforma
   - ✅ Buscar usuarios solo por email (no requiere TenantId)

4. **✅ NO Usar Subdomain**
   - ❌ NUNCA agregar propiedad Subdomain a Tenant
   - ❌ NUNCA aceptar subdomain en endpoints
   - ✅ Resolución de tenant solo por JWT claim o API Key

5. **✅ FluentValidation ÚNICAMENTE**
   - ❌ NUNCA usar DataAnnotations ([Required], [EmailAddress], etc.)
   - ❌ NUNCA usar ModelState.IsValid
   - ✅ Crear validators en Application/Common/Validators/
   - ✅ Inyectar IValidator<TDto> en controllers
   - ✅ Validar antes de enviar comando a MediatR

6. **✅ TenantId en JWT**
   - ✅ JWT SIEMPRE debe incluir claim "TenantId"
   - ✅ TenantMiddleware lee este claim
   - ✅ Requests subsecuentes usan el tenant resuelto

7. **✅ Estructura de Respuestas**
   - ✅ Errores de validación: `{ errors: [{ field, message }] }`
   - ✅ Errores de negocio: `{ success: false, message: "..." }`
   - ✅ Éxito: `{ success: true, data: {...} }`

8. **✅ Workers SOLO en MasterDb (CRÍTICO)**
   - ❌ NUNCA crear tabla Worker en TenantDbContext
   - ❌ NUNCA agregar DbSet<Worker> en TenantDbContext
   - ✅ Workers están ÚNICAMENTE en MasterDbContext
   - ✅ DatabaseConnections almacena solo AssignedWorkerId (Guid?)
   - ✅ Validar workers consultando MasterDb.Workers con TenantId
   - ✅ Si existe migración que crea Worker en TenantDb, revertirla

9. **✅ Validación de Workers en DatabaseConnections**
   - ✅ Modo Dedicado: Validar que WorkerId existe en MasterDb.Workers
   - ✅ Validar que Worker.TenantId = Usuario.TenantId
   - ✅ Usar MasterDbContext + IHttpContextAccessor en validators
   - ✅ Modo Automático: NO validar worker, solo requerir tags
   - ❌ NUNCA permitir AssignedWorkerId sin validación previa

10. **✅ Encriptación de Credenciales**
    - ✅ Password de DatabaseConnections SIEMPRE encriptado
    - ✅ Usar EncryptionService para encriptar/desencriptar
    - ✅ Desencriptar solo cuando se necesita usar (test, backup)
    - ❌ NUNCA loggear passwords desencriptados
    - ❌ NUNCA retornar passwords en DTOs del API

### Buenas Prácticas

- Siempre respetar las dependencias entre capas (sin referencias circulares)
- Mantener los handlers enfocados y con responsabilidad única
- Usar inyección de dependencias consistentemente
- Seguir las convenciones de nomenclatura existentes
- Mantener el aislamiento de tenants en todas las operaciones
- Probar escenarios multi-tenant exhaustivamente
- Documentar cambios que rompan compatibilidad
- Actualizar guías de migración al cambiar la arquitectura
- Usar async/await en toda operación de IO
- Loggear errores con Serilog
- Nunca exponer información sensible en logs o errores

### Comandos Útiles

```bash
# Compilar proyecto
dotnet build

# Crear migración master
dotnet ef migrations add MigrationName --context MasterDbContext --output-dir Infrastructure/Persistence/Migrations/Master

# Aplicar migraciones
dotnet ef database update --context MasterDbContext

# Ejecutar proyecto
dotnet run

# Limpiar y reconstruir
dotnet clean && dotnet build
```

### Debugging

- Verificar variables de entorno: `MASTER_DATABASE_CONNECTION`, `SERILOG_DATABASE_CONNECTION`
- Verificar logs en tabla `Logs` de master database
- Verificar JWT claims con jwt.io
- Verificar TenantMiddleware con logs
- Verificar Identity está registrado con MasterDbContext en Program.cs
