# Implementación de Test de Conexión en Frontend

## 📋 Resumen de Cambios

Se implementó la funcionalidad completa de test de conexión de base de datos en el frontend Angular, sincronizada con el flujo asíncrono API → RabbitMQ → Worker → API.

## ✅ Cambios Implementados

### 1. **Modelos TypeScript** (`database-connection.models.ts`)

#### Nuevos Enums y Tipos
```typescript
export enum WorkerAssignmentMode {
  Auto = 1,
  Dedicated = 2
}

export const WorkerAssignmentModeLabels: { [key in WorkerAssignmentMode]: string } = {
  [WorkerAssignmentMode.Auto]: 'Automático',
  [WorkerAssignmentMode.Dedicated]: 'Dedicado'
};
```

#### Campos Agregados a DTOs
- `DatabaseConnectionDto`:
  - `assignedWorkerId?: string`
  - `tags?: string[]`
  - `assignmentMode?: WorkerAssignmentMode`

- `CreateDatabaseConnectionDto`:
  - `assignedWorkerId?: string`
  - `tags?: string[]`
  - `assignmentMode?: WorkerAssignmentMode`

- `UpdateDatabaseConnectionDto`:
  - `assignedWorkerId?: string`
  - `tags?: string[]`
  - `assignmentMode?: WorkerAssignmentMode`

---

### 2. **Componente TypeScript** (`databases.component.ts`)

#### Nuevas Propiedades
```typescript
private pollingInterval?: any;
private readonly POLLING_INTERVAL_MS = 3000; // 3 segundos
WorkerAssignmentMode = WorkerAssignmentMode;
WorkerAssignmentModeLabels = WorkerAssignmentModeLabels;
```

#### Implementación de OnDestroy
```typescript
ngOnDestroy(): void {
  this.stopPolling();
}
```

#### Nuevos Métodos

##### 1. **startPolling()**
Inicia un intervalo que actualiza las conexiones cada 3 segundos para detectar cuando el Worker completa la prueba.

```typescript
private startPolling(): void {
  this.stopPolling();
  this.pollingInterval = setInterval(() => {
    this.loadConnectionsSilently();
  }, this.POLLING_INTERVAL_MS);
}
```

##### 2. **stopPolling()**
Detiene el polling automático.

```typescript
private stopPolling(): void {
  if (this.pollingInterval) {
    clearInterval(this.pollingInterval);
    this.pollingInterval = undefined;
  }
}
```

##### 3. **loadConnectionsSilently()**
Carga conexiones sin mostrar el loader y detecta automáticamente cuando se completa una prueba.

**Funcionalidad**:
- Carga conexiones en segundo plano
- Detecta si la conexión que se está probando ya tiene resultado
- Verifica que el resultado sea reciente (< 5 segundos)
- Muestra alerta de éxito o error automáticamente
- Detiene el polling al recibir resultado

```typescript
private loadConnectionsSilently(): void {
  this.databaseService.getAllConnections().subscribe({
    next: (data) => {
      const previousTestingId = this.testingConnectionId;
      
      // Actualizar datos
      this.connections = data;
      this.applyFilter();
      
      // Verificar si la conexión que estábamos testeando ya tiene resultado
      if (previousTestingId) {
        const testedConnection = this.connections.find(c => c.id === previousTestingId);
        if (testedConnection?.lastTestedAt) {
          const lastTestDate = new Date(testedConnection.lastTestedAt);
          const now = new Date();
          const diffSeconds = (now.getTime() - lastTestDate.getTime()) / 1000;
          
          // Si la última prueba es reciente (menos de 5 segundos), mostrar resultado
          if (diffSeconds < 5) {
            this.testingConnectionId = undefined;
            this.stopPolling();
            
            if (testedConnection.lastTestSuccessful) {
              this.showSuccessAlert(`✓ Conexión "${testedConnection.name}" exitosa: ${testedConnection.lastTestStatus}`);
            } else {
              this.showErrorAlert(`✗ Conexión "${testedConnection.name}" fallida: ${testedConnection.lastTestStatus}`);
            }
          }
        }
      }
    }
  });
}
```

#### Método Mejorado: **testConnection()**

**Mejoras**:
- Mensaje de confirmación más descriptivo con emoji ⏳
- Inicia polling automático
- Timeout de 30 segundos (detiene polling si no hay respuesta)

```typescript
testConnection(connectionId: string): void {
  const connection = this.connections.find(c => c.id === connectionId);
  if (!connection || this.testingConnectionId) return;

  this.testingConnectionId = connectionId;

  this.databaseService.testConnection(connectionId).subscribe({
    next: () => {
      console.info(`Probando conexión "${connection.name}"... El resultado llegará en breve.`);
      this.showInfoAlert(`⏳ Probando conexión "${connection.name}"... El resultado llegará en breve.`);
      
      // Iniciar polling para actualizar el estado automáticamente
      this.startPolling();
      
      // Detener polling después de 30 segundos (timeout)
      setTimeout(() => {
        if (this.testingConnectionId === connectionId) {
          this.testingConnectionId = undefined;
          this.stopPolling();
        }
      }, 30000);
    },
    error: (error) => {
      console.error('Error enviando solicitud de prueba:', error);
      this.testingConnectionId = undefined;
      this.showErrorAlert('❌ Error al enviar solicitud de prueba: ' + (error.error?.message || 'Error desconocido'));
    }
  });
}
```

---

### 3. **Template HTML** (`databases.component.html`)

#### Badge de Estado Mejorado

**Antes**: Solo mostraba "Conexión exitosa", "Conexión fallida" o "Sin probar"

**Ahora**: 
- Muestra spinner animado cuando está probando
- Iconos visuales para cada estado
- Badge con colores según estado

```html
<!-- Estado: Probando -->
<span
  *ngIf="testingConnectionId === connection.id"
  class="badge badge-light-primary"
>
  <span class="spinner-border spinner-border-sm me-2" role="status" aria-hidden="true"></span>
  Probando conexión...
</span>

<!-- Estado: Última prueba -->
<span
  *ngIf="connection.lastTestedAt && testingConnectionId !== connection.id"
  class="badge"
  [ngClass]="
    connection.lastTestSuccessful
      ? 'badge-light-success'
      : 'badge-light-danger'
  "
>
  <i [ngClass]="connection.lastTestSuccessful ? 'fa fa-check-circle' : 'fa fa-times-circle'" class="me-1"></i>
  {{
    connection.lastTestSuccessful
      ? "Conexión exitosa"
      : "Conexión fallida"
  }}
</span>

<!-- Estado: Sin probar -->
<span
  *ngIf="!connection.lastTestedAt && testingConnectionId !== connection.id"
  class="badge badge-light"
>
  <i class="fa fa-question-circle me-1"></i>
  Sin probar
</span>
```

#### Botón "Probar Conexión" Mejorado

**Mejoras**:
- Muestra spinner mientras está probando
- Se deshabilita durante la prueba
- Texto cambia a "Probando..."

```html
<div class="menu-item px-3">
  <a 
    href="javascript:void(0)" 
    class="menu-link px-3"
    (click)="testConnection(connection.id)"
    [class.disabled]="testingConnectionId === connection.id"
  >
    <span *ngIf="testingConnectionId !== connection.id">
      <i class="fa fa-check-circle text-success me-2"></i>
      Probar Conexión
    </span>
    <span *ngIf="testingConnectionId === connection.id">
      <span class="spinner-border spinner-border-sm text-primary me-2"></span>
      Probando...
    </span>
  </a>
</div>
```

---

### 4. **Estilos CSS** (`databases.component.css`)

#### Nuevos Estilos Agregados

```css
/* Estado de carga en botones */
.btn.disabled,
.menu-link.disabled {
  opacity: 0.6;
  pointer-events: none;
  cursor: not-allowed;
}

/* Spinner animation */
@keyframes spinner-border {
  to { transform: rotate(360deg); }
}

.spinner-border {
  display: inline-block;
  width: 1rem;
  height: 1rem;
  vertical-align: text-bottom;
  border: 0.15em solid currentColor;
  border-right-color: transparent;
  border-radius: 50%;
  animation: spinner-border 0.75s linear infinite;
}

.spinner-border-sm {
  width: 0.75rem;
  height: 0.75rem;
  border-width: 0.1em;
}
```

---

## 🔄 Flujo de Usuario

### Paso 1: Usuario hace click en "Probar Conexión"

**UI**:
- Badge cambia a: `⏳ Probando conexión...` (azul con spinner)
- Botón del menú muestra: `Probando...` con spinner
- Botón se deshabilita

**Backend**:
- Frontend → POST `/api/database-connections/{id}/test`
- API valida y publica mensaje a RabbitMQ
- API responde: `202 Accepted`

### Paso 2: Polling Automático (cada 3 segundos)

**UI**:
- Frontend actualiza conexiones en segundo plano
- No muestra loader (silencioso)

**Backend**:
- Frontend → GET `/api/database-connections`
- API retorna lista actualizada de conexiones

### Paso 3: Worker Procesa y Notifica

**Backend**:
- Worker consume mensaje de RabbitMQ
- Worker valida autorización
- Worker ejecuta test de conexión (pg_dump)
- Worker → POST `/api/test-connections/complete`
- API actualiza `DatabaseConnection` en base de datos:
  - `lastTestedAt = DateTime.UtcNow`
  - `lastTestSuccessful = true/false`
  - `lastTestStatus = "mensaje"`
  - `engineVersion = "versión detectada"`

### Paso 4: Frontend Detecta Resultado

**Detección**:
- Polling detecta que `lastTestedAt` es reciente (< 5 segundos)
- Detiene polling automáticamente

**UI**:
- Badge cambia según resultado:
  - **Éxito**: `✓ Conexión exitosa` (verde)
  - **Error**: `✗ Conexión fallida` (rojo)
- Muestra alerta flotante:
  - **Éxito**: `✓ Conexión "PostgreSQL Producción" exitosa: PostgreSQL version: PostgreSQL 15.3...`
  - **Error**: `✗ Conexión "PostgreSQL Producción" fallida: Connection refused...`
- Botón vuelve a estado normal

### Paso 5: Timeout (si no hay respuesta en 30 segundos)

**UI**:
- Detiene polling
- Resetea estado de "probando"
- Badge vuelve a estado anterior

---

## 🎨 Estados Visuales

### 1. **Sin Probar**
```
🔘 Sin probar (gris)
```

### 2. **Probando**
```
🔄 Probando conexión... (azul con spinner animado)
```

### 3. **Éxito**
```
✓ Conexión exitosa (verde)
Última prueba: hace 2 minutos
Resultado: ✓ Connection successful! PostgreSQL version: PostgreSQL 15.3
```

### 4. **Error**
```
✗ Conexión fallida (rojo)
Última prueba: hace 1 minuto
Resultado: ✗ PostgreSQL connection failed: Connection refused
```

---

## 🧪 Pruebas de Usuario

### Escenario 1: Conexión Exitosa
1. Usuario hace click en "Probar Conexión"
2. Badge muestra "Probando conexión..." con spinner
3. Después de ~2-5 segundos:
   - Badge cambia a "✓ Conexión exitosa"
   - Alerta verde: "✓ Conexión 'MiDB' exitosa: PostgreSQL version: 15.3"
4. Información de última prueba se actualiza

### Escenario 2: Conexión Fallida
1. Usuario hace click en "Probar Conexión"
2. Badge muestra "Probando conexión..." con spinner
3. Después de ~2-5 segundos:
   - Badge cambia a "✗ Conexión fallida"
   - Alerta roja: "✗ Conexión 'MiDB' fallida: Connection refused"
4. Información de última prueba se actualiza

### Escenario 3: Timeout (Worker no responde)
1. Usuario hace click en "Probar Conexión"
2. Badge muestra "Probando conexión..." con spinner
3. Después de 30 segundos:
   - Polling se detiene automáticamente
   - Badge vuelve a estado anterior
   - No se muestra alerta de error

### Escenario 4: Múltiples Conexiones
1. Usuario prueba Conexión A
2. Mientras A está probando, usuario intenta probar Conexión B
3. Sistema previene: botón de B está deshabilitado
4. Debe esperar a que A termine

---

## 🔧 Configuración

### Intervalos de Tiempo

```typescript
// En databases.component.ts
private readonly POLLING_INTERVAL_MS = 3000; // Polling cada 3 segundos

// Timeout para detener polling
setTimeout(() => {
  // ...
}, 30000); // 30 segundos

// Detección de resultado reciente
if (diffSeconds < 5) { // Menos de 5 segundos = resultado fresco
  // Mostrar resultado
}
```

### Ajustar Intervalos

Para cambiar los intervalos:

1. **Polling más rápido/lento**: Modificar `POLLING_INTERVAL_MS`
2. **Timeout más largo/corto**: Modificar `setTimeout(30000)`
3. **Detección de resultado**: Modificar `diffSeconds < 5`

---

## 📦 Dependencias

- ✅ Angular 17+
- ✅ RxJS (Observables)
- ✅ Bootstrap 5 (clases de spinner)
- ✅ Font Awesome (iconos)
- ✅ Metronic Theme (estilos)

---

## 🚀 Ejecución

### Compilación
```bash
cd MasterBackup-App
ng build --configuration development
```

### Desarrollo
```bash
ng serve -o
```

---

## ✅ Verificación de Implementación

- [x] Modelos TypeScript con campos de Worker Assignment
- [x] Polling automático cada 3 segundos
- [x] Detección automática de resultados
- [x] UI con spinner animado
- [x] Badge con estados visuales
- [x] Timeout de 30 segundos
- [x] Prevención de múltiples pruebas simultáneas
- [x] Alertas de éxito/error automáticas
- [x] Cleanup de intervalos en ngOnDestroy
- [x] CSS personalizado para spinner
- [x] Compilación exitosa sin errores TypeScript

---

## 🎯 Próximos Pasos Sugeridos

1. **SignalR Hub**
   - Implementar notificaciones en tiempo real
   - Eliminar polling (más eficiente)
   
2. **Worker Assignment UI**
   - Agregar campos de Worker Assignment en formulario
   - Dropdown para seleccionar Worker
   - Radio buttons para Assignment Mode
   - Tag input para tags

3. **Historial de Pruebas**
   - Tabla con todas las pruebas realizadas
   - Gráfico de disponibilidad

4. **Test Múltiple**
   - Botón para probar todas las conexiones
   - Progress bar general

---

## 📝 Notas Técnicas

### ¿Por qué Polling en lugar de SignalR?

**Ventajas del Polling**:
- ✅ Más simple de implementar
- ✅ No requiere WebSocket abierto
- ✅ Funciona con cualquier proxy/firewall
- ✅ Fácil debugging

**Desventajas**:
- ❌ Más tráfico de red (GET cada 3 segundos)
- ❌ Latencia de hasta 3 segundos

**Recomendación**: Para producción, migrar a SignalR para notificaciones en tiempo real.

### Manejo de Memoria

El componente limpia correctamente el polling en `ngOnDestroy()` para prevenir memory leaks.

```typescript
ngOnDestroy(): void {
  this.stopPolling(); // ✅ Limpieza correcta
}
```

---

## 🐛 Troubleshooting

### Problema: Polling no se detiene
**Solución**: Verificar que `ngOnDestroy()` se esté llamando. Revisar que `stopPolling()` limpie correctamente el intervalo.

### Problema: Resultado no se detecta
**Solución**: Verificar que `diffSeconds < 5` sea suficiente. Worker puede tardar más en ambientes lentos.

### Problema: Spinner no se muestra
**Solución**: Verificar que Bootstrap esté cargado. Alternativamente, el CSS personalizado tiene una implementación de spinner.

---

## 📚 Referencias

- [Angular Component Lifecycle](https://angular.io/guide/lifecycle-hooks)
- [RxJS Intervals](https://rxjs.dev/api/index/function/interval)
- [Bootstrap Spinners](https://getbootstrap.com/docs/5.0/components/spinners/)
- [Font Awesome Icons](https://fontawesome.com/icons)
