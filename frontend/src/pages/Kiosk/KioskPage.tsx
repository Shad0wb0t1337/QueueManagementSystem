import { useCallback, useEffect, useMemo, useState } from "react";
import { LoadingSpinner } from "@/components/shared/LoadingSpinner";
import { useSignalR } from "@/hooks/useSignalR";
import { useTickets } from "@/hooks/useTickets";
import { getServiceTypes } from "@/services/serviceTypeService";
import type { IssueTicketResponse, QueueUpdatedEvent, ServiceType } from "@/types";
import { getApiErrorMessage } from "@/utils/apiError";

const CONFIRMATION_SECONDS = 8;
const AUTO_RETURN_DELAY_MS = 1000;

export function KioskPage(): JSX.Element {
  const [serviceTypes, setServiceTypes] = useState<ServiceType[]>([]);
  const [isLoadingServiceTypes, setIsLoadingServiceTypes] = useState(true);
  const [serviceTypesError, setServiceTypesError] = useState<string | null>(null);
  const [confirmationTicket, setConfirmationTicket] = useState<IssueTicketResponse | null>(null);
  const [countdownSeconds, setCountdownSeconds] = useState(CONFIRMATION_SECONDS);

  const { issueTicket, isLoading: isIssuingTicket, errorMessage: issueTicketError } = useTickets();
  const serviceTypeIds = useMemo(() => serviceTypes.map((serviceType) => serviceType.id), [serviceTypes]);
  const { connection } = useSignalR({
    joinDisplay: true,
    serviceTypeIds
  });

  const loadServiceTypes = useCallback(async () => {
    setIsLoadingServiceTypes(true);
    setServiceTypesError(null);

    try {
      const response = await getServiceTypes();
      setServiceTypes(response);
    } catch (error) {
      setServiceTypesError(getApiErrorMessage(error, "Nepavyko pasiekti serverio."));
    } finally {
      setIsLoadingServiceTypes(false);
    }
  }, []);

  const resetToSelection = useCallback(() => {
    setConfirmationTicket(null);
    setCountdownSeconds(CONFIRMATION_SECONDS);
  }, []);

  const handleIssueTicket = async (serviceTypeId: number): Promise<void> => {
    try {
      const ticket = await issueTicket(serviceTypeId);
      setConfirmationTicket(ticket);
      setCountdownSeconds(CONFIRMATION_SECONDS);
    } catch {
      // Error message is handled by useTickets and axios interceptor.
    }
  };

  useEffect(() => {
    void loadServiceTypes();
  }, [loadServiceTypes]);

  useEffect(() => {
    if (!connection) {
      return;
    }

    const onQueueUpdated = (_event: QueueUpdatedEvent) => {
      void loadServiceTypes();
    };

    connection.on("QueueUpdated", onQueueUpdated);

    return () => {
      connection.off("QueueUpdated", onQueueUpdated);
    };
  }, [connection, loadServiceTypes]);

  useEffect(() => {
    if (!confirmationTicket || countdownSeconds <= 0) {
      return;
    }

    const intervalId = window.setInterval(() => {
      setCountdownSeconds((previous) => previous - 1);
    }, 1000);

    return () => {
      window.clearInterval(intervalId);
    };
  }, [confirmationTicket, countdownSeconds]);

  useEffect(() => {
    if (!confirmationTicket || countdownSeconds > 0) {
      return;
    }

    const timeoutId = window.setTimeout(() => {
      resetToSelection();
    }, AUTO_RETURN_DELAY_MS);

    return () => {
      window.clearTimeout(timeoutId);
    };
  }, [confirmationTicket, countdownSeconds, resetToSelection]);

  if (confirmationTicket) {
    return (
      <main className="min-h-screen bg-zinc-950 px-3 py-4 text-zinc-100 sm:px-6 sm:py-10">
        <div className="mx-auto flex min-h-[calc(100vh-2rem)] w-full max-w-4xl flex-col items-center justify-center rounded-md border border-zinc-800 bg-zinc-900 p-5 text-center sm:min-h-[calc(100vh-5rem)] sm:p-8">
          <p className="text-base text-zinc-400">Jusu bilieto numeris</p>
          <p className="mt-5 font-mono text-7xl font-bold text-amber-400 sm:mt-6 sm:text-8xl">{confirmationTicket.ticketNumber}</p>
          <div className="mt-8 space-y-2 text-lg text-zinc-200">
            <p>
              Preliminarus laukimas:{" "}
              <span className="font-semibold text-amber-400">{confirmationTicket.estimatedWaitMinutes} min</span>
            </p>
            <p>
              Laukiančiųjų skaičius:{" "}
              <span className="font-semibold text-amber-400">{Math.max(confirmationTicket.queuePosition - 1, 0)}</span>
            </p>
          </div>
          <p className="mt-8 text-base text-zinc-400">
            Automatinis grizimas po <span className="font-semibold text-zinc-100">{Math.max(countdownSeconds, 0)}</span> s.
          </p>

          {countdownSeconds <= 0 && (
            <button
              className="mt-6 h-12 min-w-44 rounded-md border border-amber-400 bg-amber-400 px-6 text-base font-semibold text-zinc-950 transition hover:bg-amber-300 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-amber-400 focus-visible:ring-offset-2 focus-visible:ring-offset-zinc-950"
              onClick={resetToSelection}
              type="button"
            >
              Grizti
            </button>
          )}
        </div>
      </main>
    );
  }

  return (
    <main className="min-h-screen overflow-x-hidden bg-zinc-950 px-3 py-4 text-zinc-100 sm:px-6 sm:py-8">
      <div className="mx-auto w-full max-w-6xl">
        <header className="mb-5 text-center sm:mb-8">
          <h1 className="text-3xl font-semibold text-zinc-100 sm:text-4xl">Pasirinkite paslauga</h1>
          <p className="mt-2 text-base text-zinc-400">Palieskite kortele, kad gautumete bilieta.</p>
        </header>

        {isLoadingServiceTypes && (
          <section className="rounded-md border border-zinc-800 bg-zinc-900 p-10">
            <LoadingSpinner />
            <p className="mt-4 text-center text-base text-zinc-300">Kraunamos paslaugos...</p>
          </section>
        )}

        {!isLoadingServiceTypes && serviceTypesError && (
          <section className="rounded-md border border-red-500/40 bg-zinc-900 p-6 text-center">
            <p className="text-base text-red-400">Nepavyko ikelti paslaugu.</p>
            <p className="mt-2 text-sm text-zinc-400">{serviceTypesError}</p>
            <button
              className="mt-4 h-12 min-w-44 rounded-md border border-zinc-700 bg-zinc-800 px-5 text-base text-zinc-100 transition hover:border-amber-400 hover:text-amber-400 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-amber-400 focus-visible:ring-offset-2 focus-visible:ring-offset-zinc-950"
              onClick={() => {
                void loadServiceTypes();
              }}
              type="button"
            >
              Bandyti dar karta
            </button>
          </section>
        )}

        {!isLoadingServiceTypes && !serviceTypesError && (
          <section className="space-y-4">
            {issueTicketError && (
              <div className="rounded-md border border-red-500/40 bg-zinc-900 p-4 text-base text-red-400">{issueTicketError}</div>
            )}

            {isIssuingTicket && (
              <div className="rounded-md border border-zinc-800 bg-zinc-900 p-4 text-base text-zinc-300">Generuojamas bilietas...</div>
            )}

            {serviceTypes.length === 0 && (
              <div className="rounded-md border border-zinc-800 bg-zinc-900 p-6 text-center text-base text-zinc-400">
                Siuo metu aktyviu paslaugu nera.
              </div>
            )}

            {serviceTypes.length > 0 && (
              <div className="grid grid-cols-1 gap-3 sm:grid-cols-2 xl:grid-cols-3 xl:gap-4">
                {serviceTypes.map((serviceType) => (
                  <button
                    className="min-h-32 rounded-md border border-zinc-800 bg-zinc-900 p-4 text-left transition hover:border-amber-400/80 hover:bg-zinc-800 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-amber-400 focus-visible:ring-offset-2 focus-visible:ring-offset-zinc-950 disabled:cursor-not-allowed disabled:opacity-60 sm:p-5 xl:min-h-36 xl:p-6"
                    disabled={isIssuingTicket || !serviceType.hasActiveCounter}
                    key={serviceType.id}
                    onClick={() => {
                      if (serviceType.hasActiveCounter) {
                        void handleIssueTicket(serviceType.id);
                      }
                    }}
                    type="button"
                  >
                    <div className="flex h-full flex-col justify-between">
                      <div>
                        <div className="flex flex-wrap items-start justify-between gap-2">
                          <p className="min-w-0 text-xl font-semibold text-zinc-100 sm:text-2xl">{serviceType.name}</p>
                          {!serviceType.hasActiveCounter && (
                            <span className="rounded-md border border-amber-300/40 bg-amber-400/10 px-2 py-1 text-xs font-semibold text-amber-300">
                              Šiuo metu neaptarnaujama
                            </span>
                          )}
                        </div>
                      </div>
                      <div className="mt-4 grid grid-cols-1 gap-2 text-sm text-zinc-300 sm:grid-cols-2 sm:gap-3">
                        <div>
                          <p className="text-zinc-500">Preliminarus laukimas</p>
                          <p className="font-semibold text-amber-400">
                            {serviceType.hasActiveCounter ? `${serviceType.estimatedWaitMinutes} min` : "Neaptarnaujama"}
                          </p>
                        </div>
                        <div>
                          <p className="text-zinc-500">Laukianciuju skaicius</p>
                          <p className="font-semibold text-zinc-100">{serviceType.currentQueueLength}</p>
                        </div>
                      </div>
                    </div>
                  </button>
                ))}
              </div>
            )}
          </section>
        )}
      </div>
    </main>
  );
}
