using System.Numerics;

namespace Engine.Rendering;

public static class ProceduralMeshes
{
    public static (List<Vertex> Vertices, List<uint> Indices) Cube(float size = 1f)
    {
        var s = size / 2f;
        var v = new List<Vertex>
        {
            new(new(-s,-s,-s), -Vector3.UnitZ), new(new(s,-s,-s), -Vector3.UnitZ), new(new(s,s,-s), -Vector3.UnitZ), new(new(-s,s,-s), -Vector3.UnitZ),
            new(new(-s,-s,s), Vector3.UnitZ), new(new(s,-s,s), Vector3.UnitZ), new(new(s,s,s), Vector3.UnitZ), new(new(-s,s,s), Vector3.UnitZ)
        };
        var i = new List<uint>{0,1,2,2,3,0,4,5,6,6,7,4,0,4,7,7,3,0,1,5,6,6,2,1,3,2,6,6,7,3,0,1,5,5,4,0};
        return (v, i);
    }

    public static (List<Vertex>, List<uint>) Plane(float size = 20f)
    {
        var s = size / 2f;
        return (
            [new(new(-s,0,-s), Vector3.UnitY), new(new(s,0,-s), Vector3.UnitY), new(new(s,0,s), Vector3.UnitY), new(new(-s,0,s), Vector3.UnitY)],
            [0,1,2,2,3,0]);
    }

    public static (List<Vertex>, List<uint>) HexPrism(float r = 0.6f, float h = 1f)
    {
        var v = new List<Vertex>();
        var i = new List<uint>();
        for (int k = 0; k < 6; k++)
        {
            var a = k / 6f * MathF.PI * 2;
            v.Add(new(new(MathF.Cos(a)*r, -h/2, MathF.Sin(a)*r), Vector3.UnitY));
            v.Add(new(new(MathF.Cos(a)*r, h/2, MathF.Sin(a)*r), Vector3.UnitY));
        }
        for (uint k = 2; k < 10; k += 2) { i.Add(0); i.Add(k); i.Add(k+2 > 10 ? 2u : k+2); }
        for (uint k = 3; k < 11; k += 2) { i.Add(1); i.Add(k+2 > 11 ? 3u : k+2); i.Add(k); }
        return (v, i);
    }

    public static (List<Vertex>, List<uint>) Marker() => Cube(0.2f);
}
