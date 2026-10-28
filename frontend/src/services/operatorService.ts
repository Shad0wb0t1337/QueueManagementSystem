import { api } from "@/services/api";
import type { CreateOperatorRequest, Operator, UpdateOperatorRequest } from "@/types";

export async function getOperators(): Promise<Operator[]> {
  const response = await api.get<Operator[]>("/api/operators");
  return response.data;
}

export async function createOperator(request: CreateOperatorRequest): Promise<Operator> {
  const response = await api.post<Operator>("/api/operators", request);
  return response.data;
}

export async function updateOperator(id: number, request: UpdateOperatorRequest): Promise<Operator> {
  const response = await api.put<Operator>(`/api/operators/${id}`, request);
  return response.data;
}
