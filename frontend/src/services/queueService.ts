import { api } from "@/services/api";
import type {
  CallNextRequest,
  QueueTicketResponse,
  RecentTicketHistoryItem,
  RecallTicketResponse,
  RedirectTicketRequest,
  RedirectTicketResponse,
  TicketCalledResponse
} from "@/types";

export async function callNext(request: CallNextRequest): Promise<TicketCalledResponse> {
  const response = await api.post<TicketCalledResponse>("/api/queue/callnext", request);
  return response.data;
}

export async function markServing(ticketId: number): Promise<QueueTicketResponse> {
  const response = await api.post<QueueTicketResponse>(`/api/queue/serving/${ticketId}`);
  return response.data;
}

export async function markDone(ticketId: number): Promise<QueueTicketResponse> {
  const response = await api.post<QueueTicketResponse>(`/api/queue/done/${ticketId}`);
  return response.data;
}

export async function markNoShow(ticketId: number): Promise<QueueTicketResponse> {
  const response = await api.post<QueueTicketResponse>(`/api/queue/noshow/${ticketId}`);
  return response.data;
}

export async function getRecentTicketHistory(counterId: number): Promise<RecentTicketHistoryItem[]> {
  const response = await api.get<RecentTicketHistoryItem[]>(`/api/queue/counters/${counterId}/recent-history`);
  return response.data;
}

export async function recallTicket(ticketId: number): Promise<RecallTicketResponse> {
  const response = await api.post<RecallTicketResponse>(`/api/queue/tickets/${ticketId}/recall`);
  return response.data;
}

export async function redirectTicket(ticketId: number, request: RedirectTicketRequest): Promise<RedirectTicketResponse> {
  const response = await api.post<RedirectTicketResponse>(`/api/queue/tickets/${ticketId}/redirect`, request);
  return response.data;
}
