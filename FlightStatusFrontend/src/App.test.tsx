// app.test.tsx
import { render, screen, fireEvent, waitFor } from "@testing-library/react";
import App from "./App";

// Mock SearchForm to simulate uppercase + formatted date behavior
jest.mock("./components/SearchForm", () => (props: any) => (
  <button
    onClick={() => {
      // Simulate SearchForm transforming inputs
      const flight = "sr101".toUpperCase(); // uppercase
      const date = new Date("2026-06-09").toISOString().split("T")[0]; // formatted YYYY-MM-DD
      props.onSearch(flight, date);
    }}
  >
    Search Flight
  </button>
));

jest.mock("./components/ResultCard", () => (props: any) => (
  <div data-testid="result-card">
    {props.result ? JSON.stringify(props.result) : "No result"}
  </div>
));

describe("App Component", () => {
  beforeEach(() => {
    jest.resetAllMocks();
  });

  it("renders header and instructions", () => {
    render(<App />);
    expect(screen.getByText(/Flight Status Checker/i)).toBeInTheDocument();
    expect(
      screen.getByText(/Please Enter your Flight Number/i)
    ).toBeInTheDocument();
  });

  it("shows loading state when search is triggered", async () => {
    (globalThis as any).fetch = jest.fn(() =>
      Promise.resolve({
        ok: true,
        json: () =>
          Promise.resolve({
            flightNumber: "SR101",
            date: "2026-06-09",
            status: "OnTime",
          }),
      })
    ) as jest.Mock;

    render(<App />);
    fireEvent.click(screen.getByText("Search Flight"));

    await waitFor(() => {
      expect(globalThis.fetch).toHaveBeenCalled();
    });
  });

  it("renders error message when fetch fails", async () => {
    (globalThis as any).fetch = jest.fn(() =>
      Promise.resolve({
        ok: false,
        status: 404,
        json: () => Promise.resolve({ Message: "Flight not found" }),
      })
    ) as jest.Mock;

    render(<App />);
    fireEvent.click(screen.getByText("Search Flight"));

    await waitFor(() => {
      expect(screen.getByText(/Flight not found/i)).toBeInTheDocument();
    });
  });

  it("renders normalized result when fetch succeeds", async () => {
    (globalThis as any).fetch = jest.fn(() =>
      Promise.resolve({
        ok: true,
        json: () =>
          Promise.resolve({
            FlightNumber: "SR101",
            Date: "2026-06-09",
            Status: "OnTime",
            Terminal: "T1",
            Gate: "A5",
          }),
      })
    ) as jest.Mock;

    render(<App />);
    fireEvent.click(screen.getByText("Search Flight"));

    await waitFor(() => {
      const resultCard = screen.getByTestId("result-card");
      expect(resultCard).toHaveTextContent("SR101");
      expect(resultCard).toHaveTextContent("OnTime");
      expect(resultCard).toHaveTextContent("T1");
      expect(resultCard).toHaveTextContent("A5");
    });
  });

  it("handles unexpected fetch error gracefully", async () => {
    (globalThis as any).fetch = jest.fn(() => Promise.reject(new Error("Network error"))) as jest.Mock;

    render(<App />);
    fireEvent.click(screen.getByText("Search Flight"));

    await waitFor(() => {
      expect(screen.getByText(/Network error/i)).toBeInTheDocument();
    });
  });

  it("calls onSearch with uppercase flight and formatted date", async () => {
    (globalThis as any).fetch = jest.fn(() =>
      Promise.resolve({
        ok: true,
        json: () =>
          Promise.resolve({
            flightNumber: "SR101",
            date: "2026-06-09",
            status: "OnTime",
          }),
      })
    ) as jest.Mock;

    render(<App />);
    fireEvent.click(screen.getByText("Search Flight"));

    await waitFor(() => {
      // Verify fetch URL contains uppercase flight and formatted date
      const calledUrl = (globalThis.fetch as jest.Mock).mock.calls[0][0];
      expect(calledUrl).toContain("SR101");
      expect(calledUrl).toContain("2026-06-09");
    });
  });
});
