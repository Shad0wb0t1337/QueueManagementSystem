import { useCallback, useEffect, useRef, useState } from "react";
import { getActiveTickets, getCounterQueue, getQueue } from "@/services/ticketService";
import { useQueueStore } from "@/store/queueStore";
import { useSignalR } from "@/hooks/useSignalR";
import type { DisplayUpdatedEvent, QueueUpdatedEvent, TicketCalledEvent } from "@/types";
import { getApiErrorMessage } from "@/utils/apiError";

const EMPTY_SERVICE_TYPE_IDS: number[] = [];

interface UseQueueOptions {
  serviceTypeId?: number | null;
  serviceTypeIds?: number[];
  counterId?: number | null;
  includeActive?: boolean;
  joinDisplay?: boolean;
  onQueueUpdated?: (event: QueueUpdatedEvent) => void;
}

export function useQueue(options: UseQueueOptions = {}) {
  const {
    serviceTypeId = null,
    serviceTypeIds = EMPTY_SERVICE_TYPE_IDS,
    counterId = null,
    includeActive = false,
    joinDisplay = includeActive,
    onQueueUpdated
  } = options;
  const serviceTypeIdsKey = serviceTypeIds.join(",");
  const [isLoadingQueue, setIsLoadingQueue] = useState(Boolean(serviceTypeId || counterId));
  const [queueError, setQueueError] = useState<string | null>(null);
  const [isLoadingActive, setIsLoadingActive] = useState(includeActive);
  const [activeError, setActiveError] = useState<string | null>(null);
  const [highlightedTicketNumbers, setHighlightedTicketNumbers] = useState<string[]>([]);
  const highlightTimeoutsRef = useRef<Record<string, number>>({});
  const onQueueUpdatedRef = useRef<typeof onQueueUpdated>(onQueueUpdated);
  const serviceTypeIdsRef = useRef<number[]>(serviceTypeIds);
  const refreshQueueInFlightRef = useRef<Promise<void> | null>(null);
  const refreshActiveInFlightRef = useRef<Promise<void> | null>(null);

  const currentQueue = useQueueStore((state) => state.currentQueue);
  const activeTickets = useQueueStore((state) => state.activeTickets);
  const calledTickets = useQueueStore((state) => state.calledTickets);
  const setCurrentQueue = useQueueStore((state) => state.setCurrentQueue);
  const setActiveTickets = useQueueStore((state) => state.setActiveTickets);
  const setCalledTickets = useQueueStore((state) => state.setCalledTickets);

  const { connection, status } = useSignalR({
    joinDisplay,
    serviceTypeId,
    serviceTypeIds
  });

  useEffect(() => {
    onQueueUpdatedRef.current = onQueueUpdated;
  }, [onQueueUpdated]);

  useEffect(() => {
    serviceTypeIdsRef.current = serviceTypeIds;
  }, [serviceTypeIdsKey, serviceTypeIds]);

  const refreshQueue = useCallback(async () => {
    if (!serviceTypeId && !counterId) {
      setCurrentQueue([]);
      setIsLoadingQueue(false);
      setQueueError(null);
      return;
    }

    if (refreshQueueInFlightRef.current) {
      return refreshQueueInFlightRef.current;
    }

    const request = (async () => {
      setIsLoadingQueue(true);
      setQueueError(null);

      try {
        const queue = counterId ? await getCounterQueue(counterId) : await getQueue(serviceTypeId!);
        setCurrentQueue(queue);
      } catch (error) {
        setQueueError(getApiErrorMessage(error, "Nepavyko įkelti eilės."));
      } finally {
        setIsLoadingQueue(false);
        refreshQueueInFlightRef.current = null;
      }
    })();

    refreshQueueInFlightRef.current = request;
    return request;
  }, [counterId, serviceTypeId, setCurrentQueue]);

  const refreshActive = useCallback(async () => {
    if (!includeActive) {
      return;
    }

    if (refreshActiveInFlightRef.current) {
      return refreshActiveInFlightRef.current;
    }

    const request = (async () => {
      setIsLoadingActive(true);
      setActiveError(null);

      try {
        const tickets = await getActiveTickets();
        setActiveTickets(tickets);
        setCalledTickets(tickets);
      } catch (error) {
        setActiveError(getApiErrorMessage(error, "Nepavyko įkelti aktyvių bilietų."));
      } finally {
        setIsLoadingActive(false);
        refreshActiveInFlightRef.current = null;
      }
    })();

    refreshActiveInFlightRef.current = request;
    return request;
  }, [includeActive, setActiveTickets, setCalledTickets]);

  useEffect(() => {
    void refreshQueue();
    void refreshActive();
  }, [refreshActive, refreshQueue]);

  useEffect(() => {
    if (!connection) {
      return;
    }

    const onQueueUpdated = (event: QueueUpdatedEvent) => {
      const currentServiceTypeIds = serviceTypeIdsRef.current;
      const shouldRefreshQueue = counterId
        ? currentServiceTypeIds.includes(event.serviceTypeId)
        : serviceTypeId
          ? event.serviceTypeId === serviceTypeId
          : false;

      if (shouldRefreshQueue) {
        void refreshQueue();
      }

      if (includeActive && (!counterId || shouldRefreshQueue)) {
        void refreshActive();
      }

      if (shouldRefreshQueue) {
        onQueueUpdatedRef.current?.(event);
      }
    };

    const onDisplayUpdated = (event: DisplayUpdatedEvent) => {
      setCalledTickets(event.calledTickets);
      setActiveTickets(event.calledTickets);
    };

    const onTicketCalled = (event: TicketCalledEvent) => {
      setHighlightedTicketNumbers((previous) => {
        if (previous.includes(event.ticketNumber)) {
          return previous;
        }

        return [...previous, event.ticketNumber];
      });

      const existingTimeout = highlightTimeoutsRef.current[event.ticketNumber];
      if (existingTimeout) {
        window.clearTimeout(existingTimeout);
      }

      highlightTimeoutsRef.current[event.ticketNumber] = window.setTimeout(() => {
        setHighlightedTicketNumbers((previous) =>
          previous.filter((ticketNumber) => ticketNumber !== event.ticketNumber)
        );
        delete highlightTimeoutsRef.current[event.ticketNumber];
      }, 2500);
    };

    connection.on("QueueUpdated", onQueueUpdated);
    connection.on("DisplayUpdated", onDisplayUpdated);
    connection.on("TicketCalled", onTicketCalled);

    return () => {
      connection.off("QueueUpdated", onQueueUpdated);
      connection.off("DisplayUpdated", onDisplayUpdated);
      connection.off("TicketCalled", onTicketCalled);
    };
  }, [connection, counterId, includeActive, refreshActive, refreshQueue, serviceTypeId, setActiveTickets, setCalledTickets]);

  useEffect(() => {
    return () => {
      Object.values(highlightTimeoutsRef.current).forEach((timeoutId) => {
        window.clearTimeout(timeoutId);
      });
    };
  }, []);

  return {
    currentQueue,
    activeTickets,
    calledTickets,
    isLoadingQueue,
    queueError,
    isLoadingActive,
    activeError,
    highlightedTicketNumbers,
    status,
    refreshQueue,
    refreshActive
  };
}
