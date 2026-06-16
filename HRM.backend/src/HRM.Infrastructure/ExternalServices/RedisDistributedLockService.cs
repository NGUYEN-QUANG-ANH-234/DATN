using System.Diagnostics;
using HRM.backend.src.HRM.Application.Interfaces;
using StackExchange.Redis;

namespace HRM.backend.src.HRM.Infrastructure.ExternalServices
{
    public sealed class RedisDistributedLockService : ILockService
    {
        private const string KeyPrefix = "HRMHICAS_Lock:";
        private const string ReleaseScript =
            "if redis.call('GET', KEYS[1]) == ARGV[1] then return redis.call('DEL', KEYS[1]) else return 0 end";
        private const string RenewScript =
            "if redis.call('GET', KEYS[1]) == ARGV[1] then return redis.call('PEXPIRE', KEYS[1], ARGV[2]) else return 0 end";

        private static readonly TimeSpan DefaultAcquireTimeout = TimeSpan.FromSeconds(10);
        private static readonly TimeSpan DefaultLockTtl = TimeSpan.FromSeconds(30);
        private static readonly TimeSpan RetryDelay = TimeSpan.FromMilliseconds(120);

        private readonly IConnectionMultiplexer _redis;
        private readonly ILogger<RedisDistributedLockService> _logger;

        public RedisDistributedLockService(
            IConnectionMultiplexer redis,
            ILogger<RedisDistributedLockService> logger)
        {
            _redis = redis;
            _logger = logger;
        }

        public async Task<T> GetWithLockAsync<T>(
            string key,
            Func<CancellationToken, Task<T>> action,
            TimeSpan? acquireTimeout = null,
            CancellationToken cancellationToken = default)
        {
            var database = _redis.GetDatabase();
            var redisKey = BuildRedisKey(key);
            var token = $"{Environment.MachineName}:{Guid.NewGuid():N}";
            var timeout = acquireTimeout ?? DefaultAcquireTimeout;
            var stopwatch = Stopwatch.StartNew();

            while (stopwatch.Elapsed < timeout)
            {
                cancellationToken.ThrowIfCancellationRequested();

                var acquired = await database.StringSetAsync(
                    redisKey,
                    token,
                    DefaultLockTtl,
                    When.NotExists);

                if (acquired)
                {
                    return await ExecuteWithRenewalAsync(database, redisKey, token, key, action, cancellationToken);
                }

                var remaining = timeout - stopwatch.Elapsed;
                if (remaining <= TimeSpan.Zero) break;

                await Task.Delay(remaining < RetryDelay ? remaining : RetryDelay, cancellationToken);
            }

            throw new TimeoutException($"Khong the nhan distributed lock cho key '{key}' sau {timeout.TotalSeconds} giay. Nghiep vu nay dang duoc xu ly boi request khac.");
        }

        private async Task<T> ExecuteWithRenewalAsync<T>(
            IDatabase database,
            RedisKey redisKey,
            string token,
            string originalKey,
            Func<CancellationToken, Task<T>> action,
            CancellationToken cancellationToken)
        {
            using var renewalCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            var renewalTask = RenewUntilReleasedAsync(database, redisKey, token, renewalCts.Token);

            try
            {
                _logger.LogDebug("Acquired distributed lock {LockKey}", originalKey);
                return await action(cancellationToken);
            }
            finally
            {
                renewalCts.Cancel();
                await ObserveRenewalCompletionAsync(renewalTask, originalKey);
                await ReleaseAsync(database, redisKey, token, originalKey);
            }
        }

        private async Task RenewUntilReleasedAsync(IDatabase database, RedisKey redisKey, string token, CancellationToken ct)
        {
            var renewInterval = TimeSpan.FromMilliseconds(DefaultLockTtl.TotalMilliseconds / 3);

            while (!ct.IsCancellationRequested)
            {
                await Task.Delay(renewInterval, ct);
                await database.ScriptEvaluateAsync(
                    RenewScript,
                    new RedisKey[] { redisKey },
                    new RedisValue[] { token, (long)DefaultLockTtl.TotalMilliseconds });
            }
        }

        private async Task ReleaseAsync(IDatabase database, RedisKey redisKey, string token, string originalKey)
        {
            try
            {
                await database.ScriptEvaluateAsync(
                    ReleaseScript,
                    new RedisKey[] { redisKey },
                    new RedisValue[] { token });
                _logger.LogDebug("Released distributed lock {LockKey}", originalKey);
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Khong the release distributed lock {LockKey}. Lock se tu het han theo TTL.", originalKey);
            }
        }

        private async Task ObserveRenewalCompletionAsync(Task renewalTask, string originalKey)
        {
            try
            {
                await renewalTask;
            }
            catch (OperationCanceledException)
            {
                // Expected when the protected action finishes.
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Co loi khi renew distributed lock {LockKey}.", originalKey);
            }
        }

        private static string BuildRedisKey(string key)
        {
            return $"{KeyPrefix}{key.Trim().Replace(' ', '_')}";
        }
    }
}
