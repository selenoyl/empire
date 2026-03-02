using System.Numerics;

namespace Engine.Math;

public readonly record struct Ray(Vector3 Origin, Vector3 Direction);

public readonly record struct Aabb(Vector3 Min, Vector3 Max)
{
    public bool Intersects(Ray ray, out float distance)
    {
        var tmin = (Min.X - ray.Origin.X) / ray.Direction.X;
        var tmax = (Max.X - ray.Origin.X) / ray.Direction.X;
        if (tmin > tmax) (tmin, tmax) = (tmax, tmin);

        var tymin = (Min.Y - ray.Origin.Y) / ray.Direction.Y;
        var tymax = (Max.Y - ray.Origin.Y) / ray.Direction.Y;
        if (tymin > tymax) (tymin, tymax) = (tymax, tymin);

        if ((tmin > tymax) || (tymin > tmax)) { distance = 0; return false; }
        if (tymin > tmin) tmin = tymin;
        if (tymax < tmax) tmax = tymax;

        var tzmin = (Min.Z - ray.Origin.Z) / ray.Direction.Z;
        var tzmax = (Max.Z - ray.Origin.Z) / ray.Direction.Z;
        if (tzmin > tzmax) (tzmin, tzmax) = (tzmax, tzmin);

        if ((tmin > tzmax) || (tzmin > tmax)) { distance = 0; return false; }
        distance = tmin;
        return true;
    }
}
