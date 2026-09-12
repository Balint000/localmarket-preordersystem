# LocalMarket Pre-order System

A pre-order management system for local markets, built with **.NET (target framework: net10.0)** using a Clean Architecture layout, with an ASP.NET Core Web API backend and a Blazor client frontend.

## Project Purpose

The application lets local market vendors and customers manage product pre-orders — customers can browse available items and place orders ahead of a market day, while vendors can track and fulfill incoming pre-orders.

## Architecture

The solution follows a **Clean Architecture** (layered) approach, separating concerns across independent projects:

| Project | Responsibility |
|---|---|
| `localmarket-preordersystem.Domain` | Core domain entities, value objects, and business rules.|
| `localmarket-preordersystem.Application` | Application/use-case logic, orchestrating domain objects (e.g. commands, queries, service interfaces). |
| `localmarket-preordersystem.Infrastructure` | Implementation details — data persistence, external services, and other infrastructure concerns. |
| `localmarket-preordersystem.api` | ASP.NET Core Web API exposing the application's functionality over HTTP. |
| `localmarket-preordersystem-blazerClient` | Blazor-based client application (frontend) consuming the API. |

## Tech Stack

- **.NET / C#** — target framework `net10.0`
- **ASP.NET Core** — Web API backend
- **Blazor** — client-side/server-side UI
- Configuration via `appsettings.json` / `appsettings.Development.json`
