using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace MasterBackup_API.Infrastructure.Persistence.Migrations.Tenant
{
    /// <inheritdoc />
    public partial class AddNotificationsAndInitiatedBy : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            // Add InitiatedBy column if it doesn't exist (idempotent)
            migrationBuilder.Sql(@"
                DO $$ 
                BEGIN
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.columns 
                        WHERE table_name = 'BackupHistories' 
                        AND column_name = 'InitiatedBy'
                    ) THEN
                        ALTER TABLE ""BackupHistories"" ADD COLUMN ""InitiatedBy"" text NULL;
                    END IF;
                END $$;
            ");

            // Drop ApplicationUser table first if it exists (CASCADE will drop all FK constraints)
            migrationBuilder.Sql(@"DROP TABLE IF EXISTS ""ApplicationUser"" CASCADE;");
            
            // Create notifications table if it doesn't exist (idempotent)
            migrationBuilder.Sql(@"
                DO $$ 
                DECLARE
                    fk_constraint_name TEXT;
                BEGIN
                    -- Drop any FK constraint on user_id column
                    FOR fk_constraint_name IN 
                        SELECT constraint_name 
                        FROM information_schema.table_constraints 
                        WHERE table_name = 'notifications' 
                        AND constraint_type = 'FOREIGN KEY'
                        AND constraint_name ILIKE '%ApplicationUser%'
                    LOOP
                        EXECUTE 'ALTER TABLE notifications DROP CONSTRAINT IF EXISTS ' || quote_ident(fk_constraint_name);
                        RAISE NOTICE 'Dropped constraint: %', fk_constraint_name;
                    END LOOP;
                    
                    IF NOT EXISTS (
                        SELECT 1 FROM information_schema.tables 
                        WHERE table_name = 'notifications'
                    ) THEN
                        CREATE TABLE notifications (
                            id uuid NOT NULL,
                            tenant_id uuid NOT NULL,
                            user_id character varying(450) NOT NULL,
                            type character varying(50) NOT NULL,
                            title character varying(200) NOT NULL,
                            message character varying(1000) NOT NULL,
                            redirect_url character varying(500),
                            related_entity_id uuid,
                            related_entity_type character varying(100),
                            is_read boolean NOT NULL DEFAULT false,
                            read_at timestamp with time zone,
                            metadata text,
                            created_at timestamp with time zone NOT NULL DEFAULT CURRENT_TIMESTAMP,
                            expires_at timestamp with time zone,
                            CONSTRAINT ""PK_notifications"" PRIMARY KEY (id)
                        );
                        
                        CREATE INDEX ""IX_notifications_created_at"" ON notifications (created_at);
                        CREATE INDEX ""IX_notifications_tenant_id"" ON notifications (tenant_id);
                        CREATE INDEX ""IX_notifications_user_id"" ON notifications (user_id);
                        CREATE INDEX ""IX_notifications_user_id_is_read"" ON notifications (user_id, is_read);
                    ELSE
                        -- Table exists, ensure is_read has default value
                        ALTER TABLE notifications ALTER COLUMN is_read SET DEFAULT false;
                        
                        -- Update any existing null values
                        UPDATE notifications SET is_read = false WHERE is_read IS NULL;
                    END IF;
                END $$;
            ");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "notifications");

            migrationBuilder.DropColumn(
                name: "InitiatedBy",
                table: "BackupHistories");
        }
    }
}
