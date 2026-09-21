using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace CryptoPlatform.Persistence.Migrations
{
    /// <inheritdoc />
    public partial class InitialCreate : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.AlterDatabase()
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sys_application",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    app_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    app_name = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    status = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    ip_whitelist = table.Column<string>(type: "text", nullable: true),
                    description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_application", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sys_audit_log",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    audit_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    request_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    trace_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    operator_type = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    operator_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    operator_name = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true),
                    app_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    source_ip = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true),
                    operation = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    key_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    key_version = table.Column<int>(type: "int", nullable: true),
                    result_code = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    duration_ms = table.Column<int>(type: "int", nullable: true),
                    previous_hash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true),
                    current_hash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_audit_log", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sys_config",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    config_key = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    config_value = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false),
                    description = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true),
                    updated_by = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_config", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sys_crypto_device",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    device_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    device_name = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    vendor = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    model = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    serial_number = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true),
                    endpoint = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true),
                    provider_type = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    status = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    priority = table.Column<int>(type: "int", nullable: false),
                    is_primary = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    last_health_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    error_count = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_crypto_device", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sys_idempotency_record",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    app_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    idempotency_key = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    http_method = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    request_path = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false),
                    request_hash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    response_code = table.Column<int>(type: "int", nullable: true),
                    response_body_hash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true),
                    status = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_idempotency_record", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sys_key",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    key_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    owner_app_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    key_type = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    key_usage = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    name = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    status = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    current_version = table.Column<int>(type: "int", nullable: true),
                    expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    activated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    destroyed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    description = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true),
                    concurrency_stamp = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_key", x => x.id);
                    table.UniqueConstraint("AK_sys_key_key_id", x => x.key_id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sys_permission",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    permission_code = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    permission_name = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    resource_type = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_permission", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sys_role",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    role_code = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    role_name = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    description = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_role", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sys_security_event",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    event_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    event_type = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    severity = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    app_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    operator_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    source_ip = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true),
                    related_key_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    request_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    description = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    status = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    detected_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    handled_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    handler = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    handling_result = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_security_event", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sys_user",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    username = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    password_hash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    display_name = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true),
                    status = table.Column<sbyte>(type: "tinyint", nullable: false),
                    login_fail_count = table.Column<int>(type: "int", nullable: false),
                    locked_until = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    last_login_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_user", x => x.id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sys_application_secret",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    application_id = table.Column<long>(type: "bigint", nullable: false),
                    secret_hash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    secret_version = table.Column<int>(type: "int", nullable: false),
                    status = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    last_used_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    revoked_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_application_secret", x => x.id);
                    table.ForeignKey(
                        name: "FK_sys_application_secret_sys_application_application_id",
                        column: x => x.application_id,
                        principalTable: "sys_application",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sys_key_authorization",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    key_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    app_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    permissions = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    status = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    created_by = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    updated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    updated_by = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    revoked_at = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_key_authorization", x => x.id);
                    table.ForeignKey(
                        name: "FK_sys_key_authorization_sys_key_key_id",
                        column: x => x.key_id,
                        principalTable: "sys_key",
                        principalColumn: "key_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sys_key_version",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    key_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    version_no = table.Column<int>(type: "int", nullable: false),
                    provider_type = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    device_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    provider_key_ref = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false),
                    public_key_material = table.Column<string>(type: "text", nullable: true),
                    encrypted_key_material = table.Column<string>(type: "longtext", nullable: true),
                    fingerprint = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    status = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    activated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    rotated_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    expires_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    destroyed_at = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    destroy_result = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true),
                    usage_count = table.Column<long>(type: "bigint", nullable: false),
                    usage_bytes = table.Column<long>(type: "bigint", nullable: false),
                    created_request_id = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    concurrency_stamp = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_key_version", x => x.id);
                    table.ForeignKey(
                        name: "FK_sys_key_version_sys_key_key_id",
                        column: x => x.key_id,
                        principalTable: "sys_key",
                        principalColumn: "key_id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sys_user_role",
                columns: table => new
                {
                    id = table.Column<long>(type: "bigint", nullable: false),
                    user_id = table.Column<long>(type: "bigint", nullable: false),
                    role_id = table.Column<long>(type: "bigint", nullable: false),
                    created_at = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_user_role", x => x.id);
                    table.ForeignKey(
                        name: "FK_sys_user_role_sys_user_user_id",
                        column: x => x.user_id,
                        principalTable: "sys_user",
                        principalColumn: "id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "uk_application_app_id",
                table: "sys_application",
                column: "app_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_app_secret_application",
                table: "sys_application_secret",
                column: "application_id");

            migrationBuilder.CreateIndex(
                name: "ix_app_secret_status",
                table: "sys_application_secret",
                columns: new[] { "status", "expires_at" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_app_time",
                table: "sys_audit_log",
                columns: new[] { "app_id", "timestamp" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_key_time",
                table: "sys_audit_log",
                columns: new[] { "key_id", "timestamp" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_operation",
                table: "sys_audit_log",
                columns: new[] { "operation", "timestamp" });

            migrationBuilder.CreateIndex(
                name: "ix_audit_timestamp",
                table: "sys_audit_log",
                column: "timestamp");

            migrationBuilder.CreateIndex(
                name: "uk_audit_id",
                table: "sys_audit_log",
                column: "audit_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uk_config_key",
                table: "sys_config",
                column: "config_key",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_device_provider",
                table: "sys_crypto_device",
                columns: new[] { "provider_type", "status" });

            migrationBuilder.CreateIndex(
                name: "uk_device_id",
                table: "sys_crypto_device",
                column: "device_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_idempotency_expires",
                table: "sys_idempotency_record",
                column: "expires_at");

            migrationBuilder.CreateIndex(
                name: "uk_idempotency",
                table: "sys_idempotency_record",
                columns: new[] { "app_id", "http_method", "request_path", "idempotency_key" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_key_owner_status",
                table: "sys_key",
                columns: new[] { "owner_app_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_key_owner_type_status",
                table: "sys_key",
                columns: new[] { "owner_app_id", "key_type", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_key_type_usage",
                table: "sys_key",
                columns: new[] { "key_type", "key_usage" });

            migrationBuilder.CreateIndex(
                name: "uk_key_id",
                table: "sys_key",
                column: "key_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_key_auth_app_status",
                table: "sys_key_authorization",
                columns: new[] { "app_id", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_key_auth_key_status",
                table: "sys_key_authorization",
                columns: new[] { "key_id", "status" });

            migrationBuilder.CreateIndex(
                name: "uk_key_auth",
                table: "sys_key_authorization",
                columns: new[] { "key_id", "app_id" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_key_version_fingerprint",
                table: "sys_key_version",
                column: "fingerprint");

            migrationBuilder.CreateIndex(
                name: "ix_key_version_provider",
                table: "sys_key_version",
                columns: new[] { "provider_type", "device_id" });

            migrationBuilder.CreateIndex(
                name: "ix_key_version_provider_ref",
                table: "sys_key_version",
                column: "provider_key_ref");

            migrationBuilder.CreateIndex(
                name: "ix_key_version_status",
                table: "sys_key_version",
                columns: new[] { "key_id", "status" });

            migrationBuilder.CreateIndex(
                name: "uk_key_version",
                table: "sys_key_version",
                columns: new[] { "key_id", "version_no" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uk_permission_code",
                table: "sys_permission",
                column: "permission_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uk_role_code",
                table: "sys_role",
                column: "role_code",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_event_detected",
                table: "sys_security_event",
                column: "detected_at");

            migrationBuilder.CreateIndex(
                name: "ix_event_severity",
                table: "sys_security_event",
                columns: new[] { "severity", "status" });

            migrationBuilder.CreateIndex(
                name: "ix_event_type_status",
                table: "sys_security_event",
                columns: new[] { "event_type", "status" });

            migrationBuilder.CreateIndex(
                name: "uk_event_id",
                table: "sys_security_event",
                column: "event_id",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "uk_user_username",
                table: "sys_user",
                column: "username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "ix_user_role_role",
                table: "sys_user_role",
                column: "role_id");

            migrationBuilder.CreateIndex(
                name: "ix_user_role_user",
                table: "sys_user_role",
                column: "user_id");

            migrationBuilder.CreateIndex(
                name: "uk_user_role",
                table: "sys_user_role",
                columns: new[] { "user_id", "role_id" },
                unique: true);
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "sys_application_secret");

            migrationBuilder.DropTable(
                name: "sys_audit_log");

            migrationBuilder.DropTable(
                name: "sys_config");

            migrationBuilder.DropTable(
                name: "sys_crypto_device");

            migrationBuilder.DropTable(
                name: "sys_idempotency_record");

            migrationBuilder.DropTable(
                name: "sys_key_authorization");

            migrationBuilder.DropTable(
                name: "sys_key_version");

            migrationBuilder.DropTable(
                name: "sys_permission");

            migrationBuilder.DropTable(
                name: "sys_role");

            migrationBuilder.DropTable(
                name: "sys_security_event");

            migrationBuilder.DropTable(
                name: "sys_user_role");

            migrationBuilder.DropTable(
                name: "sys_application");

            migrationBuilder.DropTable(
                name: "sys_key");

            migrationBuilder.DropTable(
                name: "sys_user");
        }
    }
}
