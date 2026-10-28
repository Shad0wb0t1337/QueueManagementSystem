import { useCallback, useEffect, useState } from "react";
import { LoadingSpinner } from "@/components/shared/LoadingSpinner";
import { useSignalR } from "@/hooks/useSignalR";
import { cn } from "@/lib/utils";
import { getDisplayOverview } from "@/services/displayService";
import type { DisplayOverview, DisplayOverviewTicket, DisplayUpdatedEvent, QueueUpdatedEvent } from "@/types";
import { getApiErrorMessage } from "@/utils/apiError";

const VisibleHistoryLimit = 6;

function formatClock(date: Date): string {
  return date.toLocaleTimeString("lt-LT", {
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
    hour12: false
  });
}

function formatTime(dateRaw: string | null): string {
  if (!dateRaw) {
    return "";
  }

  return new Date(dateRaw).toLocaleTimeString("lt-LT", {
    hour: "2-digit",
    minute: "2-digit",
    hour12: false
  });
}

function getStatusLabel(status: string): string {
  if (status === "Called") {
    return "Kviečiamas";
  }

  if (status === "Serving") {
    return "Aptarnaujamas";
  }

  if (status === "Done") {
    return "Aptarnautas";
  }

  if (status === "NoShow") {
    return "Neatvyko";
  }

  return status;
}

function getActiveStatusClassName(status: string): string {
  if (status === "Called") {
    return "border-amber-400/70 bg-amber-400/15 text-amber-300";
  }

  return "border-emerald-500/50 bg-emerald-500/15 text-emerald-300";
}

function getHistoryStatusClassName(status: string): string {
  if (status === "NoShow") {
    return "border-red-500/40 bg-red-500/15 text-red-300";
  }

  return "border-zinc-700 bg-zinc-800 text-zinc-300";
}

function DisplayTicketCard({ ticket }: { ticket: DisplayOverviewTicket }): JSX.Element {
  return (
    <article className="flex min-h-0 min-w-0 flex-col justify-center overflow-hidden rounded-md border border-zinc-800 bg-zinc-900 p-3 xl:p-4">
      <div className="flex min-w-0 items-center justify-between gap-3">
        <p className="min-w-0 font-mono text-[clamp(2.4rem,4.8vw,5.75rem)] font-bold leading-[1.08] text-amber-400">{ticket.ticketNumber}</p>
        <span className={cn("shrink-0 rounded-md border px-2 py-1 text-sm font-semibold xl:text-base", getActiveStatusClassName(ticket.ticketStatus))}>
          {getStatusLabel(ticket.ticketStatus)}
        </span>
      </div>
      <p className="mt-1 truncate text-xl font-semibold leading-snug text-zinc-100 xl:mt-2 xl:text-3xl">{ticket.counterName}</p>
      <p className="truncate text-base leading-snug text-zinc-400 xl:mt-1 xl:text-xl">{ticket.serviceTypeName}</p>
    </article>
  );
}

function HistoryTicketRow({ ticket }: { ticket: DisplayOverviewTicket }): JSX.Element {
  return (
    <article className="flex min-h-0 min-w-0 flex-col justify-center overflow-hidden rounded-md border border-zinc-800 bg-zinc-900 px-3 py-1.5">
      <div className="flex min-w-0 items-center justify-between gap-3">
        <p className="min-w-0 font-mono text-[clamp(1.55rem,3.6vh,2.35rem)] font-bold leading-[1.12] text-zinc-100">{ticket.ticketNumber}</p>
        <span className={cn("shrink-0 rounded-md border px-2 py-0.5 text-[clamp(0.78rem,1.8vh,0.95rem)] font-semibold leading-snug", getHistoryStatusClassName(ticket.ticketStatus))}>
          {getStatusLabel(ticket.ticketStatus)}
        </span>
      </div>
      <div className="mt-1 flex min-w-0 items-end justify-between gap-3">
        <div className="min-w-0">
          <p className="truncate text-[clamp(0.95rem,2.2vh,1.15rem)] font-semibold leading-snug text-zinc-200">{ticket.counterName}</p>
          <p className="truncate text-[clamp(0.82rem,1.8vh,0.95rem)] leading-snug text-zinc-400">{ticket.serviceTypeName}</p>
        </div>
        {ticket.completedAt && <p className="shrink-0 text-[clamp(0.82rem,1.8vh,0.95rem)] leading-snug text-zinc-500">{formatTime(ticket.completedAt)}</p>}
      </div>
    </article>
  );
}

export function DisplayPage(): JSX.Element {
  const [clock, setClock] = useState<string>(() => formatClock(new Date()));
  const [overview, setOverview] = useState<DisplayOverview | null>(null);
  const [isLoading, setIsLoading] = useState(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const { connection } = useSignalR({ joinDisplay: true });
  const visibleHistory = overview?.recentHistory.slice(0, VisibleHistoryLimit) ?? [];

  const loadOverview = useCallback(async (): Promise<void> => {
    setErrorMessage(null);

    try {
      const response = await getDisplayOverview();
      setOverview(response);
    } catch (error) {
      setErrorMessage(getApiErrorMessage(error, "Nepavyko įkelti iškvietimų lentos."));
    } finally {
      setIsLoading(false);
    }
  }, []);

  useEffect(() => {
    void loadOverview();
  }, [loadOverview]);

  useEffect(() => {
    const intervalId = window.setInterval(() => {
      setClock(formatClock(new Date()));
    }, 1000);

    return () => {
      window.clearInterval(intervalId);
    };
  }, []);

  useEffect(() => {
    if (!connection) {
      return;
    }

    const refreshDisplay = (_event: DisplayUpdatedEvent | QueueUpdatedEvent) => {
      void loadOverview();
    };

    connection.on("DisplayUpdated", refreshDisplay);
    connection.on("QueueUpdated", refreshDisplay);

    return () => {
      connection.off("DisplayUpdated", refreshDisplay);
      connection.off("QueueUpdated", refreshDisplay);
    };
  }, [connection, loadOverview]);

  return (
    <main className="h-screen overflow-hidden bg-zinc-950 p-2 text-zinc-100 xl:p-3">
      <div className="mx-auto flex h-full w-full max-w-[1800px] flex-col">
        {isLoading && (
          <section className="flex flex-1 flex-col items-center justify-center rounded-md border border-zinc-800 bg-zinc-900">
            <LoadingSpinner />
            <p className="mt-4 text-xl text-zinc-300">Kraunami iškvietimai...</p>
          </section>
        )}

        {!isLoading && errorMessage && (
          <section className="flex flex-1 flex-col items-center justify-center rounded-md border border-red-500/40 bg-zinc-900 p-6 text-center">
            <p className="text-3xl text-red-400">Nepavyko įkelti iškvietimų lentos.</p>
            <p className="mt-2 text-xl text-zinc-400">{errorMessage}</p>
          </section>
        )}

        {!isLoading && !errorMessage && overview && (
          <section className="grid min-h-0 flex-1 grid-cols-1 gap-3 overflow-hidden lg:grid-cols-[minmax(0,1.35fr)_minmax(320px,0.65fr)] xl:grid-cols-[minmax(0,1.35fr)_minmax(420px,0.65fr)]">
            <div className="flex min-h-0 flex-col rounded-md border border-zinc-800 bg-zinc-950">
              <div className="flex flex-wrap items-center justify-between gap-3 border-b border-zinc-800 bg-zinc-900 px-3 py-2.5 xl:px-4 xl:py-3">
                <h2 className="text-2xl font-semibold uppercase leading-snug tracking-wide text-amber-400 xl:text-3xl">Dabar kviečiami</h2>
                <div className="rounded-md border border-zinc-700 bg-zinc-800 px-3 py-1.5 font-mono text-xl font-bold text-amber-400 xl:px-4 xl:py-2 xl:text-2xl">
                  {clock}
                </div>
              </div>

              {overview.activeTickets.length === 0 ? (
                <div className="flex flex-1 items-center justify-center px-8 text-center text-4xl font-semibold text-zinc-500">
                  Šiuo metu iškvietimų nėra
                </div>
              ) : (
                <div className="grid min-h-0 flex-1 auto-rows-fr grid-cols-1 gap-2 overflow-hidden p-2 xl:grid-cols-2 xl:gap-3 xl:p-3">
                  {overview.activeTickets.map((ticket) => (
                    <DisplayTicketCard key={ticket.ticketId} ticket={ticket} />
                  ))}
                </div>
              )}
            </div>

            <div className="flex min-h-0 flex-col rounded-md border border-zinc-800 bg-zinc-950">
              <div className="border-b border-zinc-800 bg-zinc-900 px-3 py-2.5 xl:px-4 xl:py-3">
                <h2 className="text-2xl font-semibold uppercase leading-snug tracking-wide text-zinc-100 xl:text-3xl">Naujausia istorija</h2>
              </div>

              {visibleHistory.length === 0 ? (
                <div className="flex flex-1 items-center justify-center px-8 text-center text-3xl font-semibold text-zinc-500">
                  Istorijos įrašų nėra
                </div>
              ) : (
                <div className="grid min-h-0 flex-1 auto-rows-fr gap-2 overflow-hidden p-2 xl:p-3">
                  {visibleHistory.map((ticket) => (
                    <HistoryTicketRow key={ticket.ticketId} ticket={ticket} />
                  ))}
                </div>
              )}
            </div>
          </section>
        )}
      </div>
    </main>
  );
}
