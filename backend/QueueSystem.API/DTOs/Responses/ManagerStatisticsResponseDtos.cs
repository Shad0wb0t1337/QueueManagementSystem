namespace QueueSystem.API.DTOs.Responses;

public class ManagerStatisticsOverviewResponseDto
{
    public DateTime From { get; set; }
    public DateTime To { get; set; }
    public int IssuedCount { get; set; }
    public int CalledCount { get; set; }
    public int CompletedCount { get; set; }
    public int NoShowCount { get; set; }
    public int RedirectedCount { get; set; }
    public int RecalledCount { get; set; }
    public int RecallPendingCount { get; set; }
    public double AverageWaitMinutes { get; set; }
    public double AverageServiceMinutes { get; set; }
    public int SlaBreachCount { get; set; }
    public double SlaBreachPercent { get; set; }
}

public class ManagerServiceStatisticsResponseDto
{
    public int ServiceTypeId { get; set; }
    public string ServiceTypeName { get; set; } = string.Empty;
    public int IssuedCount { get; set; }
    public int CalledCount { get; set; }
    public int CompletedCount { get; set; }
    public int NoShowCount { get; set; }
    public int RedirectedCount { get; set; }
    public int RecalledCount { get; set; }
    public int RecallPendingCount { get; set; }
    public double AverageWaitMinutes { get; set; }
    public double AverageServiceMinutes { get; set; }
    public int SlaBreachCount { get; set; }
    public double SlaBreachPercent { get; set; }
}

public class ManagerCounterStatisticsResponseDto
{
    public int CounterId { get; set; }
    public string CounterName { get; set; } = string.Empty;
    public int CalledCount { get; set; }
    public int CompletedCount { get; set; }
    public int NoShowCount { get; set; }
    public int RedirectedCount { get; set; }
    public int RecalledCount { get; set; }
    public int RecallPendingCount { get; set; }
    public double AverageServiceMinutes { get; set; }
}

public class ManagerOperatorStatisticsResponseDto
{
    public int OperatorId { get; set; }
    public string OperatorName { get; set; } = string.Empty;
    public int CalledCount { get; set; }
    public int CompletedCount { get; set; }
    public int NoShowCount { get; set; }
    public int RedirectedCount { get; set; }
    public int RecalledCount { get; set; }
    public int RecallPendingCount { get; set; }
    public double AverageServiceMinutes { get; set; }
}

public class ManagerRedirectStatisticsResponseDto
{
    public int FromServiceTypeId { get; set; }
    public string FromServiceTypeName { get; set; } = string.Empty;
    public int ToServiceTypeId { get; set; }
    public string ToServiceTypeName { get; set; } = string.Empty;
    public int OperatorId { get; set; }
    public string OperatorName { get; set; } = string.Empty;
    public int Count { get; set; }
}

public class ManagerRecallStatisticsResponseDto
{
    public int? CounterId { get; set; }
    public string CounterName { get; set; } = string.Empty;
    public int? OperatorId { get; set; }
    public string OperatorName { get; set; } = string.Empty;
    public int RecalledCount { get; set; }
    public int RecallPendingCount { get; set; }
}

public class ManagerTicketEventResponseDto
{
    public int EventId { get; set; }
    public int TicketId { get; set; }
    public string TicketNumber { get; set; } = string.Empty;
    public string EventType { get; set; } = string.Empty;
    public string EventLabel { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
    public int? CounterId { get; set; }
    public string CounterName { get; set; } = string.Empty;
    public int? OperatorId { get; set; }
    public string OperatorName { get; set; } = string.Empty;
    public int ServiceTypeId { get; set; }
    public string ServiceTypeName { get; set; } = string.Empty;
    public string? FromStatus { get; set; }
    public string? ToStatus { get; set; }
    public string FromServiceTypeName { get; set; } = string.Empty;
    public string ToServiceTypeName { get; set; } = string.Empty;
    public string Details { get; set; } = string.Empty;
}
