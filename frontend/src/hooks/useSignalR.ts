import { useEffect, useState } from "react";
import * as signalR from "@microsoft/signalr";

export type SignalRConnectionStatus = "idle" | "connecting" | "connected" | "disconnected" | "error";

interface UseSignalROptions {
  autoConnect?: boolean;
  joinDisplay?: boolean;
  serviceTypeId?: number | null;
  serviceTypeIds?: number[];
  counterId?: number | null;
}

const hubUrl = import.meta.env.VITE_HUB_URL;
const EMPTY_SERVICE_TYPE_IDS: number[] = [];

export function useSignalR(options: UseSignalROptions = {}) {
  const {
    autoConnect = true,
    joinDisplay = false,
    serviceTypeId = null,
    serviceTypeIds = EMPTY_SERVICE_TYPE_IDS,
    counterId = null
  } = options;
  const joinedServiceTypeIds = Array.from(new Set(serviceTypeId ? [serviceTypeId, ...serviceTypeIds] : serviceTypeIds));
  const serviceTypeIdsKey = joinedServiceTypeIds.join(",");
  const [connection, setConnection] = useState<signalR.HubConnection | null>(null);
  const [status, setStatus] = useState<SignalRConnectionStatus>("idle");

  useEffect(() => {
    if (!autoConnect) {
      return;
    }

    const hubConnection = new signalR.HubConnectionBuilder()
      .withUrl(hubUrl, {
        accessTokenFactory: () => localStorage.getItem("auth_token") ?? ""
      })
      .withAutomaticReconnect()
      .build();

    let isMounted = true;
    setConnection(hubConnection);

    const startConnection = async () => {
      try {
        setStatus("connecting");
        await hubConnection.start();

        if (joinDisplay) {
          await hubConnection.invoke("JoinDisplay");
        }

        const serviceIdsToJoin = serviceTypeIdsKey
          .split(",")
          .map((serviceTypeIdRaw) => Number.parseInt(serviceTypeIdRaw, 10))
          .filter((serviceTypeIdValue) => !Number.isNaN(serviceTypeIdValue));

        for (const joinedServiceTypeId of serviceIdsToJoin) {
          await hubConnection.invoke("JoinService", joinedServiceTypeId);
        }

        if (counterId != null) {
          await hubConnection.invoke("JoinCounter", counterId);
        }

        if (isMounted) {
          setStatus("connected");
        }
      } catch {
        if (isMounted) {
          setStatus("error");
        }
      }
    };

    hubConnection.onclose(() => {
      if (isMounted) {
        setStatus("disconnected");
      }
    });

    void startConnection();

    return () => {
      isMounted = false;
      void hubConnection.stop();
    };
  }, [autoConnect, counterId, joinDisplay, serviceTypeIdsKey]);

  return { connection, status };
}
