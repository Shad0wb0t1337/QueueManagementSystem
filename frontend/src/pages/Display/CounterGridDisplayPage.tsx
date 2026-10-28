import { useCallback, useEffect, useState } from "react";
import { useParams } from "react-router-dom";
import { LoadingSpinner } from "@/components/shared/LoadingSpinner";
import { useSignalR } from "@/hooks/useSignalR";
import { cn } from "@/lib/utils";
import { getDisplayCounters, type DisplayCounterRange } from "@/services/displayService";
import type { CounterDisplayGrid, CounterDisplayTile, DisplayUpdatedEvent, QueueUpdatedEvent } from "@/types";
import { getApiErrorMessage } from "@/utils/apiError";

interface ParsedRange {
  range: DisplayCounterRange | null;
  isInvalid: boolean;
}

function parseRange(rangeParam: string | undefined): ParsedRange {
  if (!rangeParam) {
    return { range: null, isInvalid: false };
  }

  const match = /^(\d+)-(\d+)$/.exec(rangeParam);
  if (!match) {
    return { range: null, isInvalid: true };
  }

  const fromRaw = match[1];
  const toRaw = match[2];
  if (!fromRaw || !toRaw) {
    return { range: null, isInvalid: true };
  }

  const from = Number.parseInt(fromRaw, 10);
  const to = Number.parseInt(toRaw, 10);

  if (from <= 0 || to <= 0 || from > to) {
    return { range: null, isInvalid: true };
  }

  return { range: { from, to }, isInvalid: false };
}

function formatClock(date: Date): string {
  return date.toLocaleTimeString("lt-LT", {
    hour: "2-digit",
    minute: "2-digit",
    second: "2-digit",
    hour12: false
  });
}

function getTileStatusLabel(counter: CounterDisplayTile): string {
  if (counter.counterStatus === "Paused") {
    return "Laikinai nedirba";
  }

  if (counter.counterStatus === "Closed") {
    return "Uždaryta";
  }

  if (counter.currentTicketStatus === "Called") {
    return "KVIEČIAMAS";
  }

  if (counter.currentTicketStatus === "Serving") {
    return "APTARNAUJAMAS";
  }

  return "Laisvas";
}

function getTileClassName(counter: CounterDisplayTile): string {
  if (counter.counterStatus === "Closed") {
    return "border-red-500/40 bg-red-950/30";
  }

  if (counter.counterStatus === "Paused") {
    return "border-amber-500/40 bg-amber-500/10";
  }

  if (counter.currentTicketStatus === "Called") {
    return "border-amber-400 bg-amber-400/15";
  }

  if (counter.currentTicketStatus === "Serving") {
    return "border-emerald-500/50 bg-emerald-500/15";
  }

  return "border-zinc-800 bg-zinc-900";
}

function getStatusClassName(counter: CounterDisplayTile): string {
  if (counter.counterStatus === "Closed") {
    return "text-red-300";
  }

  if (counter.counterStatus === "Paused") {
    return "text-amber-300";
  }

  if (counter.currentTicketStatus === "Called") {
    return "text-amber-300";
  }

  if (counter.currentTicketStatus === "Serving") {
    return "text-emerald-300";
  }

  return "text-zinc-400";
}

function getGridClassName(counterCount: number): string {
  if (counterCount <= 1) {
    return "grid-cols-1";
  }

  if (counterCount === 2) {
    return "grid-cols-1 md:grid-cols-2";
  }

  if (counterCount === 3) {
    return "grid-cols-1 md:grid-cols-3";
  }

  if (counterCount === 4) {
    return "grid-cols-1 md:grid-cols-2 xl:grid-cols-4";
  }

  if (counterCount <= 6) {
    return "grid-cols-1 md:grid-cols-2 xl:grid-cols-3";
  }

  if (counterCount <= 8) {
    return "grid-cols-1 md:grid-cols-2 xl:grid-cols-4";
  }

  return "grid-cols-1 sm:grid-cols-2 lg:grid-cols-4";
}

function CounterTile({ counter, isCompact }: { counter: CounterDisplayTile; isCompact: boolean }): JSX.Element {
  const hasCurrentTicket = counter.currentTicketNumber != null;

  return (
    <article
      className={cn(
        "flex min-h-0 flex-col overflow-hidden rounded-md border",
        isCompact ? "p-3" : "p-4 xl:p-5",
        getTileClassName(counter),
      )}
    >
      <div className="flex shrink-0 items-start justify-between gap-3">
        <h2 className={cn("min-w-0 truncate font-semibold leading-snug text-zinc-100", isCompact ? "text-xl xl:text-2xl" : "text-2xl xl:text-4xl")}>
          {counter.counterName}
        </h2>
        {hasCurrentTicket && (
          <span
            className={cn(
              "shrink-0 rounded-md border px-2 py-1 font-bold uppercase leading-snug tracking-wide",
              isCompact ? "text-xs xl:text-sm" : "text-sm xl:text-lg",
              getTileClassName(counter),
              getStatusClassName(counter),
            )}
          >
            {getTileStatusLabel(counter)}
          </span>
        )}
      </div>

      <div className="flex min-h-0 flex-1 flex-col items-center justify-center px-2 text-center">
        {hasCurrentTicket ? (
          <>
            <p
              className={cn(
                "font-mono font-bold leading-[1.12] text-amber-400",
                isCompact ? "text-[clamp(2.6rem,4.8vw,4.4rem)]" : "text-[clamp(3.5rem,7vw,7rem)]",
              )}
            >
              {counter.currentTicketNumber}
            </p>
            {counter.serviceTypeName && (
              <p className={cn("line-clamp-2 font-semibold leading-snug text-zinc-100", isCompact ? "mt-1.5 text-base xl:text-lg" : "mt-2 text-xl xl:text-3xl")}>
                {counter.serviceTypeName}
              </p>
            )}
          </>
        ) : (
          <p
            className={cn(
              "font-bold uppercase leading-snug tracking-wide",
              isCompact ? "text-xl xl:text-2xl" : "text-[clamp(1.9rem,3vw,3.25rem)]",
              getStatusClassName(counter),
            )}
          >
            {getTileStatusLabel(counter)}
          </p>
        )}
      </div>
    </article>
  );
}

export function CounterGridDisplayPage(): JSX.Element {
  const { range: rangeParam } = useParams<{ range?: string }>();
  const parsedRange = parseRange(rangeParam);
  const rangeLabel = parsedRange.range ? `Langeliai ${parsedRange.range.from}–${parsedRange.range.to}` : null;
  const [clock, setClock] = useState<string>(() => formatClock(new Date()));
  const [grid, setGrid] = useState<CounterDisplayGrid | null>(null);
  const [isLoading, setIsLoading] = useState(!parsedRange.isInvalid);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);
  const { connection } = useSignalR({ joinDisplay: true });

  const loadGrid = useCallback(async (): Promise<void> => {
    if (parsedRange.isInvalid) {
      setGrid(null);
      setErrorMessage(null);
      setIsLoading(false);
      return;
    }

    setIsLoading(true);
    setErrorMessage(null);

    try {
      const response = await getDisplayCounters(parsedRange.range ?? undefined);
      setGrid(response);
    } catch (error) {
      setErrorMessage(getApiErrorMessage(error, "Nepavyko įkelti langelių ekrano."));
    } finally {
      setIsLoading(false);
    }
  }, [parsedRange.isInvalid, parsedRange.range?.from, parsedRange.range?.to]);

  useEffect(() => {
    void loadGrid();
  }, [loadGrid]);

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
      void loadGrid();
    };

    connection.on("DisplayUpdated", refreshDisplay);
    connection.on("QueueUpdated", refreshDisplay);

    return () => {
      connection.off("DisplayUpdated", refreshDisplay);
      connection.off("QueueUpdated", refreshDisplay);
    };
  }, [connection, loadGrid]);

  return (
    <main className="h-screen overflow-hidden bg-zinc-950 p-3 text-zinc-100">
      <div className="mx-auto flex h-full w-full max-w-[1800px] flex-col">
        <header className="mb-3 flex shrink-0 flex-wrap items-center justify-between gap-3 rounded-md border border-zinc-800 bg-zinc-900 px-4 py-2.5 xl:px-5 xl:py-3">
          <div className="min-w-0">
            <h1 className="pb-1 text-3xl font-semibold leading-normal text-zinc-100 xl:text-5xl">Langelių būsena</h1>
            {rangeLabel && <p className="mt-1 text-xl text-zinc-400">{rangeLabel}</p>}
          </div>
          <div className="shrink-0 rounded-md border border-zinc-700 bg-zinc-800 px-3 py-1.5 font-mono text-xl font-bold text-amber-400 xl:px-4 xl:py-2 xl:text-2xl">
            {clock}
          </div>
        </header>

        {parsedRange.isInvalid && (
          <section className="flex flex-1 flex-col items-center justify-center rounded-md border border-amber-500/40 bg-zinc-900 p-6 text-center">
            <p className="text-4xl font-semibold text-amber-300">Neteisingas langelių intervalas</p>
          </section>
        )}

        {!parsedRange.isInvalid && isLoading && (
          <section className="flex flex-1 flex-col items-center justify-center rounded-md border border-zinc-800 bg-zinc-900">
            <LoadingSpinner />
            <p className="mt-4 text-xl text-zinc-300">Kraunami langeliai...</p>
          </section>
        )}

        {!parsedRange.isInvalid && !isLoading && errorMessage && (
          <section className="flex flex-1 flex-col items-center justify-center rounded-md border border-red-500/40 bg-zinc-900 p-6 text-center">
            <p className="text-3xl text-red-400">Nepavyko įkelti langelių ekrano.</p>
            <p className="mt-2 text-xl text-zinc-400">{errorMessage}</p>
          </section>
        )}

        {!parsedRange.isInvalid && !isLoading && !errorMessage && grid && (
          <section className={cn("grid min-h-0 flex-1 auto-rows-fr gap-3 overflow-hidden", getGridClassName(grid.counters.length))}>
            {grid.counters.length === 0 ? (
              <div className="col-span-full flex items-center justify-center rounded-md border border-zinc-800 bg-zinc-900 text-4xl text-zinc-500">
                {parsedRange.range ? "Šiame intervale langelių nėra" : "Langelių nėra"}
              </div>
            ) : (
              grid.counters.map((counter) => (
                <CounterTile counter={counter} isCompact={grid.counters.length > 8} key={counter.counterId} />
              ))
            )}
          </section>
        )}
      </div>
    </main>
  );
}
