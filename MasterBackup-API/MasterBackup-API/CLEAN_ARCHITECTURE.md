# Clean Architecture + CQRS Implementation

Este proyecto implementa **Clean Architecture** junto con el patrón **CQRS** (Command Query Responsibility Segregation) usando **MediatR**.

## Estructura del Proyecto

```
MasterBackup-API/
├── Domain/                              # Capa de Dominio (núcleo del negocio)
│   ├── Entities/                        # Entidades del dominio
│   │   ├── ApplicationUser.cs
│   │   ├── Tenant.cs
│   │   └── UserInvitation.cs
│   └── Enums/
│       └── UserRole.cs
│
├── Application/                         # Capa de Aplicación (lógica de negocio)
│   ├── Common/
│   │   ├── DTOs/                        # Data Transfer Objects
│   │   │   ├── AuthResponseDto.cs
│   │   │   ├── RegisterDto.cs
│   │   │   ├── LoginDto.cs
│   │   │   └── ...
│   │   └── Interfaces/                  # Interfaces de servicios
│   │       ├── IEmailService.cs
│   │       └── ITenantService.cs
│   └── Features/                        # Organizado por características (CQRS)
│       ├── Auth/
│       │   ├── Commands/                # Commands (escritura)
│       │   │   ├── RegisterCommand.cs
│       │   │   ├── RegisterCommandHandler.cs
│       │   │   ├── LoginCommand.cs
│       │   │   ├── LoginCommandHandler.cs
│       │   │   └── ...
│       │   └── Queries/                 # Queries (lectura)
│       └── Users/
│           └── Commands/
│               ├── InviteUserCommand.cs
│               └── InviteUserCommandHandler.cs
│
├── Infrastructure/                      # Capa de Infraestructura (implementaciones)
│   ├── Persistence/                     # Acceso a datos
│   │   ├── MasterDbContext.cs
│   │   ├── TenantDbContext.cs
│   │   └── Migrations/
│   └── Services/                        # Implementaciones de servicios
│       ├── EmailService.cs
│       ├── TenantService.cs
│       └── AuthService.cs (legacy - migrar a handlers)
│
└── Presentation/                        # Capa de Presentación (API)
    └── Controllers/
        ├── AuthController.cs
        └── UsersController.cs
```

## Principios de Clean Architecture

### 1. **Domain Layer (Dominio)**
- **Responsabilidad**: Contiene la lógica de negocio pura
- **Dependencias**: No depende de ninguna otra capa
- **Contiene**:
  - Entidades del dominio
  - Enumeraciones
  - Value Objects (si aplica)
  - Interfaces de repositorios (si aplica)

### 2. **Application Layer (Aplicación)**
- **Responsabilidad**: Orquesta la lógica de negocio
- **Dependencias**: Solo depende de Domain
- **Contiene**:
  - Commands y Queries (CQRS)
  - Handlers de MediatR
  - DTOs
  - Interfaces de servicios
  - Validadores (FluentValidation)

### 3. **Infrastructure Layer (Infraestructura)**
- **Responsabilidad**: Implementa detalles técnicos
- **Dependencias**: Depende de Application y Domain
- **Contiene**:
  - DbContexts y Repositorios
  - Implementaciones de servicios (Email, Storage, etc.)
  - Migraciones de base de datos
  - Integraciones externas

### 4. **Presentation Layer (Presentación)**
- **Responsabilidad**: Maneja las interacciones con el mundo exterior
- **Dependencias**: Depende de Application
- **Contiene**:
  - Controllers
  - Middlewares
  - Filtros
  - Configuración de la API

## CQRS con MediatR

### ¿Qué es CQRS?

CQRS separa las operaciones de **lectura (Queries)** de las operaciones de **escritura (Commands)**.

### Commands (Escritura)

Los Commands modifican el estado del sistema.

**Ejemplo: RegisterCommand**

```csharp
// Command
public record RegisterCommand(
    string Email,
    string Password,
    string FirstName,
    string LastName,
    string TenantName,
    string Subdomain,
    bool EnableTwoFactor
) : IRequest<AuthResponseDto>;

// Handler
public class RegisterCommandHandler : IRequestHandler<RegisterCommand, AuthResponseDto>
{
    // Dependencies injected

    public async Task<AuthResponseDto> Handle(
        RegisterCommand request,
        CancellationToken cancellationToken)
    {
        // Implementation
    }
}
```

### Queries (Lectura)

Las Queries solo leen datos sin modificarlos.

**Ejemplo: GetUserByIdQuery** (cuando se implemente)

```csharp
// Query
public record GetUserByIdQuery(string UserId) : IRequest<UserDto>;

// Handler
public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDto>
{
    public async Task<UserDto> Handle(
        GetUserByIdQuery request,
        CancellationToken cancellationToken)
    {
        // Implementation
    }
}
```

### Uso en Controllers

Los controllers ahora solo envían commands/queries a través de MediatR:

```csharp
[ApiController]
[Route("api/[controller]")]
public class AuthController : ControllerBase
{
    private readonly IMediator _mediator;

    public AuthController(IMediator mediator)
    {
        _mediator = mediator;
    }

    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterDto dto)
    {
        var command = new RegisterCommand(
            dto.Email,
            dto.Password,
            dto.FirstName,
            dto.LastName,
            dto.TenantName,
            dto.Subdomain,
            dto.EnableTwoFactor
        );

        var result = await _mediator.Send(command);

        return result.Success ? Ok(result) : BadRequest(result);
    }
}
```

## Ventajas de Clean Architecture + CQRS

### Clean Architecture
1. **Independencia de Frameworks**: El dominio no conoce EF Core, ASP.NET, etc.
2. **Testeable**: Fácil de testear cada capa independientemente
3. **Mantenible**: Separación clara de responsabilidades
4. **Flexible**: Fácil cambiar implementaciones (ej: cambiar de PostgreSQL a SQL Server)

### CQRS
1. **Separación de Responsabilidades**: Read y Write están separados
2. **Escalabilidad**: Queries y Commands pueden escalar independientemente
3. **Optimización**: Queries optimizadas para lectura, Commands para escritura
4. **Single Responsibility**: Cada handler hace una sola cosa

## Flujo de una Petición

```
1. HTTP Request → Controller (Presentation)
                     ↓
2. Controller crea Command/Query
                     ↓
3. Controller envía a MediatR (_mediator.Send)
                     ↓
4. MediatR encuentra el Handler correcto (Application)
                     ↓
5. Handler ejecuta lógica de negocio
                     ↓
6. Handler usa servicios de Infrastructure
                     ↓
7. Handler retorna DTO
                     ↓
8. Controller retorna HTTP Response
```

## Cómo Agregar una Nueva Feature

### 1. Crear el Command/Query

```csharp
// Application/Features/Products/Commands/CreateProductCommand.cs
public record CreateProductCommand(
    string Name,
    decimal Price
) : IRequest<ProductDto>;
```

### 2. Crear el Handler

```csharp
// Application/Features/Products/Commands/CreateProductCommandHandler.cs
public class CreateProductCommandHandler : IRequestHandler<CreateProductCommand, ProductDto>
{
    private readonly IProductRepository _repository;

    public CreateProductCommandHandler(IProductRepository repository)
    {
        _repository = repository;
    }

    public async Task<ProductDto> Handle(
        CreateProductCommand request,
        CancellationToken cancellationToken)
    {
        var product = new Product(request.Name, request.Price);
        await _repository.AddAsync(product);
        return new ProductDto { /* ... */ };
    }
}
```

### 3. Agregar Endpoint en Controller

```csharp
// Presentation/Controllers/ProductsController.cs
[HttpPost]
public async Task<IActionResult> Create([FromBody] CreateProductDto dto)
{
    var command = new CreateProductCommand(dto.Name, dto.Price);
    var result = await _mediator.Send(command);
    return Ok(result);
}
```

## Validación con FluentValidation

Puedes agregar validadores para tus Commands/Queries:

```csharp
public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty()
            .EmailAddress();

        RuleFor(x => x.Password)
            .NotEmpty()
            .MinimumLength(6);

        RuleFor(x => x.Subdomain)
            .NotEmpty()
            .Matches("^[a-z0-9-]+$");
    }
}
```

## Migrando Código Existente

### Estado Actual
El código existente tiene:
- ✅ AuthService con lógica completa (en Infrastructure/Services)
- ✅ Controllers que llaman a AuthService directamente
- ❌ No usa MediatR
- ❌ Lógica mezclada

### Proceso de Migración

1. **Crear Command/Query** para cada operación
2. **Crear Handler** que use el servicio existente o reimplemente la lógica
3. **Actualizar Controller** para usar MediatR
4. **Mover lógica** del servicio al handler gradualmente
5. **Eliminar servicio** cuando todo esté en handlers

### Ejemplo: Login

**Antes:**
```csharp
[HttpPost("login")]
public async Task<IActionResult> Login([FromBody] LoginDto dto)
{
    var result = await _authService.LoginAsync(dto);
    return result.Success ? Ok(result) : Unauthorized(result);
}
```

**Después:**
```csharp
[HttpPost("login")]
public async Task<IActionResult> Login([FromBody] LoginDto dto)
{
    var command = new LoginCommand(dto.Email, dto.Password, dto.TwoFactorCode);
    var result = await _mediator.Send(command);
    return result.Success ? Ok(result) : Unauthorized(result);
}
```

## Configuración en Program.cs

```csharp
// Add MediatR
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// Add FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);

// Add Services
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IEmailService, EmailService>();
```

## Próximos Pasos

### Completar la Migración

1. ✅ RegisterCommandHandler implementado
2. ⏳ Implementar LoginCommandHandler
3. ⏳ Implementar ForgotPasswordCommandHandler
4. ⏳ Implementar ResetPasswordCommandHandler
5. ⏳ Implementar AcceptInvitationCommandHandler
6. ⏳ Implementar Enable2FACommandHandler
7. ⏳ Implementar Disable2FACommandHandler
8. ⏳ Implementar InviteUserCommandHandler

### Actualizar Namespaces

Actualizar todos los `using` en archivos existentes:
- `MasterBackup_API.Models` → `MasterBackup_API.Domain.Entities`
- `MasterBackup_API.Enums` → `MasterBackup_API.Domain.Enums`
- `MasterBackup_API.Data` → `MasterBackup_API.Infrastructure.Persistence`
- `MasterBackup_API.Services` → `MasterBackup_API.Infrastructure.Services`
- `MasterBackup_API.DTOs` → `MasterBackup_API.Application.Common.DTOs`

### Mejoras Adicionales

1. **Agregar Queries** para operaciones de lectura
2. **Implementar Repositorios** en lugar de usar DbContext directamente
3. **Agregar Behaviours** de MediatR para:
   - Logging
   - Validación automática
   - Manejo de transacciones
4. **Unit Tests** para cada Handler
5. **Integration Tests** para flujos completos

## Recursos

- [Clean Architecture - Robert C. Martin](https://blog.cleancoder.com/uncle-bob/2012/08/13/the-clean-architecture.html)
- [CQRS Pattern](https://martinfowler.com/bliki/CQRS.html)
- [MediatR Documentation](https://github.com/jbogard/MediatR)
- [FluentValidation](https://docs.fluentvalidation.net/)
