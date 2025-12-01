namespace MasterBackup_API.Domain.Enums;

public enum WorkerStatus
{
    Online = 1,      // Conectado y disponible para jobs
    Offline = 2,     // Sin heartbeat por > 2 minutos
    Busy = 3,        // Procesando jobs (MaxConcurrentJobs alcanzado)
    Error = 4,       // Último job falló o error crítico
    Maintenance = 5  // En mantenimiento (no recibe jobs)
}
