# Maileroo Backup Notification Templates

This document describes the email templates that need to be created in Maileroo dashboard for backup notifications.

## Template 1: Backup Completed (Template ID: 4540)

**Subject:** ✅ Backup completado: {{SCHEDULE_NAME}}

**Template Variables:**
- `USERNAME` - User's full name
- `SCHEDULE_NAME` - Name of the backup schedule
- `DATABASE_NAME` - Name of the database
- `BACKUP_SIZE` - Size of backup file (e.g., "123.45 MB")
- `DURATION` - Time taken to complete backup (e.g., "5m 23s")
- `COMPLETION_TIME` - When backup completed (e.g., "2024-12-04 15:30:00 UTC")
- `BLOB_URL` - URL to download the backup file

**Suggested HTML Template:**
```html
<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: #28a745; color: white; padding: 20px; border-radius: 5px 5px 0 0; }
        .content { background: #f8f9fa; padding: 20px; border: 1px solid #dee2e6; }
        .info-row { margin: 10px 0; padding: 10px; background: white; border-radius: 3px; }
        .label { font-weight: bold; color: #495057; }
        .value { color: #212529; }
        .button { display: inline-block; padding: 12px 24px; background: #007bff; color: white; text-decoration: none; border-radius: 5px; margin-top: 15px; }
        .footer { text-align: center; padding: 20px; color: #6c757d; font-size: 12px; }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h2 style="margin: 0;">✅ Backup Completado Exitosamente</h2>
        </div>
        <div class="content">
            <p>Hola <strong>{{USERNAME}}</strong>,</p>
            <p>Tu backup se ha completado exitosamente. Aquí están los detalles:</p>
            
            <div class="info-row">
                <span class="label">📋 Schedule:</span>
                <span class="value">{{SCHEDULE_NAME}}</span>
            </div>
            
            <div class="info-row">
                <span class="label">🗄️ Base de Datos:</span>
                <span class="value">{{DATABASE_NAME}}</span>
            </div>
            
            <div class="info-row">
                <span class="label">📦 Tamaño del Backup:</span>
                <span class="value">{{BACKUP_SIZE}}</span>
            </div>
            
            <div class="info-row">
                <span class="label">⏱️ Duración:</span>
                <span class="value">{{DURATION}}</span>
            </div>
            
            <div class="info-row">
                <span class="label">🕒 Completado:</span>
                <span class="value">{{COMPLETION_TIME}}</span>
            </div>
            
            <a href="{{BLOB_URL}}" class="button">📥 Descargar Backup</a>
            
            <p style="margin-top: 20px; color: #6c757d; font-size: 14px;">
                El archivo de backup está disponible en tu almacenamiento de Azure Blob Storage.
            </p>
        </div>
        <div class="footer">
            <p>MasterBackup - Sistema de Gestión de Backups</p>
            <p>Este es un mensaje automático, por favor no respondas a este correo.</p>
        </div>
    </div>
</body>
</html>
```

---

## Template 2: Backup Failed (Template ID: 4541)

**Subject:** ❌ Backup falló: {{SCHEDULE_NAME}}

**Template Variables:**
- `USERNAME` - User's full name
- `SCHEDULE_NAME` - Name of the backup schedule
- `DATABASE_NAME` - Name of the database
- `ERROR_MESSAGE` - Error description
- `FAILURE_TIME` - When backup failed (e.g., "2024-12-04 15:30:00 UTC")

**Suggested HTML Template:**
```html
<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: #dc3545; color: white; padding: 20px; border-radius: 5px 5px 0 0; }
        .content { background: #f8f9fa; padding: 20px; border: 1px solid #dee2e6; }
        .info-row { margin: 10px 0; padding: 10px; background: white; border-radius: 3px; }
        .label { font-weight: bold; color: #495057; }
        .value { color: #212529; }
        .error-box { background: #f8d7da; border: 1px solid #f5c6cb; padding: 15px; border-radius: 5px; margin: 15px 0; }
        .error-message { color: #721c24; font-family: monospace; font-size: 13px; }
        .action-items { background: #fff3cd; border: 1px solid #ffeaa7; padding: 15px; border-radius: 5px; margin: 15px 0; }
        .footer { text-align: center; padding: 20px; color: #6c757d; font-size: 12px; }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h2 style="margin: 0;">❌ Backup Falló</h2>
        </div>
        <div class="content">
            <p>Hola <strong>{{USERNAME}}</strong>,</p>
            <p>Lamentablemente, tu backup ha fallado. Por favor revisa los detalles a continuación:</p>
            
            <div class="info-row">
                <span class="label">📋 Schedule:</span>
                <span class="value">{{SCHEDULE_NAME}}</span>
            </div>
            
            <div class="info-row">
                <span class="label">🗄️ Base de Datos:</span>
                <span class="value">{{DATABASE_NAME}}</span>
            </div>
            
            <div class="info-row">
                <span class="label">🕒 Hora del Fallo:</span>
                <span class="value">{{FAILURE_TIME}}</span>
            </div>
            
            <div class="error-box">
                <strong style="color: #721c24;">⚠️ Error:</strong>
                <div class="error-message">{{ERROR_MESSAGE}}</div>
            </div>
            
            <div class="action-items">
                <strong>🔧 Acciones Recomendadas:</strong>
                <ul style="margin: 10px 0; padding-left: 20px;">
                    <li>Verifica que la conexión a la base de datos esté activa</li>
                    <li>Revisa los permisos de acceso a Azure Blob Storage</li>
                    <li>Asegúrate de que haya suficiente espacio disponible</li>
                    <li>Consulta los logs del sistema para más detalles</li>
                </ul>
            </div>
            
            <p style="margin-top: 20px; color: #6c757d; font-size: 14px;">
                Si el problema persiste, contacta al soporte técnico o revisa la configuración del schedule.
            </p>
        </div>
        <div class="footer">
            <p>MasterBackup - Sistema de Gestión de Backups</p>
            <p>Este es un mensaje automático, por favor no respondas a este correo.</p>
        </div>
    </div>
</body>
</html>
```

---

## Template 3: Backup Started (Template ID: 4542) - Optional

**Subject:** 🔄 Backup iniciado: {{SCHEDULE_NAME}}

**Template Variables:**
- `USERNAME` - User's full name
- `SCHEDULE_NAME` - Name of the backup schedule
- `DATABASE_NAME` - Name of the database
- `START_TIME` - When backup started (e.g., "2024-12-04 15:30:00 UTC")

**Suggested HTML Template:**
```html
<!DOCTYPE html>
<html>
<head>
    <meta charset="UTF-8">
    <style>
        body { font-family: Arial, sans-serif; line-height: 1.6; color: #333; }
        .container { max-width: 600px; margin: 0 auto; padding: 20px; }
        .header { background: #17a2b8; color: white; padding: 20px; border-radius: 5px 5px 0 0; }
        .content { background: #f8f9fa; padding: 20px; border: 1px solid #dee2e6; }
        .info-row { margin: 10px 0; padding: 10px; background: white; border-radius: 3px; }
        .label { font-weight: bold; color: #495057; }
        .value { color: #212529; }
        .footer { text-align: center; padding: 20px; color: #6c757d; font-size: 12px; }
    </style>
</head>
<body>
    <div class="container">
        <div class="header">
            <h2 style="margin: 0;">🔄 Backup Iniciado</h2>
        </div>
        <div class="content">
            <p>Hola <strong>{{USERNAME}}</strong>,</p>
            <p>Tu backup ha comenzado y está en progreso:</p>
            
            <div class="info-row">
                <span class="label">📋 Schedule:</span>
                <span class="value">{{SCHEDULE_NAME}}</span>
            </div>
            
            <div class="info-row">
                <span class="label">🗄️ Base de Datos:</span>
                <span class="value">{{DATABASE_NAME}}</span>
            </div>
            
            <div class="info-row">
                <span class="label">🕒 Hora de Inicio:</span>
                <span class="value">{{START_TIME}}</span>
            </div>
            
            <p style="margin-top: 20px; color: #6c757d; font-size: 14px;">
                Te notificaremos cuando el backup se complete o si ocurre algún error.
            </p>
        </div>
        <div class="footer">
            <p>MasterBackup - Sistema de Gestión de Backups</p>
            <p>Este es un mensaje automático, por favor no respondas a este correo.</p>
        </div>
    </div>
</body>
</html>
```

---

## Setup Instructions

1. **Login to Maileroo Dashboard:**
   - Go to: https://app.maileroo.com/
   - Navigate to: Templates → Create New Template

2. **Create Each Template:**
   - Copy the HTML template content
   - Set the subject line exactly as specified
   - Create template and note the **Template ID** assigned

3. **Update Template IDs in Code:**
   - Currently using temporary IDs: 4540, 4541, 4542
   - Update these in `EmailService.cs` if Maileroo assigns different IDs:
     ```csharp
     // Line ~78: Backup Completed
     await SendEmailDirectAsync(recipientEmail, recipientName, subject, "YOUR_ACTUAL_TEMPLATE_ID", templateData);
     
     // Line ~91: Backup Failed
     await SendEmailDirectAsync(recipientEmail, recipientName, subject, "YOUR_ACTUAL_TEMPLATE_ID", templateData);
     
     // Line ~103: Backup Started (optional)
     await SendEmailDirectAsync(recipientEmail, recipientName, subject, "YOUR_ACTUAL_TEMPLATE_ID", templateData);
     ```

4. **Test Templates:**
   - Use Maileroo's preview feature to test each template
   - Send test emails with sample data
   - Verify all variables render correctly

---

## Current Template IDs (To Be Updated)

| Purpose | Current ID | Description |
|---------|-----------|-------------|
| 2FA Code | 4533 | Already exists |
| Password Reset | 4539 | Already exists |
| Welcome Email | 4530 | Already exists |
| **Backup Completed** | **4540** | **TO BE CREATED** |
| **Backup Failed** | **4541** | **TO BE CREATED** |
| **Backup Started** | **4542** | **TO BE CREATED (Optional)** |

---

## Notes

- The "Backup Started" template (4542) is optional and currently not called in the code
- If you want start notifications, uncomment the call in `UpdateBackupStatusCommandHandler.HandleInProgressStatus()`
- All templates use UTF-8 encoding and support Spanish language
- Emoji in subjects are optional but make emails more visually distinctive
