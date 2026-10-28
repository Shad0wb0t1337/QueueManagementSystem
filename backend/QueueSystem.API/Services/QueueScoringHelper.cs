using QueueSystem.API.Models;

namespace QueueSystem.API.Services;

public static class QueueScoringHelper
{
    public static double CalculateScore(Ticket ticket, ServiceType serviceType, DateTime now)
    {
        return CalculateScoreDetails(ticket, serviceType, now).FinalScore;
    }

    public static QueueScoreDetails CalculateScoreDetails(Ticket ticket, ServiceType serviceType, DateTime now)
    {
        double waitingMinutes = Math.Max(0, (now - ticket.QueueEnteredAt).TotalMinutes);
        double fastTrackBonus = serviceType.IsFastTrack ? 30 : 0;
        double slaBoost = CalculateSlaBoost(waitingMinutes, serviceType.MaxWaitMinutes);
        double finalScore = serviceType.PriorityLevel * 100
            + waitingMinutes * 2
            + fastTrackBonus
            - serviceType.EstimatedDurationMinutes
            + slaBoost;

        return new QueueScoreDetails
        {
            PriorityLevel = serviceType.PriorityLevel,
            WaitingMinutes = waitingMinutes,
            FastTrackBonus = fastTrackBonus,
            SlaBoost = slaBoost,
            EstimatedDurationMinutes = serviceType.EstimatedDurationMinutes,
            FinalScore = finalScore
        };
    }

    private static double CalculateSlaBoost(double waitingMinutes, int? maxWaitMinutes)
    {
        if (!maxWaitMinutes.HasValue)
        {
            return 0;
        }

        double maxWait = maxWaitMinutes.Value;
        if (waitingMinutes >= maxWait)
        {
            return 200 + (waitingMinutes - maxWait) * 10;
        }

        double warningThreshold = maxWait * 0.75;
        if (waitingMinutes >= warningThreshold)
        {
            return 50 + (waitingMinutes - warningThreshold) * 5;
        }

        return 0;
    }
}

public class QueueScoreDetails
{
    public int PriorityLevel { get; set; }
    public double WaitingMinutes { get; set; }
    public double FastTrackBonus { get; set; }
    public double SlaBoost { get; set; }
    public int EstimatedDurationMinutes { get; set; }
    public double FinalScore { get; set; }
}
