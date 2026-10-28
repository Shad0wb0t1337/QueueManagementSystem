export type UserRole = "Admin" | "Operator" | "Manager";
export type CounterStatus = "Active" | "Paused" | "Closed";
export type LoadStatus = "Low" | "Normal" | "High" | "Critical";

export enum TicketStatus {
  Waiting = "Waiting",
  Called = "Called",
  Serving = "Serving",
  Done = "Done",
  NoShow = "NoShow",
  RecallPending = "RecallPending"
}

export type RecallMode = "Immediate" | "AfterCurrent" | "AlreadyPending" | "NotAllowed";

export interface ServiceType {
  id: number;
  name: string;
  description: string;
  estimatedDurationMinutes: number;
  isActive: boolean;
  prefix: string;
  priorityLevel: number;
  isFastTrack: boolean;
  maxWaitMinutes: number | null;
  currentQueueLength: number;
  estimatedWaitMinutes: number;
  hasActiveCounter: boolean;
}

export interface SupportedServiceType {
  id: number;
  name: string;
  isEnabled: boolean;
}

export interface Counter {
  id: number;
  name: string;
  displayOrder: number;
  departmentId: number;
  departmentName: string;
  supportedServiceTypeIds: number[];
  supportedServiceTypes: SupportedServiceType[];
  status: CounterStatus;
}

export interface Operator {
  id: number;
  name: string;
  counterId: number;
  counterName: string;
  isActive: boolean;
}

export interface Ticket {
  ticketId: number;
  ticketNumber: string;
  serviceTypeId: number;
  counterId: number | null;
  status: TicketStatus | string;
  createdAt: string;
  queueEnteredAt: string;
  calledAt: string | null;
  servedAt: string | null;
}

export interface AuthResponse {
  token: string;
  username: string;
  role: UserRole;
  operatorId: number | null;
  operatorName: string | null;
  counterId: number | null;
  counterName: string | null;
  counterStatus: CounterStatus | null;
  serviceTypeId: number | null;
  serviceTypeName: string | null;
  supportedServiceTypeIds: number[];
  supportedServiceTypes: SupportedServiceType[];
}

export interface AuthUser {
  username: string;
  role: UserRole;
  operatorId: number | null;
  operatorName: string | null;
  counterId: number | null;
  counterName: string | null;
  counterStatus: CounterStatus | null;
  serviceTypeId: number | null;
  serviceTypeName: string | null;
  supportedServiceTypeIds: number[];
  supportedServiceTypes: SupportedServiceType[];
}

export interface LoginRequest {
  username: string;
  password: string;
}

export interface IssueTicketRequest {
  serviceTypeId: number;
}

export interface IssueTicketResponse {
  ticketId: number;
  ticketNumber: string;
  serviceTypeId: number;
  serviceTypeName: string;
  queuePosition: number;
  estimatedWaitMinutes: number;
  status: TicketStatus | string;
}

export interface QueueTicketResponse {
  ticketId: number;
  ticketNumber: string;
  serviceTypeId: number;
  serviceTypeName: string;
  counterId: number | null;
  status: TicketStatus | string;
  createdAt: string;
  queueEnteredAt: string;
  calledAt: string | null;
  servedAt: string | null;
  isFastTrack: boolean;
  maxWaitMinutes: number | null;
}

export interface ActiveTicketResponse {
  ticketId: number;
  ticketNumber: string;
  counterId: number | null;
  counterName: string;
  serviceTypeId: number;
  serviceTypeName: string;
  status: TicketStatus | string;
}

export interface QueueStats {
  date: string;
  totalIssued: number;
  waitingCount: number;
  calledCount: number;
  servingCount: number;
  doneCount: number;
  noShowCount: number;
}

export type DisplayTicket = ActiveTicketResponse;

export interface CreateServiceTypeRequest {
  name: string;
  description: string;
  estimatedDurationMinutes: number;
  isActive: boolean;
  priorityLevel: number;
  isFastTrack: boolean;
  maxWaitMinutes: number | null;
}

export interface UpdateServiceTypeRequest {
  name: string;
  description: string;
  estimatedDurationMinutes: number;
  isActive: boolean;
  priorityLevel: number;
  isFastTrack: boolean;
  maxWaitMinutes: number | null;
}

export interface CreateCounterRequest {
  name: string;
  displayOrder: number;
  departmentId: number;
  serviceTypeIds: number[];
  status: CounterStatus;
}

export interface UpdateCounterRequest {
  name: string;
  displayOrder: number;
  departmentId: number;
  serviceTypeIds: number[];
  status: CounterStatus;
}

export interface UpdateOwnCounterStatusRequest {
  status: CounterStatus;
}

export interface CounterServiceProfileItem {
  serviceTypeId: number;
  serviceTypeName: string;
  isEnabled: boolean;
  waitingCount: number;
  priorityLevel: number;
  isFastTrack: boolean;
  maxWaitMinutes: number | null;
}

export interface ServiceLoadSummary {
  serviceTypeId: number;
  serviceTypeName: string;
  waitingCount: number;
  oldestWaitingMinutes: number;
  estimatedDurationMinutes: number;
  activeEnabledCounterCount: number;
  estimatedWorkloadMinutes: number;
  estimatedWaitMinutes: number;
  slaBreachCount: number;
  servicePressure: number;
  loadStatus: LoadStatus;
  recommendationMessage: string | null;
  isCurrentlyAvailable: boolean;
}

export interface HelpMostLoadedServiceResponse {
  enabledServiceTypeId: number | null;
  enabledServiceTypeName: string | null;
  message: string;
  serviceProfile: CounterServiceProfileItem[];
}

export interface UpdateCounterServiceProfileRequest {
  enabledServiceTypeIds: number[];
}

export interface CreateOperatorRequest {
  name: string;
  counterId: number;
  isActive: boolean;
}

export interface UpdateOperatorRequest {
  name: string;
  counterId: number;
  isActive: boolean;
}

export interface CallNextRequest {
  counterId: number;
}

export type TicketSelectionType = "DynamicScore" | "RecallPending" | "FallbackFifo" | "ManualRecall" | "Unknown";

export interface TicketSelectionExplanation {
  selectionType: TicketSelectionType | string;
  message: string;
  priorityLevel: number | null;
  waitingMinutes: number | null;
  fastTrackBonus: number | null;
  slaBoost: number | null;
  estimatedDurationMinutes: number | null;
  finalScore: number | null;
}

export interface TicketCalledResponse {
  ticketId: number;
  ticketNumber: string;
  counterId: number;
  counterName: string;
  serviceTypeId: number;
  serviceTypeName: string;
  status: TicketStatus | string;
  selectionExplanation?: TicketSelectionExplanation | null;
}

export interface RecentTicketHistoryItem {
  ticketId: number;
  ticketNumber: string;
  ticketStatus: TicketStatus | string;
  serviceTypeId: number;
  serviceTypeName: string;
  counterId: number | null;
  counterName: string;
  completedAt: string | null;
  recallRequestedAt: string | null;
  lastUpdatedAt: string;
  canRecall: boolean;
  recallMode: RecallMode | string;
}

export interface RecallTicketResponse {
  ticketId: number;
  ticketNumber: string;
  serviceTypeId: number;
  serviceTypeName: string;
  counterId: number;
  counterName: string;
  status: TicketStatus | string;
  recallMode: RecallMode | string;
  message: string;
}

export interface RedirectTicketRequest {
  targetServiceTypeId: number;
  reason: string;
}

export interface RedirectTicketResponse {
  ticketId: number;
  ticketNumber: string;
  fromServiceTypeId: number;
  fromServiceTypeName: string;
  toServiceTypeId: number;
  toServiceTypeName: string;
  status: TicketStatus | string;
  queueEnteredAt: string;
  message: string;
}

export interface TicketIssuedEvent {
  ticketNumber: string;
  serviceTypeName: string;
  queuePosition: number;
  estimatedWaitMinutes: number;
}

export interface TicketCalledEvent {
  ticketNumber: string;
  counterName: string;
  serviceTypeName: string;
}

export interface QueueUpdatedEvent {
  serviceTypeId: number;
}

export interface DisplayUpdatedEvent {
  calledTickets: DisplayTicket[];
}

export interface DisplayOverviewTicket {
  ticketId: number;
  ticketNumber: string;
  ticketStatus: TicketStatus | string;
  counterId: number | null;
  counterName: string;
  serviceTypeId: number;
  serviceTypeName: string;
  createdAt: string;
  calledAt: string | null;
  servingStartedAt: string | null;
  completedAt: string | null;
  lastUpdatedAt: string;
}

export interface DisplayOverview {
  activeTickets: DisplayOverviewTicket[];
  recentHistory: DisplayOverviewTicket[];
  lastUpdatedAt: string;
}

export interface CounterDisplayTile {
  counterId: number;
  counterName: string;
  displayOrder: number;
  counterStatus: CounterStatus;
  currentTicketId: number | null;
  currentTicketNumber: string | null;
  currentTicketStatus: TicketStatus | string | null;
  serviceTypeId: number | null;
  serviceTypeName: string | null;
  lastUpdatedAt: string;
}

export interface CounterDisplayGrid {
  counters: CounterDisplayTile[];
  lastUpdatedAt: string;
}

export interface CounterDisplayState {
  counterId: number;
  counterName: string;
  counterStatus: CounterStatus;
  currentTicketId: number | null;
  currentTicketNumber: string | null;
  currentTicketStatus: TicketStatus | string | null;
  serviceTypeName: string | null;
  lastUpdatedAt: string;
}

export interface CounterDisplayUpdatedEvent {
  counterId: number;
}

export interface ManagerOverviewStatistics {
  from: string;
  to: string;
  issuedCount: number;
  calledCount: number;
  completedCount: number;
  noShowCount: number;
  redirectedCount: number;
  recalledCount: number;
  recallPendingCount: number;
  averageWaitMinutes: number;
  averageServiceMinutes: number;
  slaBreachCount: number;
  slaBreachPercent: number;
}

export interface ManagerServiceStatistics {
  serviceTypeId: number;
  serviceTypeName: string;
  issuedCount: number;
  calledCount: number;
  completedCount: number;
  noShowCount: number;
  redirectedCount: number;
  recalledCount: number;
  recallPendingCount: number;
  averageWaitMinutes: number;
  averageServiceMinutes: number;
  slaBreachCount: number;
  slaBreachPercent: number;
}

export interface ManagerCounterStatistics {
  counterId: number;
  counterName: string;
  calledCount: number;
  completedCount: number;
  noShowCount: number;
  redirectedCount: number;
  recalledCount: number;
  recallPendingCount: number;
  averageServiceMinutes: number;
}

export interface ManagerOperatorStatistics {
  operatorId: number;
  operatorName: string;
  calledCount: number;
  completedCount: number;
  noShowCount: number;
  redirectedCount: number;
  recalledCount: number;
  recallPendingCount: number;
  averageServiceMinutes: number;
}

export interface ManagerRedirectStatistics {
  fromServiceTypeId: number;
  fromServiceTypeName: string;
  toServiceTypeId: number;
  toServiceTypeName: string;
  operatorId: number;
  operatorName: string;
  count: number;
}

export interface ManagerRecallStatistics {
  counterId: number | null;
  counterName: string;
  operatorId: number | null;
  operatorName: string;
  recalledCount: number;
  recallPendingCount: number;
}

export interface ManagerTicketEvent {
  eventId: number;
  ticketId: number;
  ticketNumber: string;
  eventType: string;
  eventLabel: string;
  createdAt: string;
  counterId: number | null;
  counterName: string;
  operatorId: number | null;
  operatorName: string;
  serviceTypeId: number;
  serviceTypeName: string;
  fromStatus: string | null;
  toStatus: string | null;
  fromServiceTypeName: string;
  toServiceTypeName: string;
  details: string;
}

export interface ManagerStatisticsDashboardData {
  overview: ManagerOverviewStatistics;
  services: ManagerServiceStatistics[];
  counters: ManagerCounterStatistics[];
  operators: ManagerOperatorStatistics[];
  redirects: ManagerRedirectStatistics[];
  recalls: ManagerRecallStatistics[];
  events: ManagerTicketEvent[];
}

export interface Department {
  departmentId: number;
  name: string;
  description: string;
  isActive: boolean;
  createdAt: string;
  counterCount: number;
  managerCount: number;
}

export interface CreateDepartmentRequest {
  name: string;
  description: string;
}

export interface UpdateDepartmentRequest {
  name: string;
  description: string;
  isActive: boolean;
}

export interface ManagerUser {
  userId: number;
  username: string;
  departmentIds: number[];
}

export interface UpdateManagerDepartmentsRequest {
  departmentIds: number[];
}

export interface ManagerDepartment {
  departmentId: number;
  departmentName: string;
}
