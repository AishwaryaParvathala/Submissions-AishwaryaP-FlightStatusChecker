import React, { useState } from "react";
import TextField from '@mui/material/TextField';
import dayjs, { Dayjs } from 'dayjs';
import { LocalizationProvider } from '@mui/x-date-pickers/LocalizationProvider';
import { DatePicker } from '@mui/x-date-pickers/DatePicker';
import { AdapterDayjs } from '@mui/x-date-pickers/AdapterDayjs';

type Props = { onSearch: (flight: string, date: string) => void; isLoading: boolean };

export default function SearchForm({ onSearch, isLoading }: Props) {
  const [flight, setFlight] = useState("");
  const [date, setDate] = useState<Dayjs | null>(dayjs());
  const [error, setError] = useState<string | null>(null);

  function submit(e: React.FormEvent) {
    e.preventDefault();
    setError(null);
    if (!flight.trim()) return setError("Flight number is required");
    if (!date) return setError("Date is required");
    onSearch(flight.trim().toUpperCase(), date.format('YYYY-MM-DD'));
  }

  return (
    <form className="card" onSubmit={submit}>
      <div className="form-grid">
        <div style={{flex:1}} className="field">
          <TextField
            label="Flight number"
            placeholder="AA123"
            value={flight}
            onChange={(e)=>setFlight(e.target.value)}
            fullWidth
            size="small"
          />
        </div>
        <div style={{width:200}} className="field">
          <LocalizationProvider dateAdapter={AdapterDayjs}>
            <DatePicker
              label="Date"
              value={date}
              onChange={(v)=>setDate(v)}
              disableFuture={false}
              slotProps={{ textField: { size: 'small', fullWidth: true } }}
            />
          </LocalizationProvider>
        </div>
      </div>
      <div style={{display:'flex',alignItems:'center',justifyContent:'center',marginTop:20}}>
          <button className="btn" disabled={isLoading} type="submit">{isLoading? 'Searching...':'Search'}</button>
    </div>
      {error && <div className="mt error">{error}</div>}
    </form>
  );
}
