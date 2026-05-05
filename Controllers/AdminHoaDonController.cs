/**
 * Module: AdminHoaDonController
 * Chức năng: Quản lý danh sách hoá đơn và cập nhật trạng thái thanh toán (Admin)
 * Người phụ trách: Đức
 */
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLKS.Data;
using QLKS.Models;

namespace QLKS.Controllers
{
    // Controller quản lý danh sách hoá đơn thanh toán của khách sạn
    [Authorize(Roles = "Admin")]
    public class AdminHoaDonController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminHoaDonController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Danh sách + lọc
        public async Task<IActionResult> Index(
            string? q,
            TrangThaiHoaDon? trangThai,
            PhuongThucThanhToan? phuongThuc,
            DateTime? tuNgay,
            DateTime? denNgay)
        {
            var query = _context.HoaDons
                .AsNoTracking()
                .Include(hd => hd.DatPhong)!.ThenInclude(dp => dp.KhachHang)
                .Include(hd => hd.DatPhong)!.ThenInclude(dp => dp.Phong)!.ThenInclude(p => p.LoaiPhong)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();

                // nếu nhập số -> ưu tiên tìm theo Id hóa đơn / Id đặt phòng
                if (int.TryParse(q, out var num))
                {
                    query = query.Where(hd => hd.Id == num || hd.DatPhongId == num);
                }
                else
                {
                    query = query.Where(hd =>
                        hd.DatPhong!.KhachHang!.HoTen.Contains(q) ||
                        hd.DatPhong.KhachHang.TenDangNhap.Contains(q) ||
                        (hd.DatPhong.KhachHang.SoDienThoai != null && hd.DatPhong.KhachHang.SoDienThoai.Contains(q)) ||
                        hd.DatPhong.Phong!.SoPhong.Contains(q));
                }
            }

            if (trangThai.HasValue)
                query = query.Where(hd => hd.TrangThai == trangThai.Value);

            if (phuongThuc.HasValue)
                query = query.Where(hd => hd.PhuongThuc == phuongThuc.Value);

            // lọc theo ngày đặt (NgayDat trong DatPhong)
            if (tuNgay.HasValue)
                query = query.Where(hd => hd.DatPhong!.NgayDat.Date >= tuNgay.Value.Date);

            if (denNgay.HasValue)
                query = query.Where(hd => hd.DatPhong!.NgayDat.Date <= denNgay.Value.Date);

            ViewBag.Q = q;
            ViewBag.TrangThai = trangThai;
            ViewBag.PhuongThuc = phuongThuc;
            ViewBag.TuNgay = tuNgay?.ToString("yyyy-MM-dd");
            ViewBag.DenNgay = denNgay?.ToString("yyyy-MM-dd");

            var list = await query
                .OrderBy(hd => hd.Id)
                .ToListAsync();

            return View(list);
        }

        // Chi tiết
        public async Task<IActionResult> Details(int id)
        {
            var hd = await _context.HoaDons
                .AsNoTracking()
                .Include(x => x.DatPhong)!.ThenInclude(dp => dp.KhachHang)
                .Include(x => x.DatPhong)!.ThenInclude(dp => dp.Phong)!.ThenInclude(p => p.LoaiPhong)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (hd == null) return NotFound();
            return View(hd);
        }

        // Xử lý cập nhật trạng thái: Đánh dấu hoá đơn đã thanh toán thành công
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkPaid(int id, PhuongThucThanhToan method)
        {
            var hd = await _context.HoaDons.FirstOrDefaultAsync(x => x.Id == id);
            if (hd == null) return NotFound();

            if (hd.TrangThai == TrangThaiHoaDon.DaHuy)
            {
                TempData["msg"] = "Hóa đơn đã huỷ, không thể thanh toán.";
                return RedirectToAction(nameof(Index));
            }

            hd.TrangThai = TrangThaiHoaDon.DaThanhToan;
            hd.PhuongThuc = method;
            hd.NgayThanhToan = DateTime.Now;

            await _context.SaveChangesAsync();
            TempData["msg"] = "Đã cập nhật: Đã thanh toán.";
            return RedirectToAction(nameof(Index));
        }

        // Chuyển về chưa thanh toán
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> MarkUnpaid(int id)
        {
            var hd = await _context.HoaDons.FirstOrDefaultAsync(x => x.Id == id);
            if (hd == null) return NotFound();

            if (hd.TrangThai == TrangThaiHoaDon.DaHuy)
            {
                TempData["msg"] = "Hóa đơn đã huỷ, không thể chuyển trạng thái.";
                return RedirectToAction(nameof(Index));
            }

            hd.TrangThai = TrangThaiHoaDon.ChuaThanhToan;
            hd.NgayThanhToan = null;

            await _context.SaveChangesAsync();
            TempData["msg"] = "Đã cập nhật: Chưa thanh toán.";
            return RedirectToAction(nameof(Index));
        }

        // Huỷ hóa đơn
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            var hd = await _context.HoaDons.FirstOrDefaultAsync(x => x.Id == id);
            if (hd == null) return NotFound();

            hd.TrangThai = TrangThaiHoaDon.DaHuy;
            hd.NgayThanhToan = null;

            await _context.SaveChangesAsync();
            TempData["msg"] = "Đã huỷ hóa đơn.";
            return RedirectToAction(nameof(Index));
        }
    }
}
