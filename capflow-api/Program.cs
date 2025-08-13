using CapFlow.Data;
using CapFlow.Models;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.OpenApi;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://localhost:5001");

builder.Services.AddDbContext<AppDb>(o => o.UseSqlite("Data Source=capflow.db"));
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

var app = builder.Build();
app.UseSwagger();
app.UseSwaggerUI();

app.UseDefaultFiles();
app.UseStaticFiles();


using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDb>();
    db.Database.EnsureCreated();
}

/// <summary>
/// Add sample demo users so you can test without creating accounts.
/// </summary>
// seed demo users
app.MapGet("/seed", async (AppDb db) => {
    if (!await db.Users.AnyAsync()) {
        db.Users.AddRange(
            new User { Name="Alice", Email="alice@lab", Role="Tech" },
            new User { Name="Bob",   Email="bob@qa",   Role="QA"  }
        );
        await db.SaveChangesAsync();
    }
    return Results.Ok("seeded");
}).WithSummary("Seed demo users").WithOpenApi();

/// <summary>
/// Show all requests in the system, including their history of actions.
/// </summary>
//List requests
app.MapGet("/requests", async (AppDb db) =>
    await db.Requests.Include(r => r.Actions).ToListAsync());

/// <summary>
/// Create a new request (example: SOP change, deviation, etc.).
/// </summary>
// Approve/Reject
app.MapPost("/requests", async (AppDb db, Request r) =>
{
    if (string.IsNullOrWhiteSpace(r.Title) || string.IsNullOrWhiteSpace(r.RequestedBy))
        return Results.BadRequest(new { error = "Title and RequestedBy are required." });


    r.Status = "Pending";
    db.Requests.Add(r);
    await db.SaveChangesAsync();
    return Results.Created($"/requests/{r.Id}", r);
}).WithSummary("Create a request").WithOpenApi();


/// <summary>
/// Approve or reject a request.  
/// Only users with the QA role can approve/reject.  
/// Optionally creates a CAPA record if needed.
/// </summary>
app.MapPost("/requests/{id:guid}/decision",
    async (Guid id, AppDb db,
           [FromHeader(Name = "X-User-Role")] string? role,   // QA-only 
           [FromQuery] string actor,
           [FromQuery] string outcome,
           [FromQuery] string? notes,
           [FromQuery] bool createCapa = false) =>
{
    // enforce QA-only approvals
    if (!string.Equals(role, "QA", StringComparison.OrdinalIgnoreCase))
        return Results.StatusCode(403);

    var req = await db.Requests.FindAsync(id);
    if (req is null) return Results.NotFound();

    db.ApprovalActions.Add(new ApprovalAction {
        RequestId = id,
        Actor = actor,
        Outcome = outcome,
        Notes = notes ?? ""
    });

    if (string.Equals(outcome, "Approved", StringComparison.OrdinalIgnoreCase))
    {
        req.Status = "Approved";
        req.ApprovedAt = DateTime.UtcNow;
        if (createCapa)
            db.CAPAs.Add(new CAPA { RequestId = id, Owner = actor });
    }
    else
    {
        req.Status = "Rejected";
    }

    await db.SaveChangesAsync();
    return Results.Ok(req);
}).WithSummary("Approve/Reject a request").WithOpenApi();


/// <summary>
/// Key approval KPIs
/// </summary>
/// <returns>Metrics for requests</returns>
//metrics
app.MapGet("/metrics", async (AppDb db) => {
    var total = await db.Requests.CountAsync();
    var approved = await db.Requests.CountAsync(x => x.Status == "Approved");
    var approvedRows = await db.Requests
        .Where(x => x.ApprovedAt != null)
        .Select(x => new { x.CreatedAt, x.ApprovedAt })
        .ToListAsync();
    var avgHours = approvedRows.Count == 0 ? 0 :
        approvedRows.Average(x => (x.ApprovedAt!.Value - x.CreatedAt).TotalHours);
     return Results.Ok(new 
     { 
        total, approved, avgApprovalHours = Math.Round(avgHours, 2) 
        });
}).WithSummary("Key approval KPIs")
  .WithOpenApi();

/// <summary>
/// Export a full history of approvals into a CSV file.  
/// This is useful for auditors who want a copy of all decisions.
/// </summary>
//CSV export
app.MapGet("/export/approvals.csv", async (AppDb db) =>
{
    var rows = await db.ApprovalActions
        .OrderBy(a => a.At)
        .Select(a => new { a.Id, a.RequestId, a.Actor, a.Outcome, a.Notes, a.At })
        .ToListAsync();

    string esc(string? s) => "\"" + (s ?? "").Replace("\"","\"\"") + "\"";
    var header = "Id,RequestId,Actor,Outcome,Notes,At";
    var lines = rows.Select(r => $"{r.Id},{r.RequestId},{esc(r.Actor)},{r.Outcome},{esc(r.Notes)},{r.At:O}");
    var csv = string.Join("\n", new[]{header}.Concat(lines));
    return Results.Text(csv, "text/csv");
}).WithSummary("Audit export (CSV)").WithOpenApi();


/// <summary>
/// Get one request by its unique ID, including its action history.
/// </summary>
app.MapGet("/requests/{id:guid}", async (Guid id, AppDb db) =>
    await db.Requests.Include(r => r.Actions).FirstOrDefaultAsync(r => r.Id == id) is { } req
        ? Results.Ok(req) : Results.NotFound());


/// <summary>
/// List only the requests that are still pending a decision.
/// </summary>
app.MapGet("/requests/pending", async (AppDb db) =>
    await db.Requests.Where(r => r.Status == "Pending")
                        .Include(r => r.Actions).ToListAsync());

/// <summary>
/// Show all CAPA (Corrective and Preventive Action) records in the system.
/// </summary>
app.MapGet("/capas", async (AppDb db) => await db.CAPAs.ToListAsync())
.WithSummary("List CAPA records").WithOpenApi();

app.Run();
