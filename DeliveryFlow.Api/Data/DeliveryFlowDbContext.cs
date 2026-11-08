using DeliveryFlow.Api.Models;
using Microsoft.EntityFrameworkCore;

namespace DeliveryFlow.Api.Data;

public class DeliveryFlowDbContext(
    DbContextOptions<DeliveryFlowDbContext> options) : DbContext(options)
{
    public DbSet<Order> Orders { get; set; }
    public DbSet<Courier> Couriers { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Order>()
            .HasOne<Courier>() // у заказа один курьер
            .WithMany() // у курьера много заказов
            .HasForeignKey(o => o.CourierId) // внешний ключ в Orders
            .OnDelete(DeleteBehavior.Restrict) // удаление курьера запрещено, если есть связь
            .IsRequired(false); // необязательная связь
    }
}