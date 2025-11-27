# Guía de Migración a Clean Architecture + CQRS

Esta guía te ayudará a completar la migración del proyecto a Clean Architecture con CQRS.

## Estado Actual de la Migración

### ✅ Completado

1. **Estructura de Carpetas**
   - ✅ Domain/ (Entities, Enums)
   - ✅ Application/ (Commands, Queries, DTOs, Interfaces)
   - ✅ Infrastructure/ (Services, Persistence)
   - ✅ Presentation/ (Controllers)

2. **Paquetes Instalados**
   - ✅ MediatR 12.2.0
   - ✅ MediatR.Extensions.Microsoft.DependencyInjection 11.1.0
   - ✅ FluentValidation 11.9.0
   - ✅ FluentValidation.DependencyInjectionExtensions 11.9.0

3. **Archivos Movidos**
   - ✅ Models → Domain/Entities
   - ✅ Enums → Domain/Enums
   - ✅ DTOs → Application/Common/DTOs
   - ✅ Services → Infrastructure/Services
   - ✅ Data → Infrastructure/Persistence
   - ✅ Controllers → Presentation/Controllers

4. **Archivos Creados**
   - ✅ Commands para Auth (Register, Login, ForgotPassword, etc.)
   - ✅ RegisterCommandHandler (ejemplo completo)
   - ✅ AuthControllerRefactored (ejemplo con MediatR)
   - ✅ Interfaces en Application/Common

### ⏳ Pendiente

1. **Actualizar Namespaces** en todos los archivos existentes
2. **Completar Handlers** para todos los Commands
3. **Actualizar Program.cs** para registrar MediatR
4. **Actualizar Controllers** existentes
5. **Crear Validadores** con FluentValidation
6. **Agregar Queries** para operaciones de lectura

## Pasos de Migración

### Paso 1: Actualizar Namespaces

Necesitas actualizar los `using` en todos los archivos que fueron movidos.

#### 1.1 Actualizar Infrastructure/Persistence/MasterDbContext.cs

```csharp
// Cambiar
using MasterBackup_API.Models;

// Por
using MasterBackup_API.Domain.Entities;
```

#### 1.2 Actualizar Infrastructure/Persistence/TenantDbContext.cs

```csharp
// Cambiar
using MasterBackup_API.Models;

// Por
using MasterBackup_API.Domain.Entities;
```

#### 1.3 Actualizar Infrastructure/Services/EmailService.cs

```csharp
// Ya debería estar correcto, verificar namespace es:
namespace MasterBackup_API.Infrastructure.Services;
```

#### 1.4 Actualizar Infrastructure/Services/TenantService.cs

```csharp
// Cambiar
using MasterBackup_API.Data;
using MasterBackup_API.Models;

// Por
using MasterBackup_API.Infrastructure.Persistence;
using MasterBackup_API.Domain.Entities;

// Cambiar namespace
namespace MasterBackup_API.Infrastructure.Services;
```

#### 1.5 Actualizar Infrastructure/Services/AuthService.cs

```csharp
// Cambiar
using MasterBackup_API.Data;
using MasterBackup_API.DTOs;
using MasterBackup_API.Models;

// Por
using MasterBackup_API.Infrastructure.Persistence;
using MasterBackup_API.Application.Common.DTOs;
using MasterBackup_API.Domain.Entities;

// Cambiar namespace
namespace MasterBackup_API.Infrastructure.Services;
```

#### 1.6 Actualizar Application/Common/DTOs/*.cs

Actualizar el namespace en todos los DTOs:

```csharp
// En cada archivo DTO, cambiar
namespace MasterBackup_API.DTOs;

// Por
namespace MasterBackup_API.Application.Common.DTOs;
```

Archivos a actualizar:
- RegisterDto.cs
- LoginDto.cs
- ForgotPasswordDto.cs
- ResetPasswordDto.cs
- InviteUserDto.cs
- AcceptInvitationDto.cs

#### 1.7 Actualizar Presentation/Controllers/*.cs

```csharp
// Cambiar
using MasterBackup_API.DTOs;
using MasterBackup_API.Enums;
using MasterBackup_API.Services;

// Por
using MasterBackup_API.Application.Common.DTOs;
using MasterBackup_API.Domain.Enums;
using MasterBackup_API.Infrastructure.Services;

// Cambiar namespace
namespace MasterBackup_API.Presentation.Controllers;
```

#### 1.8 Actualizar Middleware/TenantMiddleware.cs

```csharp
// Cambiar namespace
namespace MasterBackup_API.Middleware;

// Por (o mover a Infrastructure)
namespace MasterBackup_API.Infrastructure.Middleware;
```

#### 1.9 Actualizar Middleware/RoleAuthorizationAttribute.cs

```csharp
// Cambiar
using MasterBackup_API.Enums;

// Por
using MasterBackup_API.Domain.Enums;

// Cambiar namespace
namespace MasterBackup_API.Infrastructure.Middleware;
```

### Paso 2: Completar Handlers

Crea los handlers faltantes basándote en `RegisterCommandHandler.cs`.

#### 2.1 LoginCommandHandler

```csharp
// Application/Features/Auth/Commands/LoginCommandHandler.cs
public class LoginCommandHandler : IRequestHandler<LoginCommand, AuthResponseDto>
{
    private readonly MasterDbContext _masterContext;
    private readonly ITenantService _tenantService;
    private readonly IEmailService _emailService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<LoginCommandHandler> _logger;

    // Constructor...

    public async Task<AuthResponseDto> Handle(
        LoginCommand request,
        CancellationToken cancellationToken)
    {
        // Copia la lógica de AuthService.LoginAsync
        // y adapta para usar request.Email, request.Password, request.TwoFactorCode
    }
}
```

#### 2.2 Otros Handlers

Crea de manera similar:
- `ForgotPasswordCommandHandler`
- `ResetPasswordCommandHandler`
- `AcceptInvitationCommandHandler`
- `Enable2FACommandHandler`
- `Disable2FACommandHandler`
- `InviteUserCommandHandler`

### Paso 3: Actualizar Program.cs

#### 3.1 Agregar MediatR

```csharp
// Agregar después de los services existentes
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));

// Opcional: Agregar FluentValidation
builder.Services.AddValidatorsFromAssembly(typeof(Program).Assembly);
```

#### 3.2 Actualizar Using Statements

```csharp
using MasterBackup_API.Infrastructure.Persistence;
using MasterBackup_API.Infrastructure.Middleware;
using MasterBackup_API.Domain.Entities;
using MasterBackup_API.Infrastructure.Services;
using MediatR;
```

### Paso 4: Actualizar Controllers

Puedes usar dos enfoques:

#### Opción A: Gradual (Recomendado)

1. Mantén los controllers existentes funcionando
2. Crea nuevos endpoints en `/api/v2/` con MediatR
3. Una vez probado, elimina los antiguos

#### Opción B: Todo de una vez

1. Actualiza todos los controllers para usar MediatR
2. Usa `AuthControllerRefactored.cs` como referencia
3. Compila y prueba

**Ejemplo de Actualización:**

```csharp
// ANTES
[HttpPost("register")]
public async Task<IActionResult> Register([FromBody] RegisterDto registerDto)
{
    var result = await _authService.RegisterAsync(registerDto);
    return result.Success ? Ok(result) : BadRequest(result);
}

// DESPUÉS
[HttpPost("register")]
public async Task<IActionResult> Register([FromBody] RegisterDto dto)
{
    var command = new RegisterCommand(
        dto.Email, dto.Password, dto.FirstName, dto.LastName,
        dto.TenantName, dto.Subdomain, dto.EnableTwoFactor
    );
    var result = await _mediator.Send(command);
    return result.Success ? Ok(result) : BadRequest(result);
}
```

### Paso 5: Agregar Validadores (Opcional)

Crea validadores para cada Command/Query:

```csharp
// Application/Features/Auth/Commands/RegisterCommandValidator.cs
public class RegisterCommandValidator : AbstractValidator<RegisterCommand>
{
    public RegisterCommandValidator()
    {
        RuleFor(x => x.Email)
            .NotEmpty().WithMessage("Email is required")
            .EmailAddress().WithMessage("Email is invalid");

        RuleFor(x => x.Password)
            .NotEmpty().WithMessage("Password is required")
            .MinimumLength(6).WithMessage("Password must be at least 6 characters");

        RuleFor(x => x.FirstName)
            .NotEmpty().WithMessage("First name is required");

        RuleFor(x => x.LastName)
            .NotEmpty().WithMessage("Last name is required");

        RuleFor(x => x.Subdomain)
            .NotEmpty().WithMessage("Subdomain is required")
            .Matches("^[a-z0-9-]+$").WithMessage("Subdomain must be lowercase alphanumeric");
    }
}
```

#### Configurar Validación Automática

```csharp
// Application/Common/Behaviours/ValidationBehaviour.cs
public class ValidationBehaviour<TRequest, TResponse>
    : IPipelineBehavior<TRequest, TResponse>
    where TRequest : IRequest<TResponse>
{
    private readonly IEnumerable<IValidator<TRequest>> _validators;

    public ValidationBehaviour(IEnumerable<IValidator<TRequest>> validators)
    {
        _validators = validators;
    }

    public async Task<TResponse> Handle(
        TRequest request,
        RequestHandlerDelegate<TResponse> next,
        CancellationToken cancellationToken)
    {
        if (_validators.Any())
        {
            var context = new ValidationContext<TRequest>(request);

            var validationResults = await Task.WhenAll(
                _validators.Select(v => v.ValidateAsync(context, cancellationToken)));

            var failures = validationResults
                .SelectMany(r => r.Errors)
                .Where(f => f != null)
                .ToList();

            if (failures.Count != 0)
            {
                throw new ValidationException(failures);
            }
        }

        return await next();
    }
}
```

Registrar en Program.cs:

```csharp
builder.Services.AddTransient(
    typeof(IPipelineBehavior<,>),
    typeof(ValidationBehaviour<,>));
```

### Paso 6: Testing

#### 6.1 Compilar

```bash
cd MasterBackup-API/MasterBackup-API
dotnet build
```

#### 6.2 Resolver Errores de Compilación

Los errores comunes serán:
- Namespaces incorrectos
- Archivos que no encuentran las clases (verificar using statements)
- Tipos que ya no existen en el namespace antiguo

#### 6.3 Probar Endpoints

1. Ejecuta la aplicación
```bash
dotnet run
```

2. Prueba con Swagger en `https://localhost:7001/swagger`

3. Prueba los nuevos endpoints en `/api/v2/auth` si usaste el approach gradual

### Paso 7: Agregar Queries (Futuro)

Cuando necesites operaciones de lectura optimizadas:

```csharp
// Application/Features/Users/Queries/GetUserByIdQuery.cs
public record GetUserByIdQuery(string UserId) : IRequest<UserDto>;

// Handler
public class GetUserByIdQueryHandler : IRequestHandler<GetUserByIdQuery, UserDto>
{
    private readonly TenantDbContext _context;

    public GetUserByIdQueryHandler(TenantDbContext context)
    {
        _context = context;
    }

    public async Task<UserDto> Handle(
        GetUserByIdQuery request,
        CancellationToken cancellationToken)
    {
        var user = await _context.Users
            .Where(u => u.Id == request.UserId)
            .Select(u => new UserDto
            {
                Id = u.Id,
                Email = u.Email!,
                FirstName = u.FirstName,
                LastName = u.LastName,
                Role = u.Role.ToString()
            })
            .FirstOrDefaultAsync(cancellationToken);

        return user!;
    }
}
```

## Comandos Útiles

### Buscar y Reemplazar Namespaces

```bash
# En Windows PowerShell
Get-ChildItem -Recurse -Include *.cs | ForEach-Object {
    (Get-Content $_.FullName) -replace 'using MasterBackup_API.Models;', 'using MasterBackup_API.Domain.Entities;' |
    Set-Content $_.FullName
}
```

### Verificar Compilación

```bash
dotnet build --no-incremental
```

### Ejecutar Migraciones

```bash
dotnet ef migrations add MigrationName --context MasterDbContext
dotnet ef database update --context MasterDbContext
```

## Troubleshooting

### Error: "Type X could not be found"

**Solución**: Verifica que el namespace del `using` statement sea correcto.

### Error: "MediatR handler not found"

**Solución**: Asegúrate de que Program.cs tenga:
```csharp
builder.Services.AddMediatR(cfg =>
    cfg.RegisterServicesFromAssembly(typeof(Program).Assembly));
```

### Error: "Cannot resolve service"

**Solución**: Verifica que todos los servicios estén registrados en Program.cs:
```csharp
builder.Services.AddScoped<ITenantService, TenantService>();
builder.Services.AddScoped<IEmailService, EmailService>();
```

### Error de Namespace en Middleware

**Solución**: Actualiza Program.cs:
```csharp
// Cambiar
using MasterBackup_API.Middleware;

// Por
using MasterBackup_API.Infrastructure.Middleware;
```

## Checklist Final

- [ ] Todos los namespaces actualizados
- [ ] Todos los handlers creados
- [ ] Program.cs configurado con MediatR
- [ ] Controllers actualizados
- [ ] Proyecto compila sin errores
- [ ] Migraciones de base de datos funcionan
- [ ] Tests manuales en Swagger pasan
- [ ] Validadores agregados (opcional)
- [ ] Documentation actualizada

## Próximos Pasos

Una vez completada la migración básica:

1. **Implementar Repositorios** para abstraer el acceso a datos
2. **Agregar Unit Tests** para handlers
3. **Implementar Logging Behaviour** en MediatR pipeline
4. **Agregar Caching** para queries frecuentes
5. **Implementar Event Sourcing** si es necesario

## Recursos Adicionales

- `CLEAN_ARCHITECTURE.md` - Documentación completa de la arquitectura
- `README.md` - Documentación general del proyecto
- `AuthControllerRefactored.cs` - Ejemplo completo de controller con MediatR
- `RegisterCommandHandler.cs` - Ejemplo completo de handler
