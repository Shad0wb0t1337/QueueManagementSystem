import { useState } from "react";
import { issueTicket as issueTicketRequest } from "@/services/ticketService";
import type { IssueTicketResponse } from "@/types";
import { getApiErrorMessage } from "@/utils/apiError";

export function useTickets() {
  const [issuedTicket, setIssuedTicket] = useState<IssueTicketResponse | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  const issueTicket = async (serviceTypeId: number): Promise<IssueTicketResponse> => {
    setIsLoading(true);
    setErrorMessage(null);

    try {
      const response = await issueTicketRequest({ serviceTypeId });
      setIssuedTicket(response);
      return response;
    } catch (error) {
      setErrorMessage(getApiErrorMessage(error, "Nepavyko sugeneruoti bilieto."));
      throw error;
    } finally {
      setIsLoading(false);
    }
  };

  return {
    issuedTicket,
    isLoading,
    errorMessage,
    issueTicket
  };
}
