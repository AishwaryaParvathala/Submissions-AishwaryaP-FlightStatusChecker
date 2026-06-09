import axios from "axios";
import type { FlightStatusResult } from "../types/FlightStatus";


export const API_BASE = "https://localhost:5001";

export async function getFlightStatus(flightNumber: string, date: string): Promise<FlightStatusResult> {
  const url = `${API_BASE}flights/status/${encodeURIComponent(flightNumber)}/${encodeURIComponent(date)}`;
  const resp = await axios.get<FlightStatusResult>(url, { timeout: 5000 });
  return resp.data;
}
