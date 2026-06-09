import React from 'react';
import { render, screen } from '@testing-library/react';
import ResultCard from './ResultCard';
import type { FlightStatusResult } from '../types/FlightStatus';

describe('ResultCard', () => {
  it('does not render Terminal / Gate when both are absent', () => {
    const r: FlightStatusResult = {
      FlightNumber: 'AA100',
      Date: '2026-06-09',
      Status: 'OnTime',
      StatusReason: null,
      ScheduledDepartureUtc: null,
      ActualDepartureUtc: null,
      ScheduledArrivalUtc: null,
      ActualArrivalUtc: null,
      Terminal: null,
      Gate: null,
      RawProviderPayload: null,
    };

    render(<ResultCard result={r} />);

    const header = screen.getByText(/AA100 — 2026-06-09/);
    expect(header).toBeInTheDocument();

    // Terminal / Gate label should not be present
    expect(screen.queryByText('Terminal / Gate')).toBeNull();
  });

  it('renders Terminal / Gate when present', () => {
    const r: FlightStatusResult = {
      FlightNumber: 'BB200',
      Date: '2026-07-01',
      Status: 'Delayed',
      StatusReason: 'Weather',
      ScheduledDepartureUtc: '2026-07-01T10:00:00Z',
      ActualDepartureUtc: null,
      ScheduledArrivalUtc: null,
      ActualArrivalUtc: null,
      Terminal: 'T1',
      Gate: 'A5',
      RawProviderPayload: null,
    };

    render(<ResultCard result={r} />);

    expect(screen.getByText('Terminal / Gate')).toBeInTheDocument();
    // match the rendered combined value exactly
    expect(screen.getByText(/T1\s*\/\s*A5/)).toBeInTheDocument();
    expect(screen.getByText(/Weather/)).toBeInTheDocument();
  });
});
