namespace Game.CoreSim;

public static class ModifierSystem
{
    public static int Apply(SimState state, int playerId, string target, string key, int value)
    {
        if (!state.Players.TryGetValue(playerId, out var p) || !state.Civs.TryGetValue(p.CivId, out var civ)) return value;
        var cur = value;
        foreach (var m in civ.Modifiers.Where(m => m.Target == target && (m.Key == "*" || m.Key == key)))
            cur = m.Operation == "multiply" ? cur * m.Value : cur + m.Value;
        return cur;
    }
}
