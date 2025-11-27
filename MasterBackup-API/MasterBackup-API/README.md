# MasterBackup API - Multi-Tenant SaaS

API REST para SaaS con **Clean Architecture + CQRS** y arquitectura multi-tenant usando **database-per-tenant** (una base de datos por cada tenant), construida con .NET 8, PostgreSQL, JWT y Maileroo.

## 🏗️ Arquitectura

Este proyecto implementa **Clean Architecture** con el patrón **CQRS** (Command Query Responsibility Segregation) usando **MediatR**.

📖 **Documentación completa de arquitectura:** [CLEAN_ARCHITECTURE.md](CLEAN_ARCHITECTURE.md)
📋 **Guía de migración:** [MIGRATION_GUIDE.md](MIGRATION_GUIDE.md)

## Características

- **✅ Clean Architecture**: Separación en capas (Domain, Application, Infrastructure, Presentation)
- **✅ CQRS con MediatR**: Commands para escritura, Queries para lectura
- **✅ Multi-Tenant Database-per-Tenant**: Cada tenant tiene su propia base de datos PostgreSQL
- **✅ Autenticación JWT**: Sistema de autenticación seguro basado en tokens
- **✅ Autenticación de Dos Factores (2FA)**: Código numérico de 6 dígitos enviado por email
- **✅ Gestión de Usuarios y Roles**: Roles Admin y User con permisos diferenciados
- **✅ Sistema de Invitaciones**: Los administradores pueden invitar usuarios al tenant
- **✅ Recuperación de Contraseña**: Sistema de reset de contraseña por email
- **✅ Envío de Emails con Maileroo**: Integración completa para notificaciones
- **✅ Documentación Swagger**: API documentada y testeable
- **✅ FluentValidation**: Validación de Commands/Queries

## Tecnologías

- .NET 8
- PostgreSQL
- Entity Framework Core 8
- ASP.NET Core Identity
- JWT Bearer Authentication
- MediatR 12.2.0 (CQRS)
- FluentValidation 11.9.0
- Maileroo (servicio de email)
- Swagger/OpenAPI

## Estructura del Proyecto (Clean Architecture)

```
MasterBackup-API/
├── Domain/                              # 🏛️ Capa de Dominio
│   ├── Entities/                        # Entidades del dominio
│   │   ├── ApplicationUser.cs
│   │   ├── Tenant.cs
│   │   └── UserInvitation.cs
│   └── Enums/
│       └── UserRole.cs
│
├── Application/                         # 💼 Capa de Aplicación (CQRS)
│   ├── Common/
│   │   ├── DTOs/                        # Data Transfer Objects
│   │   └── Interfaces/                  # Interfaces de servicios
│   └── Features/                        # Organizado por características
│       ├── Auth/
│       │   ├── Commands/                # Commands (escritura)
│       │   │   ├── RegisterCommand.cs
│       │   │   ├── RegisterCommandHandler.cs
│       │   │   ├── LoginCommand.cs
│       │   │   └── ...
│       │   └── Queries/                 # Queries (lectura)
│       └── Users/
│           └── Commands/
│
├── Infrastructure/                      # 🔧 Capa de Infraestructura
│   ├── Persistence/                     # Acceso a datos
│   │   ├── MasterDbContext.cs
│   │   ├── TenantDbContext.cs
│   │   └── Migrations/
│   ├── Services/                        # Implementaciones
│   │   ├── EmailService.cs
│   │   ├── TenantService.cs
│   │   └── AuthService.cs
│   └── Middleware/
│
└── Presentation/                        # 🌐 Capa de Presentación
    └── Controllers/
        ├── AuthController.cs
        ├── AuthControllerRefactored.cs  # Ejemplo con MediatR
        └── UsersController.cs
    ├── EmailService.cs        # Servicio de email con Maileroo
    └── TenantService.cs       # Gestión de tenants y bases de datos
```

## Configuración

### 1. Prerrequisitos

- .NET 8 SDK
- PostgreSQL 12 o superior
- Cuenta de Maileroo (para envío de emails)

### 2. Configurar Base de Datos

Crea la base de datos master en PostgreSQL:

```sql
CREATE DATABASE master_saas;
```

### 3. Configurar appsettings.json

Edita el archivo `appsettings.json`:

```json
{
  "ConnectionStrings": {
    "MasterDatabase": "Host=localhost;Database=master_saas;Username=postgres;Password=TU_PASSWORD"
  },
  "Jwt": {
    "Key": "TU_CLAVE_SECRETA_MINIMO_32_CARACTERES",
    "Issuer": "MasterBackupAPI",
    "Audience": "MasterBackupClient"
  },
  "Maileroo": {
    "ApiKey": "TU_API_KEY_DE_MAILEROO",
    "FromEmail": "noreply@tudominio.com",
    "FromName": "MasterBackup"
  },
  "AppUrl": "http://localhost:3000"
}
```

### 4. Ejecutar Migraciones

Las migraciones se ejecutan automáticamente al iniciar la aplicación. La base de datos master se crea automáticamente. Las bases de datos de tenants se crean dinámicamente al registrar nuevos tenants.

### 5. Ejecutar la Aplicación

```bash
cd MasterBackup-API
dotnet run
```

La API estará disponible en:
- HTTPS: https://localhost:7001
- HTTP: http://localhost:5000
- Swagger: https://localhost:7001/swagger

## Endpoints de la API

### Autenticación

#### POST /api/auth/register
Registrar un nuevo tenant y usuario administrador.

**Request Body:**
```json
{
  "email": "admin@ejemplo.com",
  "password": "Password123",
  "firstName": "Juan",
  "lastName": "Pérez",
  "tenantName": "Mi Empresa",
  "subdomain": "miempresa",
  "enableTwoFactor": false
}
```

**Response:**
```json
{
  "success": true,
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "user": {
    "id": "user-id",
    "email": "admin@ejemplo.com",
    "firstName": "Juan",
    "lastName": "Pérez",
    "role": "Admin"
  }
}
```

#### POST /api/auth/login
Iniciar sesión.

**Request Body:**
```json
{
  "email": "admin@ejemplo.com",
  "password": "Password123",
  "twoFactorCode": "123456"
}
```

**Response (sin 2FA):**
```json
{
  "success": true,
  "token": "eyJhbGciOiJIUzI1NiIs...",
  "user": {
    "id": "user-id",
    "email": "admin@ejemplo.com",
    "firstName": "Juan",
    "lastName": "Pérez",
    "role": "Admin"
  }
}
```

**Response (requiere 2FA):**
```json
{
  "success": false,
  "requiresTwoFactor": true,
  "message": "2FA code sent to your email"
}
```

#### POST /api/auth/forgot-password
Solicitar recuperación de contraseña.

**Request Body:**
```json
{
  "email": "admin@ejemplo.com"
}
```

#### POST /api/auth/reset-password
Restablecer contraseña con token.

**Request Body:**
```json
{
  "email": "admin@ejemplo.com",
  "token": "reset-token-from-email",
  "newPassword": "NewPassword123"
}
```

#### POST /api/auth/enable-2fa
Habilitar autenticación de dos factores (requiere autenticación).

**Headers:**
```
Authorization: Bearer {token}
```

#### POST /api/auth/disable-2fa
Deshabilitar autenticación de dos factores (requiere autenticación).

**Headers:**
```
Authorization: Bearer {token}
```

#### GET /api/auth/me
Obtener información del usuario actual (requiere autenticación).

**Headers:**
```
Authorization: Bearer {token}
```

### Gestión de Usuarios

#### POST /api/users/invite
Invitar un nuevo usuario al tenant (solo Admin).

**Headers:**
```
Authorization: Bearer {token}
```

**Request Body:**
```json
{
  "email": "usuario@ejemplo.com",
  "role": "User"
}
```

#### POST /api/auth/accept-invitation
Aceptar invitación y crear cuenta.

**Request Body:**
```json
{
  "token": "invitation-token-from-email",
  "password": "Password123",
  "firstName": "María",
  "lastName": "García",
  "enableTwoFactor": false
}
```

## Flujos de Uso

### 1. Registro de Nuevo Tenant

1. Usuario se registra con `POST /api/auth/register`
2. Sistema crea:
   - Registro del tenant en la base de datos master
   - Nueva base de datos PostgreSQL para el tenant
   - Usuario administrador en la base de datos del tenant
3. Sistema envía email de bienvenida
4. Usuario recibe token JWT para autenticación

### 2. Login con 2FA

1. Usuario hace login con `POST /api/auth/login` (sin twoFactorCode)
2. Si tiene 2FA habilitado, recibe código por email
3. Usuario hace login nuevamente con `POST /api/auth/login` (con twoFactorCode)
4. Usuario recibe token JWT

### 3. Invitar Usuario

1. Admin invita usuario con `POST /api/users/invite`
2. Sistema crea invitación y envía email con token
3. Usuario invitado acepta con `POST /api/auth/accept-invitation`
4. Sistema crea usuario y envía email de bienvenida
5. Usuario recibe token JWT

### 4. Recuperación de Contraseña

1. Usuario solicita reset con `POST /api/auth/forgot-password`
2. Sistema envía email con token de recuperación
3. Usuario resetea contraseña con `POST /api/auth/reset-password`

## Seguridad

### Roles y Permisos

- **Admin**: Puede invitar usuarios, gestionar el tenant
- **User**: Acceso básico a funcionalidades del tenant

### Autenticación JWT

Todos los endpoints protegidos requieren:
```
Authorization: Bearer {token}
```

El token JWT contiene:
- ID del usuario
- Email
- Rol
- ID del tenant
- Expiración (7 días)

### Validación de Contraseñas

Requisitos mínimos:
- Longitud mínima: 6 caracteres
- Al menos 1 dígito
- Al menos 1 mayúscula
- Al menos 1 minúscula

### 2FA (Autenticación de Dos Factores)

- Código numérico de 6 dígitos
- Válido por 10 minutos
- Enviado por email vía Maileroo

## Arquitectura Multi-Tenant

### Base de Datos Master

Contiene:
- Tabla `Tenants`: Información de todos los tenants
  - Id, Name, Subdomain, ConnectionString, IsActive, etc.

### Bases de Datos de Tenants

Cada tenant tiene su propia base de datos que contiene:
- Tablas de ASP.NET Identity (Users, Roles, etc.)
- Tabla `UserInvitations`
- Datos específicos del tenant

### Creación Dinámica de Bases de Datos

Cuando se registra un nuevo tenant:
1. Se crea una nueva base de datos PostgreSQL
2. Se ejecutan las migraciones automáticamente
3. Se guarda la cadena de conexión en la tabla Tenants
4. Se crea el usuario administrador en la nueva base de datos

### Resolución de Tenant

El tenant se identifica mediante el claim `TenantId` en el token JWT.

## Maileroo - Configuración de Email

### Obtener API Key

1. Crea una cuenta en [Maileroo](https://maileroo.com)
2. Obtén tu API Key del dashboard
3. Configura tu dominio de envío
4. Agrega la API Key en `appsettings.json`

### Emails Enviados

La API envía los siguientes emails:

1. **Código 2FA**: Código de 6 dígitos para autenticación
2. **Reset de Contraseña**: Link con token para restablecer contraseña
3. **Invitación**: Link con token para aceptar invitación
4. **Bienvenida**: Email de bienvenida al registrarse o aceptar invitación

## Testing con Swagger

1. Abre https://localhost:7001/swagger
2. Registra un nuevo tenant con `POST /api/auth/register`
3. Copia el token de la respuesta
4. Haz clic en "Authorize" en la esquina superior derecha
5. Ingresa: `Bearer {tu-token}`
6. Ahora puedes probar todos los endpoints protegidos

## Desarrollo

### Agregar Nueva Migración

**Para Master Database:**
```bash
dotnet ef migrations add MigrationName --context MasterDbContext --output-dir Data/Migrations/Master
```

**Para Tenant Database:**
```bash
dotnet ef migrations add MigrationName --context TenantDbContext --output-dir Data/Migrations/Tenant
```

### Compilar

```bash
dotnet build
```

### Ejecutar Tests

```bash
dotnet test
```

## Troubleshooting

### Error: "Unable to connect to database"

Verifica:
- PostgreSQL está ejecutándose
- Credenciales en `appsettings.json` son correctas
- Base de datos `master_saas` existe

### Error: "JWT Key not configured"

Asegúrate de configurar una clave JWT en `appsettings.json` de al menos 32 caracteres.

### Emails no se envían

Verifica:
- API Key de Maileroo es correcta
- Dominio de envío está verificado en Maileroo
- Configuración de `FromEmail` y `FromName` son correctas

## Licencia

Este proyecto es de código abierto.

## Autor

Desarrollado para MasterBackup SaaS Platform.
