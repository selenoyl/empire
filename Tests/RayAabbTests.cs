using Engine.Math;
using System.Numerics;

namespace Tests;

public class RayAabbTests
{
    [Fact]
    public void RayHitsAabb()
    {
        var aabb = new Aabb(new Vector3(-1), new Vector3(1));
        var ray = new Ray(new Vector3(0, 0, -5), Vector3.UnitZ);
        Assert.True(aabb.Intersects(ray, out var distance));
        Assert.True(distance > 0);
    }
}
