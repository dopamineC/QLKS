/**
 * Module: PhongController
 * Chức năng: Quản lý danh sách các phòng vật lý và trạng thái phòng (Admin)
 * Người phụ trách: Việt
 */
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.Rendering;
using Microsoft.EntityFrameworkCore;
using QLKS.Data;
using QLKS.Models;

namespace QLKS.Controllers
{
    // Controller quản lý danh mục phòng vật lý trong hệ thống
    [Authorize(Roles = "Admin")]
    public class PhongController : Controller
    {
        private readonly ApplicationDbContext _context;

        public PhongController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Hiển thị danh sách các phòng và hỗ trợ tìm kiếm, lọc
        public async Task<IActionResult> Index(string? q, int? loaiPhongId, TrangThaiPhong? trangThai)
        {
            var query = _context.Phongs.AsNoTracking().Include(p => p.LoaiPhong).AsQueryable();

            if (!string.IsNullOrWhiteSpace(q))
                query = query.Where(x => x.SoPhong.Contains(q));

            if (loaiPhongId.HasValue)
                query = query.Where(x => x.LoaiPhongId == loaiPhongId.Value);

            if (trangThai.HasValue)
                query = query.Where(x => x.TrangThai == trangThai.Value);

            ViewBag.LoaiPhongs = await _context.LoaiPhongs.AsNoTracking().ToListAsync();
            ViewBag.Q = q;
            ViewBag.LoaiPhongId = loaiPhongId;
            ViewBag.TrangThai = trangThai;

            return View(await query.OrderBy(x => x.SoPhong).ToListAsync());
        }

        public async Task<IActionResult> Create()
        {
            ViewBag.LoaiPhongId = new SelectList(await _context.LoaiPhongs.ToListAsync(), "Id", "TenLoai");
            return View(new Phong());
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(Phong model)
        {
            model.SoPhong = (model.SoPhong ?? "").Trim();

            // ✅ Check trùng số phòng trước khi lưu
            if (!string.IsNullOrWhiteSpace(model.SoPhong))
            {
                bool trung = await _context.Phongs.AnyAsync(p => p.SoPhong == model.SoPhong);
                if (trung)
                    ModelState.AddModelError(nameof(model.SoPhong), "Số phòng đã tồn tại.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.LoaiPhongId = new SelectList(await _context.LoaiPhongs.ToListAsync(), "Id", "TenLoai", model.LoaiPhongId);
                return View(model);
            }

            _context.Phongs.Add(model);
            try
            {
                await _context.SaveChangesAsync();
                TempData["success"] = $"Đã thêm phòng {model.SoPhong}.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                // ✅ Phòng trường hợp race-condition vẫn bị Unique Index
                ModelState.AddModelError(nameof(model.SoPhong), "Số phòng đã tồn tại (DB từ chối).");
                ViewBag.LoaiPhongId = new SelectList(await _context.LoaiPhongs.ToListAsync(), "Id", "TenLoai", model.LoaiPhongId);
                return View(model);
            }
        }

        public async Task<IActionResult> Edit(int id)
        {
            var model = await _context.Phongs.FindAsync(id);
            if (model == null) return NotFound();

            ViewBag.LoaiPhongId = new SelectList(await _context.LoaiPhongs.ToListAsync(), "Id", "TenLoai", model.LoaiPhongId);
            return View(model);
        }

        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Edit(int id, Phong model)
        {
            if (id != model.Id) return BadRequest();

            model.SoPhong = (model.SoPhong ?? "").Trim();

            // ✅ Check trùng số phòng (trừ chính nó)
            if (!string.IsNullOrWhiteSpace(model.SoPhong))
            {
                bool trung = await _context.Phongs.AnyAsync(p => p.SoPhong == model.SoPhong && p.Id != model.Id);
                if (trung)
                    ModelState.AddModelError(nameof(model.SoPhong), "Số phòng đã tồn tại.");
            }

            if (!ModelState.IsValid)
            {
                ViewBag.LoaiPhongId = new SelectList(await _context.LoaiPhongs.ToListAsync(), "Id", "TenLoai", model.LoaiPhongId);
                return View(model);
            }

            // ✅ Update an toàn hơn (tránh overposting)
            var entity = await _context.Phongs.FirstOrDefaultAsync(p => p.Id == id);
            if (entity == null) return NotFound();

            entity.SoPhong = model.SoPhong;
            entity.Tang = model.Tang;
            entity.TrangThai = model.TrangThai;
            entity.LoaiPhongId = model.LoaiPhongId;

            try
            {
                await _context.SaveChangesAsync();
                TempData["success"] = $"Đã cập nhật phòng {entity.SoPhong}.";
                return RedirectToAction(nameof(Index));
            }
            catch (DbUpdateException)
            {
                ModelState.AddModelError(nameof(model.SoPhong), "Số phòng đã tồn tại (DB từ chối).");
                ViewBag.LoaiPhongId = new SelectList(await _context.LoaiPhongs.ToListAsync(), "Id", "TenLoai", model.LoaiPhongId);
                return View(model);
            }
        }

        public async Task<IActionResult> Details(int id)
        {
            var model = await _context.Phongs.AsNoTracking().Include(p => p.LoaiPhong).FirstOrDefaultAsync(x => x.Id == id);
            if (model == null) return NotFound();
            return View(model);
        }

        public async Task<IActionResult> Delete(int id)
        {
            var model = await _context.Phongs.AsNoTracking().Include(p => p.LoaiPhong).FirstOrDefaultAsync(x => x.Id == id);
            if (model == null) return NotFound();
            return View(model);
        }

        [HttpPost, ActionName("Delete")]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> DeleteConfirmed(int id)
        {
            var model = await _context.Phongs.FindAsync(id);
            if (model == null) return NotFound();

            _context.Phongs.Remove(model);
            try
            {
                await _context.SaveChangesAsync();
                TempData["success"] = $"Đã xóa phòng {model.SoPhong}.";
            }
            catch
            {
                TempData["error"] = "Không thể xóa phòng vì đang có dữ liệu liên quan (đặt phòng/hóa đơn).";
            }
            return RedirectToAction(nameof(Index));
        }
    }
}