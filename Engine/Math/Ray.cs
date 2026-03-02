using System.Numerics;

namespace Engine.Math;

public readonly record struct Ray(Vector3 Origin, Vector3 Direction);

public readonly record struct Aabb(Vector3 Min, Vector3 Max)
{
    public bool Intersects(Ray ray, out float distance)
    {
        distance = 0f;
        var tMin = float.NegativeInfinity;
        var tMax = float.PositiveInfinity;

        if (!IntersectAxis(ray.Origin.X, ray.Direction.X, Min.X, Max.X, ref tMin, ref tMax)) return false;
        if (!IntersectAxis(ray.Origin.Y, ray.Direction.Y, Min.Y, Max.Y, ref tMin, ref tMax)) return false;
        if (!IntersectAxis(ray.Origin.Z, ray.Direction.Z, Min.Z, Max.Z, ref tMin, ref tMax)) return false;

        if (tMax < 0f) return false;
        distance = tMin >= 0f ? tMin : tMax;
        return true;
    }

    private static bool IntersectAxis(float origin, float direction, float min, float max, ref float tMin, ref float tMax)
    {
        const float epsilon = 1e-6f;
        if (MathF.Abs(direction) < epsilon)
            return origin >= min && origin <= max;

        var t1 = (min - origin) / direction;
        var t2 = (max - origin) / direction;
        if (t1 > t2) (t1, t2) = (t2, t1);

        tMin = MathF.Max(tMin, t1);
        tMax = MathF.Min(tMax, t2);
        return tMin <= tMax;
    }
}
