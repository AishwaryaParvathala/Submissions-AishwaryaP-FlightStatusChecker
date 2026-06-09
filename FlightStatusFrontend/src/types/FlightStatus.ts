export type UnifiedFlightStatus = "OnTime" | "Delayed" | "Cancelled" | "Diverted" | "Unknown";

export interface FlightStatusResult {
  FlightNumber: string;
  Date: string;
  Status: UnifiedFlightStatus;
  StatusReason?: string | null;
  ScheduledDepartureUtc?: string | null;
  ActualDepartureUtc?: string | null;
  ScheduledArrivalUtc?: string | null;
  ActualArrivalUtc?: string | null;
  Terminal?: string | null;
  Gate?: string | null;
  RawProviderPayload?: any;
}

export interface ApiError {
  Error: string;
  Message?: string;
  Timestamp?: string;
}
