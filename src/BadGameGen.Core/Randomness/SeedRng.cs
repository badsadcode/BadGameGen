namespace BadGameGen.Core.Randomness;

/// <summary>
/// Stable hashing helpers. We deliberately avoid string.GetHashCode() (randomised per process)
/// and System.Random (implementation may change between .NET versions), because a seed must
/// reproduce the same concept forever.
/// </summary>
public static class StableHash
{
    /// <summary>FNV-1a over UTF-16 code units. Stable across platforms and runtimes.</summary>
    public static ulong Fnv1a64(string text)
    {
        ulong hash = 14695981039346656037UL;
        foreach (char c in text)
        {
            hash ^= c;
            hash *= 1099511628211UL;
        }
        return hash;
    }

    /// <summary>SplitMix64 finaliser: scrambles bits so similar inputs give unrelated outputs.</summary>
    public static ulong Mix(ulong z)
    {
        z += 0x9E3779B97F4A7C15UL;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }
}

/// <summary>
/// Tiny deterministic PRNG (SplitMix64). Every generated field gets its own stream derived from
/// (seed, stream name, nonce), so rerolling one field never disturbs the others.
/// </summary>
public sealed class SeedRng
{
    private ulong _state;

    public SeedRng(ulong seed) => _state = seed;

    /// <summary>Normalises a user seed: trimmed and case-insensitive, so "Frost" == "frost ".</summary>
    public static string NormalizeSeed(string seed) => (seed ?? "").Trim().ToLowerInvariant();

    public static SeedRng ForStream(string seed, string stream, int nonce = 0)
    {
        ulong seedHash = StableHash.Fnv1a64(NormalizeSeed(seed));
        ulong streamHash = StableHash.Mix(StableHash.Fnv1a64(stream) + (ulong)(uint)nonce * 0x9E3779B97F4A7C15UL);
        return new SeedRng(StableHash.Mix(seedHash ^ streamHash));
    }

    /// <summary>
    /// A uniform value in (0, 1] tied to one specific item within a stream. Because every candidate
    /// gets its own independent value, adding/removing/reweighting other candidates never changes it.
    /// </summary>
    public static double KeyedUniform(string seed, string stream, int nonce, string key)
    {
        var rng = ForStream(seed, stream + "\u0001" + key, nonce);
        return ((rng.NextULong() >> 11) + 1) * (1.0 / 9007199254740992.0);
    }

    /// <summary>
    /// Weighted "race" sampling (Efraimidis–Spirakis): each candidate draws key = -ln(u) / weight and the
    /// smallest key wins. Pick probability is exactly proportional to weight, but unlike a cumulative roll
    /// the winner only changes when its own odds genuinely change. That keeps partial rerolls and
    /// pack changes from reshuffling unrelated fields. Returns -1 when every weight is zero.
    /// </summary>
    public static int Race(IReadOnlyList<double> weights, Func<int, string> keyOf, string seed, string stream, int nonce)
    {
        int best = -1;
        double bestKey = double.PositiveInfinity;
        for (int i = 0; i < weights.Count; i++)
        {
            double w = weights[i];
            if (!(w > 0) || !double.IsFinite(w)) continue;
            double k = -Math.Log(KeyedUniform(seed, stream, nonce, keyOf(i))) / w;
            if (k < bestKey) { bestKey = k; best = i; }
        }
        return best;
    }

    /// <summary>Candidate indices ordered by race key (most "deserving" first); zero weights excluded.</summary>
    public static List<int> RaceOrder(IReadOnlyList<double> weights, Func<int, string> keyOf, string seed, string stream, int nonce) =>
        Enumerable.Range(0, weights.Count)
            .Where(i => weights[i] > 0 && double.IsFinite(weights[i]))
            .Select(i => (i, key: -Math.Log(KeyedUniform(seed, stream, nonce, keyOf(i))) / weights[i]))
            .OrderBy(x => x.key).ThenBy(x => x.i)
            .Select(x => x.i)
            .ToList();

    public ulong NextULong()
    {
        _state += 0x9E3779B97F4A7C15UL;
        ulong z = _state;
        z = (z ^ (z >> 30)) * 0xBF58476D1CE4E5B9UL;
        z = (z ^ (z >> 27)) * 0x94D049BB133111EBUL;
        return z ^ (z >> 31);
    }

    /// <summary>Uniform double in [0, 1).</summary>
    public double NextDouble() => (NextULong() >> 11) * (1.0 / 9007199254740992.0);

    /// <summary>Uniform int in [0, maxExclusive).</summary>
    public int NextInt(int maxExclusive)
    {
        if (maxExclusive <= 0) throw new ArgumentOutOfRangeException(nameof(maxExclusive));
        return (int)(NextULong() % (ulong)maxExclusive);
    }

    /// <summary>
    /// Weighted pick. Returns -1 when every weight is zero. Order of <paramref name="weights"/>
    /// must itself be deterministic (the library guarantees a canonical order).
    /// </summary>
    public int PickWeighted(IReadOnlyList<double> weights)
    {
        double total = 0;
        foreach (var w in weights) if (w > 0 && double.IsFinite(w)) total += w;
        if (total <= 0) return -1;
        double roll = NextDouble() * total;
        int lastPositive = -1;
        for (int i = 0; i < weights.Count; i++)
        {
            double w = weights[i];
            if (!(w > 0) || !double.IsFinite(w)) continue;
            lastPositive = i;
            roll -= w;
            if (roll < 0) return i;
        }
        return lastPositive; // floating point safety net
    }
}
