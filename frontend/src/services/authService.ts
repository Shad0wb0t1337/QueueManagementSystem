import { api } from "@/services/api";
import type { AuthResponse, LoginRequest } from "@/types";

export async function login(request: LoginRequest): Promise<AuthResponse> {
  const response = await api.post<AuthResponse>("/api/auth/login", request);
  return response.data;
}

export async function me(): Promise<AuthResponse> {
  const response = await api.get<AuthResponse>("/api/auth/me");
  return response.data;
}
