using CartAPI.MODELS.DB_MODELS;
using Microsoft.EntityFrameworkCore;

namespace CartAPI.DBcontext
{
    public class CartDbContext : DbContext
    {
        public CartDbContext(DbContextOptions<CartDbContext> options) : base(options)
        {
        }

        public DbSet<CartModel> Carts => Set<CartModel>();
    }
}
