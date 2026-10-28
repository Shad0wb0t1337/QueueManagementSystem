namespace QueueSystem.API.Constants;

public static class SignalRGroups
{
    public const string Display = "display";

    public static string Service(int serviceTypeId)
    {
        return $"service-{serviceTypeId}";
    }

    public static string Counter(int counterId)
    {
        return $"counter-{counterId}";
    }
}
