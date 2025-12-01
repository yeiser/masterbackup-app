namespace MasterBackup_API.Domain.Enums;

public enum WorkerAssignmentMode
{
    Auto = 1,      // Load balancing automático con tags
    Dedicated = 2  // Solo el worker asignado
}
