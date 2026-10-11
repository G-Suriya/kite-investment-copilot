# 🚀 Gemini + Zerodha/Kite Investment Copilot

An institutional-grade personal investment research assistant built around **Zerodha Kite Connect** and powered by **Google Gemini** with **Hybrid RAG** over regulatory filings, corporate earnings, and personal investment thesis memory.

> [!IMPORTANT]
> **Core Architecture Principle & Safety Guardrails**:
> * Gemini understands your research intent and orchestrates analytical tools.
> * Your application executes explicitly authorized data and watchlist operations.
> * **Zero Autonomous Trading**: Place order, modify order, and cancel order capabilities are intentionally **not exposed** to the AI model. Investment decisions remain 100% yours.

---

## 🏛️ System Architecture

```
                                  YOU
                                   │
                                   ▼
                        ┌─────────────────────┐
                        │     Angular UI      │
                        │  Chat + Dashboard   │
                        └──────────┬──────────┘
                                   │ HTTP
                                   ▼
                        ┌─────────────────────┐
                        │   ASP.NET Core 8    │
                        │     Web API         │
                        └──────────┬──────────┘
                                   │
                ┌──────────────────┴──────────────────┐
                │                                     │
                ▼                                     ▼
     ┌──────────────────────┐              ┌──────────────────────┐
     │  Google Gemini API   │              │   Application Core   │
     │ Function / Tool Call │              │  Tool Router & Guard │
     └──────────┬───────────┘              └──────────┬───────────┘
                │                                     │
                └──────────────────┬──────────────────┘
                                   │
         ┌─────────────────────────┼─────────────────────────┐
         ▼                         ▼                         ▼
┌──────────────────┐      ┌──────────────────┐      ┌──────────────────┐
│   Zerodha Kite   │      │ Application DB   │      │    Hybrid RAG    │
│   Structured     │      │   (Postgres/     │      │     pgvector     │
│      Data        │      │    SQLite)       │      │  Semantic Index  │
└──────────────────┘      └──────────────────┘      └──────────────────┘
• Holdings                • Custom Watchlists       • Annual Reports
• Positions               • Investment Theses       • Q2 Earnings Decks
• Margins / Funds         • Personal Notes          • DRHP / RHP (IPOs)
• Live Quotes (NSE)       • AI Audit Trail          • ETF Factsheets
```

---

## 💎 Features & Capabilities

| Capability | Status | Description |
| :--- | :---: | :--- |
| **View Kite Holdings & P&L** | ✅ | Live quantities, average buy price, current LTP, Day change, and Unrealized P&L |
| **Intraday & Open Positions** | ✅ | Real-time positions tracking across MIS, CNC, and NRML products |
| **Funds & Margin Monitor** | ✅ | Available cash margin, utilized debits, collateral margin, total purchasing power |
| **Decoupled Custom Watchlists** | ✅ | Dedicated watchlists (Long Term, ETF, IPO, Custom) stored independently of Kite UI |
| **Add / Remove Assets** | ✅ | Natural language tool calling: *"Add MON100 to ETF watchlist"*, *"Remove TCS"* |
| **Stock Deep-Dive Analysis** | ✅ | Multi-ratio valuation (P/E, Market Cap, 52W range), business summary, filings excerpt |
| **ETF Factsheet Intelligence** | ✅ | Expense ratio (TER), tracking error, AUM, top constituent weights, INR hedge note |
| **IPO Research & SEBI RHP** | ✅ | Issue size, price band, fresh vs OFS split, post-issue P/E, strengths, and risks |
| **Asset Comparison Matrix** | ✅ | Side-by-side financial and price metrics across 2–5 equities or ETFs |
| **Hybrid RAG Semantic Search** | ✅ | Vector similarity search over parsed corporate filings and financial disclosures |
| **Personal Investment Memory** | ✅ | Document your rationale, time horizon, and key risks for any equity holding |
| **Thesis Alignment Review** | ✅ | RAG comparison of your original thesis against subsequent quarterly earnings & price action |
| **Strict AI Safety Guardrail** | 🛡️ | Automatically catches and rejects any prompt requesting trade execution |
| **AI Audit Trail** | 📋 | Persistent database record of every tool invocation, arguments, duration, and status |
| **Automated Trading Bot** | ❌ | **Deliberately disabled** by architectural mandate |

---

## 🛠️ Technology Stack

* **Backend**: ASP.NET Core 8 Web API (Clean Architecture: API, Application, Domain, Infrastructure, Tests)
* **Frontend**: Angular 19/20 (Standalone Components, Signals, Reactive HTTP Client, Glassmorphism Dark Mode)
* **Database**:
  * **Local / Out-of-the-box**: SQLite with high-precision in-memory/embedded cosine vector similarity
  * **Production / Docker**: PostgreSQL 16 with `pgvector`
* **AI Orchestration**: Google Gemini 1.5/2.0 Flash Function Calling & Text Embeddings
* **Broker Integration**: Zerodha Kite Connect Personal API v3 (with 1-click Demo Mode)

---

## 🚀 Quick Start (Local Run)

### 1. Prerequisites
* [.NET 8 SDK](https://dotnet.microsoft.com/download/dotnet/8.0)
* [Node.js v18+](https://nodejs.org) and `npm`

### 2. Run with One Command
Execute the unified startup script from the project root:

```bash
cd /Users/suriya/.gemini/antigravity-ide/scratch/kite-investment-copilot
./start.sh
```

This launches:
* **Backend API**: [http://localhost:5000](http://localhost:5000)
* **Swagger API Docs**: [http://localhost:5000/swagger](http://localhost:5000/swagger)
* **Angular Web App**: [http://localhost:4200](http://localhost:4200)

---

## ⚙️ Configuration

Settings are managed in `backend/KiteCopilot.API/appsettings.json` or via environment variables:

```json
{
  "Database": {
    "Provider": "Sqlite"
  },
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=kite_copilot.db",
    "PostgreSql": "Host=localhost;Port=5432;Database=kite_copilot;Username=postgres;Password=postgres"
  },
  "Gemini": {
    "ApiKey": "YOUR_GEMINI_API_KEY",
    "Model": "gemini-1.5-flash"
  },
  "Kite": {
    "ApiKey": "YOUR_KITE_API_KEY",
    "ApiSecret": "YOUR_KITE_API_SECRET",
    "AccessToken": "",
    "RedirectUrl": "http://localhost:5000/api/kite/callback"
  }
}
```

* **Offline / Demo Mode**: If no Gemini API key or Kite credentials are provided, the system seamlessly operates in **Demo Mode** with realistic NSE market quotes and built-in semantic tool routing so you can test 100% of features locally!

---

## 🛡️ AI Safety & Tool Calling Reference

Gemini communicates with backend services strictly via structured function tools:

| Tool Name | Parameters | Purpose |
| :--- | :--- | :--- |
| `get_holdings` | *none* | Fetch Kite portfolio holdings & P&L |
| `get_positions` | *none* | Fetch active intraday positions |
| `get_funds` | *none* | Fetch available cash margins |
| `get_orders` | *none* | Read historical orderbook |
| `get_watchlists` | *none* | List all personal watchlists |
| `add_to_watchlist` | `watchlist, symbol, assetType` | Add stock/ETF/IPO to watchlist |
| `remove_from_watchlist` | `watchlist, symbol` | Remove asset from watchlist |
| `get_stock_data` | `symbol` | Fundamental stock analysis & ratios |
| `get_etf_data` | `symbol` | Factsheet, TER, tracking error |
| `get_ipo_data` | `symbol` | Issue size, price band, SEBI RHP risks |
| `compare_assets` | `symbols[]` | Multi-asset side-by-side comparison |
| `save_investment_thesis` | `symbol, title, thesis, horizon` | Store investment rationale in memory |
| `analyze_thesis_alignment` | `symbol` | Evaluate thesis vs latest corporate filings |

---

## 🧪 Automated Test Suite

Run the full suite of unit and AI safety guardrail tests:

```bash
cd backend
dotnet test
```

Test coverage includes:
* `WatchlistServiceTests`: CRUD and duplicate prevention
* `KiteServiceTests`: Holdings, funds, positions, and live calculation
* `AiSafetyAndGuardrailTests`:
  * Verifies `"Buy 50 shares of INFY"` triggers safety guardrail
  * Verifies `"Sell everything"` is rejected
  * Verifies `"Delete all watchlists"` requires manual authorization
  * Verifies legitimate research queries execute and log to `AiAuditLog`
