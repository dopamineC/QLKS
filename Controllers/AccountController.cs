/**
 * Module: AccountController
 * Chức năng: Quản lý đăng ký, đăng nhập và đăng xuất của người dùng
 * Người phụ trách: An
 */
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;
using QLKS.Data;
using QLKS.Models;
using QLKS.ViewModels;

namespace QLKS.Controllers
{
    // Controller xử lý chức năng Đăng ký, Đăng nhập và Đăng xuất cho người dùng
    public class AccountController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly PasswordHasher<NguoiDung> _hasher = new();

        public AccountController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Hiển thị giao diện Đăng ký tài khoản
        [HttpGet]
        public IActionResult Register() => View(new RegisterVM());

        // Xử lý dữ liệu khi người dùng gửi form Đăng ký
        // Bước 1: Validate đầu vào, Bước 2: Kiểm tra trùng lặp, Bước 3: Lưu DB
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterVM vm)
        {
            if (!ModelState.IsValid) return View(vm);

            // Kiểm tra tên đăng nhập đã tồn tại chưa
            var exists = await _context.NguoiDungs.AnyAsync(x => x.TenDangNhap == vm.TenDangNhap);
            if (exists)
            {
                ModelState.AddModelError(nameof(vm.TenDangNhap), "Tên đăng nhập đã tồn tại.");
                return View(vm);
            }

            // Tạo đối tượng người dùng mới
            var user = new NguoiDung
            {
                TenDangNhap = vm.TenDangNhap.Trim(),
                HoTen = vm.HoTen.Trim(),
                Email = vm.Email?.Trim(),
                SoDienThoai = vm.SoDienThoai?.Trim(),
                CCCD = vm.CCCD?.Trim(),
                VaiTro = VaiTro.Customer, // Mặc định là khách hàng
                TrangThai = true,
                NgayTao = DateTime.UtcNow
            };
            
            // Mã hoá mật khẩu (Băm mật khẩu) trước khi lưu vào DB
            // An chú ý: Tuyệt đối không lưu mật khẩu dạng plaintext để bảo mật
            user.MatKhauHash = _hasher.HashPassword(user, vm.MatKhau);

            _context.NguoiDungs.Add(user);
            await _context.SaveChangesAsync();

            // Chuyển hướng sang trang Đăng nhập sau khi đăng ký thành công
            return RedirectToAction(nameof(Login));
        }

        // Hiển thị giao diện Đăng nhập
        [HttpGet]
        public IActionResult Login(string? returnUrl = null)
            => View(new LoginVM { ReturnUrl = returnUrl });

        // Xử lý dữ liệu khi người dùng gửi form Đăng nhập
        // Bước 1: Tìm User, Bước 2: Verify Mật khẩu, Bước 3: Tạo Cookie Session
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginVM vm)
        {
            if (!ModelState.IsValid) return View(vm);

            // Tìm kiếm tài khoản
            var user = await _context.NguoiDungs.FirstOrDefaultAsync(x => x.TenDangNhap == vm.TenDangNhap);
            if (user == null || !user.TrangThai)
            {
                ModelState.AddModelError(string.Empty, "Sai tài khoản hoặc mật khẩu.");
                return View(vm);
            }

            // Kiểm tra mật khẩu
            var verify = _hasher.VerifyHashedPassword(user, user.MatKhauHash, vm.MatKhau);
            if (verify == PasswordVerificationResult.Failed)
            {
                ModelState.AddModelError(string.Empty, "Sai tài khoản hoặc mật khẩu.");
                return View(vm);
            }

            // Tạo danh sách Claims (Các mảnh thông tin định danh của người dùng)
            // An chú ý: VaiTro.ToString() rất quan trọng để phân quyền [Authorize(Roles="Admin")] ở các Controller khác
            var claims = new List<Claim>
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()), // Dùng để lấy Id người dùng hiện tại
                new Claim(ClaimTypes.Name, user.HoTen),                   // Dùng để hiển thị tên góc trên màn hình
                new Claim(ClaimTypes.Role, user.VaiTro.ToString()),       // Cấp quyền Admin hay Customer
                new Claim("username", user.TenDangNhap)
            };

            var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
            var principal = new ClaimsPrincipal(identity);

            // Thực hiện cấp phát Cookie cho trình duyệt để duy trì trạng thái Đăng nhập
            // IsPersistent = vm.GhiNho giúp giữ Cookie kể cả khi đóng trình duyệt (nếu người dùng tick "Ghi nhớ")
            await HttpContext.SignInAsync(
                CookieAuthenticationDefaults.AuthenticationScheme,
                principal,
                new AuthenticationProperties { IsPersistent = vm.GhiNho });

            // Trở về trang trước đó nếu có
            if (!string.IsNullOrWhiteSpace(vm.ReturnUrl) && Url.IsLocalUrl(vm.ReturnUrl))
                return Redirect(vm.ReturnUrl);

            return RedirectToAction("Index", "Home");
        }

        // Xử lý Đăng xuất
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);
            return RedirectToAction("Index", "Home");
        }

        // Trang thông báo từ chối truy cập (khi không đủ quyền)
        public IActionResult AccessDenied() => View();
    }
}
