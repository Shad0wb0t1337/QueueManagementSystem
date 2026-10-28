import { useCallback, useEffect, useMemo, useState } from "react";
import { useParams } from "react-router-dom";
import { LoadingSpinner } from "@/components/shared/LoadingSpinner";
import { useSignalR } from "@/hooks/useSignalR";
import { getCounterDisplayState } from "@/services/displayService";
import type { CounterDisplayState, CounterDisplayUpdatedEvent } from "@/types";
import { getApiErrorMessage } from "@/utils/apiError";

function formatClock(date: Date): string {
  return date.toLocaleTimeString("lt-LT", {
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
    hour12: false
  });
}

export function CounterDisplayPage(): JSX.Element {
  const { counterId: counterIdRaw } = useParams<{ counterId: string }>();
  const counterId = useMemo<number | null>(() => {
    const parsed = Number.parseInt(counterIdRaw ?? "", 10);
    return Number.isNaN(parsed) || parsed <= 0 ? null : parsed;
  }, [counterIdRaw]);

  const [displayState, setDisplayState] = useState<CounterDisplayState | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const [clock, setClock] = useState<string>(() => formatClock(new Date()));
  const { connection } = useSignalR({ counterId });

  const loadDisplayState = useCallback(async (): Promise<void> => {
    if (counterId == null) {
      setDisplayState(null);
      setErrorMessage("Langelis nerastas");
      setIsLoading(false);
      return;
    }

    setIsLoading(true);
    setErrorMessage(null);

    try {
      const response = await getCounterDisplayState(counterId);
      setDisplayState(response);
    } catch (error) {
      setErrorMessage(getApiErrorMessage(error, "Nepavyko įkelti langelio ekrano."));
      setDisplayState(null);
    } finally {
      setIsLoading(false);
    }
  }, [counterId]);

  useEffect(() => {
    void loadDisplayState();
  }, [loadDisplayState]);

  useEffect(() => {
    const intervalId = window.setInterval(() => {
      setClock(formatClock(new Date()));
    }, 1000);

    return () => {
      window.clearInterval(intervalId);
    };
  }, []);

  useEffect(() => {
    if (!connection || counterId == null) {
      return;
    }

    const onCounterDisplayUpdated = (event: CounterDisplayUpdatedEvent) => {
      if (event.counterId === counterId) {
        void loadDisplayState();
      }
    };

    connection.on("CounterDisplayUpdated", onCounterDisplayUpdated);

    return () => {
      connection.off("CounterDisplayUpdated", onCounterDisplayUpdated);
    };
  }, [connection, counterId, loadDisplayState]);

  if (isLoading) {
    return (
      <main className="flex min-h-screen items-center justify-center bg-zinc-950 text-zinc-100">
        <div className="rounded-md border border-zinc-800 bg-zinc-900 p-8">
          <LoadingSpinner />
          <p className="mt-4 text-center text-base text-zinc-300">Kraunamas langelio ekranas...</p>
        </div>
      </main>
    );
  }

  if (errorMessage || !displayState) {
    return (
      <main className="flex min-h-screen items-center justify-center bg-zinc-950 px-6 text-center text-zinc-100">
        <section className="w-full max-w-3xl rounded-md border border-red-500/40 bg-zinc-900 p-10">
          <p className="text-5xl font-semibold text-red-400">Langelis nerastas</p>
          {errorMessage && errorMessage !== "Langelis nerastas" && <p className="mt-4 text-xl text-zinc-300">{errorMessage}</p>}
        </section>
      </main>
    );
  }

  const hasCurrentTicket = displayState.currentTicketNumber != null;
  const isCalled = displayState.currentTicketStatus === "Called";
  const isServing = displayState.currentTicketStatus === "Serving";

  return (
    <main className="flex h-screen flex-col overflow-hidden bg-zinc-950 p-3 text-zinc-100 sm:p-6">
      <header className="flex shrink-0 flex-wrap items-start justify-between gap-3 rounded-md border border-zinc-800 bg-zinc-900 px-4 py-3 sm:px-6 sm:py-4">
        <div className="min-w-0">
          <h1 className="truncate text-3xl font-semibold text-zinc-100 md:text-5xl">{displayState.counterName}</h1>
        </div>
        <div className="shrink-0 rounded-md border border-zinc-700 bg-zinc-800 px-3 py-1.5 font-mono text-2xl font-bold text-amber-400 md:px-4 md:py-2 md:text-3xl">
          {clock}
        </div>
      </header>

      <section className="flex min-h-0 flex-1 items-center justify-center py-3 sm:py-8">
        <div className="max-h-full w-full max-w-6xl overflow-hidden rounded-md border border-zinc-800 bg-zinc-900 p-4 text-center sm:p-8">
          {displayState.counterStatus === "Paused" && (
            <p className="py-12 text-5xl font-semibold text-amber-400 md:py-24 md:text-8xl">Laikinai nedirba</p>
          )}

          {displayState.counterStatus === "Closed" && (
            <p className="py-12 text-6xl font-semibold text-red-400 md:py-24 md:text-9xl">Uždaryta</p>
          )}

          {displayState.counterStatus === "Active" && !hasCurrentTicket && (
            <div className="py-10 md:py-20">
              <p className="text-5xl font-semibold text-zinc-100 md:text-8xl">Langelis laisvas</p>
              <p className="mt-8 text-3xl text-zinc-400 md:text-4xl">Prašome laukti iškvietimo</p>
            </div>
          )}

          {displayState.counterStatus === "Active" && hasCurrentTicket && (
            <div>
              <p className={isServing ? "text-3xl font-semibold tracking-[0.25em] text-emerald-400 md:text-4xl md:tracking-[0.35em]" : "text-3xl font-semibold tracking-[0.25em] text-amber-400 md:text-4xl md:tracking-[0.35em]"}>
                {isServing ? "APTARNAUJAMAS" : isCalled ? "KVIEČIAMAS" : displayState.currentTicketStatus}
              </p>
              <p className="mt-6 font-mono text-[clamp(6rem,18vw,18rem)] font-bold leading-none text-amber-400 md:mt-10">
                {displayState.currentTicketNumber}
              </p>
              {displayState.serviceTypeName && (
                <p className="mt-5 text-3xl font-semibold text-zinc-100 md:mt-8 md:text-5xl">{displayState.serviceTypeName}</p>
              )}
            </div>
          )}
        </div>
      </section>
    </main>
  );
}
