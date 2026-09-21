namespace CryptoPlatform.Persistence.IdGeneration;

/// <summary>
/// 雪花 ID 生成器（Snowflake ID）。
/// 标准 64 位结构：1 bit 符号 + 41 bits 时间戳 + 10 bits 工作节点 + 12 bits 序列号。
/// <para>
/// - 时间戳精度：毫秒级，起始纪元 2020-01-01，可用约 69 年<br/>
/// - 工作节点：10 bits，支持 1024 个节点（0-1023）<br/>
/// - 序列号：12 bits，每毫秒最多 4096 个 ID
/// </para>
/// </summary>
public sealed class SnowflakeIdGenerator
{
    // ── 位分配 ──
    private const int SequenceBits = 12;
    private const int WorkerIdBits = 10;
    private const int TimestampBits = 41;

    // ── 掩码 ──
    private const long SequenceMask = (1L << SequenceBits) - 1;        // 0xFFF = 4095
    private const long WorkerIdMask = (1L << WorkerIdBits) - 1;        // 0x3FF = 1023

    // ── 位移 ──
    private const int WorkerIdShift = SequenceBits;                     // 12
    private const int TimestampShift = SequenceBits + WorkerIdBits;     // 22

    // ── 起始纪元：2020-01-01 00:00:00 UTC ──
    private static readonly DateTime Epoch = new(2020, 1, 1, 0, 0, 0, DateTimeKind.Utc);

    private readonly long _workerId;
    private readonly object _lock = new();

    private long _lastTimestamp = -1L;
    private long _sequence;

    /// <summary>
    /// 创建雪花 ID 生成器实例。
    /// </summary>
    /// <param name="workerId">工作节点 ID，范围 0-1023。</param>
    /// <exception cref="ArgumentOutOfRangeException">workerId 超出范围</exception>
    public SnowflakeIdGenerator(long workerId = 0)
    {
        if (workerId < 0 || workerId > WorkerIdMask)
            throw new ArgumentOutOfRangeException(nameof(workerId), $"WorkerId 必须在 0-{WorkerIdMask} 范围内");

        _workerId = workerId;
    }

    /// <summary>
    /// 生成下一个雪花 ID。
    /// </summary>
    public long NextId()
    {
        lock (_lock)
        {
            var timestamp = CurrentTimestamp();

            if (timestamp < _lastTimestamp)
            {
                // 时钟回拨，拒绝生成
                throw new InvalidOperationException(
                    $"时钟回拨 detected。上次时间戳: {_lastTimestamp}, 当前: {timestamp}。" +
                    "请检查系统时间或 NTP 同步状态。");
            }

            if (timestamp == _lastTimestamp)
            {
                _sequence = (_sequence + 1) & SequenceMask;
                if (_sequence == 0)
                {
                    // 当前毫秒序列号耗尽，等待下一毫秒
                    timestamp = WaitNextMillis(_lastTimestamp);
                }
            }
            else
            {
                _sequence = 0;
            }

            _lastTimestamp = timestamp;

            return ((timestamp - EpochOffset()) << TimestampShift)
                 | (_workerId << WorkerIdShift)
                 | _sequence;
        }
    }

    private static long CurrentTimestamp()
        => DateTimeOffset.UtcNow.ToUnixTimeMilliseconds() - EpochOffset();

    private static long EpochOffset()
        => new DateTimeOffset(Epoch, TimeSpan.Zero).ToUnixTimeMilliseconds();

    private static long WaitNextMillis(long lastTimestamp)
    {
        var timestamp = CurrentTimestamp();
        while (timestamp <= lastTimestamp)
        {
            Thread.SpinWait(1);
            timestamp = CurrentTimestamp();
        }
        return timestamp;
    }
}
