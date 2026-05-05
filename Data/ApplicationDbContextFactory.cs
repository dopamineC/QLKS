using System;
using System.IO;
using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;

namespace QLKS.Data
{
    public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
    {
        public ApplicationDbContext CreateDbContext(string[] args)
        {
            // EF Tools đôi khi chạy ở thư mục lạ -> đi ngược lên tới khi thấy appsettings.json
            var basePath = Directory.GetCurrentDirectory();
            while (!File.Exists(Path.Combine(basePath, "appsettings.json")))
            {
                var parent = Directory.GetParent(basePath);
                if (parent == null) break;
                basePath = parent.FullName;
            }

            var config = new ConfigurationBuilder()
                .SetBasePath(basePath)
                .AddJsonFile("appsettings.json", optional: false, reloadOnChange: false)
                .AddJsonFile("appsettings.Development.json", optional: true, reloadOnChange: false)
                .AddEnvironmentVariables()
                .Build();

            var conn = config.GetConnectionString("DefaultConnection");
            if (string.IsNullOrWhiteSpace(conn))
                throw new InvalidOperationException("Missing ConnectionStrings:DefaultConnection in appsettings.json");

            var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
            optionsBuilder.UseSqlServer(conn);

            return new ApplicationDbContext(optionsBuilder.Options);
        }
    }
}