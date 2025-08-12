

# CapFlow (Approvals + CAPA demo)

Local .NET 8 + SQLite prototype that turns manual SOP/deviation approvals into a traceable workflow with an audit trail and metrics. No accounts or cloud needed.

## Why
- Mirrors real QA/ISO/HACCP workflows: requests → approvals → CAPA
- Every decision is logged (who/when/what) for auditability
- Shows measurable metrics (approval cycle time) and a clean path to Power Platform (Dataverse + Approvals + Teams)

## Run
```bash
dotnet build && dotnet run
# API at http://localhost:5001/
