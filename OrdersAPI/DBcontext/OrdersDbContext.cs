using Microsoft.EntityFrameworkCore;
using OrdersAPI.Models;

namespace OrdersAPI.DBcontext
{
    public class OrdersDbContext : DbContext
    {
        public OrdersDbContext(DbContextOptions<OrdersDbContext> options) : base(options)
        {
        }

        public DbSet<OrderModel> Orders => Set<OrderModel>();

        public DbSet<OrderItemModel> OrderItems => Set<OrderItemModel>();

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<OrderModel>(entity =>
            {
                entity.HasMany(o => o.Items)
                      .WithOne(i => i.Order)
                      .HasForeignKey(i => i.OrderId)
                      .OnDelete(DeleteBehavior.Cascade);
            });
        }
    }
}
