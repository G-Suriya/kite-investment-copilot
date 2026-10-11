#!/usr/bin/env bash

# Kite Investment Copilot — Startup Script
# Starts ASP.NET Core Backend (port 5000) and Angular Frontend (port 4200)

set -e

SCRIPT_DIR="$(cd "$(dirname "${BASH_SOURCE[0]}")" && pwd)"
BACKEND_DIR="$SCRIPT_DIR/backend"
FRONTEND_DIR="$SCRIPT_DIR/frontend"

echo "=========================================================="
echo "🚀 Starting Gemini + Zerodha/Kite Investment Copilot"
echo "=========================================================="

cleanup() {
  echo ""
  echo "Shutting down Kite Investment Copilot..."
  kill $(jobs -p) 2>/dev/null || true
  exit 0
}

trap cleanup SIGINT SIGTERM EXIT

# 1. Start ASP.NET Core API
echo "Starting ASP.NET Core Web API on http://localhost:5000 ..."
cd "$BACKEND_DIR/KiteCopilot.API"
dotnet run --launch-profile http &
BACKEND_PID=$!

# Wait for backend health endpoint
echo "Waiting for API to be ready..."
for i in {1..30}; do
  if curl -s http://localhost:5000/health > /dev/null 2>&1; then
    echo "✅ Backend API is ready at http://localhost:5000"
    echo "📖 Swagger UI available at http://localhost:5000/swagger"
    break
  fi
  sleep 1
done

# 2. Start Angular Frontend
echo "Starting Angular Frontend on http://localhost:4200 ..."
cd "$FRONTEND_DIR"
npx ng serve --port 4200 --host 0.0.0.0 &
FRONTEND_PID=$!

echo ""
echo "=========================================================="
echo "🎉 Kite Investment Copilot is running!"
echo "   👉 Frontend: http://localhost:4200"
echo "   👉 Backend API: http://localhost:5000"
echo "   👉 Swagger API Docs: http://localhost:5000/swagger"
echo "=========================================================="
echo "Press Ctrl+C to stop all services."
echo ""

wait
