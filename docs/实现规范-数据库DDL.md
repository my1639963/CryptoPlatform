# 32. 完整数据库最终 DDL

> 本章节提供所有核心表的完整建表语句，基于 MySQL 8.4 语法编写。主键采用雪花 ID（Snowflake ID），由应用层生成，数据库不使用自增。实际落地到国产数据库时，需根据目标产品的 `datetime(6)` 精度、`longtext` 类型、索引长度限制等差异进行适配，见末尾适配说明。

## 32.1 sys_application — 接入应用

```sql
CREATE TABLE sys_application (
    id           BIGINT       NOT NULL COMMENT '主键（雪花 ID，应用层生成）',
    app_id       VARCHAR(64)  NOT NULL                COMMENT '应用唯一标识，对外暴露',
    app_name     VARCHAR(128) NOT NULL                COMMENT '应用名称',
    status       TINYINT      NOT NULL DEFAULT 0      COMMENT '状态：0=正常 1=禁用 2=锁定',
    ip_whitelist TEXT         NULL                    COMMENT 'IP/CIDR 白名单，逗号分隔',
    description  VARCHAR(500) NULL                    COMMENT '描述',
    created_at   DATETIME(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6) COMMENT '创建时间',
    updated_at   DATETIME(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6) COMMENT '更新时间',
    PRIMARY KEY (id),
    UNIQUE KEY uk_application_app_id (app_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='接入应用';
```

## 32.2 sys_application_secret — 应用凭据

```sql
CREATE TABLE sys_application_secret (
    id             BIGINT       NOT NULL COMMENT '主键（雪花 ID，应用层生成）',
    application_id BIGINT       NOT NULL                COMMENT '所属应用 ID',
    secret_hash    VARCHAR(255) NOT NULL                COMMENT 'AppSecret 哈希值（PBKDF2/SM3）',
    secret_version INT          NOT NULL DEFAULT 1      COMMENT '凭据版本号',
    status         TINYINT      NOT NULL DEFAULT 0      COMMENT '状态：0=ACTIVE 1=REVOKED 2=EXPIRED',
    expires_at     DATETIME(6)  NULL                    COMMENT '过期时间',
    last_used_at   DATETIME(6)  NULL                    COMMENT '最近使用时间',
    created_at     DATETIME(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6) COMMENT '创建时间',
    revoked_at     DATETIME(6)  NULL                    COMMENT '撤销时间',
    PRIMARY KEY (id),
    KEY ix_app_secret_application (application_id),
    KEY ix_app_secret_status (status, expires_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='应用凭据（仅存哈希）';
```

## 32.3 sys_key — 逻辑密钥

```sql
CREATE TABLE sys_key (
    id              BIGINT       NOT NULL COMMENT '主键（雪花 ID，应用层生成）',
    key_id          VARCHAR(64)  NOT NULL                COMMENT '逻辑密钥 ID，全局唯一，对外暴露',
    owner_app_id    VARCHAR(64)  NOT NULL                COMMENT '所属应用 app_id',
    key_type        VARCHAR(32)  NOT NULL                COMMENT '密钥类型：SM2/SM4/HMAC/ROOT',
    key_usage       VARCHAR(32)  NOT NULL                COMMENT '密钥用途：SIGN/ENCRYPT/MAC/WRAP',
    name            VARCHAR(128) NOT NULL                COMMENT '密钥名称',
    status          VARCHAR(16)  NOT NULL                COMMENT '生命周期状态：CREATED/ACTIVE/ROTATED/DISABLED/EXPIRED/REVOKED/DESTROYED',
    current_version INT          NULL                    COMMENT '当前 ACTIVE 版本号',
    expires_at      DATETIME(6)  NULL                    COMMENT '逻辑密钥有效期',
    created_at      DATETIME(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6) COMMENT '创建时间',
    activated_at    DATETIME(6)  NULL                    COMMENT '首次激活时间',
    destroyed_at    DATETIME(6)  NULL                    COMMENT '销毁时间',
    description     VARCHAR(512) NULL                    COMMENT '描述',
    concurrency_stamp VARCHAR(64) NOT NULL               COMMENT '乐观并发控制标识',
    PRIMARY KEY (id),
    UNIQUE KEY uk_key_id (key_id),
    KEY ix_key_owner_status (owner_app_id, status),
    KEY ix_key_owner_type_status (owner_app_id, key_type, status),
    KEY ix_key_type_usage (key_type, key_usage)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='逻辑密钥';
```

## 32.4 sys_key_version — 密钥版本

```sql
CREATE TABLE sys_key_version (
    id                     BIGINT       NOT NULL COMMENT '主键（雪花 ID，应用层生成）',
    key_id                 VARCHAR(64)  NOT NULL                COMMENT '逻辑密钥 ID',
    version_no             INT          NOT NULL                COMMENT '版本号，同一 key_id 下从 1 递增',
    provider_type          VARCHAR(32)  NOT NULL                COMMENT 'Provider 类型：HSM/SOFTWARE',
    device_id              VARCHAR(64)  NULL                    COMMENT 'HSM 设备 ID',
    provider_key_ref       VARCHAR(512) NOT NULL                COMMENT 'Provider 内部密钥引用',
    public_key_material    TEXT         NULL                    COMMENT 'SM2 公钥材料（Base64）',
    encrypted_key_material LONGTEXT     NULL                    COMMENT '软件模式下加密包装的密钥材料',
    fingerprint            VARCHAR(128) NOT NULL                COMMENT '密钥指纹（SM3 哈希）',
    status                 VARCHAR(16)  NOT NULL                COMMENT '版本状态',
    created_at             DATETIME(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6) COMMENT '创建时间',
    activated_at           DATETIME(6)  NULL                    COMMENT '激活时间',
    rotated_at             DATETIME(6)  NULL                    COMMENT '轮换时间',
    expires_at             DATETIME(6)  NULL                    COMMENT '版本有效期',
    destroyed_at           DATETIME(6)  NULL                    COMMENT '销毁时间',
    destroy_result         VARCHAR(32)  NULL                    COMMENT '销毁结果：Success/NotFound/Failed/Timeout',
    usage_count            BIGINT       NOT NULL DEFAULT 0      COMMENT '使用次数',
    usage_bytes            BIGINT       NOT NULL DEFAULT 0      COMMENT '处理数据量（字节）',
    created_request_id     VARCHAR(64)  NULL                    COMMENT '创建时请求追踪 ID',
    concurrency_stamp      VARCHAR(64)  NOT NULL                COMMENT '乐观并发控制标识',
    PRIMARY KEY (id),
    UNIQUE KEY uk_key_version (key_id, version_no),
    KEY ix_key_version_status (key_id, status),
    KEY ix_key_version_provider (provider_type, device_id),
    KEY ix_key_version_fingerprint (fingerprint),
    KEY ix_key_version_provider_ref (provider_key_ref(255))
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='密钥版本';
```

## 32.5 sys_key_authorization — 密钥授权

```sql
CREATE TABLE sys_key_authorization (
    id          BIGINT       NOT NULL COMMENT '主键（雪花 ID，应用层生成）',
    key_id      VARCHAR(64)  NOT NULL                COMMENT '逻辑密钥 ID',
    app_id      VARCHAR(64)  NOT NULL                COMMENT '被授权应用 app_id',
    permissions VARCHAR(500) NOT NULL                COMMENT '允许操作集合，逗号分隔：ENCRYPT,DECRYPT,SIGN,VERIFY,HMAC',
    status      VARCHAR(16)  NOT NULL DEFAULT 'ACTIVE' COMMENT '状态：ACTIVE/DISABLED/EXPIRED',
    expires_at  DATETIME(6)  NULL                    COMMENT '授权截止时间',
    created_at  DATETIME(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6) COMMENT '创建时间',
    created_by  VARCHAR(64)  NULL                    COMMENT '创建者',
    updated_at  DATETIME(6)  NULL                    COMMENT '更新时间',
    updated_by  VARCHAR(64)  NULL                    COMMENT '修改者',
    revoked_at  DATETIME(6)  NULL                    COMMENT '撤销时间',
    PRIMARY KEY (id),
    UNIQUE KEY uk_key_auth (key_id, app_id),
    KEY ix_key_auth_app_status (app_id, status),
    KEY ix_key_auth_key_status (key_id, status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='密钥授权';
```

## 32.6 sys_crypto_device — 密码设备

```sql
CREATE TABLE sys_crypto_device (
    id                BIGINT       NOT NULL COMMENT '主键（雪花 ID，应用层生成）',
    device_id         VARCHAR(64)  NOT NULL                COMMENT '设备唯一标识',
    device_name       VARCHAR(128) NOT NULL                COMMENT '设备名称',
    vendor            VARCHAR(64)  NOT NULL                COMMENT '厂商',
    model             VARCHAR(64)  NULL                    COMMENT '型号',
    serial_number     VARCHAR(128) NULL                    COMMENT '序列号',
    endpoint          VARCHAR(256) NULL                    COMMENT '连接端点',
    provider_type     VARCHAR(32)  NOT NULL                COMMENT 'Provider 类型',
    status            VARCHAR(16)  NOT NULL DEFAULT 'OFFLINE' COMMENT '状态：ONLINE/DEGRADED/OFFLINE/MAINTENANCE',
    priority          INT          NOT NULL DEFAULT 0      COMMENT '优先级，数值越大越优先',
    is_primary        TINYINT(1)   NOT NULL DEFAULT 0      COMMENT '是否主设备',
    last_health_at    DATETIME(6)  NULL                    COMMENT '最近健康检查时间',
    error_count       BIGINT       NOT NULL DEFAULT 0      COMMENT '累计错误次数',
    created_at        DATETIME(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6) COMMENT '创建时间',
    updated_at        DATETIME(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6) COMMENT '更新时间',
    PRIMARY KEY (id),
    UNIQUE KEY uk_device_id (device_id),
    KEY ix_device_provider (provider_type, status)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='密码设备注册';
```

## 32.7 sys_user — 管理用户

```sql
CREATE TABLE sys_user (
    id            BIGINT       NOT NULL COMMENT '主键（雪花 ID，应用层生成）',
    username      VARCHAR(64)  NOT NULL                COMMENT '用户名',
    password_hash VARCHAR(255) NOT NULL                COMMENT '密码哈希',
    display_name  VARCHAR(128) NULL                    COMMENT '显示名称',
    status        TINYINT      NOT NULL DEFAULT 0      COMMENT '状态：0=正常 1=禁用 2=锁定',
    login_fail_count INT       NOT NULL DEFAULT 0      COMMENT '连续登录失败次数',
    locked_until  DATETIME(6)  NULL                    COMMENT '锁定截止时间',
    last_login_at DATETIME(6)  NULL                    COMMENT '最近登录时间',
    created_at    DATETIME(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6) COMMENT '创建时间',
    updated_at    DATETIME(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6) COMMENT '更新时间',
    PRIMARY KEY (id),
    UNIQUE KEY uk_user_username (username)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='管理用户';
```

## 32.8 sys_role / sys_permission / sys_user_role

```sql
CREATE TABLE sys_role (
    id          BIGINT       NOT NULL COMMENT '主键（雪花 ID，应用层生成）',
    role_code   VARCHAR(32)  NOT NULL                COMMENT '角色编码：SYSTEM_ADMIN/KEY_ADMIN/APP_ADMIN/SECURITY_AUDITOR/OPS_ADMIN',
    role_name   VARCHAR(64)  NOT NULL                COMMENT '角色名称',
    description VARCHAR(256) NULL                    COMMENT '描述',
    created_at  DATETIME(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6) COMMENT '创建时间',
    PRIMARY KEY (id),
    UNIQUE KEY uk_role_code (role_code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='角色';

CREATE TABLE sys_permission (
    id              BIGINT       NOT NULL COMMENT '主键（雪花 ID，应用层生成）',
    permission_code VARCHAR(64)  NOT NULL                COMMENT '权限编码',
    permission_name VARCHAR(128) NOT NULL                COMMENT '权限名称',
    resource_type   VARCHAR(32)  NOT NULL                COMMENT '资源类型：KEY/APP/DEVICE/SYSTEM/AUDIT',
    created_at      DATETIME(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6) COMMENT '创建时间',
    PRIMARY KEY (id),
    UNIQUE KEY uk_permission_code (permission_code)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='权限定义';

CREATE TABLE sys_user_role (
    id         BIGINT      NOT NULL COMMENT '主键（雪花 ID，应用层生成）',
    user_id    BIGINT      NOT NULL                COMMENT '用户 ID',
    role_id    BIGINT      NOT NULL                COMMENT '角色 ID',
    created_at DATETIME(6) NOT NULL DEFAULT CURRENT_TIMESTAMP(6) COMMENT '创建时间',
    PRIMARY KEY (id),
    UNIQUE KEY uk_user_role (user_id, role_id),
    KEY ix_user_role_user (user_id),
    KEY ix_user_role_role (role_id)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='用户角色关联';
```

## 32.9 sys_audit_log — 审计日志

```sql
CREATE TABLE sys_audit_log (
    id             BIGINT       NOT NULL COMMENT '主键（雪花 ID，应用层生成）',
    audit_id       VARCHAR(64)  NOT NULL                COMMENT '审计记录唯一 ID',
    request_id     VARCHAR(64)  NULL                    COMMENT '请求追踪 ID',
    trace_id       VARCHAR(64)  NULL                    COMMENT '链路追踪 ID',
    timestamp      DATETIME(6)  NOT NULL                COMMENT '操作时间戳',
    operator_type  VARCHAR(16)  NOT NULL                COMMENT '操作者类型：APP/ADMIN/USER/SYSTEM',
    operator_id    VARCHAR(64)  NULL                    COMMENT '操作者 ID',
    operator_name  VARCHAR(128) NULL                    COMMENT '操作者名称',
    app_id         VARCHAR(64)  NULL                    COMMENT '关联应用',
    source_ip      VARCHAR(45)  NULL                    COMMENT '来源 IP',
    operation      VARCHAR(64)  NOT NULL                COMMENT '操作类型',
    key_id         VARCHAR(64)  NULL                    COMMENT '关联密钥',
    key_version    INT          NULL                    COMMENT '关联密钥版本',
    result_code    VARCHAR(32)  NOT NULL                COMMENT '结果码：SUCCESS/FAILURE/...',
    duration_ms    INT          NULL                    COMMENT '耗时（毫秒）',
    previous_hash  VARCHAR(128) NULL                    COMMENT '前一条记录哈希',
    current_hash   VARCHAR(128) NOT NULL                COMMENT '当前记录哈希',
    created_at     DATETIME(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6) COMMENT '写入时间',
    PRIMARY KEY (id),
    UNIQUE KEY uk_audit_id (audit_id),
    KEY ix_audit_timestamp (timestamp),
    KEY ix_audit_app_time (app_id, timestamp),
    KEY ix_audit_key_time (key_id, timestamp),
    KEY ix_audit_operation (operation, timestamp)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='审计日志（不可篡改）';
```

## 32.10 sys_security_event — 安全事件

```sql
CREATE TABLE sys_security_event (
    id              BIGINT       NOT NULL COMMENT '主键（雪花 ID，应用层生成）',
    event_id        VARCHAR(64)  NOT NULL                COMMENT '事件唯一 ID',
    event_type      VARCHAR(64)  NOT NULL                COMMENT '事件类型',
    severity        VARCHAR(16)  NOT NULL                COMMENT '严重等级：INFO/LOW/MEDIUM/HIGH/CRITICAL',
    app_id          VARCHAR(64)  NULL                    COMMENT '关联应用',
    operator_id     VARCHAR(64)  NULL                    COMMENT '关联操作者',
    source_ip       VARCHAR(45)  NULL                    COMMENT '来源 IP',
    related_key_id  VARCHAR(64)  NULL                    COMMENT '关联密钥',
    request_id      VARCHAR(64)  NULL                    COMMENT '请求追踪 ID',
    description     VARCHAR(1000) NULL                   COMMENT '事件描述',
    status          VARCHAR(16)  NOT NULL DEFAULT 'OPEN' COMMENT '处置状态：OPEN/ACKNOWLEDGED/HANDLING/RESOLVED/CLOSED',
    detected_at     DATETIME(6)  NOT NULL                COMMENT '检测时间',
    handled_at      DATETIME(6)  NULL                    COMMENT '处理时间',
    handler         VARCHAR(64)  NULL                    COMMENT '处理人',
    handling_result VARCHAR(500) NULL                    COMMENT '处理结果',
    created_at      DATETIME(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6) COMMENT '创建时间',
    PRIMARY KEY (id),
    UNIQUE KEY uk_event_id (event_id),
    KEY ix_event_type_status (event_type, status),
    KEY ix_event_severity (severity, status),
    KEY ix_event_detected (detected_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='安全事件';
```

## 32.11 sys_config — 系统配置

```sql
CREATE TABLE sys_config (
    id           BIGINT       NOT NULL COMMENT '主键（雪花 ID，应用层生成）',
    config_key   VARCHAR(128) NOT NULL                COMMENT '配置键',
    config_value VARCHAR(2000) NOT NULL               COMMENT '配置值',
    description  VARCHAR(256) NULL                    COMMENT '说明',
    updated_by   VARCHAR(64)  NULL                    COMMENT '修改者',
    updated_at   DATETIME(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6) ON UPDATE CURRENT_TIMESTAMP(6) COMMENT '修改时间',
    PRIMARY KEY (id),
    UNIQUE KEY uk_config_key (config_key)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='系统配置';
```

## 32.12 sys_idempotency_record — 幂等记录

```sql
CREATE TABLE sys_idempotency_record (
    id                  BIGINT       NOT NULL COMMENT '主键（雪花 ID，应用层生成）',
    app_id              VARCHAR(64)  NOT NULL                COMMENT '应用 ID',
    idempotency_key     VARCHAR(128) NOT NULL                COMMENT '幂等键',
    http_method         VARCHAR(10)  NOT NULL                COMMENT 'HTTP 方法',
    request_path        VARCHAR(256) NOT NULL                COMMENT '请求路径',
    request_hash        VARCHAR(128) NOT NULL                COMMENT '请求体哈希',
    response_code       INT          NULL                    COMMENT '响应状态码',
    response_body_hash  VARCHAR(128) NULL                    COMMENT '响应体哈希',
    status              VARCHAR(16)  NOT NULL DEFAULT 'IN_PROGRESS' COMMENT '状态：IN_PROGRESS/COMPLETED/FAILED',
    created_at          DATETIME(6)  NOT NULL DEFAULT CURRENT_TIMESTAMP(6) COMMENT '创建时间',
    expires_at          DATETIME(6)  NOT NULL                COMMENT '过期时间',
    PRIMARY KEY (id),
    UNIQUE KEY uk_idempotency (app_id, http_method, request_path, idempotency_key),
    KEY ix_idempotency_expires (expires_at)
) ENGINE=InnoDB DEFAULT CHARSET=utf8mb4 COLLATE=utf8mb4_unicode_ci COMMENT='幂等记录';
```

## 32.13 国产数据库适配说明

| 适配项       | MySQL 8.4 写法                     | 适配要点                                               |
| ------------ | ---------------------------------- | ------------------------------------------------------ |
| 主键 ID      | `BIGINT NOT NULL`（雪花 ID）    | 所有表主键由应用层雪花 ID 生成器分配，不使用数据库自增；国产数据库无需适配自增语法 |
| 微秒精度     | `DATETIME(6)`                    | 部分国产库仅支持 `DATETIME`（秒级），需验证          |
| 大文本       | `LONGTEXT`                       | 部分库用 `TEXT` 或 `CLOB`，注意最大长度差异        |
| 索引前缀长度 | `provider_key_ref(255)`          | 国产库 UTF-8 索引长度限制可能不同，需实测              |
| 字符集       | `utf8mb4 / utf8mb4_unicode_ci`   | 确认目标库默认字符集，部分国产库用 `UTF-8` 即 3 字节 |
| 布尔类型     | `TINYINT(1)`                     | 部分库无 `TINYINT`，用 `SMALLINT` 或 `BOOLEAN`   |
| ON UPDATE    | `ON UPDATE CURRENT_TIMESTAMP(6)` | 部分库不支持，需在应用层维护 `updated_at`            |
| COMMENT      | 列级 `COMMENT '...'`             | 部分库需使用独立 `COMMENT ON COLUMN` 语法            |
| ENGINE       | `ENGINE=InnoDB`                  | 国产库通常不需要此声明，直接删除                       |
