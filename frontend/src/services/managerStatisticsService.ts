import { api } from "@/services/api";
import type {
  ManagerCounterStatistics,
  ManagerOperatorStatistics,
  ManagerOverviewStatistics,
  ManagerRecallStatistics,
  ManagerRedirectStatistics,
  ManagerServiceStatistics,
  ManagerStatisticsDashboardData,
  ManagerTicketEvent
} from "@/types";

interface DateRangeParams {
  from: string;
  to: string;
  departmentId?: number | null;
  limit?: number;
}

function createParams(dateRange: DateRangeParams): URLSearchParams {
  const params = new URLSearchParams();
  params.set("from", dateRange.from);
  params.set("to", dateRange.to);
  if (dateRange.departmentId != null) {
    params.set("departmentId", String(dateRange.departmentId));
  }
  if (dateRange.limit != null) {
    params.set("limit", String(dateRange.limit));
  }
  return params;
}

export async function getManagerOverview(dateRange: DateRangeParams): Promise<ManagerOverviewStatistics> {
  const response = await api.get<ManagerOverviewStatistics>("/api/manager/statistics/overview", {
    params: createParams(dateRange)
  });
  return response.data;
}

export async function getManagerServiceStatistics(dateRange: DateRangeParams): Promise<ManagerServiceStatistics[]> {
  const response = await api.get<ManagerServiceStatistics[]>("/api/manager/statistics/services", {
    params: createParams(dateRange)
  });
  return response.data;
}

export async function getManagerCounterStatistics(dateRange: DateRangeParams): Promise<ManagerCounterStatistics[]> {
  const response = await api.get<ManagerCounterStatistics[]>("/api/manager/statistics/counters", {
    params: createParams(dateRange)
  });
  return response.data;
}

export async function getManagerOperatorStatistics(dateRange: DateRangeParams): Promise<ManagerOperatorStatistics[]> {
  const response = await api.get<ManagerOperatorStatistics[]>("/api/manager/statistics/operators", {
    params: createParams(dateRange)
  });
  return response.data;
}

export async function getManagerRedirectStatistics(dateRange: DateRangeParams): Promise<ManagerRedirectStatistics[]> {
  const response = await api.get<ManagerRedirectStatistics[]>("/api/manager/statistics/redirects", {
    params: createParams(dateRange)
  });
  return response.data;
}

export async function getManagerRecallStatistics(dateRange: DateRangeParams): Promise<ManagerRecallStatistics[]> {
  const response = await api.get<ManagerRecallStatistics[]>("/api/manager/statistics/recalls", {
    params: createParams(dateRange)
  });
  return response.data;
}

export async function getManagerTicketEvents(dateRange: DateRangeParams): Promise<ManagerTicketEvent[]> {
  const response = await api.get<ManagerTicketEvent[]>("/api/manager/statistics/events", {
    params: createParams(dateRange)
  });
  return response.data;
}

export async function getManagerDashboardData(dateRange: DateRangeParams): Promise<ManagerStatisticsDashboardData> {
  const [overview, services, counters, operators, redirects, recalls, events] = await Promise.all([
    getManagerOverview(dateRange),
    getManagerServiceStatistics(dateRange),
    getManagerCounterStatistics(dateRange),
    getManagerOperatorStatistics(dateRange),
    getManagerRedirectStatistics(dateRange),
    getManagerRecallStatistics(dateRange),
    getManagerTicketEvents({ ...dateRange, limit: 50 })
  ]);

  return {
    overview,
    services,
    counters,
    operators,
    redirects,
    recalls,
    events
  };
}
