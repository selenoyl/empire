using System.Text.Json;

namespace Game.CoreSim;

public static class ReplayDeserializer
{
    public static IEnumerable<ISimCommand> ParseCommands(IEnumerable<string> lines)
    {
        foreach (var json in lines)
        {
            using var doc = JsonDocument.Parse(json);
            if (!doc.RootElement.TryGetProperty("PlayerId", out _))
                continue;

            if (doc.RootElement.TryGetProperty("UnitId", out _) && doc.RootElement.TryGetProperty("Target", out _))
                yield return JsonSerializer.Deserialize<MoveUnitCommand>(json)!;
            else if (doc.RootElement.TryGetProperty("CityName", out _))
                yield return JsonSerializer.Deserialize<FoundCityCommand>(json)!;
            else if (doc.RootElement.TryGetProperty("Tech", out _))
                yield return JsonSerializer.Deserialize<SetResearchCommand>(json)!;
            else if (doc.RootElement.TryGetProperty("AttackerUnitId", out _))
                yield return JsonSerializer.Deserialize<AttackCommand>(json)!;
            else if (doc.RootElement.TryGetProperty("OtherPlayerId", out _) && doc.RootElement.TryGetProperty("State", out _))
                yield return JsonSerializer.Deserialize<SetWarPeaceCommand>(json)!;
            else if (doc.RootElement.TryGetProperty("ToPlayerId", out _))
                yield return JsonSerializer.Deserialize<ProposeTradeCommand>(json)!;
            else if (doc.RootElement.TryGetProperty("Enabled", out _))
                yield return JsonSerializer.Deserialize<AcceptOpenBordersCommand>(json)!;
            else if (doc.RootElement.TryGetProperty("Improvement", out _))
                yield return JsonSerializer.Deserialize<BuildImprovementCommand>(json)!;
            else if (doc.RootElement.TryGetProperty("Fortify", out _))
                yield return JsonSerializer.Deserialize<SetFortifyCommand>(json)!;
            else if (doc.RootElement.TryGetProperty("ItemId", out _))
                yield return JsonSerializer.Deserialize<SetCityProductionCommand>(json)!;
            else if (doc.RootElement.EnumerateObject().Count() == 1)
                yield return JsonSerializer.Deserialize<EndTurnCommand>(json)!;
        }
    }
}
