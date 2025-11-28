# Endpoints de Autenticación Implementados

## Resumen de Endpoints

Se han implementado 3 nuevos endpoints para soportar el flujo de login de 3 pasos:

1. **POST /api/auth/validate-email** - Validar si el email existe
2. **POST /api/auth/login** - Login con email y contraseña (actualizado)
3. **POST /api/auth/verify-2fa** - Verificar código 2FA

---

## 1. Validar Email

**Endpoint:** `POST /api/auth/validate-email`

**Descripción:** Valida si un email existe en el sistema y retorna información sobre el usuario y si tiene 2FA habilitado.

### Request Body
```json
{
  "email": "usuario@ejemplo.com",
  "subdomain": "app"
}
```

### Response Success (200 OK)
```json
{
  "exists": true,
  "twoFactorEnabled": true,
  "firstName": "Juan",
  "lastName": "Pérez"
}
```

### Response - Email no existe (200 OK)
```json
{
  "exists": false,
  "twoFactorEnabled": false,
  "firstName": null,
  "lastName": null
}
```

### Archivos Implementados
- `Application/Common/DTOs/ValidateEmailDto.cs`
- `Application/Common/DTOs/EmailValidationResponse.cs`
- `Application/Features/Auth/Commands/ValidateEmailCommand.cs`
- `Application/Features/Auth/Commands/ValidateEmailCommandHandler.cs`

---

## 2. Login (Actualizado)

**Endpoint:** `POST /api/auth/login`

**Descripción:** Login con email y contraseña. Si el usuario tiene 2FA habilitado, envía el código por email y retorna `twoFactorRequired: true`.

### Request Body
```json
{
  "email": "usuario@ejemplo.com",
  "password": "MiPassword123!",
  "subdomain": "app"
}
```

### Response - Login exitoso sin 2FA (200 OK)
```json
{
  "success": true,
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "email": "usuario@ejemplo.com",
  "firstName": "Juan",
  "lastName": "Pérez",
  "role": "Admin",
  "tenantId": "660e8400-e29b-41d4-a716-446655440000",
  "twoFactorRequired": false,
  "user": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "email": "usuario@ejemplo.com",
    "firstName": "Juan",
    "lastName": "Pérez",
    "role": "Admin"
  }
}
```

### Response - Requiere 2FA (200 OK)
```json
{
  "twoFactorRequired": true,
  "message": "2FA code sent to your email"
}
```

### Response - Error (401 Unauthorized)
```json
{
  "message": "Invalid credentials"
}
```

### Cambios Realizados
- Agregado campo `subdomain` a `LoginDto`
- Actualizado `LoginCommand` para recibir el DTO completo
- Modificado `LoginCommandHandler` para usar subdomain en lugar de buscar en todos los tenants
- Agregados campos adicionales a `AuthResponseDto` para compatibilidad con el frontend

---

## 3. Verificar Código 2FA

**Endpoint:** `POST /api/auth/verify-2fa`

**Descripción:** Verifica el código 2FA enviado al email del usuario y completa el proceso de login.

### Request Body
```json
{
  "email": "usuario@ejemplo.com",
  "password": "MiPassword123!",
  "twoFactorCode": "123456",
  "subdomain": "app"
}
```

### Response Success (200 OK)
```json
{
  "success": true,
  "token": "eyJhbGciOiJIUzI1NiIsInR5cCI6IkpXVCJ9...",
  "userId": "550e8400-e29b-41d4-a716-446655440000",
  "email": "usuario@ejemplo.com",
  "firstName": "Juan",
  "lastName": "Pérez",
  "role": "Admin",
  "tenantId": "660e8400-e29b-41d4-a716-446655440000",
  "twoFactorRequired": false,
  "user": {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "email": "usuario@ejemplo.com",
    "firstName": "Juan",
    "lastName": "Pérez",
    "role": "Admin"
  }
}
```

### Response - Error (401 Unauthorized)
```json
{
  "message": "Invalid or expired 2FA code"
}
```

### Archivos Implementados
- `Application/Common/DTOs/Verify2FADto.cs`
- `Application/Features/Auth/Commands/Verify2FACommand.cs`
- `Application/Features/Auth/Commands/Verify2FACommandHandler.cs`

---

## Flujo Completo de Login

### Caso 1: Usuario sin 2FA

1. **Frontend:** POST `/api/auth/validate-email`
   - Request: `{ email, subdomain }`
   - Response: `{ exists: true, twoFactorEnabled: false, ... }`

2. **Frontend:** POST `/api/auth/login`
   - Request: `{ email, password, subdomain }`
   - Response: `{ success: true, token: "...", ... }`

3. **Frontend:** Guarda token y redirige al dashboard

### Caso 2: Usuario con 2FA

1. **Frontend:** POST `/api/auth/validate-email`
   - Request: `{ email, subdomain }`
   - Response: `{ exists: true, twoFactorEnabled: true, ... }`

2. **Frontend:** POST `/api/auth/login`
   - Request: `{ email, password, subdomain }`
   - Response: `{ twoFactorRequired: true, message: "2FA code sent..." }`
   - **Backend:** Genera código de 6 dígitos y lo envía por email

3. **Frontend:** Usuario ingresa código recibido por email

4. **Frontend:** POST `/api/auth/verify-2fa`
   - Request: `{ email, password, twoFactorCode: "123456", subdomain }`
   - Response: `{ success: true, token: "...", ... }`

5. **Frontend:** Guarda token y redirige al dashboard

---

## Seguridad Implementada

✅ **Validación de contraseña:** Se verifica en cada paso que requiera autenticación  
✅ **Código 2FA temporal:** Expira en 10 minutos  
✅ **Código de un solo uso:** Se elimina después de ser usado  
✅ **Logging:** Todos los intentos de login se registran  
✅ **Aislamiento de tenants:** Cada tenant tiene su propia base de datos  
✅ **JWT seguro:** Token con claims de usuario, rol y tenant  

---

## Estructura de Archivos Creados

```
MasterBackup-API/
├── Application/
│   ├── Common/
│   │   └── DTOs/
│   │       ├── ValidateEmailDto.cs (NUEVO)
│   │       ├── Verify2FADto.cs (NUEVO)
│   │       ├── EmailValidationResponse.cs (NUEVO)
│   │       ├── LoginDto.cs (ACTUALIZADO - agregado subdomain)
│   │       └── AuthResponseDto.cs (ACTUALIZADO - campos adicionales)
│   └── Features/
│       └── Auth/
│           └── Commands/
│               ├── ValidateEmailCommand.cs (NUEVO)
│               ├── ValidateEmailCommandHandler.cs (NUEVO)
│               ├── Verify2FACommand.cs (NUEVO)
│               ├── Verify2FACommandHandler.cs (NUEVO)
│               ├── LoginCommand.cs (ACTUALIZADO)
│               └── LoginCommandHandler.cs (ACTUALIZADO)
└── Presentation/
    └── Controllers/
        └── AuthController.cs (ACTUALIZADO - 2 endpoints nuevos)
```

---

## Testing con Swagger

Una vez que el servidor esté corriendo, puedes probar los endpoints en:

```
https://localhost:7001/swagger
```

### Ejemplo de prueba:

1. Ir a Swagger UI
2. Expandir `POST /api/auth/validate-email`
3. Click en "Try it out"
4. Ingresar datos:
```json
{
  "email": "test@example.com",
  "subdomain": "app"
}
```
5. Click "Execute"
6. Ver respuesta

---

## Próximos Pasos

Para usar completamente el sistema:

1. ✅ Endpoints implementados
2. ⏳ Ejecutar backend: `cd MasterBackup-API && dotnet run`
3. ⏳ Ejecutar frontend: `cd MasterBackup-App && ng serve`
4. ⏳ Crear un usuario de prueba con 2FA habilitado
5. ⏳ Probar el flujo completo de login

---

## Notas Importantes

- **Subdomain:** Es obligatorio en todos los endpoints de autenticación
- **Tenant isolation:** Cada tenant tiene su propia base de datos
- **Email service:** Debe estar configurado Maileroo para enviar códigos 2FA
- **JWT expiration:** Los tokens expiran en 7 días
- **2FA code expiration:** Los códigos 2FA expiran en 10 minutos

---

## Soporte

Para más información sobre la arquitectura:
- Ver `CLEAN_ARCHITECTURE.md`
- Ver `COPILOT_INSTRUCTIONS.md`
- Ver `/swagger` para documentación interactiva de la API
