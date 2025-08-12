using CapFlow.Data;
using CapFlow.Models;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.WebHost.UseUrls("http://localhost:5001");

builder.Services.AddDbContext<AppDb>(o => o.UseSqlite("Data Source=capflow.db"));
builder.Services.AddEndpointsApiExplorer();

var app = builder.Build();
app.UseDefaultFiles();
app.UseStaticFiles();

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDb>();
    db.Database.EnsureCreated();
}


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
});

//List requests
app.MapGet("/requests", async (AppDb db) =>
    await db.Requests.Include(r => r.Actions).ToListAsync());

// Approve/Reject
app.MapPost("/requests", async (AppDb db, Request r) =>
{
    if (string.IsNullOrWhiteSpace(r.Title) || string.IsNullOrWhiteSpace(r.RequestedBy))
        return Results.BadRequest(new { error = "Title and RequestedBy are required." });


    r.Status = "Pending";
    db.Requests.Add(r);
    await db.SaveChangesAsync();
    return Results.Created($"/requests/{r.Id}", r);
});


app.MapPost("/requests/{id:guid}/decision", async (Guid id, AppDb db,
string actor, string outcome, string? notes, bool createCapa = false) =>
{
    var req = await db.Requests.FindAsync(id);
    if (req is null) return Results.NotFound();

    db.ApprovalActions.Add(new ApprovalAction {
        RequestId = id, Actor = actor, Outcome = outcome, Notes = notes ?? ""
});

if (outcome == "Approved") {
    req.Status = "Approved";
    req.ApprovedAt = DateTime.UtcNow;
    if (createCapa) db.CAPAs.Add(new CAPA { RequestId = id, Owner = actor });
} else {
    req.Status = "Rejected";
}

await db.SaveChangesAsync();
return Results.Ok(req);
});

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
     return Results.Ok(new { total, approved, avgApprovalHours = Math.Round(avgHours, 2) });
});



//GET 
app.MapGet("/requests/{id:guid}", async (Guid id, AppDb db) =>
    await db.Requests.Include(r => r.Actions).FirstOrDefaultAsync(r => r.Id == id) is { } req
        ? Results.Ok(req) : Results.NotFound());

// GET pending only
app.MapGet("/requests/pending", async (AppDb db) =>
    await db.Requests.Where(r => r.Status == "Pending")
                        .Include(r => r.Actions).ToListAsync());

// CAPA list
app.MapGet("/capas", async (AppDb db) => await db.CAPAs.ToListAsync());

app.Run();
