using Microsoft.EntityFrameworkCore;
using QueueSystem.API.Models;

namespace QueueSystem.API.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<ServiceType> ServiceTypes => Set<ServiceType>();
    public DbSet<Department> Departments => Set<Department>();
    public DbSet<Counter> Counters => Set<Counter>();
    public DbSet<CounterServiceType> CounterServiceTypes => Set<CounterServiceType>();
    public DbSet<Ticket> Tickets => Set<Ticket>();
    public DbSet<TicketRedirect> TicketRedirects => Set<TicketRedirect>();
    public DbSet<TicketEvent> TicketEvents => Set<TicketEvent>();
    public DbSet<ManagerDepartment> ManagerDepartments => Set<ManagerDepartment>();
    public DbSet<Operator> Operators => Set<Operator>();
    public DbSet<AppUser> AppUsers => Set<AppUser>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.Entity<ServiceType>()
            .HasIndex(serviceType => serviceType.Prefix)
            .IsUnique();

        modelBuilder.Entity<AppUser>()
            .HasIndex(user => user.Username)
            .IsUnique();

        modelBuilder.Entity<Department>()
            .HasIndex(department => department.Name)
            .IsUnique();

        modelBuilder.Entity<Counter>()
            .HasIndex(counter => counter.DisplayOrder);

        modelBuilder.Entity<AppUser>()
            .HasIndex(user => user.OperatorId)
            .IsUnique()
            .HasFilter("OperatorId IS NOT NULL");

        modelBuilder.Entity<CounterServiceType>()
            .HasKey(item => new { item.CounterId, item.ServiceTypeId });

        modelBuilder.Entity<ManagerDepartment>()
            .HasKey(item => new { item.AppUserId, item.DepartmentId });

        modelBuilder.Entity<ManagerDepartment>()
            .HasOne(item => item.AppUser)
            .WithMany(user => user.ManagerDepartments)
            .HasForeignKey(item => item.AppUserId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<ManagerDepartment>()
            .HasOne(item => item.Department)
            .WithMany(department => department.ManagerDepartments)
            .HasForeignKey(item => item.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Counter>()
            .HasOne(counter => counter.Department)
            .WithMany(department => department.Counters)
            .HasForeignKey(counter => counter.DepartmentId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<CounterServiceType>()
            .HasOne(item => item.Counter)
            .WithMany(counter => counter.CounterServiceTypes)
            .HasForeignKey(item => item.CounterId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<CounterServiceType>()
            .HasOne(item => item.ServiceType)
            .WithMany(serviceType => serviceType.CounterServiceTypes)
            .HasForeignKey(item => item.ServiceTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Operator>()
            .HasOne(@operator => @operator.Counter)
            .WithMany(counter => counter.Operators)
            .HasForeignKey(@operator => @operator.CounterId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<AppUser>()
            .HasOne(user => user.Operator)
            .WithOne(@operator => @operator.AppUser)
            .HasForeignKey<AppUser>(user => user.OperatorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Ticket>()
            .HasOne(ticket => ticket.ServiceType)
            .WithMany(serviceType => serviceType.Tickets)
            .HasForeignKey(ticket => ticket.ServiceTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<Ticket>()
            .HasOne(ticket => ticket.Counter)
            .WithMany(counter => counter.Tickets)
            .HasForeignKey(ticket => ticket.CounterId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TicketRedirect>()
            .HasOne(redirect => redirect.Ticket)
            .WithMany(ticket => ticket.Redirects)
            .HasForeignKey(redirect => redirect.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TicketRedirect>()
            .HasOne(redirect => redirect.FromServiceType)
            .WithMany()
            .HasForeignKey(redirect => redirect.FromServiceTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TicketRedirect>()
            .HasOne(redirect => redirect.ToServiceType)
            .WithMany()
            .HasForeignKey(redirect => redirect.ToServiceTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TicketRedirect>()
            .HasOne(redirect => redirect.FromCounter)
            .WithMany()
            .HasForeignKey(redirect => redirect.FromCounterId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TicketRedirect>()
            .HasOne(redirect => redirect.RedirectedByOperator)
            .WithMany()
            .HasForeignKey(redirect => redirect.RedirectedByOperatorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TicketEvent>()
            .HasIndex(ticketEvent => ticketEvent.CreatedAt);

        modelBuilder.Entity<TicketEvent>()
            .HasIndex(ticketEvent => ticketEvent.EventType);

        modelBuilder.Entity<TicketEvent>()
            .HasOne(ticketEvent => ticketEvent.Ticket)
            .WithMany()
            .HasForeignKey(ticketEvent => ticketEvent.TicketId)
            .OnDelete(DeleteBehavior.Cascade);

        modelBuilder.Entity<TicketEvent>()
            .HasOne(ticketEvent => ticketEvent.Operator)
            .WithMany()
            .HasForeignKey(ticketEvent => ticketEvent.OperatorId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TicketEvent>()
            .HasOne(ticketEvent => ticketEvent.Counter)
            .WithMany()
            .HasForeignKey(ticketEvent => ticketEvent.CounterId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TicketEvent>()
            .HasOne(ticketEvent => ticketEvent.ServiceType)
            .WithMany()
            .HasForeignKey(ticketEvent => ticketEvent.ServiceTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TicketEvent>()
            .HasOne(ticketEvent => ticketEvent.FromServiceType)
            .WithMany()
            .HasForeignKey(ticketEvent => ticketEvent.FromServiceTypeId)
            .OnDelete(DeleteBehavior.Restrict);

        modelBuilder.Entity<TicketEvent>()
            .HasOne(ticketEvent => ticketEvent.ToServiceType)
            .WithMany()
            .HasForeignKey(ticketEvent => ticketEvent.ToServiceTypeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
