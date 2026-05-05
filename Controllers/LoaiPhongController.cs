/**
 * Module: LoaiPhongController
 * Chức năng: Quản lý thêm, sửa, xóa, hiển thị danh mục loại phòng (Admin)
 * Người phụ trách: Việt
 */
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLKS.Data;
using QLKS.Models;

namespace QLKS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class LoaiPhongController : Controller
    {
        private readonly ApplicationDbContext _context;
        private readonly IWebHostEnvironment _env;

        private const long MaxUploadBytes = 2 * 1024 * 1024; // 2MB
        private static readonly string[] AllowedExt = new[] { ".jpg", ".jpeg", ".png", ".webp" };

        public LoaiPhongController(ApplicationDbContext context, IWebHostEnvironment env)
        {
            _context = context;
            _env = env;
        }

        public async Task<IActionResult> Index(string? q)
        {
            var query = _context.LoaiPhongs.AsNoTracking().AsQueryable();
            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(x => x.TenLoai.Contains(q));

            ViewBag.Q = q;
            return View(await query.OrderBy(x => x.TenLoai).ToListAsync());
        }

        // Hiển thị form tạo mới Loại phòng
        public IActionResult Create() => View(new LoaiPhong());

        // Xử lý lưu Loại phòng mới kèm tính năng upload Ảnh
        // Việt chú ý: Logic xử lý file upload ở cuối file (TrySaveImage)
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(LoaiPhong model, IFormFile? fileAnh)
        {
            if (!ModelState.IsValid) return View(model);

            // Upload ảnh (nếu có)
            if (fileAnh != null && fileAnh.Length > 0)
            {
                var (ok, savedPath, err) = await TrySaveImage(fileAnh);
                if (!ok)
                {
                    // KHÔNG throw nữa -> trả về form + hiện lỗi dưới input file
                    ModelState.AddModelError("fileAnh", err ?? "Ảnh không hợp lệ.");
                    return View(model);
                }
                model.HinhAnh = savedPath;
            }

            _context.LoaiPhongs.Add(model);
            await _context.SaveChangesAsync();

            TempData["success"] = $"Đã thêm loại phòng \"{model.TenLoai}\".";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Edit(int id)
        {
            var model = await _context.LoaiPhongs.FindAsync(id);
            if (model == null) return NotFound();
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, LoaiPhong model, IFormFile? fileAnh)
        {
            if (id != model.Id) return BadRequest();
            if (!ModelState.IsValid) return View(model);

            var db = await _context.LoaiPhongs.FindAsync(id);
            if (db == null) return NotFound();

            db.TenLoai = model.TenLoai;
            db.GiaMoiDem = model.GiaMoiDem;
            db.SoNguoi = model.SoNguoi;
            db.MoTa = model.MoTa;

            // Upload ảnh mới (nếu có)
            if (fileAnh != null && fileAnh.Length > 0)
            {
                var old = db.HinhAnh;

                var (ok, savedPath, err) = await TrySaveImage(fileAnh);
                if (!ok)
                {
                    // ✅ giữ ảnh cũ + báo lỗi trên form
                    ModelState.AddModelError("fileAnh", err ?? "Ảnh không hợp lệ.");
                    model.HinhAnh = old; // để preview vẫn hiện ảnh cũ
                    return View(model);
                }

                db.HinhAnh = savedPath;
                TryDeleteOldImage(old); // xoá ảnh cũ sau khi upload mới ok
            }

            await _context.SaveChangesAsync();

            TempData["success"] = $"Đã cập nhật loại phòng \"{db.TenLoai}\".";
            return RedirectToAction(nameof(Index));
        }

        public async Task<IActionResult> Details(int id)
        {
            var model = await _context.LoaiPhongs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (model == null) return NotFound();
            return View(model);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var model = await _context.LoaiPhongs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == id);
            if (model == null) return NotFound();
            return View(model);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var model = await _context.LoaiPhongs.FindAsync(id);
            if (model == null) return NotFound();

            _context.LoaiPhongs.Remove(model);
            try
            {
                await _context.SaveChangesAsync();
                TempData["success"] = $"Đã xóa loại phòng \"{model.TenLoai}\".";
            }
            catch
            {
                TempData["error"] = "Không thể xóa loại phòng vì đang được sử dụng bởi phòng/đặt phòng.";
            }

            return RedirectToAction(nameof(Index));
        }

        // =========================
        // Upload helpers (KHÔNG throw)
        // Việt chú ý: Hàm này dùng để lưu ảnh vào thư mục wwwroot/uploads/loaiphong
        // =========================
        private async Task<(bool ok, string? savedPath, string? error)> TrySaveImage(IFormFile file)
        {
            if (file == null || file.Length <= 0)
                return (false, null, "Vui lòng chọn ảnh.");

            var ext = Path.GetExtension(file.FileName).ToLowerInvariant();
            if (string.IsNullOrWhiteSpace(ext) || !AllowedExt.Contains(ext))
                return (false, null, "Chỉ cho phép ảnh .jpg/.jpeg/.png/.webp");

            if (file.Length > MaxUploadBytes)
                return (false, null, "Ảnh tối đa 2MB");

            try
            {
                var folder = Path.Combine(_env.WebRootPath, "uploads", "loaiphong");
                Directory.CreateDirectory(folder);

                var fileName = $"{Guid.NewGuid():N}{ext}";
                var fullPath = Path.Combine(folder, fileName);

                using var stream = new FileStream(fullPath, FileMode.Create);
                await file.CopyToAsync(stream);

                return (true, $"/uploads/loaiphong/{fileName}", null);
            }
            catch
            {
                return (false, null, "Không thể lưu ảnh. Vui lòng thử lại.");
            }
        }

        private void TryDeleteOldImage(string? oldPath)
        {
            if (string.IsNullOrWhiteSpace(oldPath)) return;

            // chỉ xoá trong uploads/loaiphong để an toàn
            if (!oldPath.StartsWith("/uploads/loaiphong/", StringComparison.OrdinalIgnoreCase)) return;

            try
            {
                var physical = Path.Combine(_env.WebRootPath,
                    oldPath.TrimStart('/').Replace('/', Path.DirectorySeparatorChar));

                if (System.IO.File.Exists(physical))
                    System.IO.File.Delete(physical);
            }
            catch { /* ignore */ }
        }
    }
}
