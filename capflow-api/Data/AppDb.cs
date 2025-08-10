using Microsoft.EntityFrameworkCore;
using CapFlow.Models;

namespace CapFlow.Data;

public class AppDb : DbContext {
    public AppDb(DbContextOptions<AppDb> o) : base(o) {}

    public DbSet<Request> Requests => Set<Request>();
    public DbSet<ApprovalAction> ApprovalActions => Set<ApprovalAction>();
    public DbSet<CAPA> CAPAs => Set<CAPA>();
    public DbSet<User> Users => Set<User>();

    protected override void OnModelCreating(ModelBuilder b){
        b.Entity<Request>()
            .HasMany(r => r.Actions)
            .WithOne(a => a.Request!)
            .HasForeignKey(a => a.RequestId);
        b.Entity<Request>().Property(x => x.Status).HasMaxLength(20);
        b.Entity<User>().HasIndex(u => u.Email).IsUnique();
    }
}
