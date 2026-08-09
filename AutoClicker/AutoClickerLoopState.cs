namespace AutoClicker;

public static class AutoClickerLoopState
{
    public static bool ShouldContinue(int clicksPerformed, int? repeatCount)
    {
        if (repeatCount is null || repeatCount <= 0)
        {
            return true;
        }

        return clicksPerformed < repeatCount.Value;
    }
}
