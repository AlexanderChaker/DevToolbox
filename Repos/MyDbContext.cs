using Microsoft.EntityFrameworkCore;

namespace DeployGitBranch.Repos;

public class MyDbContext : DbContext
{
    public MyDbContext(DbContextOptions<MyDbContext> options) : base(options)
    {

    }
}
