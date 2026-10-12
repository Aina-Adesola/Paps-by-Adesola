using Microsoft.Extensions.Caching.Memory;
using PAPPSADMIN.Services;
using System.Security.Cryptography;
using System.Text;

namespace PAPPSADMIN.Services
{
    // One-time codes live in a memory cache, NOT in the database:
    //  - they disappear by themselves after 5 minutes,
    //  - only a hash of the code is kept (never the code itself),
    //  - after 5 wrong guesses the code is destroyed,
    //  - a code can be used once.
    // Limitation: the cache lives inside this one running app. A restart clears pending
    // codes (users just log in again), and if you ever run several servers behind a load
    // balancer you must switch to a shared cache such as Redis (IDistributedCache).
    public class OtpService : IOtpService
    {
        private const int MaxAttempts = 5;
        private static readonly TimeSpan Lifetime = TimeSpan.FromMinutes(5);
        private static readonly object Gate = new(); // keeps the attempt counter accurate

        private readonly IMemoryCache _cache;

        private sealed class OtpEntry
        {
            public byte[] CodeHash { get; init; } = Array.Empty<byte>();
            public int Attempts { get; set; }
        }

        public OtpService(IMemoryCache cache)
        {
            _cache = cache;
        }

        private static string Key(int userId) => $"otp:{userId}";

        private static byte[] ComputeHash(string code)
            => SHA256.HashData(Encoding.UTF8.GetBytes(code));

        public string Generate(int userId)
        {
            var code = RandomNumberGenerator.GetInt32(100_000, 1_000_000).ToString();

            // Setting the same key again replaces any earlier code for this user.
            _cache.Set(Key(userId), new OtpEntry { CodeHash = ComputeHash(code) }, Lifetime);
            return code;
        }

        public OtpResult Verify(int userId, string code)
        {
            lock (Gate)
            {
                if (!_cache.TryGetValue(Key(userId), out OtpEntry? entry) || entry is null)
                    return OtpResult.Expired;

                if (entry.Attempts >= MaxAttempts)
                {
                    _cache.Remove(Key(userId));
                    return OtpResult.TooManyAttempts;
                }

                entry.Attempts++;

                // FixedTimeEquals compares without leaking, through timing, how many characters matched.
                if (CryptographicOperations.FixedTimeEquals(entry.CodeHash, ComputeHash(code ?? string.Empty)))
                {
                    _cache.Remove(Key(userId)); // single use
                    return OtpResult.Valid;
                }

                if (entry.Attempts >= MaxAttempts)
                {
                    _cache.Remove(Key(userId));
                    return OtpResult.TooManyAttempts;
                }

                return OtpResult.Invalid;
            }
        }

        public void Remove(int userId) => _cache.Remove(Key(userId));
    }
}