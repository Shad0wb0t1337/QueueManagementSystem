import { useEffect, useState } from "react";
import type { ReactNode } from "react";
import { Button } from "@/components/ui/button";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { getMyManagerDepartments } from "@/services/departmentService";
import { getManagerDashboardData } from "@/services/managerStatisticsService";
import { getApiErrorMessage } from "@/utils/apiError";
import type {
  ManagerDepartment,
  ManagerCounterStatistics,
  ManagerOperatorStatistics,
  ManagerRecallStatistics,
  ManagerRedirectStatistics,
  ManagerServiceStatistics,
  ManagerStatisticsDashboardData,
  ManagerTicketEvent
} from "@/types";

function getTodayDateInputValue(): string {
  const now = new Date();
  const timezoneOffsetMs = now.getTimezoneOffset() * 60_000;
  return new Date(now.getTime() - timezoneOffsetMs).toISOString().slice(0, 10);
}

function addOneDay(dateValue: string): string {
  const date = new Date(`${dateValue}T00:00:00`);
  date.setDate(date.getDate() + 1);
  return date.toISOString().slice(0, 10);
}

function formatNumber(value: number | null | undefined): string {
  if (value === null || value === undefined) {
    return "—";
  }

  return new Intl.NumberFormat("lt-LT").format(value);
}

function formatMinutes(value: number | null | undefined): string {
  if (value === null || value === undefined) {
    return "—";
  }

  return `${new Intl.NumberFormat("lt-LT", { maximumFractionDigits: 1 }).format(value)} min.`;
}

function formatPercent(value: number | null | undefined): string {
  if (value === null || value === undefined) {
    return "—";
  }

  return `${new Intl.NumberFormat("lt-LT", { maximumFractionDigits: 1 }).format(value)} %`;
}

function formatDateTime(value: string): string {
  return new Date(value).toLocaleString("lt-LT", {
    year: "numeric",
    month: "2-digit",
    day: "2-digit",
    hour: "2-digit",
    minute: "2-digit"
  });
}

function getEventDetails(event: ManagerTicketEvent): string {
  if (event.eventType === "Redirected") {
    const route =
      event.fromServiceTypeName.length > 0 && event.toServiceTypeName.length > 0
        ? `${event.fromServiceTypeName} → ${event.toServiceTypeName}`
        : "";
    const reason = event.details.trim().length > 0 ? `Priežastis: ${event.details}` : "";
    return [route, reason].filter((item) => item.length > 0).join(" · ");
  }

  if (event.eventType === "RecallPending" || event.eventType === "Recalled") {
    return "Pakartotinis kvietimas";
  }

  return event.details;
}

function hasStatistics(data: ManagerStatisticsDashboardData): boolean {
  return (
    data.overview.issuedCount +
      data.overview.calledCount +
      data.overview.completedCount +
      data.overview.noShowCount +
      data.overview.redirectedCount +
      data.overview.recalledCount +
      data.overview.recallPendingCount >
    0
  );
}

function sumRedirectsByOperator(redirects: ManagerRedirectStatistics[]): Array<{ operatorName: string; count: number }> {
  const totals = new Map<string, number>();

  redirects.forEach((redirect) => {
    const operatorName = redirect.operatorName || "—";
    totals.set(operatorName, (totals.get(operatorName) ?? 0) + redirect.count);
  });

  return Array.from(totals.entries())
    .map(([operatorName, count]) => ({ operatorName, count }))
    .sort((first, second) => second.count - first.count);
}

function sumRecallsByCounter(recalls: ManagerRecallStatistics[]): Array<{ counterName: string; recalledCount: number; recallPendingCount: number }> {
  const totals = new Map<string, { recalledCount: number; recallPendingCount: number }>();

  recalls.forEach((recall) => {
    const counterName = recall.counterName || "—";
    const current = totals.get(counterName) ?? { recalledCount: 0, recallPendingCount: 0 };
    current.recalledCount += recall.recalledCount;
    current.recallPendingCount += recall.recallPendingCount;
    totals.set(counterName, current);
  });

  return Array.from(totals.entries())
    .map(([counterName, counts]) => ({ counterName, ...counts }))
    .sort((first, second) => second.recalledCount + second.recallPendingCount - first.recalledCount - first.recallPendingCount);
}

function sumRecallsByOperator(recalls: ManagerRecallStatistics[]): Array<{ operatorName: string; recalledCount: number; recallPendingCount: number }> {
  const totals = new Map<string, { recalledCount: number; recallPendingCount: number }>();

  recalls.forEach((recall) => {
    const operatorName = recall.operatorName || "—";
    const current = totals.get(operatorName) ?? { recalledCount: 0, recallPendingCount: 0 };
    current.recalledCount += recall.recalledCount;
    current.recallPendingCount += recall.recallPendingCount;
    totals.set(operatorName, current);
  });

  return Array.from(totals.entries())
    .map(([operatorName, counts]) => ({ operatorName, ...counts }))
    .sort((first, second) => second.recalledCount + second.recallPendingCount - first.recalledCount - first.recallPendingCount);
}

interface SectionProps {
  title: string;
  children: ReactNode;
}

interface InsightCard {
  title: string;
  primaryText: string;
  secondaryText?: string;
  hasData: boolean;
}

function Section({ title, children }: SectionProps): JSX.Element {
  return (
    <section className="min-w-0 rounded-md border border-zinc-800 bg-zinc-900">
      <div className="border-b border-zinc-800 px-4 py-3">
        <h2 className="text-base font-semibold text-zinc-100">{title}</h2>
      </div>
      <div className="min-w-0 p-3 sm:p-4">{children}</div>
    </section>
  );
}

function getTopByValue<T>(items: T[], getValue: (item: T) => number): T | null {
  let topItem: T | null = null;
  let topValue = 0;

  items.forEach((item) => {
    const value = getValue(item);
    if (value > topValue) {
      topItem = item;
      topValue = value;
    }
  });

  return topItem;
}

function getMostCommonRedirectRoute(redirects: ManagerRedirectStatistics[]): {
  fromServiceTypeName: string;
  toServiceTypeName: string;
  count: number;
} | null {
  const routeTotals = new Map<string, { fromServiceTypeName: string; toServiceTypeName: string; count: number }>();

  redirects.forEach((redirect) => {
    const key = `${redirect.fromServiceTypeId}-${redirect.toServiceTypeId}`;
    const current = routeTotals.get(key) ?? {
      fromServiceTypeName: redirect.fromServiceTypeName,
      toServiceTypeName: redirect.toServiceTypeName,
      count: 0
    };
    current.count += redirect.count;
    routeTotals.set(key, current);
  });

  return getTopByValue(Array.from(routeTotals.values()), (route) => route.count);
}

function buildInsightCards(data: ManagerStatisticsDashboardData): InsightCard[] {
  const highestWaitService = getTopByValue(data.services, (service) => service.averageWaitMinutes);
  const mostSlaBreachesService = getTopByValue(data.services, (service) => service.slaBreachCount);
  const busiestCounterByCompleted = getTopByValue(data.counters, (counter) => counter.completedCount);
  const busiestCounterByCalled = getTopByValue(data.counters, (counter) => counter.calledCount);
  const busiestCounter = busiestCounterByCompleted ?? busiestCounterByCalled;
  const busiestCounterValue = busiestCounterByCompleted?.completedCount ?? busiestCounterByCalled?.calledCount ?? 0;
  const busiestCounterLabel = busiestCounterByCompleted ? "aptarnauti bilietai" : "iškviesti bilietai";
  const mostCommonRedirectRoute = getMostCommonRedirectRoute(data.redirects);
  const highestNoShowService = getTopByValue(data.services, (service) => service.noShowCount);

  return [
    highestWaitService
      ? {
          title: "Ilgiausias vidutinis laukimas",
          primaryText: `${highestWaitService.serviceTypeName} — ${formatMinutes(highestWaitService.averageWaitMinutes)}`,
          hasData: true
        }
      : {
          title: "Ilgiausias vidutinis laukimas",
          primaryText: "Laukimo duomenų nėra",
          hasData: false
        },
    mostSlaBreachesService
      ? {
          title: "Daugiausia SLA pažeidimų",
          primaryText: `${mostSlaBreachesService.serviceTypeName} — ${formatNumber(mostSlaBreachesService.slaBreachCount)}`,
          hasData: true
        }
      : {
          title: "Daugiausia SLA pažeidimų",
          primaryText: "SLA pažeidimų nėra",
          hasData: false
        },
    busiestCounter
      ? {
          title: "Apkraučiausias langelis",
          primaryText: `${busiestCounter.counterName} — ${formatNumber(busiestCounterValue)} ${busiestCounterLabel}`,
          hasData: true
        }
      : {
          title: "Apkraučiausias langelis",
          primaryText: "Langelių apkrovos duomenų nėra",
          hasData: false
        },
    mostCommonRedirectRoute
      ? {
          title: "Dažniausias nukreipimas",
          primaryText: `${mostCommonRedirectRoute.fromServiceTypeName} → ${mostCommonRedirectRoute.toServiceTypeName}`,
          secondaryText: `${formatNumber(mostCommonRedirectRoute.count)} nukreipimai`,
          hasData: true
        }
      : {
          title: "Dažniausias nukreipimas",
          primaryText: "Nukreipimų nėra",
          hasData: false
        },
    highestNoShowService
      ? {
          title: "Daugiausia neatvykusių",
          primaryText: `${highestNoShowService.serviceTypeName} — ${formatNumber(highestNoShowService.noShowCount)}`,
          hasData: true
        }
      : {
          title: "Daugiausia neatvykusių",
          primaryText: "Neatvykimų nėra",
          hasData: false
        }
  ];
}

function ManagerInsightsSection({ data }: { data: ManagerStatisticsDashboardData }): JSX.Element {
  const insights = buildInsightCards(data);
  const hasAnyInsight = insights.some((insight) => insight.hasData);

  return (
    <section className="rounded-md border border-zinc-800 bg-zinc-900 p-4">
      <h2 className="text-base font-semibold text-zinc-100">Svarbiausios įžvalgos</h2>
      {!hasAnyInsight && <p className="mt-3 text-sm text-zinc-400">Pasirinktam laikotarpiui įžvalgų nėra.</p>}
      {hasAnyInsight && (
        <div className="mt-3 grid gap-3 sm:grid-cols-2 xl:grid-cols-5">
          {insights.map((insight) => (
            <article className="min-w-0 rounded-md border border-zinc-800 bg-zinc-950 p-3" key={insight.title}>
              <p className="text-sm font-medium text-zinc-400">{insight.title}</p>
              <p className="mt-2 text-base font-semibold leading-snug text-zinc-100">{insight.primaryText}</p>
              {insight.secondaryText && <p className="mt-1 text-sm text-zinc-400">{insight.secondaryText}</p>}
            </article>
          ))}
        </div>
      )}
    </section>
  );
}

interface EmptyTableProps {
  colSpan: number;
}

function EmptyTableRow({ colSpan }: EmptyTableProps): JSX.Element {
  return (
    <TableRow>
      <TableCell className="py-6 text-center text-zinc-400" colSpan={colSpan}>
        Pasirinktam laikotarpiui duomenų nėra.
      </TableCell>
    </TableRow>
  );
}

export function ManagerPage(): JSX.Element {
  const today = getTodayDateInputValue();
  const [fromDate, setFromDate] = useState<string>(today);
  const [toDate, setToDate] = useState<string>(today);
  const [departments, setDepartments] = useState<ManagerDepartment[]>([]);
  const [selectedDepartmentId, setSelectedDepartmentId] = useState<string>("all");
  const [data, setData] = useState<ManagerStatisticsDashboardData | null>(null);
  const [isLoading, setIsLoading] = useState<boolean>(true);
  const [isLoadingDepartments, setIsLoadingDepartments] = useState<boolean>(true);
  const [errorMessage, setErrorMessage] = useState<string | null>(null);

  async function loadStatistics(nextDepartments: ManagerDepartment[] = departments): Promise<void> {
    if (nextDepartments.length === 0) {
      setData(null);
      setIsLoading(false);
      return;
    }

    setIsLoading(true);
    setErrorMessage(null);

    try {
      const response = await getManagerDashboardData({
        from: fromDate,
        to: addOneDay(toDate),
        departmentId: selectedDepartmentId === "all" ? null : Number.parseInt(selectedDepartmentId, 10)
      });
      setData(response);
    } catch (error) {
      setErrorMessage(getApiErrorMessage(error, "Nepavyko įkelti statistikos duomenų."));
    } finally {
      setIsLoading(false);
    }
  }

  function handleDepartmentChange(value: string): void {
    setSelectedDepartmentId(value);
    void (async () => {
      if (departments.length === 0) {
        return;
      }

      setIsLoading(true);
      setErrorMessage(null);

      try {
        const response = await getManagerDashboardData({
          from: fromDate,
          to: addOneDay(toDate),
          departmentId: value === "all" ? null : Number.parseInt(value, 10)
        });
        setData(response);
      } catch (error) {
        setErrorMessage(getApiErrorMessage(error, "Nepavyko įkelti statistikos duomenų."));
      } finally {
        setIsLoading(false);
      }
    })();
  }

  useEffect(() => {
    void (async () => {
      setIsLoadingDepartments(true);
      setErrorMessage(null);
      try {
        const assignedDepartments = await getMyManagerDepartments();
        setDepartments(assignedDepartments);
        const onlyDepartment = assignedDepartments.length === 1 ? assignedDepartments[0] : null;
        if (onlyDepartment) {
          setSelectedDepartmentId(String(onlyDepartment.departmentId));
        }
        await loadStatistics(assignedDepartments);
      } catch (error) {
        setErrorMessage(getApiErrorMessage(error, "Nepavyko įkelti statistikos duomenų."));
        setIsLoading(false);
      } finally {
        setIsLoadingDepartments(false);
      }
    })();
  }, []);

  const overviewCards = data
    ? [
        { label: "Išduota bilietų", value: formatNumber(data.overview.issuedCount) },
        { label: "Iškviesta", value: formatNumber(data.overview.calledCount) },
        { label: "Aptarnauta", value: formatNumber(data.overview.completedCount) },
        { label: "Neatvyko", value: formatNumber(data.overview.noShowCount) },
        { label: "Nukreipta", value: formatNumber(data.overview.redirectedCount) },
        { label: "Pakartotinai kviesta", value: formatNumber(data.overview.recalledCount) },
        { label: "Laukia pakartotinio kvietimo", value: formatNumber(data.overview.recallPendingCount) },
        { label: "Vidutinis laukimas", value: formatMinutes(data.overview.averageWaitMinutes) },
        { label: "Vidutinė aptarnavimo trukmė", value: formatMinutes(data.overview.averageServiceMinutes) },
        { label: "SLA pažeidimai", value: formatNumber(data.overview.slaBreachCount) },
        { label: "SLA pažeidimų dalis", value: formatPercent(data.overview.slaBreachPercent) }
      ]
    : [];

  const redirectOperatorTotals = data ? sumRedirectsByOperator(data.redirects) : [];
  const recallCounterTotals = data ? sumRecallsByCounter(data.recalls) : [];
  const recallOperatorTotals = data ? sumRecallsByOperator(data.recalls) : [];
  const singleDepartment = departments.length === 1 ? departments[0] : null;

  return (
    <div className="space-y-4 overflow-x-hidden sm:space-y-6">
      <div className="flex min-w-0 flex-col gap-4 rounded-md border border-zinc-800 bg-zinc-900 p-4 xl:flex-row xl:items-end xl:justify-between">
        <div className="min-w-0">
          <h1 className="text-2xl font-semibold text-zinc-100">Skyriaus vadovo suvestinė</h1>
          {singleDepartment && (
            <p className="mt-2 text-sm font-medium text-amber-300">Skyrius: {singleDepartment.departmentName}</p>
          )}
        </div>

        <div className="min-w-0 space-y-2">
          <p className="text-sm text-zinc-400">Operacinė statistika pagal pasirinktą laikotarpį.</p>
          <div className="grid gap-3 sm:grid-cols-[1fr_1fr_auto] xl:grid-cols-[1fr_1fr_1fr_auto] xl:items-end">
          {departments.length > 1 && (
            <div className="space-y-2">
              <Label className="text-zinc-300" htmlFor="manager-department">
                Skyrius
              </Label>
              <Select value={selectedDepartmentId} onValueChange={handleDepartmentChange}>
                <SelectTrigger className="border-zinc-700 bg-zinc-950 text-zinc-100" id="manager-department">
                  <SelectValue placeholder="Pasirinkite skyrių" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="all">Visi mano skyriai</SelectItem>
                  {departments.map((department) => (
                    <SelectItem key={department.departmentId} value={String(department.departmentId)}>
                      {department.departmentName}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
            </div>
          )}
          <div className="space-y-2">
            <Label className="text-zinc-300" htmlFor="manager-from-date">
              Data nuo
            </Label>
            <Input
              className="border-zinc-700 bg-zinc-950 text-zinc-100"
              id="manager-from-date"
              type="date"
              value={fromDate}
              onChange={(event) => setFromDate(event.target.value)}
            />
          </div>
          <div className="space-y-2">
            <Label className="text-zinc-300" htmlFor="manager-to-date">
              Data iki
            </Label>
            <Input
              className="border-zinc-700 bg-zinc-950 text-zinc-100"
              id="manager-to-date"
              type="date"
              value={toDate}
              onChange={(event) => setToDate(event.target.value)}
            />
          </div>
          <Button
            className="h-10 rounded-md bg-amber-400 text-zinc-950 hover:bg-amber-300"
            disabled={isLoading || departments.length === 0 || fromDate.length === 0 || toDate.length === 0}
            onClick={() => void loadStatistics()}
          >
            Atnaujinti
          </Button>
          </div>
        </div>
      </div>

      {(isLoading || isLoadingDepartments) && (
        <div className="rounded-md border border-zinc-800 bg-zinc-900 p-6 text-zinc-300">Kraunami statistikos duomenys...</div>
      )}

      {!isLoading && !isLoadingDepartments && errorMessage && (
        <div className="rounded-md border border-red-500/40 bg-red-500/15 p-4 text-sm text-red-300">{errorMessage}</div>
      )}

      {!isLoading && !isLoadingDepartments && !errorMessage && departments.length === 0 && (
        <div className="rounded-md border border-amber-500/40 bg-amber-500/10 p-4 text-sm text-amber-200">
          Jums nepriskirtas nė vienas skyrius. Kreipkitės į administratorių.
        </div>
      )}

      {!isLoading && !isLoadingDepartments && !errorMessage && data && !hasStatistics(data) && (
        <div className="rounded-md border border-zinc-800 bg-zinc-900 p-4 text-sm text-zinc-400">
          Pasirinktam laikotarpiui duomenų nėra.
        </div>
      )}

      {!isLoading && !isLoadingDepartments && !errorMessage && data && (
        <>
          <ManagerInsightsSection data={data} />

          <div className="grid gap-3 sm:grid-cols-2 lg:grid-cols-4 2xl:grid-cols-6">
            {overviewCards.map((card) => (
              <div className="min-w-0 rounded-md border border-zinc-800 bg-zinc-900 p-4" key={card.label}>
                <p className="text-sm text-zinc-400">{card.label}</p>
                <p className="mt-2 text-2xl font-semibold text-zinc-100">{card.value}</p>
              </div>
            ))}
          </div>

          <ServiceStatisticsTable services={data.services} />
          <CounterStatisticsTable counters={data.counters} />
          <OperatorStatisticsTable operators={data.operators} />
          <RedirectStatisticsTable redirects={data.redirects} operatorTotals={redirectOperatorTotals} />
          <RecallStatisticsTable recalls={data.recalls} counterTotals={recallCounterTotals} operatorTotals={recallOperatorTotals} />
          <RecentTicketEventsTable events={data.events} />
        </>
      )}
    </div>
  );
}

function RecentTicketEventsTable({ events }: { events: ManagerTicketEvent[] }): JSX.Element {
  return (
    <Section title="Paskutiniai įvykiai">
      <div className="overflow-x-auto">
        <Table>
          <TableHeader>
            <TableRow className="border-zinc-800 hover:bg-transparent">
              <TableHead className="text-zinc-400">Laikas</TableHead>
              <TableHead className="text-zinc-400">Bilietas</TableHead>
              <TableHead className="text-zinc-400">Įvykis</TableHead>
              <TableHead className="text-zinc-400">Langelis</TableHead>
              <TableHead className="text-zinc-400">Operatorius</TableHead>
              <TableHead className="text-zinc-400">Paslauga</TableHead>
              <TableHead className="text-zinc-400">Detalės</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {events.length === 0 && (
              <TableRow>
                <TableCell className="py-6 text-center text-zinc-400" colSpan={7}>
                  Pasirinktam laikotarpiui įvykių nėra.
                </TableCell>
              </TableRow>
            )}
            {events.map((event) => (
              <TableRow className="border-zinc-800 hover:bg-zinc-800/50" key={event.eventId}>
                <TableCell className="whitespace-nowrap text-zinc-300">{formatDateTime(event.createdAt)}</TableCell>
                <TableCell className="font-mono font-semibold text-zinc-100">{event.ticketNumber}</TableCell>
                <TableCell className="min-w-44 text-zinc-300">{event.eventLabel}</TableCell>
                <TableCell className="text-zinc-300">{event.counterName || "—"}</TableCell>
                <TableCell className="text-zinc-300">{event.operatorName || "—"}</TableCell>
                <TableCell className="min-w-44 text-zinc-300">{event.serviceTypeName || "—"}</TableCell>
                <TableCell className="min-w-64 text-zinc-400">{getEventDetails(event) || "—"}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>
    </Section>
  );
}

function ServiceStatisticsTable({ services }: { services: ManagerServiceStatistics[] }): JSX.Element {
  return (
    <Section title="Paslaugų statistika">
      <Table>
        <TableHeader>
          <TableRow className="border-zinc-800 hover:bg-transparent">
            <TableHead className="text-zinc-400">Paslauga</TableHead>
            <TableHead className="text-zinc-400">Išduota</TableHead>
            <TableHead className="text-zinc-400">Iškviesta</TableHead>
            <TableHead className="text-zinc-400">Aptarnauta</TableHead>
            <TableHead className="text-zinc-400">Neatvyko</TableHead>
            <TableHead className="text-zinc-400">Nukreipta</TableHead>
            <TableHead className="text-zinc-400">Pakartotinai</TableHead>
            <TableHead className="text-zinc-400">Vid. laukimas</TableHead>
            <TableHead className="text-zinc-400">Vid. aptarnavimas</TableHead>
            <TableHead className="text-zinc-400">SLA</TableHead>
            <TableHead className="text-zinc-400">SLA %</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {services.length === 0 && <EmptyTableRow colSpan={11} />}
          {services.map((service) => (
            <TableRow className="border-zinc-800 hover:bg-zinc-800/50" key={service.serviceTypeId}>
              <TableCell className="font-medium text-zinc-100">{service.serviceTypeName}</TableCell>
              <TableCell className="text-zinc-300">{formatNumber(service.issuedCount)}</TableCell>
              <TableCell className="text-zinc-300">{formatNumber(service.calledCount)}</TableCell>
              <TableCell className="text-zinc-300">{formatNumber(service.completedCount)}</TableCell>
              <TableCell className="text-zinc-300">{formatNumber(service.noShowCount)}</TableCell>
              <TableCell className="text-zinc-300">{formatNumber(service.redirectedCount)}</TableCell>
              <TableCell className="text-zinc-300">{formatNumber(service.recalledCount)}</TableCell>
              <TableCell className="text-zinc-300">{formatMinutes(service.averageWaitMinutes)}</TableCell>
              <TableCell className="text-zinc-300">{formatMinutes(service.averageServiceMinutes)}</TableCell>
              <TableCell className="text-zinc-300">{formatNumber(service.slaBreachCount)}</TableCell>
              <TableCell className="text-zinc-300">{formatPercent(service.slaBreachPercent)}</TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </Section>
  );
}

function CounterStatisticsTable({ counters }: { counters: ManagerCounterStatistics[] }): JSX.Element {
  return (
    <Section title="Langelių statistika">
      <Table>
        <TableHeader>
          <TableRow className="border-zinc-800 hover:bg-transparent">
            <TableHead className="text-zinc-400">Langelis</TableHead>
            <TableHead className="text-zinc-400">Iškviesta</TableHead>
            <TableHead className="text-zinc-400">Aptarnauta</TableHead>
            <TableHead className="text-zinc-400">Neatvyko</TableHead>
            <TableHead className="text-zinc-400">Nukreipta</TableHead>
            <TableHead className="text-zinc-400">Pakartotinai kviesta</TableHead>
            <TableHead className="text-zinc-400">Laukia pakartojimo</TableHead>
            <TableHead className="text-zinc-400">Vid. aptarnavimas</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {counters.length === 0 && <EmptyTableRow colSpan={8} />}
          {counters.map((counter) => (
            <TableRow className="border-zinc-800 hover:bg-zinc-800/50" key={counter.counterId}>
              <TableCell className="font-medium text-zinc-100">{counter.counterName}</TableCell>
              <TableCell className="text-zinc-300">{formatNumber(counter.calledCount)}</TableCell>
              <TableCell className="text-zinc-300">{formatNumber(counter.completedCount)}</TableCell>
              <TableCell className="text-zinc-300">{formatNumber(counter.noShowCount)}</TableCell>
              <TableCell className="text-zinc-300">{formatNumber(counter.redirectedCount)}</TableCell>
              <TableCell className="text-zinc-300">{formatNumber(counter.recalledCount)}</TableCell>
              <TableCell className="text-zinc-300">{formatNumber(counter.recallPendingCount)}</TableCell>
              <TableCell className="text-zinc-300">{formatMinutes(counter.averageServiceMinutes)}</TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </Section>
  );
}

function OperatorStatisticsTable({ operators }: { operators: ManagerOperatorStatistics[] }): JSX.Element {
  return (
    <Section title="Operatorių statistika">
      <Table>
        <TableHeader>
          <TableRow className="border-zinc-800 hover:bg-transparent">
            <TableHead className="text-zinc-400">Operatorius</TableHead>
            <TableHead className="text-zinc-400">Iškviesta</TableHead>
            <TableHead className="text-zinc-400">Aptarnauta</TableHead>
            <TableHead className="text-zinc-400">Neatvyko</TableHead>
            <TableHead className="text-zinc-400">Nukreipta</TableHead>
            <TableHead className="text-zinc-400">Pakartotinai kviesta</TableHead>
            <TableHead className="text-zinc-400">Laukia pakartojimo</TableHead>
            <TableHead className="text-zinc-400">Vid. aptarnavimas</TableHead>
          </TableRow>
        </TableHeader>
        <TableBody>
          {operators.length === 0 && <EmptyTableRow colSpan={8} />}
          {operators.map((operator) => (
            <TableRow className="border-zinc-800 hover:bg-zinc-800/50" key={operator.operatorId}>
              <TableCell className="font-medium text-zinc-100">{operator.operatorName}</TableCell>
              <TableCell className="text-zinc-300">{formatNumber(operator.calledCount)}</TableCell>
              <TableCell className="text-zinc-300">{formatNumber(operator.completedCount)}</TableCell>
              <TableCell className="text-zinc-300">{formatNumber(operator.noShowCount)}</TableCell>
              <TableCell className="text-zinc-300">{formatNumber(operator.redirectedCount)}</TableCell>
              <TableCell className="text-zinc-300">{formatNumber(operator.recalledCount)}</TableCell>
              <TableCell className="text-zinc-300">{formatNumber(operator.recallPendingCount)}</TableCell>
              <TableCell className="text-zinc-300">{formatMinutes(operator.averageServiceMinutes)}</TableCell>
            </TableRow>
          ))}
        </TableBody>
      </Table>
    </Section>
  );
}

function RedirectStatisticsTable({
  redirects,
  operatorTotals
}: {
  redirects: ManagerRedirectStatistics[];
  operatorTotals: Array<{ operatorName: string; count: number }>;
}): JSX.Element {
  const totalRedirects = redirects.reduce((sum, redirect) => sum + redirect.count, 0);

  return (
    <Section title="Nukreipimų statistika">
      <div className="mb-4 text-sm text-zinc-300">Iš viso nukreipimų: {formatNumber(totalRedirects)}</div>
      <div className="grid gap-4 xl:grid-cols-2">
        <Table>
          <TableHeader>
            <TableRow className="border-zinc-800 hover:bg-transparent">
              <TableHead className="text-zinc-400">Iš paslaugos</TableHead>
              <TableHead className="text-zinc-400">Į paslaugą</TableHead>
              <TableHead className="text-zinc-400">Operatorius</TableHead>
              <TableHead className="text-zinc-400">Kiekis</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {redirects.length === 0 && <EmptyTableRow colSpan={4} />}
            {redirects.map((redirect) => (
              <TableRow
                className="border-zinc-800 hover:bg-zinc-800/50"
                key={`${redirect.fromServiceTypeId}-${redirect.toServiceTypeId}-${redirect.operatorId}`}
              >
                <TableCell className="text-zinc-300">{redirect.fromServiceTypeName}</TableCell>
                <TableCell className="text-zinc-300">{redirect.toServiceTypeName}</TableCell>
                <TableCell className="text-zinc-300">{redirect.operatorName || "—"}</TableCell>
                <TableCell className="text-zinc-300">{formatNumber(redirect.count)}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>

        <Table>
          <TableHeader>
            <TableRow className="border-zinc-800 hover:bg-transparent">
              <TableHead className="text-zinc-400">Operatorius</TableHead>
              <TableHead className="text-zinc-400">Kiekis</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {operatorTotals.length === 0 && <EmptyTableRow colSpan={2} />}
            {operatorTotals.map((operator) => (
              <TableRow className="border-zinc-800 hover:bg-zinc-800/50" key={operator.operatorName}>
                <TableCell className="text-zinc-300">{operator.operatorName}</TableCell>
                <TableCell className="text-zinc-300">{formatNumber(operator.count)}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>
    </Section>
  );
}

function RecallStatisticsTable({
  recalls,
  counterTotals,
  operatorTotals
}: {
  recalls: ManagerRecallStatistics[];
  counterTotals: Array<{ counterName: string; recalledCount: number; recallPendingCount: number }>;
  operatorTotals: Array<{ operatorName: string; recalledCount: number; recallPendingCount: number }>;
}): JSX.Element {
  const totalRecalled = recalls.reduce((sum, recall) => sum + recall.recalledCount, 0);
  const totalPending = recalls.reduce((sum, recall) => sum + recall.recallPendingCount, 0);

  return (
    <Section title="Pakartotinių kvietimų statistika">
      <div className="mb-4 flex flex-wrap gap-4 text-sm text-zinc-300">
        <span>Pakartotinai kviesta: {formatNumber(totalRecalled)}</span>
        <span>Laukia pakartotinio kvietimo: {formatNumber(totalPending)}</span>
      </div>
      <div className="grid gap-4 xl:grid-cols-2">
        <Table>
          <TableHeader>
            <TableRow className="border-zinc-800 hover:bg-transparent">
              <TableHead className="text-zinc-400">Langelis</TableHead>
              <TableHead className="text-zinc-400">Pakartotinai kviesta</TableHead>
              <TableHead className="text-zinc-400">Laukia pakartojimo</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {counterTotals.length === 0 && <EmptyTableRow colSpan={3} />}
            {counterTotals.map((counter) => (
              <TableRow className="border-zinc-800 hover:bg-zinc-800/50" key={counter.counterName}>
                <TableCell className="text-zinc-300">{counter.counterName}</TableCell>
                <TableCell className="text-zinc-300">{formatNumber(counter.recalledCount)}</TableCell>
                <TableCell className="text-zinc-300">{formatNumber(counter.recallPendingCount)}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>

        <Table>
          <TableHeader>
            <TableRow className="border-zinc-800 hover:bg-transparent">
              <TableHead className="text-zinc-400">Operatorius</TableHead>
              <TableHead className="text-zinc-400">Pakartotinai kviesta</TableHead>
              <TableHead className="text-zinc-400">Laukia pakartojimo</TableHead>
            </TableRow>
          </TableHeader>
          <TableBody>
            {operatorTotals.length === 0 && <EmptyTableRow colSpan={3} />}
            {operatorTotals.map((operator) => (
              <TableRow className="border-zinc-800 hover:bg-zinc-800/50" key={operator.operatorName}>
                <TableCell className="text-zinc-300">{operator.operatorName}</TableCell>
                <TableCell className="text-zinc-300">{formatNumber(operator.recalledCount)}</TableCell>
                <TableCell className="text-zinc-300">{formatNumber(operator.recallPendingCount)}</TableCell>
              </TableRow>
            ))}
          </TableBody>
        </Table>
      </div>
    </Section>
  );
}
