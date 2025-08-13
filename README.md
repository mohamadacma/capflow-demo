

# CapFlow (Approvals + CAPA demo)

Local .NET 9 + SQLite prototype that turns manual SOP/deviation approvals into a traceable workflow with an audit trail and metrics.
Runs entirely locally. No accounts, logins, or cloud services needed.

## Why
- Mirrors real QA/ISO/HACCP workflows: requests → approvals → CAPA
- Full audit trail
- Every decision is logged (who/when/what) for auditability
- Measurable KPIs
- Shows measurable metrics (approval cycle time) and a clean path to Power Platform (Dataverse + Approvals + Teams)

## Features
Create requests (SOP change, deviation, etc.)

Approve or reject with optional CAPA creation

**Audit trail** (`ApprovalAction` entries per decision).

Automatic logging of approval actions

**Metrics** endpoint (totals, approved, avg approval hours).

 **QA-only approvals** via `X-User-Role: QA` header.
 - CSV export for auditors.
 -  Swagger UI for exploring the API

### Endpoints

POST /requests — create a request

GET /requests — list all (includes action history)

GET /requests/{id} — get one by id

GET /requests/pending — list only pending (if added)

POST /requests/{id}/decision?actor=...&outcome=Approved|Rejected&notes=...&createCapa=true|false — approve/reject

(If QA-only is enabled) add header: X-User-Role: QA

GET /capas — list CAPA records (if added)

GET /metrics — { total, approved, avgApprovalHours }

GET /export/approvals.csv — CSV audit export (if added)

## Run
```bash
dotnet build && dotnet run
# API at http://localhost:5001/
# Swagger UI: http://localhost:5001/swagger


Example API Calls

Create a request: 
curl -X POST http://localhost:5001/requests \
  -H "Content-Type: application/json" \
  --data '{"Title":"Change incubation temp","Type":"SOP Change","Description":"Update step 3 to 38C","RequestedBy":"alice@lab","RequiresCAPA":true}'

Approve the request: 
curl -X POST "http://localhost:5001/requests/{id}/decision?actor=bob@qa&outcome=Approved&notes=Looks good&createCapa=true" \
  -H "X-User-Role: QA"