import { useEffect, useMemo, useState } from "react";
import axios from "axios";
import { LoadingSpinner } from "@/components/shared/LoadingSpinner";
import { Badge } from "@/components/ui/badge";
import { Button } from "@/components/ui/button";
import {
  Dialog,
  DialogContent,
  DialogDescription,
  DialogFooter,
  DialogHeader,
  DialogTitle
} from "@/components/ui/dialog";
import { Input } from "@/components/ui/input";
import { Label } from "@/components/ui/label";
import { Select, SelectContent, SelectItem, SelectTrigger, SelectValue } from "@/components/ui/select";
import { Table, TableBody, TableCell, TableHead, TableHeader, TableRow } from "@/components/ui/table";
import { Tabs, TabsContent, TabsList, TabsTrigger } from "@/components/ui/tabs";
import { createCounter, getCounters, updateCounter } from "@/services/counterService";
import {
  createDepartment,
  deactivateDepartment,
  getDepartments,
  getManagerUsers,
  updateDepartment,
  updateManagerDepartments
} from "@/services/departmentService";
import { createOperator, getOperators, updateOperator } from "@/services/operatorService";
import { getServiceLoad } from "@/services/serviceLoadService";
import { createServiceType, getServiceTypes, updateServiceType } from "@/services/serviceTypeService";
import { getActiveTickets, getTodayStats } from "@/services/ticketService";
import { getApiErrorMessage } from "@/utils/apiError";
import type {
  CounterStatus,
  Counter,
  CreateDepartmentRequest,
  CreateCounterRequest,
  CreateOperatorRequest,
  CreateServiceTypeRequest,
  Department,
  Operator,
  QueueStats,
  ServiceType,
  LoadStatus,
  ServiceLoadSummary,
  ManagerUser,
  UpdateDepartmentRequest,
  UpdateCounterRequest,
  UpdateOperatorRequest,
  UpdateServiceTypeRequest
} from "@/types";

type TabKey = "services" | "counters" | "operators" | "departments" | "stats" | "load";
type BooleanChoice = "true" | "false";

interface ServiceFormState {
  name: string;
  estimatedDurationMinutes: string;
  isActive: BooleanChoice;
  priorityLevel: string;
  isFastTrack: BooleanChoice;
  maxWaitMinutes: string;
}

interface ServiceFormErrors {
  name?: string;
  estimatedDurationMinutes?: string;
  priorityLevel?: string;
  maxWaitMinutes?: string;
}

interface CounterFormState {
  name: string;
  displayOrder: string;
  departmentId: string;
  serviceTypeIds: string[];
  status: CounterStatus;
}

interface CounterFormErrors {
  name?: string;
  displayOrder?: string;
  departmentId?: string;
  serviceTypeIds?: string;
}

interface DepartmentFormState {
  name: string;
  description: string;
  isActive: BooleanChoice;
}

interface DepartmentFormErrors {
  name?: string;
  description?: string;
}

interface OperatorFormState {
  name: string;
  counterId: string;
  isActive: BooleanChoice;
}

interface OperatorFormErrors {
  name?: string;
  counterId?: string;
}

interface ApiValidationErrors {
  [key: string]: string;
}

function createDefaultServiceFormState(): ServiceFormState {
  return {
    name: "",
    estimatedDurationMinutes: "10",
    isActive: "true",
    priorityLevel: "3",
    isFastTrack: "false",
    maxWaitMinutes: ""
  };
}

function createDefaultCounterFormState(): CounterFormState {
  return {
    name: "",
    displayOrder: "",
    departmentId: "",
    serviceTypeIds: [],
    status: "Active"
  };
}

function createDefaultDepartmentFormState(): DepartmentFormState {
  return {
    name: "",
    description: "",
    isActive: "true"
  };
}

function createDefaultOperatorFormState(): OperatorFormState {
  return {
    name: "",
    counterId: "",
    isActive: "true"
  };
}

function isRecord(value: unknown): value is Record<string, unknown> {
  return typeof value === "object" && value !== null;
}

function getErrorMessage(error: unknown, fallbackMessage: string): string {
  return getApiErrorMessage(error, fallbackMessage);
}

function getValidationErrors(error: unknown): ApiValidationErrors {
  if (!axios.isAxiosError(error)) {
    return {};
  }

  const data = error.response?.data;
  if (!isRecord(data)) {
    return {};
  }

  const rawErrors = data.errors;
  if (!isRecord(rawErrors)) {
    return {};
  }

  const result: ApiValidationErrors = {};
  for (const [key, value] of Object.entries(rawErrors)) {
    const normalizedKey = key.toLowerCase();

    if (typeof value === "string") {
      result[normalizedKey] = value;
      continue;
    }

    if (Array.isArray(value) && value.length > 0) {
      const firstMessage = value[0];
      if (typeof firstMessage === "string") {
        result[normalizedKey] = firstMessage;
      }
    }
  }

  return result;
}

function getFieldValidationError(errors: ApiValidationErrors, fieldName: string): string | undefined {
  const normalizedField = fieldName.toLowerCase();

  for (const [key, value] of Object.entries(errors)) {
    if (key === normalizedField || key.endsWith(`.${normalizedField}`)) {
      return value;
    }
  }

  return undefined;
}

function getStatusBadge(isActive: boolean): JSX.Element {
  if (isActive) {
    return <Badge className="rounded-md border-emerald-200 bg-emerald-100 text-emerald-700">Aktyvus</Badge>;
  }

  return <Badge className="rounded-md border-zinc-800 bg-zinc-800 text-zinc-300">Neaktyvus</Badge>;
}

function isCounterStatus(value: string): value is CounterStatus {
  return value === "Active" || value === "Paused" || value === "Closed";
}

function getCounterStatusBadge(status: CounterStatus): JSX.Element {
  if (status === "Active") {
    return <Badge className="rounded-md border-emerald-200 bg-emerald-100 text-emerald-700">Aktyvus</Badge>;
  }

  if (status === "Paused") {
    return <Badge className="rounded-md border-amber-200 bg-amber-100 text-amber-700">Pauze</Badge>;
  }

  return <Badge className="rounded-md border-red-200 bg-red-100 text-red-700">Uzdarytas</Badge>;
}

function getLoadStatusLabel(status: LoadStatus): string {
  if (status === "Low") {
    return "Maza";
  }

  if (status === "Normal") {
    return "Normali";
  }

  if (status === "High") {
    return "Didele";
  }

  return "Kritine";
}

function getLoadStatusBadge(status: LoadStatus): JSX.Element {
  if (status === "Critical") {
    return <Badge className="rounded-md border-red-500/40 bg-red-500/15 text-red-400">Kritine</Badge>;
  }

  if (status === "High") {
    return <Badge className="rounded-md border-amber-500/40 bg-amber-500/15 text-amber-400">Didele</Badge>;
  }

  if (status === "Normal") {
    return <Badge className="rounded-md border-emerald-500/40 bg-emerald-500/15 text-emerald-400">Normali</Badge>;
  }

  return <Badge className="rounded-md border-zinc-700 bg-zinc-800 text-zinc-300">Maza</Badge>;
}

export function AdminPage(): JSX.Element {
  const [activeTab, setActiveTab] = useState<TabKey>("services");

  const [services, setServices] = useState<ServiceType[]>([]);
  const [counters, setCounters] = useState<Counter[]>([]);
  const [operators, setOperators] = useState<Operator[]>([]);
  const [departments, setDepartments] = useState<Department[]>([]);
  const [managerUsers, setManagerUsers] = useState<ManagerUser[]>([]);
  const [stats, setStats] = useState<QueueStats | null>(null);
  const [serviceLoad, setServiceLoad] = useState<ServiceLoadSummary[]>([]);

  const [isLoadingServices, setIsLoadingServices] = useState<boolean>(true);
  const [isLoadingCounters, setIsLoadingCounters] = useState<boolean>(true);
  const [isLoadingOperators, setIsLoadingOperators] = useState<boolean>(true);
  const [isLoadingDepartments, setIsLoadingDepartments] = useState<boolean>(true);
  const [isLoadingManagers, setIsLoadingManagers] = useState<boolean>(true);
  const [isLoadingStats, setIsLoadingStats] = useState<boolean>(true);
  const [isLoadingServiceLoad, setIsLoadingServiceLoad] = useState<boolean>(true);
  const [isRefreshingStats, setIsRefreshingStats] = useState<boolean>(false);

  const [servicesError, setServicesError] = useState<string | null>(null);
  const [countersError, setCountersError] = useState<string | null>(null);
  const [operatorsError, setOperatorsError] = useState<string | null>(null);
  const [departmentsError, setDepartmentsError] = useState<string | null>(null);
  const [statsError, setStatsError] = useState<string | null>(null);
  const [serviceLoadError, setServiceLoadError] = useState<string | null>(null);
  const [successMessage, setSuccessMessage] = useState<string | null>(null);

  const [serviceDialogOpen, setServiceDialogOpen] = useState<boolean>(false);
  const [counterDialogOpen, setCounterDialogOpen] = useState<boolean>(false);
  const [operatorDialogOpen, setOperatorDialogOpen] = useState<boolean>(false);
  const [departmentDialogOpen, setDepartmentDialogOpen] = useState<boolean>(false);

  const [editingService, setEditingService] = useState<ServiceType | null>(null);
  const [editingCounter, setEditingCounter] = useState<Counter | null>(null);
  const [editingOperator, setEditingOperator] = useState<Operator | null>(null);
  const [editingDepartment, setEditingDepartment] = useState<Department | null>(null);

  const [serviceForm, setServiceForm] = useState<ServiceFormState>(createDefaultServiceFormState());
  const [counterForm, setCounterForm] = useState<CounterFormState>(createDefaultCounterFormState());
  const [operatorForm, setOperatorForm] = useState<OperatorFormState>(createDefaultOperatorFormState());
  const [departmentForm, setDepartmentForm] = useState<DepartmentFormState>(createDefaultDepartmentFormState());

  const [serviceFormErrors, setServiceFormErrors] = useState<ServiceFormErrors>({});
  const [counterFormErrors, setCounterFormErrors] = useState<CounterFormErrors>({});
  const [operatorFormErrors, setOperatorFormErrors] = useState<OperatorFormErrors>({});
  const [departmentFormErrors, setDepartmentFormErrors] = useState<DepartmentFormErrors>({});

  const [isSubmittingService, setIsSubmittingService] = useState<boolean>(false);
  const [isSubmittingCounter, setIsSubmittingCounter] = useState<boolean>(false);
  const [isSubmittingOperator, setIsSubmittingOperator] = useState<boolean>(false);
  const [isSubmittingDepartment, setIsSubmittingDepartment] = useState<boolean>(false);
  const [savingManagerId, setSavingManagerId] = useState<number | null>(null);
  const [deactivatingDepartmentId, setDeactivatingDepartmentId] = useState<number | null>(null);
  const [toggleServiceId, setToggleServiceId] = useState<number | null>(null);
  const [counterHasActiveTicket, setCounterHasActiveTicket] = useState<boolean>(false);

  const averageWaitMinutes = useMemo<number>(() => {
    const activeServices = services.filter((serviceType) => serviceType.isActive);
    if (activeServices.length === 0) {
      return 0;
    }

    const totalEstimatedWait = activeServices.reduce((sum, serviceType) => sum + serviceType.estimatedWaitMinutes, 0);
    return Math.round(totalEstimatedWait / activeServices.length);
  }, [services]);

  const serviceStatsRows = useMemo<ServiceType[]>(() => {
    return [...services].sort((first, second) => first.name.localeCompare(second.name, "lt-LT"));
  }, [services]);

  const serviceLoadRows = useMemo<ServiceLoadSummary[]>(() => {
    return [...serviceLoad].sort((first, second) => {
      if (first.loadStatus === "Critical" && second.loadStatus !== "Critical") {
        return -1;
      }

      if (first.loadStatus !== "Critical" && second.loadStatus === "Critical") {
        return 1;
      }

      return second.servicePressure - first.servicePressure;
    });
  }, [serviceLoad]);

  useEffect(() => {
    if (!successMessage) {
      return;
    }

    const timeoutId = window.setTimeout(() => {
      setSuccessMessage(null);
    }, 3500);

    return () => {
      window.clearTimeout(timeoutId);
    };
  }, [successMessage]);

  useEffect(() => {
    void Promise.all([loadServices(), loadCounters(), loadOperators(), loadDepartments(), loadManagerUsers(), loadStats(), loadServiceLoad()]);
  }, []);

  async function loadServices(): Promise<void> {
    setIsLoadingServices(true);
    setServicesError(null);

    try {
      const result = await getServiceTypes(true);
      setServices(result);
    } catch (error) {
      setServicesError(getErrorMessage(error, "Nepavyko ikelti paslaugu tipu."));
    } finally {
      setIsLoadingServices(false);
    }
  }

  async function loadCounters(): Promise<void> {
    setIsLoadingCounters(true);
    setCountersError(null);

    try {
      const result = await getCounters();
      setCounters(result);
    } catch (error) {
      setCountersError(getErrorMessage(error, "Nepavyko ikelti langeliu."));
    } finally {
      setIsLoadingCounters(false);
    }
  }

  async function loadOperators(): Promise<void> {
    setIsLoadingOperators(true);
    setOperatorsError(null);

    try {
      const result = await getOperators();
      setOperators(result);
    } catch (error) {
      setOperatorsError(getErrorMessage(error, "Nepavyko ikelti operatoriu."));
    } finally {
      setIsLoadingOperators(false);
    }
  }

  async function loadDepartments(): Promise<void> {
    setIsLoadingDepartments(true);
    setDepartmentsError(null);

    try {
      const result = await getDepartments();
      setDepartments(result);
    } catch (error) {
      setDepartmentsError(getErrorMessage(error, "Nepavyko ikelti skyriu."));
    } finally {
      setIsLoadingDepartments(false);
    }
  }

  async function loadManagerUsers(): Promise<void> {
    setIsLoadingManagers(true);
    setDepartmentsError(null);

    try {
      const result = await getManagerUsers();
      setManagerUsers(result);
    } catch (error) {
      setDepartmentsError(getErrorMessage(error, "Nepavyko ikelti vadovu."));
    } finally {
      setIsLoadingManagers(false);
    }
  }

  async function loadStats(isRefresh = false): Promise<void> {
    if (isRefresh) {
      setIsRefreshingStats(true);
    } else {
      setIsLoadingStats(true);
    }
    setStatsError(null);

    try {
      const result = await getTodayStats();
      setStats(result);
    } catch (error) {
      setStatsError(getErrorMessage(error, "Nepavyko ikelti statistikos."));
    } finally {
      if (isRefresh) {
        setIsRefreshingStats(false);
      } else {
        setIsLoadingStats(false);
      }
    }
  }

  async function loadServiceLoad(): Promise<void> {
    setIsLoadingServiceLoad(true);
    setServiceLoadError(null);

    try {
      const result = await getServiceLoad();
      setServiceLoad(result);
    } catch (error) {
      setServiceLoadError(getErrorMessage(error, "Nepavyko ikelti paslaugu apkrovos."));
    } finally {
      setIsLoadingServiceLoad(false);
    }
  }

  function openCreateServiceDialog(): void {
    setEditingService(null);
    setServiceForm(createDefaultServiceFormState());
    setServiceFormErrors({});
    setServiceDialogOpen(true);
  }

  function openEditServiceDialog(serviceType: ServiceType): void {
    setEditingService(serviceType);
    setServiceForm({
      name: serviceType.name,
      estimatedDurationMinutes: String(serviceType.estimatedDurationMinutes),
      isActive: serviceType.isActive ? "true" : "false",
      priorityLevel: String(serviceType.priorityLevel),
      isFastTrack: serviceType.isFastTrack ? "true" : "false",
      maxWaitMinutes: serviceType.maxWaitMinutes == null ? "" : String(serviceType.maxWaitMinutes)
    });
    setServiceFormErrors({});
    setServiceDialogOpen(true);
  }

  function openCreateCounterDialog(): void {
    setEditingCounter(null);
    setCounterForm({
      ...createDefaultCounterFormState(),
      displayOrder: String(Math.max(0, ...counters.map((counter) => counter.displayOrder)) + 1),
      departmentId: String(departments.find((department) => department.isActive)?.departmentId ?? "")
    });
    setCounterFormErrors({});
    setCounterHasActiveTicket(false);
    setCounterDialogOpen(true);
  }

  function openEditCounterDialog(counter: Counter): void {
    setEditingCounter(counter);
    setCounterForm({
      name: counter.name,
      displayOrder: String(counter.displayOrder),
      departmentId: String(counter.departmentId),
      serviceTypeIds: counter.supportedServiceTypeIds.map((serviceTypeId) => String(serviceTypeId)),
      status: counter.status
    });
    setCounterFormErrors({});
    setCounterHasActiveTicket(false);
    setCounterDialogOpen(true);

    void (async () => {
      try {
        const activeTickets = await getActiveTickets();
        const hasActiveTicket = activeTickets.some((ticket) => ticket.counterId === counter.id);
        setCounterHasActiveTicket(hasActiveTicket);
      } catch {
        setCounterHasActiveTicket(false);
      }
    })();
  }

  function openCreateOperatorDialog(): void {
    setEditingOperator(null);
    setOperatorForm(createDefaultOperatorFormState());
    setOperatorFormErrors({});
    setOperatorDialogOpen(true);
  }

  function openCreateDepartmentDialog(): void {
    setEditingDepartment(null);
    setDepartmentForm(createDefaultDepartmentFormState());
    setDepartmentFormErrors({});
    setDepartmentDialogOpen(true);
  }

  function openEditDepartmentDialog(department: Department): void {
    setEditingDepartment(department);
    setDepartmentForm({
      name: department.name,
      description: department.description,
      isActive: department.isActive ? "true" : "false"
    });
    setDepartmentFormErrors({});
    setDepartmentDialogOpen(true);
  }

  function openEditOperatorDialog(operator: Operator): void {
    setEditingOperator(operator);
    setOperatorForm({
      name: operator.name,
      counterId: String(operator.counterId),
      isActive: operator.isActive ? "true" : "false"
    });
    setOperatorFormErrors({});
    setOperatorDialogOpen(true);
  }

  function validateServiceForm(): ServiceFormErrors {
    const errors: ServiceFormErrors = {};

    if (serviceForm.name.trim().length === 0) {
      errors.name = "Pavadinimas yra privalomas.";
    }

    const duration = Number.parseInt(serviceForm.estimatedDurationMinutes, 10);
    if (Number.isNaN(duration) || duration <= 0) {
      errors.estimatedDurationMinutes = "Trukme turi buti teigiamas skaicius.";
    }

    const priorityLevel = Number.parseInt(serviceForm.priorityLevel, 10);
    if (Number.isNaN(priorityLevel) || priorityLevel < 1 || priorityLevel > 5) {
      errors.priorityLevel = "Prioritetas turi buti nuo 1 iki 5.";
    }

    if (serviceForm.maxWaitMinutes.trim().length > 0) {
      const maxWaitMinutes = Number.parseInt(serviceForm.maxWaitMinutes, 10);
      if (Number.isNaN(maxWaitMinutes) || maxWaitMinutes <= 0) {
        errors.maxWaitMinutes = "Laukimo riba turi buti teigiamas skaicius.";
      }
    }

    return errors;
  }

  function validateCounterForm(): CounterFormErrors {
    const errors: CounterFormErrors = {};

    if (counterForm.name.trim().length === 0) {
      errors.name = "Pavadinimas yra privalomas.";
    }

    const displayOrder = Number.parseInt(counterForm.displayOrder, 10);
    if (Number.isNaN(displayOrder) || displayOrder <= 0) {
      errors.displayOrder = "Ekrano eiles numeris turi buti teigiamas skaicius.";
    }

    const departmentId = Number.parseInt(counterForm.departmentId, 10);
    if (Number.isNaN(departmentId) || departmentId <= 0) {
      errors.departmentId = "Skyrius yra privalomas.";
    }

    if (counterForm.serviceTypeIds.length === 0) {
      errors.serviceTypeIds = "Pasirinkite bent viena aptarnaujama paslauga.";
    }

    return errors;
  }

  function validateDepartmentForm(): DepartmentFormErrors {
    const errors: DepartmentFormErrors = {};

    if (departmentForm.name.trim().length === 0) {
      errors.name = "Pavadinimas yra privalomas.";
    }

    if (departmentForm.description.length > 500) {
      errors.description = "Aprasymas negali virsyti 500 simboliu.";
    }

    return errors;
  }

  function validateOperatorForm(): OperatorFormErrors {
    const errors: OperatorFormErrors = {};

    if (operatorForm.name.trim().length === 0) {
      errors.name = "Vardas yra privalomas.";
    }

    const counterId = Number.parseInt(operatorForm.counterId, 10);
    if (Number.isNaN(counterId) || counterId <= 0) {
      errors.counterId = "Langelis yra privalomas.";
    }

    return errors;
  }

  async function handleServiceSubmit(event: React.FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();

    const localErrors = validateServiceForm();
    setServiceFormErrors(localErrors);
    if (Object.keys(localErrors).length > 0) {
      return;
    }

    const payload: CreateServiceTypeRequest | UpdateServiceTypeRequest = {
      name: serviceForm.name.trim(),
      description: serviceForm.name.trim(),
      estimatedDurationMinutes: Number.parseInt(serviceForm.estimatedDurationMinutes, 10),
      isActive: serviceForm.isActive === "true",
      priorityLevel: Number.parseInt(serviceForm.priorityLevel, 10),
      isFastTrack: serviceForm.isFastTrack === "true",
      maxWaitMinutes:
        serviceForm.maxWaitMinutes.trim().length === 0 ? null : Number.parseInt(serviceForm.maxWaitMinutes, 10)
    };

    setIsSubmittingService(true);
    setServicesError(null);

    try {
      if (editingService) {
        await updateServiceType(editingService.id, payload);
        setSuccessMessage("Paslaugos tipas atnaujintas.");
      } else {
        await createServiceType(payload);
        setSuccessMessage("Paslaugos tipas sukurtas.");
      }

      setServiceDialogOpen(false);
      await Promise.all([loadServices(), loadStats(true), loadServiceLoad()]);
    } catch (error) {
      const validationErrors = getValidationErrors(error);
      setServiceFormErrors({
        name: getFieldValidationError(validationErrors, "name"),
        estimatedDurationMinutes:
          getFieldValidationError(validationErrors, "estimateddurationminutes") ??
          getFieldValidationError(validationErrors, "estimatedDurationMinutes"),
        priorityLevel:
          getFieldValidationError(validationErrors, "prioritylevel") ??
          getFieldValidationError(validationErrors, "priorityLevel"),
        maxWaitMinutes:
          getFieldValidationError(validationErrors, "maxwaitminutes") ??
          getFieldValidationError(validationErrors, "maxWaitMinutes")
      });
      setServicesError(getErrorMessage(error, "Nepavyko issaugoti paslaugos tipo."));
    } finally {
      setIsSubmittingService(false);
    }
  }

  async function handleCounterSubmit(event: React.FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();

    const localErrors = validateCounterForm();
    setCounterFormErrors(localErrors);
    if (Object.keys(localErrors).length > 0) {
      return;
    }

    const payload: CreateCounterRequest | UpdateCounterRequest = {
      name: counterForm.name.trim(),
      displayOrder: Number.parseInt(counterForm.displayOrder, 10),
      departmentId: Number.parseInt(counterForm.departmentId, 10),
      serviceTypeIds: counterForm.serviceTypeIds.map((serviceTypeId) => Number.parseInt(serviceTypeId, 10)),
      status: counterForm.status
    };

    setIsSubmittingCounter(true);
    setCountersError(null);

    try {
      if (editingCounter) {
        await updateCounter(editingCounter.id, payload);
        setSuccessMessage("Langelis atnaujintas.");
      } else {
        await createCounter(payload);
        setSuccessMessage("Langelis sukurtas.");
      }

      setCounterDialogOpen(false);
      await Promise.all([loadCounters(), loadOperators(), loadServiceLoad()]);
    } catch (error) {
      const validationErrors = getValidationErrors(error);
      setCounterFormErrors({
        name: getFieldValidationError(validationErrors, "name"),
        displayOrder:
          getFieldValidationError(validationErrors, "displayOrder") ??
          getFieldValidationError(validationErrors, "displayorder"),
        departmentId:
          getFieldValidationError(validationErrors, "departmentId") ??
          getFieldValidationError(validationErrors, "departmentid"),
        serviceTypeIds:
          getFieldValidationError(validationErrors, "serviceTypeIds") ??
          getFieldValidationError(validationErrors, "servicetypeids")
      });
      setCountersError(getErrorMessage(error, "Nepavyko issaugoti langelio."));
    } finally {
      setIsSubmittingCounter(false);
    }
  }

  async function handleDepartmentSubmit(event: React.FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();

    const localErrors = validateDepartmentForm();
    setDepartmentFormErrors(localErrors);
    if (Object.keys(localErrors).length > 0) {
      return;
    }

    const payload: CreateDepartmentRequest | UpdateDepartmentRequest = {
      name: departmentForm.name.trim(),
      description: departmentForm.description.trim(),
      ...(editingDepartment ? { isActive: departmentForm.isActive === "true" } : {})
    };

    setIsSubmittingDepartment(true);
    setDepartmentsError(null);

    try {
      if (editingDepartment) {
        await updateDepartment(editingDepartment.departmentId, payload as UpdateDepartmentRequest);
        setSuccessMessage("Skyrius atnaujintas.");
      } else {
        await createDepartment(payload as CreateDepartmentRequest);
        setSuccessMessage("Skyrius sukurtas.");
      }

      setDepartmentDialogOpen(false);
      await Promise.all([loadDepartments(), loadManagerUsers(), loadCounters()]);
    } catch (error) {
      const validationErrors = getValidationErrors(error);
      setDepartmentFormErrors({
        name: getFieldValidationError(validationErrors, "name"),
        description: getFieldValidationError(validationErrors, "description")
      });
      setDepartmentsError(getErrorMessage(error, "Nepavyko issaugoti skyriaus."));
    } finally {
      setIsSubmittingDepartment(false);
    }
  }

  async function handleDeactivateDepartment(department: Department): Promise<void> {
    setDeactivatingDepartmentId(department.departmentId);
    setDepartmentsError(null);

    try {
      await deactivateDepartment(department.departmentId);
      setSuccessMessage("Skyrius isjungtas.");
      await Promise.all([loadDepartments(), loadManagerUsers()]);
    } catch (error) {
      setDepartmentsError(getErrorMessage(error, "Nepavyko isjungti skyriaus."));
    } finally {
      setDeactivatingDepartmentId(null);
    }
  }

  async function handleSaveManagerDepartments(manager: ManagerUser): Promise<void> {
    setSavingManagerId(manager.userId);
    setDepartmentsError(null);

    try {
      await updateManagerDepartments(manager.userId, { departmentIds: manager.departmentIds });
      setSuccessMessage("Vadovo skyriai atnaujinti.");
      await Promise.all([loadDepartments(), loadManagerUsers()]);
    } catch (error) {
      setDepartmentsError(getErrorMessage(error, "Nepavyko issaugoti vadovo skyriu."));
    } finally {
      setSavingManagerId(null);
    }
  }

  async function handleOperatorSubmit(event: React.FormEvent<HTMLFormElement>): Promise<void> {
    event.preventDefault();

    const localErrors = validateOperatorForm();
    setOperatorFormErrors(localErrors);
    if (Object.keys(localErrors).length > 0) {
      return;
    }

    const payload: CreateOperatorRequest | UpdateOperatorRequest = {
      name: operatorForm.name.trim(),
      counterId: Number.parseInt(operatorForm.counterId, 10),
      isActive: operatorForm.isActive === "true"
    };

    setIsSubmittingOperator(true);
    setOperatorsError(null);

    try {
      if (editingOperator) {
        await updateOperator(editingOperator.id, payload);
        setSuccessMessage("Operatorius atnaujintas.");
      } else {
        await createOperator(payload);
        setSuccessMessage("Operatorius sukurtas.");
      }

      setOperatorDialogOpen(false);
      await loadOperators();
    } catch (error) {
      const validationErrors = getValidationErrors(error);
      setOperatorFormErrors({
        name: getFieldValidationError(validationErrors, "name"),
        counterId:
          getFieldValidationError(validationErrors, "counterId") ??
          getFieldValidationError(validationErrors, "counterid")
      });
      setOperatorsError(getErrorMessage(error, "Nepavyko issaugoti operatoriaus."));
    } finally {
      setIsSubmittingOperator(false);
    }
  }

  async function handleToggleServiceActive(serviceType: ServiceType): Promise<void> {
    setToggleServiceId(serviceType.id);
    setServicesError(null);

    try {
      await updateServiceType(serviceType.id, {
        name: serviceType.name,
        description: serviceType.description,
        estimatedDurationMinutes: serviceType.estimatedDurationMinutes,
        isActive: !serviceType.isActive,
        priorityLevel: serviceType.priorityLevel,
        isFastTrack: serviceType.isFastTrack,
        maxWaitMinutes: serviceType.maxWaitMinutes
      });

      setSuccessMessage(serviceType.isActive ? "Paslaugos tipas isjungtas." : "Paslaugos tipas ijungtas.");
      await Promise.all([loadServices(), loadStats(true), loadServiceLoad()]);
    } catch (error) {
      setServicesError(getErrorMessage(error, "Nepavyko pakeisti paslaugos tipo busenos."));
    } finally {
      setToggleServiceId(null);
    }
  }

  return (
    <div className="min-h-full space-y-4 overflow-x-hidden bg-zinc-950 sm:space-y-6">
      <section>
        <h2 className="text-xl font-semibold text-zinc-100">Administravimas</h2>
        <p className="mt-1 text-sm text-zinc-400">
          Valdykite paslaugas, langelius, operatorius ir stebekite dienos statistika.
        </p>
      </section>

      {successMessage && (
        <section className="rounded-md border border-emerald-200 bg-emerald-50 px-4 py-3">
          <p className="text-sm font-medium text-emerald-700">{successMessage}</p>
        </section>
      )}

      <Tabs value={activeTab} onValueChange={(value) => setActiveTab(value as TabKey)}>
        <TabsList className="h-auto justify-start rounded-md border border-zinc-800 bg-zinc-800">
          <TabsTrigger className="rounded-md" value="services">
            Paslaugos
          </TabsTrigger>
          <TabsTrigger className="rounded-md" value="counters">
            Langeliai
          </TabsTrigger>
          <TabsTrigger className="rounded-md" value="operators">
            Operatoriai
          </TabsTrigger>
          <TabsTrigger className="rounded-md" value="departments">
            Skyriai
          </TabsTrigger>
          <TabsTrigger className="rounded-md" value="stats">
            Statistika
          </TabsTrigger>
          <TabsTrigger className="rounded-md" value="load">
            Apkrova
          </TabsTrigger>
        </TabsList>

        <TabsContent value="services">
          <section className="rounded-md border border-zinc-800 bg-zinc-900">
            <div className="flex flex-wrap items-center justify-between gap-3 border-b border-zinc-800 px-4 py-3">
              <h3 className="text-base font-semibold text-zinc-100">Paslaugu tipai</h3>
              <Button className="rounded-md bg-amber-400 text-zinc-900 hover:bg-amber-300" onClick={openCreateServiceDialog}>
                Prideti paslauga
              </Button>
            </div>

            {servicesError && <p className="px-4 py-3 text-sm text-red-600">{servicesError}</p>}

            {isLoadingServices ? (
              <LoadingSpinner />
            ) : (
              <Table>
                <TableHeader>
                  <TableRow className="bg-zinc-800 hover:bg-zinc-800">
                    <TableHead>Pavadinimas</TableHead>
                    <TableHead>Prefiksas</TableHead>
                    <TableHead>Trukme (min)</TableHead>
                    <TableHead>Prioritetas</TableHead>
                    <TableHead>Greita</TableHead>
                    <TableHead>Laukimo riba</TableHead>
                    <TableHead>Busena</TableHead>
                    <TableHead className="text-right">Veiksmai</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {services.length === 0 && (
                    <TableRow>
                      <TableCell className="py-8 text-center text-sm text-zinc-400" colSpan={8}>
                        Paslaugu tipu dar nera.
                      </TableCell>
                    </TableRow>
                  )}

                  {services.map((serviceType, index) => (
                    <TableRow className={index % 2 === 0 ? "bg-zinc-900" : "bg-zinc-950"} key={serviceType.id}>
                      <TableCell className="font-medium text-zinc-100">{serviceType.name}</TableCell>
                      <TableCell className="font-mono font-semibold text-zinc-100">{serviceType.prefix}</TableCell>
                      <TableCell className="text-zinc-300">{serviceType.estimatedDurationMinutes}</TableCell>
                      <TableCell className="text-zinc-300">{serviceType.priorityLevel}</TableCell>
                      <TableCell className="text-zinc-300">{serviceType.isFastTrack ? "Taip" : "Ne"}</TableCell>
                      <TableCell className="text-zinc-300">{serviceType.maxWaitMinutes ?? "-"}</TableCell>
                      <TableCell>{getStatusBadge(serviceType.isActive)}</TableCell>
                      <TableCell>
                        <div className="flex justify-end gap-2">
                          <Button className="rounded-md" size="sm" variant="outline" onClick={() => openEditServiceDialog(serviceType)}>
                            Redaguoti
                          </Button>
                          <Button
                            className="rounded-md"
                            disabled={toggleServiceId === serviceType.id}
                            size="sm"
                            variant="outline"
                            onClick={() => {
                              void handleToggleServiceActive(serviceType);
                            }}
                          >
                            {toggleServiceId === serviceType.id ? "Vykdoma..." : serviceType.isActive ? "Isjungti" : "Ijungti"}
                          </Button>
                        </div>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </section>
        </TabsContent>

        <TabsContent value="counters">
          <section className="rounded-md border border-zinc-800 bg-zinc-900">
            <div className="flex flex-wrap items-center justify-between gap-3 border-b border-zinc-800 px-4 py-3">
              <h3 className="text-base font-semibold text-zinc-100">Langeliai</h3>
              <Button className="rounded-md bg-amber-400 text-zinc-900 hover:bg-amber-300" onClick={openCreateCounterDialog}>
                Prideti langeli
              </Button>
            </div>

            {countersError && <p className="px-4 py-3 text-sm text-red-600">{countersError}</p>}

            {isLoadingCounters ? (
              <LoadingSpinner />
            ) : (
              <Table>
                <TableHeader>
                  <TableRow className="bg-zinc-800 hover:bg-zinc-800">
                    <TableHead>Pavadinimas</TableHead>
                    <TableHead>Ekrano eile</TableHead>
                    <TableHead>Skyrius</TableHead>
                    <TableHead>Aptarnaujamos paslaugos</TableHead>
                    <TableHead>Busena</TableHead>
                    <TableHead className="text-right">Veiksmai</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {counters.length === 0 && (
                    <TableRow>
                      <TableCell className="py-8 text-center text-sm text-zinc-400" colSpan={6}>
                        Langeliu dar nera.
                      </TableCell>
                    </TableRow>
                  )}

                  {counters.map((counter, index) => (
                    <TableRow className={index % 2 === 0 ? "bg-zinc-900" : "bg-zinc-950"} key={counter.id}>
                      <TableCell className="font-medium text-zinc-100">{counter.name}</TableCell>
                      <TableCell className="text-zinc-300">{counter.displayOrder}</TableCell>
                      <TableCell className="text-zinc-300">{counter.departmentName || "-"}</TableCell>
                      <TableCell className="text-zinc-300">
                        {counter.supportedServiceTypes
                          .map((serviceType) => `${serviceType.name}${serviceType.isEnabled ? "" : " (isjungta)"}`)
                          .join(", ")}
                      </TableCell>
                      <TableCell>{getCounterStatusBadge(counter.status)}</TableCell>
                      <TableCell>
                        <div className="flex justify-end gap-2">
                          <Button className="rounded-md" size="sm" variant="outline" onClick={() => openEditCounterDialog(counter)}>
                            Redaguoti
                          </Button>
                        </div>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </section>
        </TabsContent>

        <TabsContent value="operators">
          <section className="rounded-md border border-zinc-800 bg-zinc-900">
            <div className="flex flex-wrap items-center justify-between gap-3 border-b border-zinc-800 px-4 py-3">
              <h3 className="text-base font-semibold text-zinc-100">Operatoriai</h3>
              <Button className="rounded-md bg-amber-400 text-zinc-900 hover:bg-amber-300" onClick={openCreateOperatorDialog}>
                Prideti operatoriu
              </Button>
            </div>

            {operatorsError && <p className="px-4 py-3 text-sm text-red-600">{operatorsError}</p>}

            {isLoadingOperators ? (
              <LoadingSpinner />
            ) : (
              <Table>
                <TableHeader>
                  <TableRow className="bg-zinc-800 hover:bg-zinc-800">
                    <TableHead>Vardas</TableHead>
                    <TableHead>Priskirtas langelis</TableHead>
                    <TableHead>Busena</TableHead>
                    <TableHead className="text-right">Veiksmai</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {operators.length === 0 && (
                    <TableRow>
                      <TableCell className="py-8 text-center text-sm text-zinc-400" colSpan={4}>
                        Operatoriu dar nera.
                      </TableCell>
                    </TableRow>
                  )}

                  {operators.map((operator, index) => (
                    <TableRow className={index % 2 === 0 ? "bg-zinc-900" : "bg-zinc-950"} key={operator.id}>
                      <TableCell className="font-medium text-zinc-100">{operator.name}</TableCell>
                      <TableCell className="text-zinc-300">{operator.counterName}</TableCell>
                      <TableCell>{getStatusBadge(operator.isActive)}</TableCell>
                      <TableCell>
                        <div className="flex justify-end gap-2">
                          <Button className="rounded-md" size="sm" variant="outline" onClick={() => openEditOperatorDialog(operator)}>
                            Redaguoti
                          </Button>
                        </div>
                      </TableCell>
                    </TableRow>
                  ))}
                </TableBody>
              </Table>
            )}
          </section>
        </TabsContent>

        <TabsContent value="departments">
          <section className="space-y-4">
            <div className="rounded-md border border-zinc-800 bg-zinc-900">
              <div className="flex flex-wrap items-center justify-between gap-3 border-b border-zinc-800 px-4 py-3">
                <h3 className="text-base font-semibold text-zinc-100">Skyriai</h3>
                <Button className="rounded-md bg-amber-400 text-zinc-900 hover:bg-amber-300" onClick={openCreateDepartmentDialog}>
                  Prideti skyriu
                </Button>
              </div>

              {departmentsError && <p className="px-4 py-3 text-sm text-red-600">{departmentsError}</p>}

              {isLoadingDepartments ? (
                <LoadingSpinner />
              ) : (
                <Table>
                  <TableHeader>
                    <TableRow className="bg-zinc-800 hover:bg-zinc-800">
                      <TableHead>Pavadinimas</TableHead>
                      <TableHead>Aprasymas</TableHead>
                      <TableHead>Langeliai</TableHead>
                      <TableHead>Vadovai</TableHead>
                      <TableHead>Busena</TableHead>
                      <TableHead className="text-right">Veiksmai</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {departments.length === 0 && (
                      <TableRow>
                        <TableCell className="py-8 text-center text-sm text-zinc-400" colSpan={6}>
                          Skyriu dar nera.
                        </TableCell>
                      </TableRow>
                    )}

                    {departments.map((department, index) => (
                      <TableRow className={index % 2 === 0 ? "bg-zinc-900" : "bg-zinc-950"} key={department.departmentId}>
                        <TableCell className="font-medium text-zinc-100">{department.name}</TableCell>
                        <TableCell className="text-zinc-300">{department.description || "-"}</TableCell>
                        <TableCell className="text-zinc-300">{department.counterCount}</TableCell>
                        <TableCell className="text-zinc-300">{department.managerCount}</TableCell>
                        <TableCell>{getStatusBadge(department.isActive)}</TableCell>
                        <TableCell>
                          <div className="flex justify-end gap-2">
                            <Button className="rounded-md" size="sm" variant="outline" onClick={() => openEditDepartmentDialog(department)}>
                              Redaguoti
                            </Button>
                            <Button
                              className="rounded-md"
                              disabled={!department.isActive || deactivatingDepartmentId === department.departmentId}
                              size="sm"
                              variant="outline"
                              onClick={() => {
                                void handleDeactivateDepartment(department);
                              }}
                            >
                              {deactivatingDepartmentId === department.departmentId ? "Vykdoma..." : "Isjungti"}
                            </Button>
                          </div>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              )}
            </div>

            <div className="rounded-md border border-zinc-800 bg-zinc-900">
              <div className="border-b border-zinc-800 px-4 py-3">
                <h3 className="text-base font-semibold text-zinc-100">Vadovu skyriu priskyrimai</h3>
              </div>

              {isLoadingManagers ? (
                <LoadingSpinner />
              ) : (
                <Table>
                  <TableHeader>
                    <TableRow className="bg-zinc-800 hover:bg-zinc-800">
                      <TableHead>Vadovas</TableHead>
                      <TableHead>Priskirti skyriai</TableHead>
                      <TableHead className="text-right">Veiksmai</TableHead>
                    </TableRow>
                  </TableHeader>
                  <TableBody>
                    {managerUsers.length === 0 && (
                      <TableRow>
                        <TableCell className="py-8 text-center text-sm text-zinc-400" colSpan={3}>
                          Vadovu naudotoju dar nera.
                        </TableCell>
                      </TableRow>
                    )}

                    {managerUsers.map((manager) => (
                      <TableRow className="bg-zinc-950" key={manager.userId}>
                        <TableCell className="font-medium text-zinc-100">{manager.username}</TableCell>
                        <TableCell>
                          <div className="flex flex-wrap gap-3">
                            {departments
                              .filter((department) => department.isActive)
                              .map((department) => {
                                const isChecked = manager.departmentIds.includes(department.departmentId);
                                return (
                                  <label className="flex items-center gap-2 text-sm text-zinc-300" key={department.departmentId}>
                                    <input
                                      checked={isChecked}
                                      className="h-4 w-4 rounded border-zinc-700 bg-zinc-950"
                                      type="checkbox"
                                      onChange={(event) => {
                                        setManagerUsers((previous) =>
                                          previous.map((item) => {
                                            if (item.userId !== manager.userId) {
                                              return item;
                                            }

                                            const nextDepartmentIds = event.target.checked
                                              ? [...item.departmentIds, department.departmentId]
                                              : item.departmentIds.filter((departmentId) => departmentId !== department.departmentId);

                                            return { ...item, departmentIds: nextDepartmentIds };
                                          })
                                        );
                                      }}
                                    />
                                    {department.name}
                                  </label>
                                );
                              })}
                          </div>
                        </TableCell>
                        <TableCell>
                          <div className="flex justify-end">
                            <Button
                              className="rounded-md"
                              disabled={savingManagerId === manager.userId}
                              size="sm"
                              variant="outline"
                              onClick={() => {
                                void handleSaveManagerDepartments(manager);
                              }}
                            >
                              {savingManagerId === manager.userId ? "Saugoma..." : "Issaugoti"}
                            </Button>
                          </div>
                        </TableCell>
                      </TableRow>
                    ))}
                  </TableBody>
                </Table>
              )}
            </div>
          </section>
        </TabsContent>

        <TabsContent value="stats">
          <section className="space-y-4">
            <div className="flex flex-wrap items-center justify-between gap-3">
              <h3 className="text-base font-semibold text-zinc-100">Siandienos statistika</h3>
              <Button
                className="rounded-md"
                disabled={isRefreshingStats}
                variant="outline"
                onClick={() => {
                  void Promise.all([loadStats(true), loadServices(), loadServiceLoad()]);
                }}
              >
                {isRefreshingStats ? "Atnaujinama..." : "Atnaujinti"}
              </Button>
            </div>

            {statsError && <p className="text-sm text-red-600">{statsError}</p>}

            {isLoadingStats ? (
              <div className="rounded-md border border-zinc-800 bg-zinc-900">
                <LoadingSpinner />
              </div>
            ) : (
              <>
                <div className="grid grid-cols-1 gap-4 sm:grid-cols-2 xl:grid-cols-4">
                  <div className="rounded-md border border-zinc-800 bg-zinc-900 p-4">
                    <p className="text-sm text-zinc-400">Isduota bilietu</p>
                    <p className="mt-2 text-3xl font-semibold text-amber-400">{stats?.totalIssued ?? 0}</p>
                  </div>
                  <div className="rounded-md border border-zinc-800 bg-zinc-900 p-4">
                    <p className="text-sm text-zinc-400">Baigta aptarnauti</p>
                    <p className="mt-2 text-3xl font-semibold text-amber-400">{stats?.doneCount ?? 0}</p>
                  </div>
                  <div className="rounded-md border border-zinc-800 bg-zinc-900 p-4">
                    <p className="text-sm text-zinc-400">Neatvyko</p>
                    <p className="mt-2 text-3xl font-semibold text-amber-400">{stats?.noShowCount ?? 0}</p>
                  </div>
                  <div className="rounded-md border border-zinc-800 bg-zinc-900 p-4">
                    <p className="text-sm text-zinc-400">Vidutinis laukimas (min)</p>
                    <p className="mt-2 text-3xl font-semibold text-amber-400">{averageWaitMinutes}</p>
                  </div>
                </div>

                <div className="rounded-md border border-zinc-800 bg-zinc-900">
                  <div className="border-b border-zinc-800 px-4 py-3">
                    <h4 className="text-sm font-semibold text-zinc-100">Paslaugu suvestine</h4>
                  </div>

                  <Table>
                    <TableHeader>
                      <TableRow className="bg-zinc-800 hover:bg-zinc-800">
                        <TableHead>Paslauga</TableHead>
                        <TableHead>Prefiksas</TableHead>
                        <TableHead>Laukianciu kiekis</TableHead>
                        <TableHead>Numatomas laukimas (min)</TableHead>
                        <TableHead>Busena</TableHead>
                      </TableRow>
                    </TableHeader>
                    <TableBody>
                      {serviceStatsRows.length === 0 && (
                        <TableRow>
                          <TableCell className="py-8 text-center text-sm text-zinc-400" colSpan={5}>
                            Paslaugu statistikai nerasta.
                          </TableCell>
                        </TableRow>
                      )}

                      {serviceStatsRows.map((serviceType, index) => (
                        <TableRow className={index % 2 === 0 ? "bg-zinc-900" : "bg-zinc-950"} key={serviceType.id}>
                          <TableCell className="font-medium text-zinc-100">{serviceType.name}</TableCell>
                          <TableCell className="font-mono text-zinc-200">{serviceType.prefix}</TableCell>
                          <TableCell className="text-zinc-300">{serviceType.currentQueueLength}</TableCell>
                          <TableCell className="text-zinc-300">{serviceType.estimatedWaitMinutes}</TableCell>
                          <TableCell>{getStatusBadge(serviceType.isActive)}</TableCell>
                        </TableRow>
                      ))}
                    </TableBody>
                  </Table>
                </div>
              </>
            )}
          </section>
        </TabsContent>

        <TabsContent value="load">
          <section className="rounded-md border border-zinc-800 bg-zinc-900">
            <div className="flex flex-wrap items-center justify-between gap-3 border-b border-zinc-800 px-4 py-3">
              <h3 className="text-base font-semibold text-zinc-100">Paslaugu apkrova</h3>
              <Button
                className="rounded-md"
                disabled={isLoadingServiceLoad}
                variant="outline"
                onClick={() => {
                  void loadServiceLoad();
                }}
              >
                {isLoadingServiceLoad ? "Atnaujinama..." : "Atnaujinti"}
              </Button>
            </div>

            {serviceLoadError && <p className="px-4 py-3 text-sm text-red-600">{serviceLoadError}</p>}

            {isLoadingServiceLoad ? (
              <LoadingSpinner />
            ) : (
              <Table>
                <TableHeader>
                  <TableRow className="bg-zinc-800 hover:bg-zinc-800">
                    <TableHead>Paslauga</TableHead>
                    <TableHead>Laukia</TableHead>
                    <TableHead>Seniausias</TableHead>
                    <TableHead>Aktyvus langeliai</TableHead>
                    <TableHead>Darbo kruvis</TableHead>
                    <TableHead>Preliminarus laukimas</TableHead>
                    <TableHead>SLA</TableHead>
                    <TableHead>Busena</TableHead>
                    <TableHead>Rekomendacija</TableHead>
                    <TableHead>Prieinamumas</TableHead>
                  </TableRow>
                </TableHeader>
                <TableBody>
                  {serviceLoadRows.length === 0 && (
                    <TableRow>
                      <TableCell className="py-8 text-center text-sm text-zinc-400" colSpan={10}>
                        Paslaugu apkrovos duomenu nera.
                      </TableCell>
                    </TableRow>
                  )}

                  {serviceLoadRows.map((summary, index) => {
                    const isWarning = !summary.isCurrentlyAvailable && summary.waitingCount > 0;

                    return (
                      <TableRow
                        className={isWarning ? "bg-red-500/10" : index % 2 === 0 ? "bg-zinc-900" : "bg-zinc-950"}
                        key={summary.serviceTypeId}
                      >
                        <TableCell className="font-medium text-zinc-100">{summary.serviceTypeName}</TableCell>
                        <TableCell className="text-zinc-300">{summary.waitingCount}</TableCell>
                        <TableCell className="text-zinc-300">{summary.oldestWaitingMinutes} min</TableCell>
                        <TableCell className="text-zinc-300">{summary.activeEnabledCounterCount}</TableCell>
                        <TableCell className="text-zinc-300">{summary.estimatedWorkloadMinutes} min</TableCell>
                        <TableCell className="text-zinc-300">{summary.estimatedWaitMinutes} min</TableCell>
                        <TableCell className="text-zinc-300">{summary.slaBreachCount}</TableCell>
                        <TableCell>{getLoadStatusBadge(summary.loadStatus)}</TableCell>
                        <TableCell className="max-w-xs text-zinc-300">
                          {summary.recommendationMessage && summary.recommendationMessage.trim().length > 0
                            ? summary.recommendationMessage
                            : "-"}
                        </TableCell>
                        <TableCell className={summary.isCurrentlyAvailable ? "text-emerald-400" : "text-amber-400"}>
                          {summary.isCurrentlyAvailable ? "Aptarnaujama" : "Šiuo metu neaptarnaujama"}
                        </TableCell>
                      </TableRow>
                    );
                  })}
                </TableBody>
              </Table>
            )}
          </section>
        </TabsContent>
      </Tabs>

      <Dialog open={serviceDialogOpen} onOpenChange={setServiceDialogOpen}>
        <DialogContent className="app-dark rounded-md border-zinc-800 bg-zinc-900 text-zinc-100">
          <DialogHeader>
            <DialogTitle>{editingService ? "Redaguoti paslaugos tipa" : "Naujas paslaugos tipas"}</DialogTitle>
            <DialogDescription>Validacijos klaidos rodomos zemiau kiekvieno formos laukelio.</DialogDescription>
          </DialogHeader>

          <form className="space-y-4" onSubmit={(event) => void handleServiceSubmit(event)}>
            <div className="space-y-2">
              <Label htmlFor="service-name">Pavadinimas</Label>
              <Input
                id="service-name"
                value={serviceForm.name}
                onChange={(event) => setServiceForm((previous) => ({ ...previous, name: event.target.value }))}
              />
              {serviceFormErrors.name && <p className="text-sm text-red-600">{serviceFormErrors.name}</p>}
            </div>

            <div className="space-y-2">
              <Label htmlFor="service-duration">Numatoma trukme (min)</Label>
              <Input
                id="service-duration"
                min={1}
                type="number"
                value={serviceForm.estimatedDurationMinutes}
                onChange={(event) =>
                  setServiceForm((previous) => ({
                    ...previous,
                    estimatedDurationMinutes: event.target.value
                  }))
                }
              />
              {serviceFormErrors.estimatedDurationMinutes && (
                <p className="text-sm text-red-600">{serviceFormErrors.estimatedDurationMinutes}</p>
              )}
            </div>

            <div className="space-y-2">
              <Label htmlFor="service-priority">Prioritetas (1-5)</Label>
              <Input
                id="service-priority"
                max={5}
                min={1}
                type="number"
                value={serviceForm.priorityLevel}
                onChange={(event) =>
                  setServiceForm((previous) => ({
                    ...previous,
                    priorityLevel: event.target.value
                  }))
                }
              />
              {serviceFormErrors.priorityLevel && <p className="text-sm text-red-600">{serviceFormErrors.priorityLevel}</p>}
            </div>

            <div className="space-y-2">
              <Label>Greita paslauga</Label>
              <Select
                value={serviceForm.isFastTrack}
                onValueChange={(value) => {
                  if (value === "true" || value === "false") {
                    setServiceForm((previous) => ({ ...previous, isFastTrack: value }));
                  }
                }}
              >
                <SelectTrigger>
                  <SelectValue placeholder="Pasirinkite tipa" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="false">Ne</SelectItem>
                  <SelectItem value="true">Taip</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <div className="space-y-2">
              <Label htmlFor="service-max-wait">Maksimali laukimo riba (min)</Label>
              <Input
                id="service-max-wait"
                min={1}
                type="number"
                value={serviceForm.maxWaitMinutes}
                onChange={(event) =>
                  setServiceForm((previous) => ({
                    ...previous,
                    maxWaitMinutes: event.target.value
                  }))
                }
              />
              {serviceFormErrors.maxWaitMinutes && <p className="text-sm text-red-600">{serviceFormErrors.maxWaitMinutes}</p>}
            </div>

            {editingService && (
              <div className="space-y-2">
                <Label>Prefiksas</Label>
                <div className="rounded-md border border-zinc-800 bg-zinc-950 px-3 py-2 font-mono text-sm font-semibold text-zinc-200">
                  {editingService.prefix}
                </div>
              </div>
            )}

            <div className="space-y-2">
              <Label>Busena</Label>
              <Select
                value={serviceForm.isActive}
                onValueChange={(value) => {
                  if (value === "true" || value === "false") {
                    setServiceForm((previous) => ({ ...previous, isActive: value }));
                  }
                }}
              >
                <SelectTrigger>
                  <SelectValue placeholder="Pasirinkite busena" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="true">Aktyvus</SelectItem>
                  <SelectItem value="false">Neaktyvus</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <DialogFooter className="gap-2">
              <Button className="rounded-md" type="button" variant="outline" onClick={() => setServiceDialogOpen(false)}>
                Atsaukti
              </Button>
              <Button className="rounded-md bg-amber-400 text-zinc-900 hover:bg-amber-300" disabled={isSubmittingService} type="submit">
                {isSubmittingService ? "Saugoma..." : "Issaugoti"}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      <Dialog open={counterDialogOpen} onOpenChange={setCounterDialogOpen}>
        <DialogContent className="app-dark rounded-md border-zinc-800 bg-zinc-900 text-zinc-100">
          <DialogHeader>
            <DialogTitle>{editingCounter ? "Redaguoti langeli" : "Naujas langelis"}</DialogTitle>
            <DialogDescription>Pasirinkite aptarnaujamas paslaugas ir issaugokite pakeitimus.</DialogDescription>
          </DialogHeader>

          <form className="space-y-4" onSubmit={(event) => void handleCounterSubmit(event)}>
            <div className="space-y-2">
              <Label htmlFor="counter-name">Pavadinimas</Label>
              <Input
                id="counter-name"
                value={counterForm.name}
                onChange={(event) => setCounterForm((previous) => ({ ...previous, name: event.target.value }))}
              />
              {counterFormErrors.name && <p className="text-sm text-red-600">{counterFormErrors.name}</p>}
            </div>

            <div className="space-y-2">
              <Label htmlFor="counter-display-order">Ekrano eiles numeris</Label>
              <Input
                id="counter-display-order"
                min={1}
                type="number"
                value={counterForm.displayOrder}
                onChange={(event) => setCounterForm((previous) => ({ ...previous, displayOrder: event.target.value }))}
              />
              {counterFormErrors.displayOrder && <p className="text-sm text-red-600">{counterFormErrors.displayOrder}</p>}
            </div>

            <div className="space-y-2">
              <Label>Skyrius</Label>
              <Select
                value={counterForm.departmentId}
                onValueChange={(value) => setCounterForm((previous) => ({ ...previous, departmentId: value }))}
              >
                <SelectTrigger>
                  <SelectValue placeholder="Pasirinkite skyriu" />
                </SelectTrigger>
                <SelectContent>
                  {departments
                    .filter((department) => department.isActive || department.departmentId === editingCounter?.departmentId)
                    .map((department) => (
                      <SelectItem
                        disabled={!department.isActive}
                        key={department.departmentId}
                        value={String(department.departmentId)}
                      >
                        {department.name}
                      </SelectItem>
                    ))}
                </SelectContent>
              </Select>
              {counterFormErrors.departmentId && <p className="text-sm text-red-600">{counterFormErrors.departmentId}</p>}
            </div>

            <div className="space-y-2">
              <Label>Aptarnaujamos paslaugos</Label>
              <div className="max-h-48 space-y-2 overflow-y-auto rounded-md border border-zinc-800 bg-zinc-950 p-3">
                {services.map((serviceType) => {
                  const serviceTypeId = String(serviceType.id);
                  const isChecked = counterForm.serviceTypeIds.includes(serviceTypeId);

                  return (
                    <label className="flex items-center gap-2 text-sm text-zinc-200" key={serviceType.id}>
                      <input
                        checked={isChecked}
                        className="h-4 w-4 rounded border-zinc-700 bg-zinc-900 text-amber-400 focus:ring-amber-400"
                        type="checkbox"
                        onChange={(event) =>
                          setCounterForm((previous) => ({
                            ...previous,
                            serviceTypeIds: event.target.checked
                              ? [...previous.serviceTypeIds, serviceTypeId]
                              : previous.serviceTypeIds.filter((selectedId) => selectedId !== serviceTypeId)
                          }))
                        }
                      />
                      <span>{serviceType.name}</span>
                    </label>
                  );
                })}
              </div>
              {counterFormErrors.serviceTypeIds && <p className="text-sm text-red-600">{counterFormErrors.serviceTypeIds}</p>}
            </div>

            <div className="space-y-2">
              <Label>Busena</Label>
              <Select
                value={counterForm.status}
                onValueChange={(value) => {
                  if (isCounterStatus(value)) {
                    setCounterForm((previous) => ({ ...previous, status: value }));
                  }
                }}
              >
                <SelectTrigger>
                  <SelectValue placeholder="Pasirinkite busena" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="Active">Aktyvus</SelectItem>
                  <SelectItem value="Paused">Pauze</SelectItem>
                  <SelectItem value="Closed">Uzdarytas</SelectItem>
                </SelectContent>
              </Select>
            </div>

            {editingCounter && counterHasActiveTicket && counterForm.status !== "Active" && (
              <div className="rounded-md border border-amber-200 bg-amber-50 px-3 py-2">
                <p className="text-sm text-amber-700">
                  Perspejimas: sis langelis dabar turi aktyvu bilieta. Keiciant busena i Pauze ar Uzdarytas, dabartinis
                  bilietas nebus perkeltas automatiskai ir turi buti uzbaigtas rankiniu budu.
                </p>
              </div>
            )}

            <DialogFooter className="gap-2">
              <Button className="rounded-md" type="button" variant="outline" onClick={() => setCounterDialogOpen(false)}>
                Atsaukti
              </Button>
              <Button className="rounded-md bg-amber-400 text-zinc-900 hover:bg-amber-300" disabled={isSubmittingCounter} type="submit">
                {isSubmittingCounter ? "Saugoma..." : "Issaugoti"}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      <Dialog open={departmentDialogOpen} onOpenChange={setDepartmentDialogOpen}>
        <DialogContent className="app-dark rounded-md border-zinc-800 bg-zinc-900 text-zinc-100">
          <DialogHeader>
            <DialogTitle>{editingDepartment ? "Redaguoti skyriu" : "Naujas skyrius"}</DialogTitle>
            <DialogDescription>Skyrius naudojamas langeliams ir vadovu statistikos apribojimams.</DialogDescription>
          </DialogHeader>

          <form className="space-y-4" onSubmit={(event) => void handleDepartmentSubmit(event)}>
            <div className="space-y-2">
              <Label htmlFor="department-name">Pavadinimas</Label>
              <Input
                id="department-name"
                value={departmentForm.name}
                onChange={(event) => setDepartmentForm((previous) => ({ ...previous, name: event.target.value }))}
              />
              {departmentFormErrors.name && <p className="text-sm text-red-600">{departmentFormErrors.name}</p>}
            </div>

            <div className="space-y-2">
              <Label htmlFor="department-description">Aprasymas</Label>
              <Input
                id="department-description"
                value={departmentForm.description}
                onChange={(event) => setDepartmentForm((previous) => ({ ...previous, description: event.target.value }))}
              />
              {departmentFormErrors.description && <p className="text-sm text-red-600">{departmentFormErrors.description}</p>}
            </div>

            {editingDepartment && (
              <div className="space-y-2">
                <Label>Busena</Label>
                <Select
                  value={departmentForm.isActive}
                  onValueChange={(value) => {
                    if (value === "true" || value === "false") {
                      setDepartmentForm((previous) => ({ ...previous, isActive: value }));
                    }
                  }}
                >
                  <SelectTrigger>
                    <SelectValue placeholder="Pasirinkite busena" />
                  </SelectTrigger>
                  <SelectContent>
                    <SelectItem value="true">Aktyvus</SelectItem>
                    <SelectItem value="false">Neaktyvus</SelectItem>
                  </SelectContent>
                </Select>
              </div>
            )}

            <DialogFooter className="gap-2">
              <Button className="rounded-md" type="button" variant="outline" onClick={() => setDepartmentDialogOpen(false)}>
                Atsaukti
              </Button>
              <Button className="rounded-md bg-amber-400 text-zinc-900 hover:bg-amber-300" disabled={isSubmittingDepartment} type="submit">
                {isSubmittingDepartment ? "Saugoma..." : "Issaugoti"}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>

      <Dialog open={operatorDialogOpen} onOpenChange={setOperatorDialogOpen}>
        <DialogContent className="app-dark rounded-md border-zinc-800 bg-zinc-900 text-zinc-100">
          <DialogHeader>
            <DialogTitle>{editingOperator ? "Redaguoti operatoriu" : "Naujas operatorius"}</DialogTitle>
            <DialogDescription>Pasirinkite langeli ir issaugokite operatoriaus duomenis.</DialogDescription>
          </DialogHeader>

          <form className="space-y-4" onSubmit={(event) => void handleOperatorSubmit(event)}>
            <div className="space-y-2">
              <Label htmlFor="operator-name">Vardas</Label>
              <Input
                id="operator-name"
                value={operatorForm.name}
                onChange={(event) => setOperatorForm((previous) => ({ ...previous, name: event.target.value }))}
              />
              {operatorFormErrors.name && <p className="text-sm text-red-600">{operatorFormErrors.name}</p>}
            </div>

            <div className="space-y-2">
              <Label>Langelis</Label>
              <Select
                value={operatorForm.counterId}
                onValueChange={(value) => setOperatorForm((previous) => ({ ...previous, counterId: value }))}
              >
                <SelectTrigger>
                  <SelectValue placeholder="Pasirinkite langeli" />
                </SelectTrigger>
                <SelectContent>
                  {counters.map((counter) => (
                    <SelectItem key={counter.id} value={String(counter.id)}>
                      {counter.name}
                    </SelectItem>
                  ))}
                </SelectContent>
              </Select>
              {operatorFormErrors.counterId && <p className="text-sm text-red-600">{operatorFormErrors.counterId}</p>}
            </div>

            <div className="space-y-2">
              <Label>Busena</Label>
              <Select
                value={operatorForm.isActive}
                onValueChange={(value) => {
                  if (value === "true" || value === "false") {
                    setOperatorForm((previous) => ({ ...previous, isActive: value }));
                  }
                }}
              >
                <SelectTrigger>
                  <SelectValue placeholder="Pasirinkite busena" />
                </SelectTrigger>
                <SelectContent>
                  <SelectItem value="true">Aktyvus</SelectItem>
                  <SelectItem value="false">Neaktyvus</SelectItem>
                </SelectContent>
              </Select>
            </div>

            <DialogFooter className="gap-2">
              <Button className="rounded-md" type="button" variant="outline" onClick={() => setOperatorDialogOpen(false)}>
                Atsaukti
              </Button>
              <Button className="rounded-md bg-amber-400 text-zinc-900 hover:bg-amber-300" disabled={isSubmittingOperator} type="submit">
                {isSubmittingOperator ? "Saugoma..." : "Issaugoti"}
              </Button>
            </DialogFooter>
          </form>
        </DialogContent>
      </Dialog>
    </div>
  );
}
