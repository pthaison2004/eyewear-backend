using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using VisionCare.DataAccessLayer.Models;

namespace VisionCare.DataAccessLayer;

public class DesignTimeDbContextFactory : IDesignTimeDbContextFactory<VisionCareContext>
{
    public VisionCareContext CreateDbContext(string[] args)
    {
        var optionsBuilder = new DbContextOptionsBuilder<VisionCareContext>();

        // Use a dummy connection string — migrations only need the model, not the DB
        optionsBuilder.UseSqlServer("Server=.;Database=VisionCare;Integrated Security=True;TrustServerCertificate=True;Encrypt=False;");

        return new VisionCareContext(optionsBuilder.Options);
    }
}