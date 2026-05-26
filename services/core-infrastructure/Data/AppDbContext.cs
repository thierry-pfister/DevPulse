using Microsoft.EntityFrameworkCore;

namespace DevPulse.Infrastructure.Data;

public class AppDbContext(DbContextOptions<AppDbContext> options) : DbContext(options)
{
}
