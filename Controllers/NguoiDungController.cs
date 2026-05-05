/**
 * Module: NguoiDungController
 * Chức năng: Quản lý danh sách tài khoản người dùng và phân quyền (dành cho Admin)
 * Người phụ trách: An
 */

using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using QLKS.Data;
using QLKS.Models;
using QLKS.ViewModels;

namespace QLKS.Controllers
{
    // Controller quản lý tài khoản người dùng (bao gồm Admin và Khách hàng)
    [Authorize(Roles = "Admin")]
    public class NguoiDungController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordHasher<NguoiDung> _hasher = new();

        public NguoiDungController(ApplicationDbContext context)
        {
            _context = context;
        }

        // ===== Danh sách + lọc =====
        // Hiển thị danh sách tài khoản kèm bộ lọc tìm kiếm
        public async Task<IActionResult> Index(string? q, VaiTro? vaiTro, bool? trangThai)
        {
            var query = _context.NguoiDungs.AsNoTracking().AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
            {
                q = q.Trim();
                query = query.Where(u =>
                    u.HoTen.Contains(q) ||
                    u.TenDangNhap.Contains(q) ||
                    (u.SoDienThoai != null && u.SoDienThoai.Contains(q)) ||
                    (u.Email != null && u.Email.Contains(q)));
            }

            if (vaiTro.HasValue)
                query = query.Where(u => u.VaiTro == vaiTro.Value);

            if (trangThai.HasValue)
                query = query.Where(u => u.TrangThai == trangThai.Value);

            // FIX: dùng subquery Count() để khỏi lỗi nullable từ LEFT JOIN
            var list = await query
                .OrderBy(u => u.Id)
                .Select(u => new AdminUserListItemVM
                {
                    Id = u.Id,
                    TenDangNhap = u.TenDangNhap,
                    HoTen = u.HoTen,
                    SoDienThoai = u.SoDienThoai,
                    Email = u.Email,
                    VaiTro = u.VaiTro,
                    TrangThai = u.TrangThai,
                    NgayTao = u.NgayTao,
                    SoDatPhong = _context.DatPhongs.Count(dp => dp.KhachHangId == u.Id)
                })
                .ToListAsync();

            ViewBag.Q = q;
            ViewBag.VaiTro = vaiTro;
            ViewBag.TrangThai = trangThai;

            return View(list);
        }

        // ===== Tạo người dùng mới từ trang Quản trị (Dành cho Admin) =====
        [HttpGet]
        public IActionResult Create() => View(new AdminUserCreateVM());

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(AdminUserCreateVM vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var username = vm.TenDangNhap.Trim();
            bool exists = await _context.NguoiDungs.AnyAsync(x => x.TenDangNhap == username);
            if (exists)
            {
                ModelState.AddModelError(nameof(vm.TenDangNhap), "Tên đăng nhập đã tồn tại.");
                return View(vm);
            }

            var user = new NguoiDung
            {
                TenDangNhap = username,
                HoTen = vm.HoTen.Trim(),
                SoDienThoai = vm.SoDienThoai?.Trim(),
                Email = vm.Email?.Trim(),
                CCCD = vm.CCCD?.Trim(),
                VaiTro = vm.VaiTro,
                TrangThai = vm.TrangThai,
                NgayTao = DateTime.UtcNow
            };

            user.MatKhauHash = _hasher.HashPassword(user, vm.MatKhau);

            _context.NguoiDungs.Add(user);
            await _context.SaveChangesAsync();

            TempData["msg"] = "Đã tạo người dùng.";
            return RedirectToAction(nameof(Index));
        }

        // ===== Sửa người dùng =====
        [HttpGet]
        public async Task<IActionResult> Edit(int id)
        {
            var u = await _context.NguoiDungs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (u == null) return NotFound();

            var vm = new AdminUserEditVM
            {
                Id = u.Id,
                TenDangNhap = u.TenDangNhap,
                HoTen = u.HoTen,
                SoDienThoai = u.SoDienThoai,
                Email = u.Email,
                CCCD = u.CCCD,
                VaiTro = u.VaiTro,
                TrangThai = u.TrangThai
            };

            return View(vm);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(AdminUserEditVM vm)
        {
            if (!ModelState.IsValid) return View(vm);

            var u = await _context.NguoiDungs.FirstOrDefaultAsync(x => x.Id == vm.Id);
            if (u == null) return NotFound();

            u.HoTen = vm.HoTen.Trim();
            u.SoDienThoai = vm.SoDienThoai?.Trim();
            u.Email = vm.Email?.Trim();
            u.CCCD = vm.CCCD?.Trim();
            u.VaiTro = vm.VaiTro;
            u.TrangThai = vm.TrangThai;

            await _context.SaveChangesAsync();

            TempData["msg"] = "Đã cập nhật người dùng.";
            return RedirectToAction(nameof(Index));
        }

        // ===== Reset mật khẩu (Trường hợp khách quan quên mật khẩu và nhờ Admin) =====
        [HttpGet]
        public async Task<IActionResult> ResetPassword(int id)
        {
            var u = await _context.NguoiDungs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (u == null) return NotFound();

            ViewBag.User = u;
            return View(new AdminResetPasswordVM { Id = id });
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ResetPassword(AdminResetPasswordVM vm)
        {
            var u0 = await _context.NguoiDungs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == vm.Id);
            ViewBag.User = u0;

            if (!ModelState.IsValid) return View(vm);

            var u = await _context.NguoiDungs.FirstOrDefaultAsync(x => x.Id == vm.Id);
            if (u == null) return NotFound();

            u.MatKhauHash = _hasher.HashPassword(u, vm.MatKhauMoi);
            await _context.SaveChangesAsync();

            TempData["msg"] = "Đã đặt lại mật khẩu.";
            return RedirectToAction(nameof(Index));
        }

        // ===== Khoá / Mở khoá tài khoản (Vô hiệu hoá tài khoản vi phạm) =====
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> ToggleStatus(int id)
        {
            var u = await _context.NguoiDungs.FirstOrDefaultAsync(x => x.Id == id);
            if (u == null) return NotFound();

            // Lấy ID của Admin đang thao tác (từ Cookie) để chặn việc Admin tự khoá chính mình
            var meIdStr = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(meIdStr, out var meId) && meId == id)
            {
                TempData["msg"] = "Không thể tự khoá tài khoản đang đăng nhập.";
                return RedirectToAction(nameof(Index));
            }

            u.TrangThai = !u.TrangThai;
            await _context.SaveChangesAsync();

            TempData["msg"] = u.TrangThai ? "Đã mở khoá tài khoản." : "Đã khoá tài khoản.";
            return RedirectToAction(nameof(Index));
        }

        // ===== Chi tiết =====
        public async Task<IActionResult> Details(int id)
        {
            var u = await _context.NguoiDungs.AsNoTracking()
                .Include(x => x.DatPhongs)
                .FirstOrDefaultAsync(x => x.Id == id);

            if (u == null) return NotFound();
            return View(u);
        }
    }
}
