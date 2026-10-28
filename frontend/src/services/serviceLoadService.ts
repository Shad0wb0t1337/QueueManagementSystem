import { api } from "@/services/api";
import type { ServiceLoadSummary } from "@/types";

export async function getServiceLoad(): Promise<ServiceLoadSummary[]> {
  const response = await api.get<ServiceLoadSummary[]>("/api/service-load");
  return response.data;
}

export async function getOperatorServiceLoad(counterId: number): Promise<ServiceLoadSummary[]> {
  const response = await api.get<ServiceLoadSummary[]>(`/api/service-load/operator/${counterId}`);
  return response.data;
}
