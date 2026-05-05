using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using QLKS.Models;

namespace QLKS.Data
{
    public static class DbInitializer
    {
        public static void Initialize(ApplicationDbContext context)
        {
            if (context.LoaiPhongs.Any()) return; // đã seed

            var hasher = new PasswordHasher<NguoiDung>();

            // Admin mặc định
            var admin = new NguoiDung
            {
                TenDangNhap = "admin",
                HoTen = "Administrator",
                VaiTro = VaiTro.Admin,
                TrangThai = true,
                NgayTao = DateTime.UtcNow
            };
            admin.MatKhauHash = hasher.HashPassword(admin, "Admin@123");
            context.NguoiDungs.Add(admin);

            // 40 khách hàng
            var customers = new List<NguoiDung>();
            for (int i = 1; i <= 40; i++)
            {
                var u = new NguoiDung
                {
                    TenDangNhap = $"khach{i:00}",
                    HoTen = $"Khách {i:00}",
                    VaiTro = VaiTro.Customer,
                    TrangThai = true,
                    SoDienThoai = $"09{i:00000000}",
                    Email = $"khach{i:00}@mail.com",
                    CCCD = $"0{i:000000000000}",
                    NgayTao = DateTime.UtcNow
                };
                u.MatKhauHash = hasher.HashPassword(u, "123456");
                customers.Add(u);
            }
            context.NguoiDungs.AddRange(customers);

            // 3 loại phòng
            var loaiPhongs = new List<LoaiPhong>
            {
                new LoaiPhong { TenLoai = "Single", GiaMoiDem = 350000, SoNguoi = 1, MoTa = "Phòng đơn cơ bản" },
                new LoaiPhong { TenLoai = "Double", GiaMoiDem = 550000, SoNguoi = 2, MoTa = "Phòng đôi tiêu chuẩn" },
                new LoaiPhong { TenLoai = "Suite",  GiaMoiDem = 950000, SoNguoi = 3, MoTa = "Phòng suite cao cấp" },
            };
            context.LoaiPhongs.AddRange(loaiPhongs);
            context.SaveChanges();

            // 20 phòng
            var rooms = new List<Phong>();
            int idSingle = context.LoaiPhongs.First(x => x.TenLoai == "Single").Id;
            int idDouble = context.LoaiPhongs.First(x => x.TenLoai == "Double").Id;
            int idSuite = context.LoaiPhongs.First(x => x.TenLoai == "Suite").Id;

            for (int i = 1; i <= 20; i++)
            {
                int tang = (i <= 10) ? 1 : 2;
                int typeId = (i <= 8) ? idSingle : (i <= 16 ? idDouble : idSuite);
                rooms.Add(new Phong
                {
                    SoPhong = $"{tang}{i:00}",
                    Tang = tang,
                    LoaiPhongId = typeId,
                    TrangThai = TrangThaiPhong.Trong
                });
            }
            context.Phongs.AddRange(rooms);
            context.SaveChanges();

            // 80 đặt phòng: 50 hoàn thành, 20 đã đặt, 10 đã hủy (tránh trùng lịch theo phòng)
            var rand = new Random(1);
            var allRooms = context.Phongs.AsNoTracking().ToList();
            var allCustomers = context.NguoiDungs.AsNoTracking().Where(x => x.VaiTro == VaiTro.Customer).ToList();
            var mapBookingsByRoom = new Dictionary<int, List<(DateTime from, DateTime to)>>();

            bool IsOverlap(int roomId, DateTime from, DateTime to)
            {
                if (!mapBookingsByRoom.ContainsKey(roomId)) return false;
                return mapBookingsByRoom[roomId].Any(b => from < b.to && to > b.from);
            }

            void AddBookingWindow(int roomId, DateTime from, DateTime to)
            {
                if (!mapBookingsByRoom.ContainsKey(roomId))
                    mapBookingsByRoom[roomId] = new List<(DateTime, DateTime)>();
                mapBookingsByRoom[roomId].Add((from, to));
            }

            DateTime today = DateTime.Today;
            var bookings = new List<DatPhong>();

            // helper tạo booking
            DatPhong CreateBooking(TrangThaiDatPhong status, DateTime baseStartMin, DateTime baseStartMax)
            {
                for (int attempt = 0; attempt < 500; attempt++)
                {
                    var room = allRooms[rand.Next(allRooms.Count)];
                    var cust = allCustomers[rand.Next(allCustomers.Count)];

                    int startOffset = rand.Next((baseStartMax - baseStartMin).Days + 1);
                    var checkIn = baseStartMin.AddDays(startOffset);
                    int nights = rand.Next(1, 6);
                    var checkOut = checkIn.AddDays(nights);

                    // tránh trùng lịch cho trạng thái vẫn chiếm phòng
                    if (status == TrangThaiDatPhong.DaDat || status == TrangThaiDatPhong.DaNhanPhong || status == TrangThaiDatPhong.DaTraPhong)
                    {
                        if (IsOverlap(room.Id, checkIn, checkOut)) continue;
                        AddBookingWindow(room.Id, checkIn, checkOut);
                    }

                    // tính tiền
                    var loai = context.LoaiPhongs.AsNoTracking().First(lp => lp.Id == room.LoaiPhongId);
                    decimal total = loai.GiaMoiDem * nights;

                    return new DatPhong
                    {
                        KhachHangId = cust.Id,
                        PhongId = room.Id,
                        NgayNhanPhong = checkIn,
                        NgayTraPhong = checkOut,
                        SoDem = nights,
                        TongTien = total,
                        TrangThai = status,
                        NgayDat = checkIn.AddDays(-rand.Next(1, 10))
                    };
                }

                // fallback (rất hiếm)
                var fallbackRoom = allRooms[0];
                var fallbackCust = allCustomers[0];
                var ci = baseStartMin;
                var co = ci.AddDays(1);
                var lp0 = context.LoaiPhongs.AsNoTracking().First(lp => lp.Id == fallbackRoom.LoaiPhongId);
                return new DatPhong
                {
                    KhachHangId = fallbackCust.Id,
                    PhongId = fallbackRoom.Id,
                    NgayNhanPhong = ci,
                    NgayTraPhong = co,
                    SoDem = 1,
                    TongTien = lp0.GiaMoiDem,
                    TrangThai = status
                };
            }

            // 50 hoàn thành: nằm trong quá khứ
            for (int i = 0; i < 50; i++)
            {
                var dp = CreateBooking(TrangThaiDatPhong.DaTraPhong, today.AddDays(-120), today.AddDays(-5));
                bookings.Add(dp);
            }

            // 20 đã đặt: tương lai
            for (int i = 0; i < 20; i++)
            {
                var dp = CreateBooking(TrangThaiDatPhong.DaDat, today.AddDays(1), today.AddDays(60));
                bookings.Add(dp);
            }

            // 10 đã hủy: có thể quá khứ hoặc tương lai (không cần giữ lịch)
            for (int i = 0; i < 10; i++)
            {
                var dp = CreateBooking(TrangThaiDatPhong.DaHuy, today.AddDays(-30), today.AddDays(30));
                bookings.Add(dp);
            }

            context.DatPhongs.AddRange(bookings);
            context.SaveChanges();

            // hóa đơn cho các booking đã trả (để thống kê doanh thu)
            var paidBookings = context.DatPhongs.Where(x => x.TrangThai == TrangThaiDatPhong.DaTraPhong).ToList();
            var invoices = paidBookings.Select(dp => new HoaDon
            {
                DatPhongId = dp.Id,
                SoTien = dp.TongTien,
                TrangThai = TrangThaiHoaDon.DaThanhToan,
                PhuongThuc = (rand.Next(2) == 0) ? PhuongThucThanhToan.TienMat : PhuongThucThanhToan.ChuyenKhoan,
                NgayThanhToan = dp.NgayTraPhong
            }).ToList();

            context.HoaDons.AddRange(invoices);
            context.SaveChanges();
        }
    }
}
