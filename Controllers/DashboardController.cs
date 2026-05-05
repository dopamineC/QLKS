/**
 * Module: DashboardController
 * Chức năng: Xử lý số liệu thống kê (doanh thu, đơn đặt phòng, tỷ lệ lấp đầy) và Todo list (Admin)
 * Người phụ trách: Đức
 */
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLKS.Data;
using QLKS.Models;
using System.Security.Claims;

namespace QLKS.Controllers
{
    // Controller Bảng điều khiển (Dashboard) và Danh sách công việc (Todo)
    // Đức phụ trách: Tính toán doanh thu, số đêm bán được, tỷ lệ lấp đầy, KPI, TODO
    [Authorize(Roles = "Admin")]
    public class DashboardController : Controller
    {
        private readonly ApplicationDbContext _context;

        public DashboardController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Hàm chính khởi tạo mọi dữ liệu để vẽ lên giao diện Admin
        public async Task<IActionResult> Index()
        {
            var today = DateTime.Today;
            var from = new DateTime(today.Year, today.Month, 1).AddMonths(-11); // Lấy 12 tháng gần nhất
            var to = from.AddMonths(12);

            // Bước 1: Tính Doanh thu 12 tháng (Dùng cho biểu đồ dạng Cột/Đường)
            // Lọc các hoá đơn Đã Thanh Toán và nhóm (Group By) theo Tháng
            var doanhThu = await _context.HoaDons.AsNoTracking()
                .Where(h => h.TrangThai == TrangThaiHoaDon.DaThanhToan
                            && h.NgayThanhToan != null
                            && h.NgayThanhToan.Value >= from
                            && h.NgayThanhToan.Value < to)
                .GroupBy(h => new { h.NgayThanhToan!.Value.Year, h.NgayThanhToan!.Value.Month })
                .Select(g => new
                {
                    g.Key.Year,
                    g.Key.Month,
                    Tong = g.Sum(x => x.SoTien)
                })
                .ToListAsync();

            // Bước 2: Tính KPI (So sánh tháng này và tháng trước)
            var doanhThuThangNay = await _context.HoaDons.AsNoTracking()
                .Where(h => h.TrangThai == TrangThaiHoaDon.DaThanhToan
                            && h.NgayThanhToan != null
                            && h.NgayThanhToan.Value.Year == today.Year
                            && h.NgayThanhToan.Value.Month == today.Month)
                .SumAsync(h => (decimal?)h.SoTien) ?? 0;

            var thangTruoc = today.AddMonths(-1);
            var doanhThuThangTruoc = await _context.HoaDons.AsNoTracking()
                .Where(h => h.TrangThai == TrangThaiHoaDon.DaThanhToan
                            && h.NgayThanhToan != null
                            && h.NgayThanhToan.Value.Year == thangTruoc.Year
                            && h.NgayThanhToan.Value.Month == thangTruoc.Month)
                .SumAsync(h => (decimal?)h.SoTien) ?? 0;

            // Bước 3: Tính số lượng Đơn Đặt Phòng
            var soDatPhongHomNay = await _context.DatPhongs.AsNoTracking()
                .Where(dp => dp.NgayNhanPhong.Date == today)
                .CountAsync();

            var soDatPhongThangNay = await _context.DatPhongs.AsNoTracking()
                .Where(dp => dp.NgayNhanPhong.Year == today.Year
                          && dp.NgayNhanPhong.Month == today.Month)
                .CountAsync();

            var soPhong = await _context.Phongs.AsNoTracking().CountAsync();
            var soLoai = await _context.LoaiPhongs.AsNoTracking().CountAsync();

            // Bước 4: Thống kê trạng thái Phòng thực tế (Vẽ biểu đồ hình tròn)
            var thongKePhong = await _context.Phongs.AsNoTracking()
                .GroupBy(p => p.TrangThai)
                .Select(g => new { TrangThai = g.Key, SoLuong = g.Count() })
                .ToListAsync();

            int phongTrong = thongKePhong.FirstOrDefault(x => x.TrangThai == TrangThaiPhong.Trong)?.SoLuong ?? 0;
            int phongDangSuDung = thongKePhong.FirstOrDefault(x => x.TrangThai == TrangThaiPhong.DangSuDung)?.SoLuong ?? 0;
            int phongBaoTri = thongKePhong.FirstOrDefault(x => x.TrangThai == TrangThaiPhong.BaoTri)?.SoLuong ?? 0;

            // Bước 5: Tìm Top 5 phòng được đặt nhiều nhất
            var topPhong = await _context.DatPhongs.AsNoTracking()
                .Include(x => x.Phong)
                .Where(x => x.TrangThai != TrangThaiDatPhong.DaHuy)
                .GroupBy(x => x.Phong!.SoPhong)
                .Select(g => new { SoPhong = g.Key, SoLan = g.Count() })
                .OrderByDescending(x => x.SoLan)
                .Take(5)
                .ToListAsync();

            // Bước 6: Thống kê Số đêm đã bán theo từng Loại phòng
            // Giúp chủ khách sạn biết phòng nào "ăn khách" nhất
            var soDemTheoLoai = await _context.DatPhongs.AsNoTracking()
                .Include(x => x.Phong)!.ThenInclude(p => p!.LoaiPhong)
                .Where(x => x.TrangThai != TrangThaiDatPhong.DaHuy
                            && x.NgayNhanPhong >= from
                            && x.NgayNhanPhong < to)
                .GroupBy(x => x.Phong!.LoaiPhong!.TenLoai)
                .Select(g => new { Loai = g.Key, SoDem = g.Sum(t => t.SoDem) })
                .OrderByDescending(x => x.SoDem)
                .ToListAsync();

            // Bước 7: Hiển thị nhanh 8 đơn đặt phòng mới nhất
            var recent = await _context.DatPhongs.AsNoTracking()
                .Include(x => x.Phong)
                .ThenInclude(p => p!.LoaiPhong)
                .OrderByDescending(x => x.Id)
                .Take(8)
                .Select(x => new
                {
                    x.Id,
                    SoPhong = x.Phong!.SoPhong,
                    Loai = x.Phong!.LoaiPhong!.TenLoai,
                    NgayNhan = x.NgayNhanPhong,
                    NgayTra = x.NgayTraPhong,
                    x.TrangThai
                })
                .ToListAsync();

            // Gắn toàn bộ dữ liệu vào ViewBag để View (HTML) có thể render
            ViewBag.DoanhThu = doanhThu;
            ViewBag.KPI_DoanhThuThang = doanhThuThangNay;
            ViewBag.KPI_DoanhThuThangTruoc = doanhThuThangTruoc;

            ViewBag.KPI_SoDatPhongThang = soDatPhongThangNay;
            ViewBag.KPI_SoDatPhongHomNay = soDatPhongHomNay;

            ViewBag.KPI_SoPhong = soPhong;
            ViewBag.KPI_SoLoai = soLoai;

            ViewBag.KPI_PhongTrong = phongTrong;
            ViewBag.KPI_PhongDangSuDung = phongDangSuDung;
            ViewBag.KPI_PhongBaoTri = phongBaoTri;

            ViewBag.TopPhong = topPhong;
            ViewBag.SoDemTheoLoai = soDemTheoLoai;
            ViewBag.Recent = recent;
            ViewBag.From = from;

            // ===== TODO LIST: Xử lý hiển thị danh sách việc cần làm của riêng Admin này =====
            var meIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            int meId = int.TryParse(meIdStr, out var tmp) ? tmp : 0;

            var todos = await _context.TodoItems
                .AsNoTracking()
                .Where(t => t.NguoiDungId == meId)
                .OrderBy(t => t.DaXong)          // Ưu tiên đưa việc chưa xong lên trên
                .ThenBy(t => t.Han == null)      // Việc có hạn chót (Deadline) ưu tiên lên trước
                .ThenBy(t => t.Han)
                .ThenByDescending(t => t.NgayTao)
                .ToListAsync();

            ViewBag.Todos = todos;

            return View();
        }

        // ===== Chức năng Thêm Todo =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AddTodo(string tieuDe, DateTime? han)
        {
            if (string.IsNullOrWhiteSpace(tieuDe))
            {
                TempData["todo_msg"] = "Bạn chưa nhập nội dung todo.";
                return RedirectToAction(nameof(Index));
            }

            var meIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(meIdStr, out var meId) || meId <= 0)
                return RedirectToAction(nameof(Index));

            var item = new TodoItem
            {
                NguoiDungId = meId,
                TieuDe = tieuDe.Trim(),
                Han = han?.Date,
                DaXong = false,
                NgayTao = DateTime.UtcNow
            };

            _context.TodoItems.Add(item);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // ===== Đánh dấu Todo là Xong/Chưa xong =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleTodo(int id)
        {
            var meIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(meIdStr, out var meId) || meId <= 0)
                return RedirectToAction(nameof(Index));

            // Phải Where theo NguoiDungId để tránh Admin này sửa Todo của Admin khác
            var item = await _context.TodoItems
                .FirstOrDefaultAsync(x => x.Id == id && x.NguoiDungId == meId);

            if (item == null) return RedirectToAction(nameof(Index));

            item.DaXong = !item.DaXong;
            item.NgayCapNhat = DateTime.UtcNow;

            await _context.SaveChangesAsync();
            return RedirectToAction(nameof(Index));
        }

        // ===== Xóa Todo =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteTodo(int id)
        {
            var meIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (!int.TryParse(meIdStr, out var meId) || meId <= 0)
                return RedirectToAction(nameof(Index));

            var item = await _context.TodoItems
                .FirstOrDefaultAsync(x => x.Id == id && x.NguoiDungId == meId);

            if (item == null) return RedirectToAction(nameof(Index));

            _context.TodoItems.Remove(item);
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }
    }
}
