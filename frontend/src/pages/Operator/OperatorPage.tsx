import { useCallback, useEffect, useMemo, useState } from "react";
import { useNavigate } from "react-router-dom";
import { LoadingSpinner } from "@/components/shared/LoadingSpinner";
import { useAuth } from "@/hooks/useAuth";
import { useQueue } from "@/hooks/useQueue";
import {
  getCounterServiceProfile,
  helpMostLoadedService,
  updateCounterServiceProfile,
  updateOwnCounterStatus
} from "@/services/counterService";
import { getOperatorServiceLoad } from "@/services/serviceLoadService";
import { getServiceTypes } from "@/services/serviceTypeService";
import { callNext, getRecentTicketHistory, markDone, markNoShow, markServing, recallTicket, redirectTicket } from "@/services/queueService";
import { getApiErrorMessage } from "@/utils/apiError";
import type {
  ActiveTicketResponse,
  CounterServiceProfileItem,
  CounterStatus,
  LoadStatus,
  QueueTicketResponse,
  RecentTicketHistoryItem,
  ServiceType,
  ServiceLoadSummary,
  TicketSelectionExplanation
} from "@/types";

type ActionType =
  | "call-next"
  | "mark-serving"
  | "mark-done"
  | "mark-no-show"
  | "update-counter-status"
  | "update-service-profile"
  | "help-most-loaded"
  | "recall-ticket"
  | "redirect-ticket"
  | null;

function parseBackendDate(dateRaw: string): Date {
  const hasTimezoneInfo = /(?:Z|[+-]\d{2}:?\d{2})$/.test(dateRaw);
  const normalizedDate = hasTimezoneInfo ? dateRaw : `${dateRaw}Z`;
  const parsedDate = new Date(normalizedDate);

  if (!Number.isNaN(parsedDate.getTime())) {
    return parsedDate;
  }

  return new Date(dateRaw);
}

function formatCreatedAt(dateRaw: string): string {
  return parseBackendDate(dateRaw).toLocaleTimeString("lt-LT", {
    hour: "2-digit",
    minute: "2-digit"
  });
}

function getWaitMinutes(dateRaw: string, nowMs: number): number {
  const createdAtMs = parseBackendDate(dateRaw).getTime();
  return Math.max(0, Math.floor((nowMs - createdAtMs) / 60000));
}

function isNearMaxWait(ticket: QueueTicketResponse, waitMinutes: number): boolean {
  return ticket.maxWaitMinutes != null && waitMinutes >= ticket.maxWaitMinutes * 0.75;
}

function isCounterStatus(value: string): value is CounterStatus {
  return value === "Active" || value === "Paused" || value === "Closed";
}

function getCounterStatusLabel(status: CounterStatus | null): string {
  if (status === "Active") {
    return "Aktyvus";
  }

  if (status === "Paused") {
    return "Pauze";
  }

  if (status === "Closed") {
    return "Uzdarytas";
  }

  return "Nezinoma";
}

function getCounterStatusBadgeClassName(status: CounterStatus | null): string {
  if (status === "Active") {
    return "rounded-md border border-emerald-200 bg-emerald-50 px-3 py-1 text-sm font-semibold text-emerald-700";
  }

  if (status === "Paused") {
    return "rounded-md border border-amber-200 bg-amber-50 px-3 py-1 text-sm font-semibold text-amber-700";
  }

  if (status === "Closed") {
    return "rounded-md border border-red-200 bg-red-50 px-3 py-1 text-sm font-semibold text-red-700";
  }

  return "rounded-md border border-zinc-800 bg-zinc-800 px-3 py-1 text-sm font-semibold text-zinc-300";
}

function getTicketStatusLabel(status: string): string {
  if (status === "Waiting") {
    return "Laukia";
  }

  if (status === "Called") {
    return "Pakviestas";
  }

  if (status === "Serving") {
    return "Aptarnaujamas";
  }

  if (status === "Done") {
    return "Baigtas";
  }

  if (status === "NoShow") {
    return "Neatvyko";
  }

  if (status === "RecallPending") {
    return "Laukia pakartotinio kvietimo";
  }

  return status;
}

function getRecentHistoryTime(ticket: RecentTicketHistoryItem): string {
  const dateRaw = ticket.recallRequestedAt ?? ticket.completedAt ?? ticket.lastUpdatedAt;
  return formatCreatedAt(dateRaw);
}

function getRecallButtonLabel(recallMode: string): string {
  if (recallMode === "Immediate") {
    return "Kviesti dar kartą";
  }

  if (recallMode === "AfterCurrent") {
    return "Kviesti po dabartinio";
  }

  return "";
}

function getLoadStatusLabel(status: LoadStatus): string {
  if (status === "Low") {
    return "Maza apkrova";
  }

  if (status === "Normal") {
    return "Normali";
  }

  if (status === "High") {
    return "Didele apkrova";
  }

  return "Kritine";
}

function getLoadStatusBadgeClassName(status: LoadStatus): string {
  if (status === "Critical") {
    return "rounded-md border border-red-500/40 bg-red-500/15 px-2 py-1 text-sm font-semibold text-red-400";
  }

  if (status === "High") {
    return "rounded-md border border-amber-500/40 bg-amber-500/15 px-2 py-1 text-sm font-semibold text-amber-400";
  }

  if (status === "Normal") {
    return "rounded-md border border-emerald-500/40 bg-emerald-500/15 px-2 py-1 text-sm font-semibold text-emerald-400";
  }

  return "rounded-md border border-zinc-700 bg-zinc-800 px-2 py-1 text-sm font-semibold text-zinc-300";
}

function formatExplanationNumber(value: number | null | undefined): string {
  if (value == null) {
    return "-";
  }

  return Number.isInteger(value) ? value.toString() : value.toFixed(1);
}

function formatExplanationBonus(value: number | null | undefined): string {
  if (value == null || value <= 0) {
    return "nėra";
  }

  return `+${formatExplanationNumber(value)}`;
}

export function OperatorPage(): JSX.Element {
  const navigate = useNavigate();
  const { user, loadMe, logout } = useAuth();
  const [isProfileLoading, setIsProfileLoading] = useState(true);
  const [profileError, setProfileError] = useState<string | null>(null);
  const [actionError, setActionError] = useState<string | null>(null);
  const [actionInProgress, setActionInProgress] = useState<ActionType>(null);
  const [nowMs, setNowMs] = useState(Date.now());
  const [counterStatusFormValue, setCounterStatusFormValue] = useState<CounterStatus>("Active");
  const [serviceProfile, setServiceProfile] = useState<CounterServiceProfileItem[]>([]);
  const [isLoadingServiceProfile, setIsLoadingServiceProfile] = useState(false);
  const [serviceProfileError, setServiceProfileError] = useState<string | null>(null);
  const [serviceLoad, setServiceLoad] = useState<ServiceLoadSummary[]>([]);
  const [isLoadingServiceLoad, setIsLoadingServiceLoad] = useState(false);
  const [serviceLoadError, setServiceLoadError] = useState<string | null>(null);
  const [recentHistory, setRecentHistory] = useState<RecentTicketHistoryItem[]>([]);
  const [isLoadingRecentHistory, setIsLoadingRecentHistory] = useState(false);
  const [recentHistoryError, setRecentHistoryError] = useState<string | null>(null);
  const [redirectTargetServices, setRedirectTargetServices] = useState<ServiceType[]>([]);
  const [isRedirectDialogOpen, setIsRedirectDialogOpen] = useState(false);
  const [redirectTargetServiceId, setRedirectTargetServiceId] = useState("");
  const [redirectReason, setRedirectReason] = useState("");
  const [successMessage, setSuccessMessage] = useState<string | null>(null);
  const [lastSelectionExplanation, setLastSelectionExplanation] = useState<{
    ticketId: number;
    explanation: TicketSelectionExplanation;
  } | null>(null);

  const supportedServiceTypeIds = useMemo(() => user?.supportedServiceTypeIds ?? [], [user?.supportedServiceTypeIds]);
  const supportedServiceTypes = useMemo(() => user?.supportedServiceTypes ?? [], [user?.supportedServiceTypes]);
  const counterId = user?.counterId ?? null;
  const counterName = user?.counterName ?? null;
  const counterStatus = user?.counterStatus ?? null;
  const operatorName = user?.operatorName ?? user?.username ?? null;
  const supportedServiceNames = useMemo(
    () => supportedServiceTypes.map((serviceType) => serviceType.name).join(", "),
    [supportedServiceTypes]
  );
  const enabledServiceTypeIds = useMemo(
    () =>
      serviceProfile
        .filter((serviceType) => serviceType.isEnabled)
        .map((serviceType) => serviceType.serviceTypeId),
    [serviceProfile]
  );
  const serviceLoadByServiceTypeId = useMemo(
    () => new Map(serviceLoad.map((summary) => [summary.serviceTypeId, summary])),
    [serviceLoad]
  );

  const loadServiceProfile = useCallback(async (): Promise<void> => {
    if (!counterId) {
      setServiceProfile([]);
      return;
    }

    setIsLoadingServiceProfile(true);
    setServiceProfileError(null);

    try {
      const profile = await getCounterServiceProfile(counterId);
      setServiceProfile(profile);
    } catch (error) {
      setServiceProfileError(getApiErrorMessage(error, "Nepavyko įkelti aptarnaujamų paslaugų."));
    } finally {
      setIsLoadingServiceProfile(false);
    }
  }, [counterId]);

  const loadServiceLoad = useCallback(async (): Promise<void> => {
    if (!counterId) {
      setServiceLoad([]);
      return;
    }

    setIsLoadingServiceLoad(true);
    setServiceLoadError(null);

    try {
      const summaries = await getOperatorServiceLoad(counterId);
      setServiceLoad(summaries);
    } catch (error) {
      setServiceLoadError(getApiErrorMessage(error, "Nepavyko įkelti paslaugų apkrovos."));
    } finally {
      setIsLoadingServiceLoad(false);
    }
  }, [counterId]);

  const loadRecentHistory = useCallback(async (): Promise<void> => {
    if (!counterId) {
      setRecentHistory([]);
      return;
    }

    setIsLoadingRecentHistory(true);
    setRecentHistoryError(null);

    try {
      const history = await getRecentTicketHistory(counterId);
      setRecentHistory(history);
    } catch (error) {
      setRecentHistoryError(getApiErrorMessage(error, "Nepavyko įkelti naujausių bilietų."));
    } finally {
      setIsLoadingRecentHistory(false);
    }
  }, [counterId]);

  const handleQueueUpdated = useCallback((): void => {
    void loadServiceProfile();
    void loadServiceLoad();
    void loadRecentHistory();
  }, [loadRecentHistory, loadServiceLoad, loadServiceProfile]);

  const {
    currentQueue,
    activeTickets,
    isLoadingQueue,
    queueError,
    isLoadingActive,
    activeError,
    refreshQueue,
    refreshActive
  } = useQueue({
    counterId,
    serviceTypeIds: supportedServiceTypeIds,
    includeActive: true,
    joinDisplay: false,
    onQueueUpdated: handleQueueUpdated
  });

  const currentTicket = useMemo<ActiveTicketResponse | null>(() => {
    if (!counterId) {
      return null;
    }

    const assignedCounterTickets = activeTickets.filter((ticket) => ticket.counterId === counterId);
    const servingTicket = assignedCounterTickets.find((ticket) => ticket.status === "Serving");
    const calledTicket = assignedCounterTickets.find((ticket) => ticket.status === "Called");
    return servingTicket ?? calledTicket ?? null;
  }, [activeTickets, counterId]);
  const currentSelectionExplanation =
    currentTicket && lastSelectionExplanation?.ticketId === currentTicket.ticketId
      ? lastSelectionExplanation.explanation
      : null;

  const canCallNext =
    Boolean(counterId) &&
    counterStatus === "Active" &&
    enabledServiceTypeIds.length > 0 &&
    currentQueue.length > 0 &&
    !currentTicket &&
    !actionInProgress;
  const canMarkServing = currentTicket?.status === "Called" && !actionInProgress;
  const canMarkDoneOrNoShow =
    (currentTicket?.status === "Called" || currentTicket?.status === "Serving") && !actionInProgress;
  const canRedirectTicket =
    (currentTicket?.status === "Called" || currentTicket?.status === "Serving") && !actionInProgress;
  const canSaveCounterStatus = Boolean(counterStatus) && counterStatusFormValue !== counterStatus && !actionInProgress;

  useEffect(() => {
    let isActive = true;

    const hydrateProfile = async () => {
      setIsProfileLoading(true);
      setProfileError(null);

      try {
        await loadMe();
      } catch (error) {
        const message = getApiErrorMessage(error, "Nepavyko įkelti operatoriaus profilio.");
        if (isActive) {
          setProfileError(message);
        }
      } finally {
        if (isActive) {
          setIsProfileLoading(false);
        }
      }
    };

    void hydrateProfile();

    return () => {
      isActive = false;
    };
  }, [loadMe]);

  useEffect(() => {
    if (!counterStatus) {
      return;
    }

    setCounterStatusFormValue(counterStatus);
  }, [counterStatus]);

  useEffect(() => {
    void loadServiceProfile();
  }, [loadServiceProfile]);

  useEffect(() => {
    void loadServiceLoad();
  }, [loadServiceLoad]);

  useEffect(() => {
    void loadRecentHistory();
  }, [loadRecentHistory]);

  useEffect(() => {
    const loadRedirectTargets = async () => {
      try {
        const serviceTypes = await getServiceTypes(false);
        setRedirectTargetServices(serviceTypes.filter((serviceType) => serviceType.isActive && serviceType.hasActiveCounter));
      } catch {
        setRedirectTargetServices([]);
      }
    };

    void loadRedirectTargets();
  }, []);

  useEffect(() => {
    const intervalId = window.setInterval(() => {
      setNowMs(Date.now());
    }, 30000);

    return () => {
      window.clearInterval(intervalId);
    };
  }, []);

  const handleCallNext = async () => {
    if (!counterId) {
      return;
    }

    setActionError(null);
    setActionInProgress("call-next");
    try {
      const response = await callNext({ counterId });
      setLastSelectionExplanation(
        response.selectionExplanation
          ? {
              ticketId: response.ticketId,
              explanation: response.selectionExplanation
            }
          : null
      );
      await Promise.all([loadServiceLoad(), loadRecentHistory(), refreshQueue(), refreshActive()]);
    } catch (error) {
      setActionError(getApiErrorMessage(error, "Nepavyko iškviesti kito bilieto."));
    } finally {
      setActionInProgress(null);
    }
  };

  const handleTicketAction = async (action: "mark-serving" | "mark-done" | "mark-no-show") => {
    if (!currentTicket) {
      return;
    }

    setActionError(null);
    setActionInProgress(action);

    try {
      if (action === "mark-serving") {
        await markServing(currentTicket.ticketId);
      } else if (action === "mark-done") {
        await markDone(currentTicket.ticketId);
        setLastSelectionExplanation(null);
      } else {
        await markNoShow(currentTicket.ticketId);
        setLastSelectionExplanation(null);
      }

      await Promise.all([loadServiceLoad(), loadRecentHistory(), refreshQueue(), refreshActive()]);
    } catch (error) {
      setActionError(getApiErrorMessage(error, "Nepavyko atnaujinti bilieto būsenos."));
    } finally {
      setActionInProgress(null);
    }
  };

  const handleUpdateCounterStatus = async () => {
    if (!counterStatus) {
      return;
    }

    setActionError(null);
    setActionInProgress("update-counter-status");

    try {
      await updateOwnCounterStatus({ status: counterStatusFormValue });
      await Promise.all([loadMe(), loadServiceLoad(), loadRecentHistory(), refreshQueue(), refreshActive()]);
    } catch (error) {
      setActionError(getApiErrorMessage(error, "Nepavyko atnaujinti langelio būsenos."));
    } finally {
      setActionInProgress(null);
    }
  };

  const handleToggleService = async (serviceTypeId: number, isEnabled: boolean) => {
    if (!counterId) {
      return;
    }

    const enabledIds = isEnabled
      ? Array.from(new Set([...enabledServiceTypeIds, serviceTypeId]))
      : enabledServiceTypeIds.filter((enabledServiceTypeId) => enabledServiceTypeId !== serviceTypeId);

    setActionError(null);
    setActionInProgress("update-service-profile");

    try {
      const profile = await updateCounterServiceProfile(counterId, { enabledServiceTypeIds: enabledIds });
      setServiceProfile(profile);
      await Promise.all([loadServiceLoad(), loadRecentHistory(), refreshQueue(), refreshActive()]);
    } catch (error) {
      setActionError(getApiErrorMessage(error, "Nepavyko atnaujinti aptarnaujamų paslaugų."));
    } finally {
      setActionInProgress(null);
    }
  };

  const handleHelpMostLoaded = async () => {
    if (!counterId) {
      return;
    }

    setActionError(null);
    setSuccessMessage(null);
    setActionInProgress("help-most-loaded");

    try {
      const response = await helpMostLoadedService(counterId);
      setServiceProfile(response.serviceProfile);
      setSuccessMessage(response.message);
      await Promise.all([loadServiceLoad(), loadRecentHistory(), refreshQueue(), refreshActive()]);
    } catch (error) {
      setActionError(getApiErrorMessage(error, "Nepavyko įjungti labiausiai apkrautos paslaugos."));
    } finally {
      setActionInProgress(null);
    }
  };

  const handleRecallTicket = async (ticketId: number) => {
    setActionError(null);
    setSuccessMessage(null);
    setActionInProgress("recall-ticket");

    try {
      const response = await recallTicket(ticketId);
      setSuccessMessage(response.message);
      await Promise.all([loadServiceLoad(), loadRecentHistory(), refreshQueue(), refreshActive()]);
    } catch (error) {
      setActionError(getApiErrorMessage(error, "Nepavyko pakartotinai iškviesti bilieto."));
    } finally {
      setActionInProgress(null);
    }
  };

  const openRedirectDialog = () => {
    setActionError(null);
    setRedirectTargetServiceId("");
    setRedirectReason("");
    setIsRedirectDialogOpen(true);
  };

  const handleRedirectTicket = async () => {
    if (!currentTicket) {
      return;
    }

    const targetServiceTypeId = Number.parseInt(redirectTargetServiceId, 10);
    if (Number.isNaN(targetServiceTypeId) || targetServiceTypeId <= 0 || redirectReason.trim().length === 0) {
      setActionError("Pasirinkite naują paslaugą ir įveskite nukreipimo priežastį.");
      return;
    }

    setActionError(null);
    setSuccessMessage(null);
    setActionInProgress("redirect-ticket");

    try {
      const response = await redirectTicket(currentTicket.ticketId, {
        targetServiceTypeId,
        reason: redirectReason.trim()
      });
      setSuccessMessage(response.message);
      setLastSelectionExplanation(null);
      setIsRedirectDialogOpen(false);
      await Promise.all([loadServiceProfile(), loadServiceLoad(), loadRecentHistory(), refreshQueue(), refreshActive()]);
    } catch (error) {
      setActionError(getApiErrorMessage(error, "Nepavyko nukreipti bilieto."));
    } finally {
      setActionInProgress(null);
    }
  };

  const handleLogout = () => {
    logout();
    navigate("/login", { replace: true });
  };

  if (isProfileLoading) {
    return (
      <div className="rounded-md border border-zinc-800 bg-zinc-900 p-8">
        <LoadingSpinner />
        <p className="mt-3 text-center text-sm text-zinc-400">Kraunamas operatoriaus profilis...</p>
      </div>
    );
  }

  if (profileError) {
    return (
      <div className="rounded-md border border-red-200 bg-zinc-900 p-6">
        <p className="text-base font-medium text-red-600">Nepavyko ikelti operatoriaus profilio.</p>
        <p className="mt-2 text-sm text-zinc-300">{profileError}</p>
      </div>
    );
  }

  if (!user || !counterId || !counterName || supportedServiceTypeIds.length === 0) {
    return (
      <div className="rounded-md border border-red-200 bg-zinc-900 p-6">
        <p className="text-base font-medium text-red-600">Operatoriaus kontekstas neuzbaigtas.</p>
        <p className="mt-2 text-sm text-zinc-300">Prisijunges naudotojas neturi priskirto langelio arba aptarnaujamu paslaugu.</p>
      </div>
    );
  }

  return (
    <div className="h-[calc(100vh-1rem)] overflow-hidden bg-zinc-950 sm:h-[calc(100vh-1.5rem)]">
      <div className="mx-auto flex h-full w-full max-w-[1800px] flex-col gap-2.5 overflow-hidden">
      <section className="grid shrink-0 grid-cols-1 gap-2.5 sm:grid-cols-2 xl:grid-cols-4">
        <div className="min-w-0 rounded-md border border-zinc-800 bg-zinc-900 px-4 py-2.5">
          <p className="text-xs uppercase tracking-wide text-zinc-400">Operatorius</p>
          <p className="mt-1 text-base font-semibold text-zinc-100">{operatorName}</p>
        </div>
        <div className="min-w-0 rounded-md border border-zinc-800 bg-zinc-900 px-4 py-2.5">
          <p className="text-xs uppercase tracking-wide text-zinc-400">Priskirtas langelis</p>
          <p className="mt-1 text-base font-semibold text-zinc-100">{counterName}</p>
        </div>
        <div className="min-w-0 rounded-md border border-zinc-800 bg-zinc-900 px-4 py-2.5">
          <p className="text-xs uppercase tracking-wide text-zinc-400">Aptarnaujamos paslaugos</p>
          <p className="mt-1 truncate text-base font-semibold text-zinc-100" title={supportedServiceNames}>
            {supportedServiceNames}
          </p>
        </div>
        <div className="min-w-0 rounded-md border border-zinc-800 bg-zinc-900 px-4 py-2.5">
          <p className="text-xs uppercase tracking-wide text-zinc-400">Langelio busena</p>
          <div className="mt-1">
            <span className={getCounterStatusBadgeClassName(counterStatus)}>{getCounterStatusLabel(counterStatus)}</span>
          </div>
        </div>
      </section>

      {successMessage && (
        <section className="shrink-0 rounded-md border border-emerald-500/40 bg-emerald-500/10 px-4 py-2">
          <p className="text-sm font-medium text-emerald-400">{successMessage}</p>
        </section>
      )}

      {(queueError || activeError || serviceProfileError || serviceLoadError || recentHistoryError || actionError) && (
        <section className="shrink-0 rounded-md border border-red-200 bg-zinc-900 px-4 py-3">
          <p className="text-sm font-medium text-red-600">Ivyko klaida.</p>
          {queueError && <p className="mt-1 text-sm text-zinc-300">Eile: {queueError}</p>}
          {activeError && <p className="mt-1 text-sm text-zinc-300">Aktyvus bilietai: {activeError}</p>}
          {serviceProfileError && <p className="mt-1 text-sm text-zinc-300">Paslaugos: {serviceProfileError}</p>}
          {serviceLoadError && <p className="mt-1 text-sm text-zinc-300">Apkrova: {serviceLoadError}</p>}
          {recentHistoryError && <p className="mt-1 text-sm text-zinc-300">Naujausi bilietai: {recentHistoryError}</p>}
          {actionError && <p className="mt-1 text-sm text-zinc-300">Veiksmas: {actionError}</p>}
        </section>
      )}

      <section className="shrink-0 rounded-md border border-zinc-800 bg-zinc-900 p-2.5">
        <div className="flex items-center justify-between gap-3">
          <h2 className="text-base font-semibold text-zinc-100">Aptarnaujamos paslaugos</h2>
          {(isLoadingServiceProfile || isLoadingServiceLoad) && <span className="text-sm text-zinc-400">Atnaujinama...</span>}
        </div>

        <div className="mt-2 grid grid-cols-1 gap-1.5 md:grid-cols-2 xl:grid-cols-3">
          {serviceProfile.map((serviceType) => {
            const loadSummary = serviceLoadByServiceTypeId.get(serviceType.serviceTypeId);
            const waitingCount = loadSummary?.waitingCount ?? serviceType.waitingCount;
            const oldestWaitingMinutes = loadSummary?.oldestWaitingMinutes ?? 0;
            const estimatedWaitMinutes = loadSummary?.estimatedWaitMinutes ?? 0;

            return (
              <label
                className="block rounded-md border border-zinc-800 bg-zinc-950 px-3 py-1.5 text-sm text-zinc-200"
                key={serviceType.serviceTypeId}
              >
                  <span className="flex min-w-0 flex-wrap items-center justify-between gap-2">
                  <span className="flex min-w-0 items-center gap-2">
                    <input
                      checked={serviceType.isEnabled}
                      className="h-4 w-4 rounded border-zinc-700 bg-zinc-900 text-amber-400 focus:ring-amber-400"
                      disabled={actionInProgress === "update-service-profile" || actionInProgress === "help-most-loaded"}
                      type="checkbox"
                      onChange={(event) => {
                        void handleToggleService(serviceType.serviceTypeId, event.target.checked);
                      }}
                    />
                    <span className="min-w-0">
                      <span className="block truncate font-medium text-zinc-100" title={serviceType.serviceTypeName}>
                        {serviceType.serviceTypeName}
                      </span>
                      {serviceType.isFastTrack && <span className="ml-2 text-emerald-400">Greita paslauga</span>}
                    </span>
                  </span>
                  {loadSummary && (
                    <span className={getLoadStatusBadgeClassName(loadSummary.loadStatus)}>
                      {getLoadStatusLabel(loadSummary.loadStatus)}
                    </span>
                  )}
                </span>

                <span className="mt-1 flex flex-wrap gap-x-3 gap-y-0.5 text-zinc-400">
                  <span>Laukia: {waitingCount}</span>
                  <span>Seniausias: {oldestWaitingMinutes} min</span>
                  <span>Laukimas: {estimatedWaitMinutes} min</span>
                </span>

                {loadSummary?.recommendationMessage && loadSummary.recommendationMessage.trim().length > 0 && (
                  <span className="mt-1 block text-zinc-300">
                    Rekomendacija: {loadSummary.recommendationMessage}
                  </span>
                )}
              </label>
            );
          })}
        </div>

        {!isLoadingServiceProfile && serviceProfile.length > 0 && enabledServiceTypeIds.length === 0 && (
          <p className="mt-4 rounded-md border border-amber-500/40 bg-amber-500/10 px-3 py-2 text-sm text-amber-300">
            Nera ijungtu paslaugu. Ijunkite bent viena paslauga, kad galetumete kviesti kita bilieta.
          </p>
        )}

        <button
          className="mt-2 h-9 rounded-md border border-amber-400 bg-amber-400 px-4 text-sm font-semibold text-zinc-950 transition hover:bg-amber-300 disabled:cursor-not-allowed disabled:opacity-50"
          disabled={Boolean(actionInProgress)}
          onClick={() => {
            void handleHelpMostLoaded();
          }}
          type="button"
        >
          {actionInProgress === "help-most-loaded" ? "Tikrinama..." : "Padėti labiausiai apkrautai paslaugai"}
        </button>
      </section>

      <section className="grid min-h-0 flex-1 grid-cols-1 items-stretch gap-3 xl:grid-cols-[minmax(0,1.4fr)_minmax(320px,0.6fr)]">
        <div className="flex min-h-0 min-w-0 flex-col rounded-md border border-zinc-800 bg-zinc-900 p-3">
          <div className="flex shrink-0 flex-wrap items-center justify-between gap-2">
            <h2 className="text-lg font-semibold text-zinc-100">Laukianti eile</h2>
            <span className="rounded-md border border-zinc-800 bg-zinc-800 px-3 py-1 text-sm font-medium text-zinc-300">
              Laukia: {currentQueue.length}
            </span>
          </div>

          {(isLoadingQueue || isLoadingActive) && (
            <div className="mt-4 shrink-0">
              <LoadingSpinner />
            </div>
          )}

          {!isLoadingQueue && !isLoadingActive && currentQueue.length === 0 && (
            <p className="mt-4 text-sm text-zinc-400">Siuo metu laukianciu bilietu nera.</p>
          )}

          {!isLoadingQueue && !isLoadingActive && currentQueue.length > 0 && (
            <ul className="mt-3 min-h-0 flex-1 space-y-2 overflow-y-auto pr-1">
              {currentQueue.map((ticket: QueueTicketResponse) => {
                const waitMinutes = getWaitMinutes(ticket.queueEnteredAt ?? ticket.createdAt, nowMs);
                const isTicketNearMaxWait = isNearMaxWait(ticket, waitMinutes);

                return (
                  <li className="rounded-md border border-zinc-800 bg-zinc-950 px-3 py-2" key={ticket.ticketId}>
                    <div className="flex flex-wrap items-center justify-between gap-3">
                      <div className="min-w-0">
                        <div className="flex flex-wrap items-center gap-x-3 gap-y-1">
                          <p className="font-mono text-xl font-bold text-zinc-100">{ticket.ticketNumber}</p>
                          <p className="truncate text-sm text-zinc-300">{ticket.serviceTypeName}</p>
                        </div>
                        <p className="text-sm text-zinc-400">
                          {formatCreatedAt(ticket.queueEnteredAt ?? ticket.createdAt)} - laukia {waitMinutes} min
                        </p>
                      </div>
                      {(ticket.isFastTrack || isTicketNearMaxWait) && (
                        <div className="flex shrink-0 flex-wrap justify-end gap-2">
                          {ticket.isFastTrack && (
                            <span className="rounded-md border border-emerald-500/40 bg-emerald-500/15 px-2 py-1 text-sm text-emerald-400">
                              Greita paslauga
                            </span>
                          )}
                          {isTicketNearMaxWait && (
                            <span className="rounded-md border border-amber-500/40 bg-amber-500/15 px-2 py-1 text-sm text-amber-400">
                              Arteja laukimo riba
                            </span>
                          )}
                        </div>
                      )}
                    </div>
                  </li>
                );
              })}
            </ul>
          )}
        </div>

        <div className="grid min-h-0 min-w-0 grid-rows-[1fr_1.35fr_1.25fr_auto] gap-1.5 overflow-hidden">
          <div className="flex min-h-0 flex-col rounded-md border border-zinc-800 bg-zinc-900 p-2">
            <h2 className="shrink-0 text-base font-semibold text-zinc-100">Dabartinis bilietas</h2>
            {!currentTicket && <p className="mt-2 text-sm text-zinc-400">Siuo metu bilietas neaptarnaujamas.</p>}
            {currentTicket && (
              <div className="mt-1.5 flex min-h-0 flex-1 flex-col justify-center rounded-md border border-zinc-800 bg-zinc-950 px-3 py-2">
                <p className="font-mono text-3xl font-bold leading-tight text-zinc-100">{currentTicket.ticketNumber}</p>
                <p className="text-sm leading-tight text-zinc-300">
                  Busena: <span className="font-semibold">{getTicketStatusLabel(currentTicket.status)}</span>
                </p>
                <p className="truncate text-sm leading-tight text-zinc-400">{currentTicket.serviceTypeName}</p>
                {currentSelectionExplanation && currentSelectionExplanation.message.trim().length > 0 && (
                  <div className="mt-2 rounded-md border border-amber-500/30 bg-amber-500/10 px-2 py-1.5 text-sm text-zinc-200">
                    <p className="font-semibold text-amber-300">Parinkimo priežastis</p>
                    <p className="mt-0.5 leading-tight">{currentSelectionExplanation.message}</p>
                    {currentSelectionExplanation.finalScore != null && (
                      <dl className="mt-1 grid grid-cols-2 gap-x-3 gap-y-0.5 text-zinc-300">
                        <div>
                          <dt className="text-zinc-500">Prioritetas</dt>
                          <dd className="font-semibold">{formatExplanationNumber(currentSelectionExplanation.priorityLevel)}</dd>
                        </div>
                        <div>
                          <dt className="text-zinc-500">Laukimo laikas</dt>
                          <dd className="font-semibold">
                            {formatExplanationNumber(currentSelectionExplanation.waitingMinutes)} min
                          </dd>
                        </div>
                        <div>
                          <dt className="text-zinc-500">Greitos paslaugos priedas</dt>
                          <dd className="font-semibold">{formatExplanationBonus(currentSelectionExplanation.fastTrackBonus)}</dd>
                        </div>
                        <div>
                          <dt className="text-zinc-500">SLA priedas</dt>
                          <dd className="font-semibold">{formatExplanationBonus(currentSelectionExplanation.slaBoost)}</dd>
                        </div>
                        <div>
                          <dt className="text-zinc-500">Numatoma trukmė</dt>
                          <dd className="font-semibold">
                            {formatExplanationNumber(currentSelectionExplanation.estimatedDurationMinutes)} min
                          </dd>
                        </div>
                        <div>
                          <dt className="text-zinc-500">Galutinis įvertis</dt>
                          <dd className="font-semibold">{formatExplanationNumber(currentSelectionExplanation.finalScore)}</dd>
                        </div>
                      </dl>
                    )}
                  </div>
                )}
              </div>
            )}
          </div>

          <div className="flex min-h-0 flex-col rounded-md border border-zinc-800 bg-zinc-900 p-2">
            <div className="flex shrink-0 items-center justify-between gap-2">
              <h2 className="text-base font-semibold text-zinc-100">Neseniai užbaigti / neatvykę</h2>
              {isLoadingRecentHistory && <span className="text-sm text-zinc-400">Atnaujinama...</span>}
            </div>

            {!isLoadingRecentHistory && recentHistory.length === 0 && (
              <p className="mt-2 text-sm text-zinc-400">Naujausių bilietų nėra.</p>
            )}

            {recentHistory.length > 0 && (
              <ul className="mt-1.5 min-h-0 flex-1 space-y-1 overflow-y-auto pr-1">
                {recentHistory.map((ticket) => {
                  const recallButtonLabel = getRecallButtonLabel(ticket.recallMode);

                  return (
                    <li className="rounded-md border border-zinc-800 bg-zinc-950 px-2.5 py-1" key={ticket.ticketId}>
                      <div className="flex items-center justify-between gap-2">
                        <div className="min-w-0">
                          <p className="font-mono text-base font-bold leading-tight text-zinc-100">{ticket.ticketNumber}</p>
                          <p className="truncate text-sm leading-tight text-zinc-300">{ticket.serviceTypeName}</p>
                          <p className="text-sm leading-tight text-zinc-400">
                            {getTicketStatusLabel(ticket.ticketStatus)} - {getRecentHistoryTime(ticket)}
                          </p>
                        </div>

                        {ticket.recallMode === "AlreadyPending" && (
                          <span className="rounded-md border border-amber-500/40 bg-amber-500/10 px-2 py-1 text-right text-sm leading-tight text-amber-300">
                            Laukia pakartotinio kvietimo
                          </span>
                        )}

                        {ticket.canRecall && recallButtonLabel && (
                          <button
                            className="h-8 shrink-0 rounded-md border border-amber-400 bg-amber-400 px-2.5 text-sm font-semibold text-zinc-950 transition hover:bg-amber-300 disabled:cursor-not-allowed disabled:opacity-50"
                            disabled={Boolean(actionInProgress)}
                            onClick={() => {
                              void handleRecallTicket(ticket.ticketId);
                            }}
                            type="button"
                          >
                            {actionInProgress === "recall-ticket" ? "Kviečiama..." : recallButtonLabel}
                          </button>
                        )}
                      </div>
                    </li>
                  );
                })}
              </ul>
            )}
          </div>

          <div className="min-h-0 rounded-md border border-zinc-800 bg-zinc-900 p-2">
            <h2 className="text-base font-semibold text-zinc-100">Veiksmai</h2>
            <div className="mt-1.5 grid grid-cols-1 gap-1.5 sm:grid-cols-2">
              <button
                className="h-9 rounded-md bg-amber-400 px-3 text-sm font-semibold text-zinc-900 transition hover:bg-amber-300 disabled:cursor-not-allowed disabled:opacity-50"
                disabled={!canCallNext}
                onClick={() => {
                  void handleCallNext();
                }}
                type="button"
              >
                {actionInProgress === "call-next" ? "Vykdoma..." : "Kviesti kita"}
              </button>
              <button
                className="h-9 rounded-md bg-emerald-500 px-3 text-sm font-semibold text-white transition hover:bg-emerald-600 disabled:cursor-not-allowed disabled:opacity-50"
                disabled={!canMarkServing}
                onClick={() => {
                  void handleTicketAction("mark-serving");
                }}
                type="button"
              >
                {actionInProgress === "mark-serving" ? "Vykdoma..." : "Aptarnauju"}
              </button>
              <button
                className="h-9 rounded-md bg-zinc-600 px-3 text-sm font-semibold text-white transition hover:bg-zinc-700 disabled:cursor-not-allowed disabled:opacity-50"
                disabled={!canMarkDoneOrNoShow}
                onClick={() => {
                  void handleTicketAction("mark-done");
                }}
                type="button"
              >
                {actionInProgress === "mark-done" ? "Vykdoma..." : "Baigta"}
              </button>
              <button
                className="h-9 rounded-md bg-red-500 px-3 text-sm font-semibold text-white transition hover:bg-red-600 disabled:cursor-not-allowed disabled:opacity-50"
                disabled={!canMarkDoneOrNoShow}
                onClick={() => {
                  void handleTicketAction("mark-no-show");
                }}
                type="button"
              >
                {actionInProgress === "mark-no-show" ? "Vykdoma..." : "Neatvyko"}
              </button>
              <button
                className="h-9 rounded-md border border-amber-400 bg-zinc-950 px-3 text-sm font-semibold text-amber-300 transition hover:bg-zinc-800 disabled:cursor-not-allowed disabled:opacity-50"
                disabled={!canRedirectTicket}
                onClick={openRedirectDialog}
                type="button"
              >
                Nukreipti
              </button>
            </div>
          </div>

          <div className="rounded-md border border-zinc-800 bg-zinc-900 p-2">
            <div className="flex items-center justify-between gap-2">
              <h2 className="text-base font-semibold text-zinc-100">Langelio busena</h2>
              <button
                className="h-9 rounded-md border border-zinc-700 bg-zinc-800 px-3 text-sm font-semibold text-zinc-100 transition hover:bg-zinc-700"
                onClick={handleLogout}
                type="button"
              >
                Atsijungti
              </button>
            </div>

            <div className="mt-1.5 flex flex-col gap-1.5 sm:flex-row sm:items-center">
              <select
                className="h-9 rounded-md border border-zinc-700 bg-zinc-900 px-3 text-sm text-zinc-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-amber-400"
                value={counterStatusFormValue}
                onChange={(event) => {
                  if (isCounterStatus(event.target.value)) {
                    setCounterStatusFormValue(event.target.value);
                  }
                }}
              >
                <option value="Active">Aktyvus</option>
                <option value="Paused">Pauze</option>
                <option value="Closed">Uzdarytas</option>
              </select>

              <button
                className="h-9 rounded-md bg-amber-400 px-4 text-sm font-semibold text-zinc-900 transition hover:bg-amber-300 disabled:cursor-not-allowed disabled:opacity-50"
                disabled={!canSaveCounterStatus}
                onClick={() => {
                  void handleUpdateCounterStatus();
                }}
                type="button"
              >
                {actionInProgress === "update-counter-status" ? "Saugoma..." : "Atnaujinti"}
              </button>
            </div>

            {counterStatus !== "Active" && (
              <p className="mt-2 rounded-md border border-amber-200 bg-amber-50 px-3 py-1.5 text-sm text-amber-700">
                Kviesti kita bilieta negalima, nes langelio busena yra {getCounterStatusLabel(counterStatus).toLowerCase()}.
                Galite uzbaigti tik jau aktyvu bilieta.
              </p>
            )}
          </div>
        </div>
      </section>

      {isRedirectDialogOpen && currentTicket && (
        <div className="fixed inset-0 z-50 flex items-center justify-center overflow-y-auto bg-zinc-950/80 px-4 py-4">
          <section className="max-h-[90vh] w-full max-w-lg overflow-y-auto rounded-md border border-zinc-800 bg-zinc-900 p-5 text-zinc-100">
            <h2 className="text-lg font-semibold">Nukreipti bilietą</h2>
            <p className="mt-2 text-sm text-zinc-400">
              {currentTicket.ticketNumber} - {currentTicket.serviceTypeName}
            </p>

            <label className="mt-4 block text-sm font-medium text-zinc-200" htmlFor="redirect-service">
              Nauja paslauga
            </label>
            <select
              className="mt-2 h-11 w-full rounded-md border border-zinc-700 bg-zinc-950 px-3 text-sm text-zinc-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-amber-400"
              id="redirect-service"
              value={redirectTargetServiceId}
              onChange={(event) => setRedirectTargetServiceId(event.target.value)}
            >
              <option value="">Pasirinkite paslaugą</option>
              {redirectTargetServices
                .filter((serviceType) => serviceType.id !== currentTicket.serviceTypeId)
                .map((serviceType) => (
                  <option key={serviceType.id} value={serviceType.id}>
                    {serviceType.name}
                  </option>
                ))}
            </select>

            <label className="mt-4 block text-sm font-medium text-zinc-200" htmlFor="redirect-reason">
              Priežastis
            </label>
            <textarea
              className="mt-2 min-h-24 w-full rounded-md border border-zinc-700 bg-zinc-950 px-3 py-2 text-sm text-zinc-100 focus-visible:outline-none focus-visible:ring-2 focus-visible:ring-amber-400"
              id="redirect-reason"
              value={redirectReason}
              onChange={(event) => setRedirectReason(event.target.value)}
              placeholder="Pvz. pasirinkta netinkama paslauga"
            />

            <div className="mt-5 flex flex-wrap justify-end gap-2">
              <button
                className="h-10 rounded-md border border-zinc-700 px-4 text-sm font-semibold text-zinc-200 transition hover:bg-zinc-800"
                disabled={actionInProgress === "redirect-ticket"}
                onClick={() => setIsRedirectDialogOpen(false)}
                type="button"
              >
                Atšaukti
              </button>
              <button
                className="h-10 rounded-md bg-amber-400 px-4 text-sm font-semibold text-zinc-950 transition hover:bg-amber-300 disabled:cursor-not-allowed disabled:opacity-50"
                disabled={actionInProgress === "redirect-ticket"}
                onClick={() => {
                  void handleRedirectTicket();
                }}
                type="button"
              >
                {actionInProgress === "redirect-ticket" ? "Nukreipiama..." : "Nukreipti"}
              </button>
            </div>
          </section>
        </div>
      )}
      </div>
    </div>
  );
}
