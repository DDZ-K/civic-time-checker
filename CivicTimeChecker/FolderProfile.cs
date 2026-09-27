namespace CivicTimeChecker;

internal sealed class FolderProfile
{
    public string Name { get; set; } = "新目录";
    public string MessageFolder { get; set; } = "";
    public string Role { get; set; } = Roles.Dispense;
    public string Pair { get; set; } = Pairs.Housing25;
    public int ClearGlueMinutes { get; set; } = 15;

    public bool IsBind => string.Equals(Role, Roles.Bind, StringComparison.OrdinalIgnoreCase);
    public string RoleLabel => IsBind ? "组装站" : "点胶站";
    public string PairLabel => Pair == Pairs.Cover100 ? "Cover 100" : Pair == Pairs.Housing60 ? "Housing 60" : "Housing 25";

    public FolderProfile Clone()
    {
        return new FolderProfile
        {
            Name = Name,
            MessageFolder = MessageFolder,
            Role = Role,
            Pair = Pair,
            ClearGlueMinutes = ClearGlueMinutes
        };
    }

    public void Normalize()
    {
        Name = string.IsNullOrWhiteSpace(Name) ? "未命名目录" : Name.Trim();
        MessageFolder = (MessageFolder ?? "").Trim();
        Role = Roles.Normalize(Role);
        Pair = Pairs.Normalize(Pair);
        ClearGlueMinutes = Math.Max(0, ClearGlueMinutes);
    }
}

internal static class Roles
{
    public const string Dispense = "Dispense";
    public const string Bind = "Bind";

    public static string Normalize(string? raw)
    {
        var value = (raw ?? "").Trim();
        if (value.Equals(Bind, StringComparison.OrdinalIgnoreCase)
            || value.Contains("组装", StringComparison.Ordinal)
            || value.Contains("绑定", StringComparison.Ordinal)
            || value.Equals("Assembly", StringComparison.OrdinalIgnoreCase))
            return Bind;
        return Dispense;
    }
}

internal static class Pairs
{
    public const string Housing25 = "25";
    public const string Housing60 = "60";
    public const string Cover100 = "100";

    public static readonly string[] All = [Housing25, Housing60, Cover100];

    public static string Normalize(string? raw)
    {
        var value = (raw ?? "").Trim();
        if (int.TryParse(value, out var n))
        {
            if (n is 25 or 30) return Housing25;
            if (n is 60 or 70) return Housing60;
            if (n is 100 or 110) return Cover100;
        }

        if (value.Contains("100", StringComparison.Ordinal) || value.Contains("110", StringComparison.Ordinal)
            || value.Contains("Cover", StringComparison.OrdinalIgnoreCase) || value.Contains("CSB", StringComparison.OrdinalIgnoreCase))
            return Cover100;
        if (value.Contains("60", StringComparison.Ordinal) || value.Contains("70", StringComparison.Ordinal)
            || value.Contains("BBB", StringComparison.OrdinalIgnoreCase))
            return Housing60;
        return Housing25;
    }

    public static string Display(string pair) => Normalize(pair) switch
    {
        Cover100 => "100 / Cover",
        Housing60 => "60 / Housing",
        _ => "25 / Housing"
    };
}
