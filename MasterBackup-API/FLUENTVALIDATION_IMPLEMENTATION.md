# FluentValidation Implementation

## Resumen

Se ha implementado FluentValidation para todos los endpoints de la API que reciben DTOs, reemplazando la validación básica de DataAnnotations y ModelState por un sistema más robusto y mantenible.

## Validators Implementados

### 1. RegisterDtoValidator
**Ubicación:** `Application/Common/Validators/RegisterDtoValidator.cs`

**Validaciones:**
- **Email**: Requerido, formato válido, máximo 255 caracteres
- **Password**: Requerido, 8-100 caracteres, debe contener:
  - Al menos una mayúscula
  - Al menos una minúscula
  - Al menos un dígito
- **FirstName**: Requerido, máximo 50 caracteres
- **LastName**: Requerido, máximo 50 caracteres
- **TenantName**: Requerido, máximo 100 caracteres

### 2. LoginDtoValidator
**Ubicación:** `Application/Common/Validators/LoginDtoValidator.cs`

**Validaciones:**
- **Email**: Requerido, formato válido
- **Password**: Requerido
- **TwoFactorCode**: Opcional, si se proporciona debe tener exactamente 6 dígitos

### 3. ValidateEmailDtoValidator
**Ubicación:** `Application/Common/Validators/ValidateEmailDtoValidator.cs`

**Validaciones:**
- **Email**: Requerido, formato válido, máximo 255 caracteres

### 4. Verify2FADtoValidator
**Ubicación:** `Application/Common/Validators/Verify2FADtoValidator.cs`

**Validaciones:**
- **Email**: Requerido, formato válido, máximo 255 caracteres
- **Password**: Requerido
- **TwoFactorCode**: Requerido, exactamente 6 dígitos

### 5. ForgotPasswordDtoValidator
**Ubicación:** `Application/Common/Validators/ForgotPasswordDtoValidator.cs`

**Validaciones:**
- **Email**: Requerido, formato válido, máximo 255 caracteres

### 6. ResetPasswordDtoValidator
**Ubicación:** `Application/Common/Validators/ResetPasswordDtoValidator.cs`

**Validaciones:**
- **Email**: Requerido, formato válido, máximo 255 caracteres
- **Token**: Requerido
- **NewPassword**: Requerido, 8-100 caracteres, debe contener:
  - Al menos una mayúscula
  - Al menos una minúscula
  - Al menos un dígito

### 7. AcceptInvitationDtoValidator
**Ubicación:** `Application/Common/Validators/AcceptInvitationDtoValidator.cs`

**Validaciones:**
- **Token**: Requerido
- **FirstName**: Requerido, máximo 50 caracteres
- **LastName**: Requerido, máximo 50 caracteres
- **Password**: Requerido, 8-100 caracteres, debe contener:
  - Al menos una mayúscula
  - Al menos una minúscula
  - Al menos un dígito

### 8. InviteUserDtoValidator
**Ubicación:** `Application/Common/Validators/InviteUserDtoValidator.cs`

**Validaciones:**
- **Email**: Requerido, formato válido, máximo 255 caracteres
- **Role**: Requerido, debe ser un valor válido del enum UserRole

## Configuración

### Program.cs

Se registraron todos los validators automáticamente mediante assembly scanning:

```csharp
using FluentValidation;

// ...

builder.Services.AddValidatorsFromAssemblyContaining<Program>();
```

Esto registra automáticamente todos los validators que heredan de `AbstractValidator<T>` en el contenedor de DI.

## Uso en Controllers

### AuthController

Se inyectaron todos los validators necesarios en el constructor:

```csharp
private readonly IValidator<RegisterDto> _registerValidator;
private readonly IValidator<LoginDto> _loginValidator;
private readonly IValidator<ValidateEmailDto> _validateEmailValidator;
private readonly IValidator<Verify2FADto> _verify2FAValidator;
private readonly IValidator<ForgotPasswordDto> _forgotPasswordValidator;
private readonly IValidator<ResetPasswordDto> _resetPasswordValidator;
private readonly IValidator<AcceptInvitationDto> _acceptInvitationValidator;
private readonly IValidator<InviteUserDto> _inviteUserValidator;
```

### Patrón de Validación

Todos los endpoints siguen el mismo patrón:

```csharp
[HttpPost("endpoint")]
public async Task<IActionResult> EndpointName([FromBody] DtoType dto)
{
    var validationResult = await _validator.ValidateAsync(dto);
    if (!validationResult.IsValid)
    {
        return BadRequest(new { 
            errors = validationResult.Errors.Select(e => new { 
                field = e.PropertyName, 
                message = e.ErrorMessage 
            }) 
        });
    }

    // Lógica del endpoint...
}
```

### Formato de Errores

Las validaciones retornan errores en el siguiente formato JSON:

```json
{
  "errors": [
    {
      "field": "Email",
      "message": "Email must be a valid email address"
    },
    {
      "field": "Password",
      "message": "Password must contain at least one uppercase letter"
    }
  ]
}
```

Este formato estructurado facilita el manejo de errores en el frontend.

## Endpoints Actualizados

### AuthController
1. ✅ `POST /api/auth/register` - RegisterDtoValidator
2. ✅ `POST /api/auth/validate-email` - ValidateEmailDtoValidator
3. ✅ `POST /api/auth/login` - LoginDtoValidator
4. ✅ `POST /api/auth/verify-2fa` - Verify2FADtoValidator
5. ✅ `POST /api/auth/forgot-password` - ForgotPasswordDtoValidator
6. ✅ `POST /api/auth/reset-password` - ResetPasswordDtoValidator
7. ✅ `POST /api/auth/accept-invitation` - AcceptInvitationDtoValidator

### UsersController
1. ✅ `POST /api/users/invite` - InviteUserDtoValidator

## Beneficios

1. **Separación de Responsabilidades**: La lógica de validación está separada de los DTOs y controllers
2. **Reutilización**: Los validators pueden ser reutilizados en diferentes contextos
3. **Testabilidad**: Los validators pueden ser probados de forma independiente
4. **Mensajes Claros**: Mensajes de error descriptivos y personalizables
5. **Validación Compleja**: Soporte para reglas de validación avanzadas
6. **Formato Consistente**: Todos los endpoints retornan errores en el mismo formato estructurado

## Estado Actual

✅ **Implementación Completa**
- Todos los validators creados
- Todos los endpoints actualizados
- FluentValidation registrado en DI
- Compilación exitosa sin errores
- Formato de errores estandarizado

## Siguientes Pasos Recomendados

1. **Pruebas Unitarias**: Crear tests para cada validator
2. **Validaciones Personalizadas**: Agregar validaciones más específicas según necesidades de negocio
3. **Frontend Integration**: Actualizar el frontend para manejar el nuevo formato de errores
4. **Documentación API**: Actualizar Swagger/OpenAPI con los nuevos formatos de error

## Referencias

- [FluentValidation Documentation](https://docs.fluentvalidation.net/)
- [ASP.NET Core Integration](https://docs.fluentvalidation.net/en/latest/aspnet.html)
