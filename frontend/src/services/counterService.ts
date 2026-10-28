import { api } from "@/services/api";
import type {
  Counter,
  CounterServiceProfileItem,
  CreateCounterRequest,
  HelpMostLoadedServiceResponse,
  UpdateCounterRequest,
  UpdateCounterServiceProfileRequest,
  UpdateOwnCounterStatusRequest
} from "@/types";

export async function getCounters(): Promise<Counter[]> {
  const response = await api.get<Counter[]>("/api/counters");
  return response.data;
}

export async function createCounter(request: CreateCounterRequest): Promise<Counter> {
  const response = await api.post<Counter>("/api/counters", request);
  return response.data;
}

export async function updateCounter(id: number, request: UpdateCounterRequest): Promise<Counter> {
  const response = await api.put<Counter>(`/api/counters/${id}`, request);
  return response.data;
}

export async function updateOwnCounterStatus(request: UpdateOwnCounterStatusRequest): Promise<Counter> {
  const response = await api.put<Counter>("/api/counters/my/status", request);
  return response.data;
}

export async function getCounterServiceProfile(counterId: number): Promise<CounterServiceProfileItem[]> {
  const response = await api.get<CounterServiceProfileItem[]>(`/api/counters/${counterId}/service-profile`);
  return response.data;
}

export async function updateCounterServiceProfile(
  counterId: number,
  request: UpdateCounterServiceProfileRequest
): Promise<CounterServiceProfileItem[]> {
  const response = await api.put<CounterServiceProfileItem[]>(`/api/counters/${counterId}/service-profile`, request);
  return response.data;
}

export async function helpMostLoadedService(counterId: number): Promise<HelpMostLoadedServiceResponse> {
  const response = await api.post<HelpMostLoadedServiceResponse>(`/api/counters/${counterId}/service-profile/help-most-loaded`);
  return response.data;
}
