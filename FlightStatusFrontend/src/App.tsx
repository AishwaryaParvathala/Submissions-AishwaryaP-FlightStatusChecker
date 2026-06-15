import { useState } from "react";
import SearchForm from "./components/SearchForm";
import ResultCard from "./components/ResultCard";
import FlightTakeoffIcon from '@mui/icons-material/FlightTakeoff';
import type { FlightStatusResult } from "./types/FlightStatus";
import { API_BASE } from "../src/config";

export default function App() {
  const [result, setResult] = useState<FlightStatusResult | null>(null);
  const [isLoading, setIsLoading] = useState(false);
  const [error, setError] = useState<string | null>(null);

  async function handleSearch(flight: string, date: string) {
    setIsLoading(true);
    setError(null);
    setResult(null);
    try {
      const baseRaw = API_BASE;
      const baseTrim = (baseRaw as string).replace(/\/$/, "");
      const url = `${baseTrim}/flights/status/${encodeURIComponent(flight)}/${encodeURIComponent(date)}`;
      const res = await fetch(url);
      if (!res.ok) {
        // try to parse API error shape
        let msg = `Request failed: ${res.status}`;
        try {
          const err = await res.json();
          msg = err?.Message || err?.error || msg;
        } catch { }
        throw new Error(msg);
      }
      const data = await res.json();
      // Normalize response keys (support camelCase and PascalCase)
      const normalized: FlightStatusResult = {
        FlightNumber: data.flightNumber ?? data.FlightNumber ?? "",
        Date: data.date ?? data.Date ?? "",
        Status: data.status ?? data.Status ?? (data.StatusReason ? "Unknown" : "Unknown"),
        StatusReason: data.statusReason ?? data.StatusReason ?? null,
        ScheduledDepartureUtc: data.scheduledDepartureUtc ?? data.ScheduledDepartureUtc ?? null,
        ActualDepartureUtc: data.actualDepartureUtc ?? data.ActualDepartureUtc ?? null,
        ScheduledArrivalUtc: data.scheduledArrivalUtc ?? data.ScheduledArrivalUtc ?? null,
        ActualArrivalUtc: data.actualArrivalUtc ?? data.ActualArrivalUtc ?? null,
        Terminal: data.terminal ?? data.Terminal ?? null,
        Gate: data.gate ?? data.Gate ?? null,
        RawProviderPayload: data.rawProviderPayload ?? data.RawProviderPayload ?? null,
      };

      setResult(normalized);
    } catch (e: any) {
      setError(e?.message || "Request failed");
    } finally {
      setIsLoading(false);
    }
  }

  return (
    <div className="container">
      <div className="page-header">
        <div>
          <h1 style={{display:'flex',alignItems:'center',gap:8}}><FlightTakeoffIcon /> Flight Status Checker</h1>
          <div className="page-sub">Please Enter your Flight Number and Date to know the status of your flight.</div>
        </div>
      </div>
      <SearchForm onSearch={handleSearch} isLoading={isLoading} />
      {error && <div className="mt error">{error}</div>}
      <ResultCard result={result} />
    </div>
  );
}
