import { create } from "zustand";
import type { ActiveTicketResponse, DisplayTicket, QueueTicketResponse } from "@/types";

interface QueueStoreState {
  currentQueue: QueueTicketResponse[];
  activeTickets: ActiveTicketResponse[];
  calledTickets: DisplayTicket[];
  setCurrentQueue: (queue: QueueTicketResponse[]) => void;
  setActiveTickets: (tickets: ActiveTicketResponse[]) => void;
  setCalledTickets: (tickets: DisplayTicket[]) => void;
}

export const useQueueStore = create<QueueStoreState>((set) => ({
  currentQueue: [],
  activeTickets: [],
  calledTickets: [],
  setCurrentQueue: (queue) => set({ currentQueue: queue }),
  setActiveTickets: (tickets) => set({ activeTickets: tickets }),
  setCalledTickets: (tickets) => set({ calledTickets: tickets })
}));
