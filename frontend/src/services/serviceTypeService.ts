import { api } from "@/services/api";
import type { CreateServiceTypeRequest, ServiceType, UpdateServiceTypeRequest } from "@/types";

export async function getServiceTypes(includeInactive = false): Promise<ServiceType[]> {
  const response = await api.get<ServiceType[]>("/api/servicetypes", {
    params: { includeInactive }
  });
  return response.data;
}

export async function createServiceType(request: CreateServiceTypeRequest): Promise<ServiceType> {
  const response = await api.post<ServiceType>("/api/servicetypes", request);
  return response.data;
}

export async function updateServiceType(id: number, request: UpdateServiceTypeRequest): Promise<ServiceType> {
  const response = await api.put<ServiceType>(`/api/servicetypes/${id}`, request);
  return response.data;
}

export async function deleteServiceType(id: number): Promise<void> {
  await api.delete(`/api/servicetypes/${id}`);
}
