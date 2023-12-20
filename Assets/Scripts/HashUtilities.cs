using Unity.Mathematics;

public static class HashUtilities
{
    public static int3 Quantize(float3 position, float size)
    {
        return new int3((int)math.floor(position.x / size), (int)math.floor(position.y / size),
            (int)math.floor(position.z / size));
    }

    // FNV-1 hash https://en.wikipedia.org/wiki/Fowler%E2%80%93Noll%E2%80%93Vo_hash_function 
    public static uint Hash(int3 p)
    {
        var hash = 2166136261u;

        for (var i = 0; i < 3; i++)
        {
            for (var j = 0; j < sizeof(int); j++)
            {
                var b = (byte)(p[i] >> (j * 8));
                hash *= 16777619u;
                hash ^= b;
            }
        }

        return hash;
    }
}