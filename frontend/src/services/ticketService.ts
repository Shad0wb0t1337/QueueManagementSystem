import { api } from "@/services/api";
import type { ActiveTicketResponse, IssueTicketRequest, IssueTicketResponse, QueueStats, QueueTicketResponse } from "@/types";

export async function issueTicket(request: IssueTicketRequest): Promise<IssueTicketResponse> {
  const response = await api.post<IssueTicketResponse>("/api/tickets", request);
  return response.data;
}

export async function getQueue(serviceTypeId: number): Promise<QueueTicketResponse[]> {
  const response = await api.get<QueueTicketResponse[]>(`/api/tickets/queue/${serviceTypeId}`);
  return response.data;
}

export async function getCounterQueue(counterId: number): Promise<QueueTicketResponse[]> {
  const response = await api.get<QueueTicketResponse[]>(`/api/tickets/queue/counter/${counterId}`);
  return response.data;
}

export async function getActiveTickets(): Promise<ActiveTicketResponse[]> {
  const response = await api.get<ActiveTicketResponse[]>("/api/tickets/active");
  return response.data;
}

export async function getTodayStats(): Promise<QueueStats> {
  const response = await api.get<QueueStats>("/api/tickets/stats/today");
  return response.data;
}
