import React from "react";
import type { FlightStatusResult } from "../types/FlightStatus";

type Props = { result: FlightStatusResult | null };

export default function ResultCard({ result }: Props) {
  if (!result) return null;

  return (
    <div className="card">
      <div className="page-header">
        <div>
          <h3 style={{ margin: 0 }}>{result.FlightNumber} — {result.Date}</h3>
          <div className="page-sub">Flight status summary</div>
        </div>
        <div>
          <span className={`status-badge status-${result.Status}`}>{result.Status}</span>
        </div>
      </div>

      {result.StatusReason && <div className="mt"><strong>Reason:</strong> {result.StatusReason}</div>}

      <div className="info-grid">
        <div className="info-item">
          <strong>Scheduled Departure (UTC)</strong>
          <div className="value">{result.ScheduledDepartureUtc ?? '—'}</div>
        </div>
        <div className="info-item">
          <strong>Actual Departure (UTC)</strong>
          <div className="value">{result.ActualDepartureUtc ?? '—'}</div>
        </div>
        <div className="info-item">
          <strong>Scheduled Arrival (UTC)</strong>
          <div className="value">{result.ScheduledArrivalUtc ?? '—'}</div>
        </div>
        {(result.Terminal || result.Gate) && (
          <div className="info-item">
            <strong>Terminal / Gate</strong>
            <div className="value">{result.Terminal ?? ''}{result.Gate ? ` / ${result.Gate}` : ''}</div>
          </div>
        )}
      </div>
    </div>
  );
}
