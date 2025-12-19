# API de Suscripciones y Planes

## Resumen

Se ha implementado un sistema completo de suscripciones y planes para el SaaS MasterBackup. El sistema permite:

- Gestionar planes de suscripción con características personalizables
- Crear, actualizar y cancelar suscripciones
- Soporte para facturación mensual y anual
- Periodo de prueba (trial) de 14 días
- Registro de pagos
- Límites por plan (bases de datos, usuarios, almacenamiento, etc.)

## Entidades Creadas

### 1. **Plan**
Tabla principal de planes de suscripción con campos:
- Información básica (nombre, descripción, precios)
- Límites (MaxDatabases, MaxUsers, MaxStorageGB, BackupRetentionDays)
- Características habilitadas (CloudStorage, ScheduledBackups, ApiAccess, etc.)
- Propiedades de UI (DisplayOrder, IsFeatured, BadgeText, BadgeColor)

### 2. **PlanFeature**
Características individuales de cada plan (lista de bullets para mostrar en UI)

### 3. **Subscription**
Suscripciones activas de los tenants con:
- Relación al Plan y Tenant
- Fechas de inicio y fin
- Estado (Active, Trialing, Canceled, Expired, PastDue)
- Ciclo de facturación (Monthly, Yearly)
- Control de trial period

### 4. **Payment**
Registro de pagos realizados con:
- Monto y moneda
- Estado del pago
- Método de pago
- ID de transacción de pasarela externa
- Número de factura

## Endpoints Disponibles

### **Planes (Públicos)**

#### GET `/api/plans`
Obtiene todos los planes disponibles (para mostrar en landing page/pricing)

**Query Parameters:**
- `activeOnly` (bool, opcional): Filtrar solo planes activos (default: true)

**Respuesta:**
```json
[
  {
    "id": "guid",
    "name": "basic",
    "displayName": "Basic",
    "description": "For small teams",
    "monthlyPrice": 29.00,
    "yearlyPrice": 290.00,
    "currency": "USD",
    "maxDatabases": 5,
    "maxUsers": 3,
    "maxStorageGB": 50,
    "backupRetentionDays": 30,
    "cloudStorageEnabled": true,
    "scheduledBackupsEnabled": true,
    "apiAccessEnabled": false,
    "prioritySupport": false,
    "customBrandingEnabled": false,
    "displayOrder": 2,
    "isFeatured": true,
    "badgeText": "Most Popular",
    "badgeColor": "primary",
    "features": [
      {
        "id": "guid",
        "name": "5 Databases",
        "isIncluded": true,
        "displayOrder": 1
      },
      ...
    ]
  }
]
```

#### GET `/api/plans/{id}`
Obtiene un plan específico por ID

**Respuesta:** Igual que el objeto individual del endpoint anterior

---

### **Suscripciones (Autenticadas)**

Todos los endpoints de suscripciones requieren autenticación JWT.

#### GET `/api/subscriptions/current`
Obtiene la suscripción activa del tenant actual

**Respuesta:**
```json
{
  "id": "guid",
  "tenantId": "guid",
  "planId": "guid",
  "planName": "basic",
  "planDisplayName": "Basic",
  "startDate": "2025-01-01T00:00:00Z",
  "endDate": "2025-02-01T00:00:00Z",
  "status": "Active",
  "billingCycle": "Monthly",
  "amount": 29.00,
  "currency": "USD",
  "autoRenew": true,
  "canceledAt": null,
  "cancellationReason": null,
  "trialEndDate": null,
  "daysRemaining": 15
}
```

#### POST `/api/subscriptions`
Crea una nueva suscripción para el tenant actual

**Body:**
```json
{
  "planId": "guid",
  "billingCycle": "Monthly",  // "Monthly" o "Yearly"
  "useTrial": true
}
```

**Respuesta:** Objeto SubscriptionDto

**Errores:**
- 400: Ya tiene una suscripción activa
- 400: Trial period ya usado
- 404: Plan no encontrado

#### PUT `/api/subscriptions/upgrade`
Actualiza/upgrade a un plan diferente (cancela la actual y crea una nueva)

**Body:**
```json
{
  "newPlanId": "guid",
  "billingCycle": "Yearly"
}
```

**Respuesta:** Objeto SubscriptionDto de la nueva suscripción

**Errores:**
- 400: No tiene suscripción activa
- 404: Nuevo plan no encontrado

#### DELETE `/api/subscriptions/cancel`
Cancela la suscripción actual

**Body:**
```json
{
  "reason": "Too expensive"
}
```

**Respuesta:**
```json
{
  "message": "Subscription canceled successfully"
}
```

**Errores:**
- 400: No tiene suscripción activa

---

## Planes Predefinidos

Se incluyen 4 planes por defecto:

### 1. **Free**
- **Precio:** $0
- **Bases de datos:** 1
- **Usuarios:** 1
- **Almacenamiento:** 5 GB
- **Retención:** 7 días
- **Características:** Backups manuales, soporte por email

### 2. **Basic** (Most Popular)
- **Precio:** $29/mes o $290/año
- **Bases de datos:** 5
- **Usuarios:** 3
- **Almacenamiento:** 50 GB
- **Retención:** 30 días
- **Características:** Backups programados, almacenamiento en nube, soporte prioritario

### 3. **Professional** (Best Value)
- **Precio:** $79/mes o $790/año
- **Bases de datos:** 20
- **Usuarios:** 10
- **Almacenamiento:** 200 GB
- **Retención:** 90 días
- **Características:** Todo de Basic + API, branding personalizado, soporte 24/7

### 4. **Enterprise**
- **Precio:** $199/mes o $1990/año
- **Bases de datos:** Ilimitadas
- **Usuarios:** Ilimitados
- **Almacenamiento:** 1 TB
- **Retención:** 365 días
- **Características:** Todo de Pro + soporte dedicado, SLA, integraciones personalizadas

---

## Lógica de Negocio Implementada

### Trial Period
- 14 días de prueba gratuita
- Solo se puede usar una vez por tenant
- Se valida en el backend

### Upgrade/Downgrade
- Al hacer upgrade, la suscripción anterior se cancela automáticamente
- Se crea una nueva suscripción con el nuevo plan
- El trial usado se mantiene (no se puede usar de nuevo)

### Facturación
- Mensual: cargo cada 30 días
- Anual: cargo cada 365 días con descuento (aprox. 17% de ahorro)

### Validaciones
- No se puede crear suscripción si ya tiene una activa
- No se puede usar trial si ya lo usó antes
- No se puede cancelar si no tiene suscripción activa

---

## Base de Datos

### Migraciones Aplicadas
- **AddSubscriptionsAndPlans**: Crea las 4 nuevas tablas (Plans, PlanFeatures, Subscriptions, Payments)
- **PlanSeeder**: Inserta los 4 planes predefinidos con sus características

### Índices Creados
- `Plans.Name` (único)
- `Plans.IsActive`
- `Plans.DisplayOrder`
- `Subscriptions.TenantId`
- `Subscriptions.Status`
- `Subscriptions (TenantId, Status)` (compuesto)
- `Payments.TransactionId`

---

## Próximos Pasos Sugeridos

Para el frontend:
1. Crear componente de pricing page con los 4 planes
2. Crear página de gestión de suscripción en el perfil del usuario
3. Implementar validaciones de límites según el plan activo
4. Mostrar alertas cuando esté cerca de exceder límites
5. Integrar pasarela de pago (Stripe, PayPal, etc.)

Para el backend:
1. Implementar job de Quartz para verificar suscripciones expiradas
2. Implementar job para renovación automática
3. Integrar con pasarela de pagos
4. Implementar webhooks para pagos
5. Agregar endpoints de historial de pagos
6. Implementar middleware para validar límites del plan

---

## Ejemplos de Uso

### 1. Obtener planes para mostrar en landing page
```bash
curl -X GET "https://api.masterbackup.com/api/plans"
```

### 2. Crear suscripción con trial
```bash
curl -X POST "https://api.masterbackup.com/api/subscriptions" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "planId": "guid-del-plan-basic",
    "billingCycle": "Monthly",
    "useTrial": true
  }'
```

### 3. Upgrade a plan superior
```bash
curl -X PUT "https://api.masterbackup.com/api/subscriptions/upgrade" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "newPlanId": "guid-del-plan-pro",
    "billingCycle": "Yearly"
  }'
```

### 4. Ver suscripción actual
```bash
curl -X GET "https://api.masterbackup.com/api/subscriptions/current" \
  -H "Authorization: Bearer {token}"
```

### 5. Cancelar suscripción
```bash
curl -X DELETE "https://api.masterbackup.com/api/subscriptions/cancel" \
  -H "Authorization: Bearer {token}" \
  -H "Content-Type: application/json" \
  -d '{
    "reason": "Switching to another provider"
  }'
```
