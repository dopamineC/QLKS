/**
 * Module: AdminBookingController
 * Chức năng: Quản lý danh sách đặt phòng, tạo đơn đặt phòng mới và check-in/check-out (Admin)
 * Người phụ trách: Sơn
 */
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QLKS.Data;
using QLKS.Models;
using QLKS.ViewModels;

namespace QLKS.Controllers
{
    // Controller quản lý toàn bộ quá trình đặt phòng dành cho Admin
    [Authorize(Roles = "Admin")]
    public class AdminBookingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminBookingController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Hiển thị danh sách tất cả các đơn đặt phòng kèm bộ lọc tìm kiếm
        public async Task<IActionResult> Index(
            string? q,
            TrangThaiDatPhong? trangThai,
            DateTime? tuNgay,
            DateTime? denNgay)
        {
            var query = _context.DatPhongs
                .AsNoTracking()
                .Include(dp => dp.KhachHang)
                .Include(dp => dp.Phong)!.ThenInclude(p => p.LoaiPhong)
                .Include(dp => dp.HoaDon)
                .AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(dp =>
                    dp.KhachHang!.HoTen.Contains(q) ||
                    dp.KhachHang.TenDangNhap.Contains(q) ||
                    (dp.KhachHang.SoDienThoai != null && dp.KhachHang.SoDienThoai.Contains(q)) ||
                    dp.Phong!.SoPhong.Contains(q));
            }

            if (trangThai.HasValue)
                query = query.Where(dp => dp.TrangThai == trangThai.Value);

            if (tuNgay.HasValue)
                query = query.Where(dp => dp.NgayNhanPhong.Date >= tuNgay.Value.Date);

            if (denNgay.HasValue)
                query = query.Where(dp => dp.NgayNhanPhong.Date <= denNgay.Value.Date);

            ViewBag.Q = q;
            ViewBag.TrangThai = trangThai;
            ViewBag.TuNgay = tuNgay?.ToString("yyyy-MM-dd");
            ViewBag.DenNgay = denNgay?.ToString("yyyy-MM-dd");

            var list = await query.OrderByDescending(dp => dp.NgayDat).ToListAsync();
            return View(list);
        }

        // ===== Admin tạo đặt phòng cho khách =====
        [HttpGet]
        public async Task<IActionResult> Create()
        {
            var vm = new AdminCreateBookingVM
            {
                NgayNhan = DateTime.Today.AddDays(1),
                NgayTra = DateTime.Today.AddDays(2)
            };

            await LoadCreateDropdowns();
            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdminCreateBookingVM vm)
        {
            if (vm.NgayNhan.Date < DateTime.Today)
                ModelState.AddModelError(nameof(vm.NgayNhan), "Ngày nhận phải >= hôm nay.");

            if (vm.NgayTra.Date <= vm.NgayNhan.Date)
                ModelState.AddModelError(nameof(vm.NgayTra), "Ngày trả phải > ngày nhận.");

            var room = await _context.Phongs.Include(p => p.LoaiPhong).FirstOrDefaultAsync(p => p.Id == vm.PhongId);
            if (room == null) ModelState.AddModelError(nameof(vm.PhongId), "Vui lòng chọn phòng.");
            else if (room.TrangThai == TrangThaiPhong.BaoTri)
                ModelState.AddModelError(nameof(vm.PhongId), "Phòng đang bảo trì.");

            var user = await _context.NguoiDungs.FirstOrDefaultAsync(u => u.Id == vm.KhachHangId && u.VaiTro == VaiTro.Customer);
            if (user == null) ModelState.AddModelError(nameof(vm.KhachHangId), "Vui lòng chọn khách hàng.");

            if (room != null)
            {
                bool hasOverlap = await _context.DatPhongs.AnyAsync(dp =>
                    dp.PhongId == vm.PhongId
                    && (dp.TrangThai == TrangThaiDatPhong.DaDat || dp.TrangThai == TrangThaiDatPhong.DaNhanPhong)
                    && vm.NgayNhan < dp.NgayTraPhong
                    && vm.NgayTra > dp.NgayNhanPhong
                );
                if (hasOverlap)
                    ModelState.AddModelError("", "Phòng đã có đơn chồng lấn trong khoảng ngày này.");
            }

            if (!ModelState.IsValid)
            {
                await LoadCreateDropdowns();
                return View(vm);
            }

            int soDem = (vm.NgayTra.Date - vm.NgayNhan.Date).Days;
            decimal tongTien = soDem * room!.LoaiPhong!.GiaMoiDem;

            var booking = new DatPhong
            {
                KhachHangId = vm.KhachHangId,
                PhongId = vm.PhongId,
                NgayNhanPhong = vm.NgayNhan.Date,
                NgayTraPhong = vm.NgayTra.Date,
                SoDem = soDem,
                TongTien = tongTien,
                TrangThai = TrangThaiDatPhong.DaDat,
                NgayDat = DateTime.UtcNow
            };

            _context.DatPhongs.Add(booking);
            await _context.SaveChangesAsync();

            _context.HoaDons.Add(new HoaDon
            {
                DatPhongId = booking.Id,
                SoTien = booking.TongTien,
                TrangThai = TrangThaiHoaDon.ChuaThanhToan,
                PhuongThuc = vm.PhuongThuc
            });
            await _context.SaveChangesAsync();

            TempData["success"] = $"Đã tạo đặt phòng #{booking.Id} cho khách hàng.";

            return RedirectToAction(nameof(Index));
        }

        private async Task LoadCreateDropdowns()
        {
            ViewBag.Customers = new SelectList(
                await _context.NguoiDungs.AsNoTracking()
                    .Where(x => x.VaiTro == VaiTro.Customer && x.TrangThai)
                    .OrderBy(x => x.HoTen)
                    .Select(x => new { x.Id, Text = x.HoTen + " (" + x.TenDangNhap + ")" })
                    .ToListAsync(),
                "Id", "Text"
            );

            ViewBag.Rooms = new SelectList(
                await _context.Phongs.AsNoTracking()
                    .Include(p => p.LoaiPhong)
                    .OrderBy(p => p.SoPhong)
                    .Select(p => new { p.Id, Text = p.SoPhong + " - " + p.LoaiPhong!.TenLoai + " (" + p.LoaiPhong.GiaMoiDem + "/đêm)" })
                    .ToListAsync(),
                "Id", "Text"
            );
        }

        // ===== Admin check-in/check-out =====
        // Xử lý Check-in: Khách hàng đến nhận phòng
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckIn(int id)
        {
            var dp = await _context.DatPhongs
                .Include(x => x.Phong)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (dp == null) return NotFound();

            if (dp.TrangThai != TrangThaiDatPhong.DaDat)
            {
                TempData["error"] = "Chỉ check-in được đơn ở trạng thái Đã đặt.";
                return RedirectToAction(nameof(Index));
            }

            // siết: không cho check-in trước ngày nhận
            if (DateTime.Today < dp.NgayNhanPhong.Date)
            {
                TempData["warning"] = "Chưa đến ngày nhận phòng, không thể check-in.";
                return RedirectToAction(nameof(Index));
            }

            if (dp.Phong == null)
            {
                TempData["error"] = "Thiếu phòng.";
                return RedirectToAction(nameof(Index));
            }
            if (dp.Phong.TrangThai == TrangThaiPhong.BaoTri)
            {
                TempData["error"] = "Phòng đang bảo trì.";
                return RedirectToAction(nameof(Index));
            }

            dp.TrangThai = TrangThaiDatPhong.DaNhanPhong;
            dp.Phong.TrangThai = TrangThaiPhong.DangSuDung;

            await _context.SaveChangesAsync();
            TempData["success"] = $"Đã check-in đặt phòng #{dp.Id}.";
            return RedirectToAction(nameof(Index));
        }

        // Xử lý Check-out: Khách hàng trả phòng và thanh toán
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CheckOut(int id, PhuongThucThanhToan phuongThuc = PhuongThucThanhToan.TienMat)
        {
            var dp = await _context.DatPhongs
                .Include(x => x.Phong)
                .Include(x => x.HoaDon)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (dp == null) return NotFound();

            if (dp.TrangThai != TrangThaiDatPhong.DaNhanPhong)
            {
                TempData["error"] = "Chỉ check-out được khi đang ở trạng thái Đã nhận phòng.";
                return RedirectToAction(nameof(Index));
            }

            // siết: không cho check-out trước ngày nhận
            if (DateTime.Today < dp.NgayNhanPhong.Date)
            {
                TempData["warning"] = "Không thể check-out trước ngày nhận phòng.";
                return RedirectToAction(nameof(Index));
            }

            if (dp.Phong == null)
            {
                TempData["error"] = "Thiếu phòng.";
                return RedirectToAction(nameof(Index));
            }

            dp.TrangThai = TrangThaiDatPhong.DaTraPhong;
            dp.Phong.TrangThai = TrangThaiPhong.Trong;

            if (dp.HoaDon != null)
            {
                dp.HoaDon.PhuongThuc = phuongThuc;
                dp.HoaDon.TrangThai = TrangThaiHoaDon.DaThanhToan;
                dp.HoaDon.NgayThanhToan = DateTime.Today;
                dp.HoaDon.SoTien = dp.TongTien;
            }

            await _context.SaveChangesAsync();
            TempData["success"] = $"Đã check-out đặt phòng #{dp.Id} (đã thanh toán).";
            return RedirectToAction(nameof(Index));
        }

        // Xem chi tiết một đơn đặt phòng
        public async Task<IActionResult> Details(int id)
        {
            var dp = await _context.DatPhongs
                .AsNoTracking()
                .Include(x => x.KhachHang)
                .Include(x => x.Phong)!.ThenInclude(p => p.LoaiPhong)
                .Include(x => x.HoaDon)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (dp == null) return NotFound();
            return View(dp);
        }

        public async Task<IActionResult> Edit(int id)
        {
            var dp = await _context.DatPhongs
                .Include(x => x.Phong).ThenInclude(p => p!.LoaiPhong)
                .Include(x => x.KhachHang)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (dp == null) return NotFound();

            var vm = new AdminEditBookingVM
            {
                Id = dp.Id,
                PhongId = dp.PhongId,
                NgayNhanPhong = dp.NgayNhanPhong.Date,
                NgayTraPhong = dp.NgayTraPhong.Date,
                TenKhach = dp.KhachHang?.HoTen,
                SoPhong = dp.Phong?.SoPhong.ToString(),
                TenLoaiPhong = dp.Phong?.LoaiPhong?.TenLoai,
                GiaMoiDem = dp.Phong?.LoaiPhong?.GiaMoiDem ?? 0
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AdminEditBookingVM vm)
        {
            if (vm.NgayTraPhong.Date <= vm.NgayNhanPhong.Date)
                ModelState.AddModelError("", "Ngày trả phải sau ngày nhận.");

            var dp = await _context.DatPhongs
                .Include(x => x.Phong).ThenInclude(p => p!.LoaiPhong)
                .FirstOrDefaultAsync(x => x.Id == vm.Id);

            if (dp == null) return NotFound();

            // Load info hiển thị lại nếu lỗi
            vm.GiaMoiDem = dp.Phong?.LoaiPhong?.GiaMoiDem ?? 0;
            vm.SoPhong = dp.Phong?.SoPhong.ToString();
            vm.TenLoaiPhong = dp.Phong?.LoaiPhong?.TenLoai;

            if (!ModelState.IsValid) return View(vm);

            // ✅ Check trùng lịch: bỏ qua chính booking hiện tại (x.Id != vm.Id)
            bool biTrung = await _context.DatPhongs.AnyAsync(x =>
                x.Id != vm.Id &&
                x.PhongId == vm.PhongId &&
                (x.TrangThai == TrangThaiDatPhong.DaDat || x.TrangThai == TrangThaiDatPhong.DaNhanPhong) &&
                x.NgayNhanPhong < vm.NgayTraPhong &&
                vm.NgayNhanPhong < x.NgayTraPhong
            );

            if (biTrung)
            {
                ModelState.AddModelError("", "Phòng này đã có đặt trong khoảng thời gian bạn chọn.");
                return View(vm);
            }

            // ✅ Update
            dp.NgayNhanPhong = vm.NgayNhanPhong.Date;
            dp.NgayTraPhong = vm.NgayTraPhong.Date;

            dp.SoDem = (int)(dp.NgayTraPhong.Date - dp.NgayNhanPhong.Date).TotalDays;
            var gia = dp.Phong?.LoaiPhong?.GiaMoiDem ?? 0;
            dp.TongTien = gia * dp.SoDem;

            await _context.SaveChangesAsync();

            TempData["success"] = "Cập nhật đặt phòng thành công.";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Delete(int id)
        {
            var dp = await _context.DatPhongs
                .Include(x => x.Phong).ThenInclude(p => p!.LoaiPhong)
                .Include(x => x.KhachHang)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (dp == null) return NotFound();

            return View(dp);
        }


        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var dp = await _context.DatPhongs.FindAsync(id);
            if (dp == null) return NotFound();

            _context.DatPhongs.Remove(dp);
            await _context.SaveChangesAsync();

            TempData["success"] = "Đã xóa đặt phòng.";
            return RedirectToAction(nameof(Index));
        }

    }
}
