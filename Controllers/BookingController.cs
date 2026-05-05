/**
 * Module: BookingController
 * Chức năng: Xử lý luồng đặt phòng dành cho khách hàng (Tìm phòng, đặt phòng, lịch sử)
 * Người phụ trách: Sơn
 */
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLKS.Data;
using QLKS.Models;
using Microsoft.AspNetCore.Mvc.Rendering;
using QLKS.ViewModels;
using System.Security.Claims;

namespace QLKS.Controllers
{
    // Controller xử lý toàn bộ luồng Đặt phòng (Booking) dành cho Khách hàng
    // Sơn phụ trách: Luồng tìm kiếm phòng trống, xác nhận đặt, lịch sử đặt phòng và huỷ đơn
    [Authorize(Roles = "Customer,Admin")]
    public class BookingController : Controller
    {
        private readonly ApplicationDbContext _context;

        public BookingController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int CurrentUserId()
            => int.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

        // Hiển thị giao diện Tìm kiếm phòng trống
        // Bước 1: Lấy danh sách Loại phòng đổ vào Dropdown
        // Bước 2: Khởi tạo View Model rỗng cho form tìm kiếm
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> Search()
        {
            var loaiPhongs = await _context.LoaiPhongs.AsNoTracking().ToListAsync();
            ViewBag.LoaiPhongs = loaiPhongs;
            ViewBag.LoaiPhongItems = new SelectList(loaiPhongs, "Id", "TenLoai");

            return View(new TimPhongTrongVM());
        }

        // Xử lý logic tìm kiếm phòng trống sau khi khách hàng bấm "Tìm"
        // Sơn chú ý: Logic cốt lõi để loại trừ các phòng đã có người đặt nằm ở đây!
        [AllowAnonymous]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Search(TimPhongTrongVM vm)
        {
            // Tải lại Dropdown Loại phòng để hiển thị nếu có lỗi form
            var loaiPhongs = await _context.LoaiPhongs.AsNoTracking().ToListAsync();
            ViewBag.LoaiPhongs = loaiPhongs;
            ViewBag.LoaiPhongItems = new SelectList(loaiPhongs, "Id", "TenLoai", vm.LoaiPhongId);

            // Validate logic ngày tháng
            if (vm.NgayNhan.Date < DateTime.Today)
                ModelState.AddModelError(nameof(vm.NgayNhan), "Ngày nhận phòng phải >= hôm nay.");

            if (vm.NgayTra.Date <= vm.NgayNhan.Date)
                ModelState.AddModelError(nameof(vm.NgayTra), "Ngày trả phòng phải > ngày nhận phòng.");

            if (!ModelState.IsValid) return View(vm);

            // Bắt đầu query tìm phòng: Bỏ qua các phòng đang bảo trì
            var query = _context.Phongs
                .AsNoTracking()
                .Include(p => p.LoaiPhong)
                .Where(p => p.TrangThai != TrangThaiPhong.BaoTri);

            // Lọc theo Loại phòng và Số người
            if (vm.LoaiPhongId.HasValue)
                query = query.Where(p => p.LoaiPhongId == vm.LoaiPhongId.Value);

            if (vm.SoNguoi.HasValue)
                query = query.Where(p => p.LoaiPhong!.SoNguoi >= vm.SoNguoi.Value);

            // LOGIC TÌM PHÒNG TRỐNG (Cực kỳ quan trọng)
            // Loại bỏ những phòng đang có đơn đặt phòng thoả mãn cả 3 điều kiện:
            // 1. Cùng ID phòng
            // 2. Trạng thái Đơn là Đã Đặt hoặc Đã Nhận Phòng
            // 3. Khoảng thời gian khách muốn đặt [NgayNhan, NgayTra] có ĐÈ LÊN (overlap) [NgayNhanPhong, NgayTraPhong] của đơn hiện tại
            query = query.Where(p =>
                !_context.DatPhongs.Any(dp =>
                    dp.PhongId == p.Id
                    && (dp.TrangThai == TrangThaiDatPhong.DaDat || dp.TrangThai == TrangThaiDatPhong.DaNhanPhong)
                    && vm.NgayNhan < dp.NgayTraPhong
                    && vm.NgayTra > dp.NgayNhanPhong
                ));

            var rooms = await query.OrderBy(p => p.SoPhong).ToListAsync();

            ViewBag.VM = vm; // giữ lại ngày để truyền vào link bấm đặt
            return View("SearchResult", rooms);
        }

        // Màn hình xem lại thông tin phòng trước khi chốt đơn
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Create(int phongId, DateTime ngayNhan, DateTime ngayTra)
        {
            if (ngayNhan.Date < DateTime.Today || ngayTra.Date <= ngayNhan.Date)
            {
                TempData["error"] = "Ngày không hợp lệ.";
                return RedirectToAction(nameof(Search));
            }

            var room = await _context.Phongs.Include(p => p.LoaiPhong).FirstOrDefaultAsync(p => p.Id == phongId);
            if (room == null) return NotFound();
            if (room.TrangThai == TrangThaiPhong.BaoTri)
            {
                TempData["warning"] = "Phòng đang bảo trì.";
                return RedirectToAction(nameof(Search));
            }

            // Kiểm tra lại phòng trống lần nữa (tránh việc khách treo màn hình lâu rồi mới bấm đặt)
            bool hasOverlap = await _context.DatPhongs.AnyAsync(dp =>
                dp.PhongId == phongId
                && (dp.TrangThai == TrangThaiDatPhong.DaDat || dp.TrangThai == TrangThaiDatPhong.DaNhanPhong)
                && ngayNhan < dp.NgayTraPhong
                && ngayTra > dp.NgayNhanPhong
            );
            if (hasOverlap)
            {
                TempData["warning"] = "Phòng đã được đặt trong khoảng thời gian này.";
                return RedirectToAction(nameof(Search));
            }

            // Tính tiền
            int soDem = (ngayTra.Date - ngayNhan.Date).Days;
            decimal tongTien = soDem * room.LoaiPhong!.GiaMoiDem;

            ViewBag.Room = room;
            ViewBag.NgayNhan = ngayNhan.Date;
            ViewBag.NgayTra = ngayTra.Date;
            ViewBag.SoDem = soDem;
            ViewBag.TongTien = tongTien;

            return View();
        }

        // Xác nhận chốt đơn: Ghi vào bảng DatPhong và sinh ra một Hoá Đơn Chờ
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> CreateConfirm(int phongId, DateTime ngayNhan, DateTime ngayTra)
        {
            // Các bước kiểm tra lại (Validation) tương tự như trên
            if (ngayNhan.Date < DateTime.Today || ngayTra.Date <= ngayNhan.Date)
            {
                TempData["error"] = "Ngày không hợp lệ.";
                return RedirectToAction(nameof(Search));
            }

            var room = await _context.Phongs.Include(p => p.LoaiPhong).FirstOrDefaultAsync(p => p.Id == phongId);
            if (room == null) return NotFound();
            if (room.TrangThai == TrangThaiPhong.BaoTri)
            {
                TempData["warning"] = "Phòng đang bảo trì.";
                return RedirectToAction(nameof(Search));
            }

            bool hasOverlap = await _context.DatPhongs.AnyAsync(dp =>
                dp.PhongId == phongId
                && (dp.TrangThai == TrangThaiDatPhong.DaDat || dp.TrangThai == TrangThaiDatPhong.DaNhanPhong)
                && ngayNhan < dp.NgayTraPhong
                && ngayTra > dp.NgayNhanPhong
            );
            if (hasOverlap)
            {
                TempData["warning"] = "Phòng đã được đặt trong khoảng thời gian này.";
                return RedirectToAction(nameof(Search));
            }

            int soDem = (ngayTra.Date - ngayNhan.Date).Days;
            decimal tongTien = soDem * room.LoaiPhong!.GiaMoiDem;

            // Bước 1: Lưu đơn Đặt phòng
            var dpNew = new DatPhong
            {
                KhachHangId = CurrentUserId(),
                PhongId = phongId,
                NgayNhanPhong = ngayNhan.Date,
                NgayTraPhong = ngayTra.Date,
                SoDem = soDem,
                TongTien = tongTien,
                TrangThai = TrangThaiDatPhong.DaDat,
                NgayDat = DateTime.UtcNow
            };

            _context.DatPhongs.Add(dpNew);
            await _context.SaveChangesAsync(); // Phải SaveChanges để có dpNew.Id

            // Bước 2: Tạo hóa đơn chờ thanh toán liên kết với đơn Đặt phòng
            // (Đức sẽ là người xử lý việc thanh toán hoá đơn này)
            _context.HoaDons.Add(new HoaDon
            {
                DatPhongId = dpNew.Id,
                SoTien = dpNew.TongTien,
                TrangThai = TrangThaiHoaDon.ChuaThanhToan,
                PhuongThuc = PhuongThucThanhToan.TienMat
            });
            await _context.SaveChangesAsync();

            TempData["success"] = $"Đặt phòng thành công (mã #{dpNew.Id}).";
            return RedirectToAction(nameof(MyBookings));
        }

        // Xem lịch sử Đặt phòng của Khách hàng hiện tại
        [Authorize]
        public async Task<IActionResult> MyBookings()
        {
            int uid = CurrentUserId();

            var list = await _context.DatPhongs
                .AsNoTracking()
                .Include(dp => dp.Phong)!.ThenInclude(p => p.LoaiPhong)
                .Where(dp => dp.KhachHangId == uid)
                .OrderByDescending(dp => dp.NgayDat)
                .ToListAsync();

            return View(list);
        }

        // Xem chi tiết Đơn đặt phòng
        [Authorize]
        [HttpGet]
        public async Task<IActionResult> Details(int id)
        {
            int uid = CurrentUserId();

            var query = _context.DatPhongs
                .AsNoTracking()
                .Include(dp => dp.KhachHang)
                .Include(dp => dp.Phong)!.ThenInclude(p => p.LoaiPhong)
                .Include(dp => dp.HoaDon);

            DatPhong? dp;

            // Admin được xem mọi đơn, Khách chỉ được xem đơn của mình
            if (User.IsInRole("Admin"))
                dp = await query.FirstOrDefaultAsync(x => x.Id == id);
            else
                dp = await query.FirstOrDefaultAsync(x => x.Id == id && x.KhachHangId == uid);

            if (dp == null) return NotFound();

            // Khách hàng chỉ được huỷ nếu: (Trạng thái đang là Đã Đặt) VÀ (Cách ngày nhận phòng ít nhất 1 ngày)
            ViewBag.CanCancel =
                dp.TrangThai == TrangThaiDatPhong.DaDat
                && (dp.NgayNhanPhong.Date - DateTime.Today).Days >= 1;

            return View(dp);
        }

        // Khách hàng tự huỷ Đơn đặt phòng
        [Authorize]
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancel(int id)
        {
            int uid = CurrentUserId();
            var dp = await _context.DatPhongs.FirstOrDefaultAsync(x => x.Id == id && x.KhachHangId == uid);
            if (dp == null) return NotFound();

            // Ràng buộc thời gian huỷ
            if ((dp.NgayNhanPhong.Date - DateTime.Today).Days < 1)
            {
                TempData["warning"] = "Chỉ được hủy trước ngày nhận phòng ít nhất 1 ngày.";
                return RedirectToAction(nameof(MyBookings));
            }

            if (dp.TrangThai != TrangThaiDatPhong.DaDat)
            {
                TempData["warning"] = "Chỉ hủy được đơn ở trạng thái Đã đặt.";
                return RedirectToAction(nameof(MyBookings));
            }

            // Đánh dấu huỷ đơn đặt phòng
            dp.TrangThai = TrangThaiDatPhong.DaHuy;

            // Đánh dấu huỷ hoá đơn liên kết
            var hd = await _context.HoaDons.FirstOrDefaultAsync(x => x.DatPhongId == dp.Id);
            if (hd != null) hd.TrangThai = TrangThaiHoaDon.DaHuy;

            await _context.SaveChangesAsync();
            TempData["success"] = $"Đã hủy đặt phòng #{dp.Id}.";
            return RedirectToAction(nameof(MyBookings));
        }
    }
}
