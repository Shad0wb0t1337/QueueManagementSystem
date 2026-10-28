import { api } from "@/services/api";
import type { CounterDisplayGrid, CounterDisplayState, DisplayOverview } from "@/types";

export async function getDisplayOverview(): Promise<DisplayOverview> {
  const response = await api.get<DisplayOverview>("/api/display/overview");
  return response.data;
}

export interface DisplayCounterRange {
  from: number;
  to: number;
}

export async function getDisplayCounters(range?: DisplayCounterRange): Promise<CounterDisplayGrid> {
  const response = await api.get<CounterDisplayGrid>("/api/display/counters", {
    params: range
  });
  return response.data;
}

export async function getCounterDisplayState(counterId: number): Promise<CounterDisplayState> {
  const response = await api.get<CounterDisplayState>(`/api/display/counter/${counterId}`);
  return response.data;
}
