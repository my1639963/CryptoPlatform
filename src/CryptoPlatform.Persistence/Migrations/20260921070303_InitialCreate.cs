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
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    AppId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    AppName = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    Status = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    IpWhitelist = table.Column<string>(type: "text", nullable: true),
                    Description = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_application", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sys_audit_log",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    AuditId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    RequestId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    TraceId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    Timestamp = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    OperatorType = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    OperatorId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    OperatorName = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true),
                    AppId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    SourceIp = table.Column<string>(type: "varchar(45)", maxLength: 45, nullable: true),
                    Operation = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    KeyId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    KeyVersion = table.Column<int>(type: "int", nullable: true),
                    ResultCode = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    DurationMs = table.Column<int>(type: "int", nullable: true),
                    PreviousHash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true),
                    CurrentHash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_audit_log", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sys_config",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    ConfigKey = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    ConfigValue = table.Column<string>(type: "varchar(2000)", maxLength: 2000, nullable: false),
                    Description = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true),
                    UpdatedBy = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_config", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sys_crypto_device",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    DeviceId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    DeviceName = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    Vendor = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    Model = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    SerialNumber = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true),
                    Endpoint = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true),
                    ProviderType = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    Status = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    Priority = table.Column<int>(type: "int", nullable: false),
                    IsPrimary = table.Column<bool>(type: "tinyint(1)", nullable: false),
                    LastHealthAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ErrorCount = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_crypto_device", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sys_idempotency_record",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    AppId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    IdempotencyKey = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    HttpMethod = table.Column<string>(type: "varchar(10)", maxLength: 10, nullable: false),
                    RequestPath = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: false),
                    RequestHash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    ResponseCode = table.Column<int>(type: "int", nullable: true),
                    ResponseBodyHash = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true),
                    Status = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_idempotency_record", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sys_key",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    KeyId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    OwnerAppId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    KeyType = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    KeyUsage = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    Name = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    Status = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    CurrentVersion = table.Column<int>(type: "int", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ActivatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DestroyedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Description = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_key", x => x.Id);
                    table.UniqueConstraint("AK_sys_key_KeyId", x => x.KeyId);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sys_permission",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    PermissionCode = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    PermissionName = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    ResourceType = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_permission", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sys_role",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    RoleCode = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    RoleName = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    Description = table.Column<string>(type: "varchar(256)", maxLength: 256, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_role", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sys_security_event",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    EventId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    EventType = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    Severity = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    AppId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    OperatorId = table.Column<string>(type: "longtext", nullable: true),
                    SourceIp = table.Column<string>(type: "longtext", nullable: true),
                    RelatedKeyId = table.Column<string>(type: "longtext", nullable: true),
                    RequestId = table.Column<string>(type: "longtext", nullable: true),
                    Description = table.Column<string>(type: "varchar(1000)", maxLength: 1000, nullable: true),
                    Status = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    DetectedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    HandledAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    Handler = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    HandlingResult = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_security_event", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sys_user",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    Username = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    PasswordHash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    DisplayName = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: true),
                    Status = table.Column<sbyte>(type: "tinyint", nullable: false),
                    LoginFailCount = table.Column<int>(type: "int", nullable: false),
                    LockedUntil = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LastLoginAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_user", x => x.Id);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sys_application_secret",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    ApplicationId = table.Column<long>(type: "bigint", nullable: false),
                    SecretHash = table.Column<string>(type: "varchar(255)", maxLength: 255, nullable: false),
                    SecretVersion = table.Column<int>(type: "int", nullable: false),
                    Status = table.Column<byte>(type: "tinyint unsigned", nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    LastUsedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    RevokedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_application_secret", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sys_application_secret_sys_application_ApplicationId",
                        column: x => x.ApplicationId,
                        principalTable: "sys_application",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sys_key_authorization",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    KeyId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    AppId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    Permissions = table.Column<string>(type: "varchar(500)", maxLength: 500, nullable: false),
                    Status = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    CreatedBy = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    UpdatedBy = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    RevokedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_key_authorization", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sys_key_authorization_sys_key_KeyId",
                        column: x => x.KeyId,
                        principalTable: "sys_key",
                        principalColumn: "KeyId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sys_key_version",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    KeyId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false),
                    VersionNo = table.Column<int>(type: "int", nullable: false),
                    ProviderType = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: false),
                    DeviceId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    ProviderKeyRef = table.Column<string>(type: "varchar(512)", maxLength: 512, nullable: false),
                    PublicKeyMaterial = table.Column<string>(type: "text", nullable: true),
                    EncryptedKeyMaterial = table.Column<string>(type: "longtext", nullable: true),
                    Fingerprint = table.Column<string>(type: "varchar(128)", maxLength: 128, nullable: false),
                    Status = table.Column<string>(type: "varchar(16)", maxLength: 16, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false),
                    ActivatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    RotatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    ExpiresAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DestroyedAt = table.Column<DateTime>(type: "datetime(6)", nullable: true),
                    DestroyResult = table.Column<string>(type: "varchar(32)", maxLength: 32, nullable: true),
                    UsageCount = table.Column<long>(type: "bigint", nullable: false),
                    UsageBytes = table.Column<long>(type: "bigint", nullable: false),
                    CreatedRequestId = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: true),
                    ConcurrencyStamp = table.Column<string>(type: "varchar(64)", maxLength: 64, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_key_version", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sys_key_version_sys_key_KeyId",
                        column: x => x.KeyId,
                        principalTable: "sys_key",
                        principalColumn: "KeyId",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateTable(
                name: "sys_user_role",
                columns: table => new
                {
                    Id = table.Column<long>(type: "bigint", nullable: false),
                    UserId = table.Column<long>(type: "bigint", nullable: false),
                    RoleId = table.Column<long>(type: "bigint", nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime(6)", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_sys_user_role", x => x.Id);
                    table.ForeignKey(
                        name: "FK_sys_user_role_sys_user_UserId",
                        column: x => x.UserId,
                        principalTable: "sys_user",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                })
                .Annotation("MySQL:Charset", "utf8mb4");

            migrationBuilder.CreateIndex(
                name: "IX_sys_application_AppId",
                table: "sys_application",
                column: "AppId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sys_application_secret_ApplicationId",
                table: "sys_application_secret",
                column: "ApplicationId");

            migrationBuilder.CreateIndex(
                name: "IX_sys_application_secret_Status_ExpiresAt",
                table: "sys_application_secret",
                columns: new[] { "Status", "ExpiresAt" });

            migrationBuilder.CreateIndex(
                name: "IX_sys_audit_log_AppId_Timestamp",
                table: "sys_audit_log",
                columns: new[] { "AppId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_sys_audit_log_AuditId",
                table: "sys_audit_log",
                column: "AuditId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sys_audit_log_KeyId_Timestamp",
                table: "sys_audit_log",
                columns: new[] { "KeyId", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_sys_audit_log_Operation_Timestamp",
                table: "sys_audit_log",
                columns: new[] { "Operation", "Timestamp" });

            migrationBuilder.CreateIndex(
                name: "IX_sys_audit_log_Timestamp",
                table: "sys_audit_log",
                column: "Timestamp");

            migrationBuilder.CreateIndex(
                name: "IX_sys_config_ConfigKey",
                table: "sys_config",
                column: "ConfigKey",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sys_crypto_device_DeviceId",
                table: "sys_crypto_device",
                column: "DeviceId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sys_crypto_device_ProviderType_Status",
                table: "sys_crypto_device",
                columns: new[] { "ProviderType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_sys_idempotency_record_AppId_HttpMethod_RequestPath_Idempote~",
                table: "sys_idempotency_record",
                columns: new[] { "AppId", "HttpMethod", "RequestPath", "IdempotencyKey" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sys_idempotency_record_ExpiresAt",
                table: "sys_idempotency_record",
                column: "ExpiresAt");

            migrationBuilder.CreateIndex(
                name: "IX_sys_key_KeyId",
                table: "sys_key",
                column: "KeyId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sys_key_OwnerAppId_KeyType_Status",
                table: "sys_key",
                columns: new[] { "OwnerAppId", "KeyType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_sys_key_OwnerAppId_Status",
                table: "sys_key",
                columns: new[] { "OwnerAppId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_sys_key_authorization_AppId_Status",
                table: "sys_key_authorization",
                columns: new[] { "AppId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_sys_key_authorization_KeyId_AppId",
                table: "sys_key_authorization",
                columns: new[] { "KeyId", "AppId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sys_key_authorization_KeyId_Status",
                table: "sys_key_authorization",
                columns: new[] { "KeyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_sys_key_version_Fingerprint",
                table: "sys_key_version",
                column: "Fingerprint");

            migrationBuilder.CreateIndex(
                name: "IX_sys_key_version_KeyId_Status",
                table: "sys_key_version",
                columns: new[] { "KeyId", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_sys_key_version_KeyId_VersionNo",
                table: "sys_key_version",
                columns: new[] { "KeyId", "VersionNo" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sys_key_version_ProviderType_DeviceId",
                table: "sys_key_version",
                columns: new[] { "ProviderType", "DeviceId" });

            migrationBuilder.CreateIndex(
                name: "IX_sys_permission_PermissionCode",
                table: "sys_permission",
                column: "PermissionCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sys_role_RoleCode",
                table: "sys_role",
                column: "RoleCode",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sys_security_event_DetectedAt",
                table: "sys_security_event",
                column: "DetectedAt");

            migrationBuilder.CreateIndex(
                name: "IX_sys_security_event_EventId",
                table: "sys_security_event",
                column: "EventId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sys_security_event_EventType_Status",
                table: "sys_security_event",
                columns: new[] { "EventType", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_sys_security_event_Severity_Status",
                table: "sys_security_event",
                columns: new[] { "Severity", "Status" });

            migrationBuilder.CreateIndex(
                name: "IX_sys_user_Username",
                table: "sys_user",
                column: "Username",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_sys_user_role_RoleId",
                table: "sys_user_role",
                column: "RoleId");

            migrationBuilder.CreateIndex(
                name: "IX_sys_user_role_UserId",
                table: "sys_user_role",
                column: "UserId");

            migrationBuilder.CreateIndex(
                name: "IX_sys_user_role_UserId_RoleId",
                table: "sys_user_role",
                columns: new[] { "UserId", "RoleId" },
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
