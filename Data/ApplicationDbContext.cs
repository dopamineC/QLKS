using Microsoft.EntityFrameworkCore;
using QLKS.Models;

namespace QLKS.Data
{
    public class ApplicationDbContext : DbContext
    {
        public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options) { }

        public DbSet<NguoiDung> NguoiDungs => Set<NguoiDung>();
        public DbSet<LoaiPhong> LoaiPhongs => Set<LoaiPhong>();
        public DbSet<Phong> Phongs => Set<Phong>();
        public DbSet<DatPhong> DatPhongs => Set<DatPhong>();
        public DbSet<HoaDon> HoaDons => Set<HoaDon>();

        public DbSet<TodoItem> TodoItems { get; set; }
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            modelBuilder.Entity<NguoiDung>()
                .HasIndex(x => x.TenDangNhap).IsUnique();

            modelBuilder.Entity<Phong>()
                .HasIndex(x => x.SoPhong).IsUnique();

            modelBuilder.Entity<LoaiPhong>()
                .Property(x => x.GiaMoiDem).HasPrecision(18, 2);

            modelBuilder.Entity<DatPhong>()
                .Property(x => x.TongTien).HasPrecision(18, 2);

            modelBuilder.Entity<HoaDon>()
                .Property(x => x.SoTien).HasPrecision(18, 2);

            modelBuilder.Entity<DatPhong>()
                .HasOne(dp => dp.HoaDon)
                .WithOne(hd => hd.DatPhong!)
                .HasForeignKey<HoaDon>(hd => hd.DatPhongId)
                .OnDelete(DeleteBehavior.Cascade);
        }
    }
}
