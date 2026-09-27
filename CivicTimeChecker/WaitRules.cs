namespace CivicTimeChecker;

internal static class WaitRules
{
    public static bool CanClearGlue(TimeSpan elapsed, int clearGlueMinutes)
        => elapsed.TotalMinutes >= Math.Max(0, clearGlueMinutes);
}
