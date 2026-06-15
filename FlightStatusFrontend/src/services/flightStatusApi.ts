import axios from "axios";
import type { FlightStatusResult } from "../types/FlightStatus";
import { API_BASE } from "../config";



export async function getFlightStatus(flightNumber: string, date: string): Promise<FlightStatusResult> {
  const url = `${API_BASE}flights/status/${encodeURIComponent(flightNumber)}/${encodeURIComponent(date)}`;
  const resp = await axios.get<FlightStatusResult>(url, { timeout: 5000 });
  return resp.data;
}
