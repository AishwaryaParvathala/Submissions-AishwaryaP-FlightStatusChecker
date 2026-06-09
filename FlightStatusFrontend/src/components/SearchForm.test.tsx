import React from 'react';
import { render, screen, fireEvent } from '@testing-library/react';
import userEvent from '@testing-library/user-event';
import SearchForm from './SearchForm';

describe('SearchForm', () => {
  it('shows validation when flight is empty', async () => {
    const onSearch = jest.fn();
    render(<SearchForm onSearch={onSearch} isLoading={false} />);

    const btn = screen.getByRole('button', { name: /search/i });
    await userEvent.click(btn);

    expect(screen.getByText(/Flight number is required/)).toBeInTheDocument();
    expect(onSearch).not.toHaveBeenCalled();
  });

 it("calls onSearch with uppercase flight and formatted date", async () => {
  const onSearch = jest.fn();
  render(<SearchForm onSearch={onSearch} isLoading={false} />);

  const flightInput = screen.getByLabelText(/Flight number/i);
 //  const dateInput = screen.getByRole("textbox", { name: /Date/i });
  const btn = screen.getByRole("button", { name: /search/i });

  await userEvent.type(flightInput, "AA123");

  // Simulate user typing a date string into the DatePicker's textbox
  // fireEvent.change(dateInput, { target: { value: "2026-06-09" } });

  await userEvent.click(btn);

  expect(onSearch).toHaveBeenCalledTimes(1);
  expect(onSearch).toHaveBeenCalledWith("AA123", "2026-06-09");
});
});
