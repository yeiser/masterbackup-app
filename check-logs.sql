-- Verificar si la tabla Logs existe
SELECT EXISTS (
    SELECT FROM information_schema.tables 
    WHERE table_name = 'Logs'
);

-- Ver estructura de la tabla Logs
SELECT column_name, data_type, character_maximum_length
FROM information_schema.columns
WHERE table_name = 'Logs'
ORDER BY ordinal_position;

-- Ver los últimos 10 logs ordenados por timestamp
SELECT "Id", "Message", "Level", "TimeStamp", "Exception"
FROM "Logs"
ORDER BY "TimeStamp" DESC
LIMIT 10;

-- Contar total de logs
SELECT COUNT(*) as total_logs FROM "Logs";

-- Ver logs por nivel
SELECT "Level", COUNT(*) as count
FROM "Logs"
GROUP BY "Level"
ORDER BY count DESC;
