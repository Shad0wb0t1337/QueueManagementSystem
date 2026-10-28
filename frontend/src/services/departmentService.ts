import { api } from "@/services/api";
import type {
  CreateDepartmentRequest,
  Department,
  ManagerDepartment,
  ManagerUser,
  UpdateDepartmentRequest,
  UpdateManagerDepartmentsRequest
} from "@/types";

export async function getDepartments(): Promise<Department[]> {
  const response = await api.get<Department[]>("/api/admin/departments");
  return response.data;
}

export async function createDepartment(request: CreateDepartmentRequest): Promise<Department> {
  const response = await api.post<Department>("/api/admin/departments", request);
  return response.data;
}

export async function updateDepartment(id: number, request: UpdateDepartmentRequest): Promise<Department> {
  const response = await api.put<Department>(`/api/admin/departments/${id}`, request);
  return response.data;
}

export async function deactivateDepartment(id: number): Promise<Department> {
  const response = await api.patch<Department>(`/api/admin/departments/${id}/deactivate`);
  return response.data;
}

export async function getManagerUsers(): Promise<ManagerUser[]> {
  const response = await api.get<ManagerUser[]>("/api/admin/managers");
  return response.data;
}

export async function updateManagerDepartments(
  userId: number,
  request: UpdateManagerDepartmentsRequest
): Promise<ManagerDepartment[]> {
  const response = await api.put<ManagerDepartment[]>(`/api/admin/managers/${userId}/departments`, request);
  return response.data;
}

export async function getMyManagerDepartments(): Promise<ManagerDepartment[]> {
  const response = await api.get<ManagerDepartment[]>("/api/manager/departments");
  return response.data;
}
