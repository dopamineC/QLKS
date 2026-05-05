/**
 * Module: LoaiPhongPublicController
 * Chức năng: Hiển thị danh sách và chi tiết các loại phòng cho khách hàng ngoài trang chủ
 * Người phụ trách: Việt
 */
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLKS.Data;

namespace QLKS.Controllers
{
    // Controller hiển thị danh sách và chi tiết các loại phòng cho khách hàng
    public class LoaiPhongPublicController : Controller
    {
        private readonly ApplicationDbContext _context;

        public LoaiPhongPublicController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Trang danh sách các loại phòng
        public async Task<IActionResult> Index()
        {
            var list = await _context.LoaiPhongs.AsNoTracking()
                .OrderBy(x => x.TenLoai)
                .ToListAsync();

            return View(list);
        }

        // Trang xem chi tiết một loại phòng cụ thể
        public async Task<IActionResult> Details(int id)
        {
            var model = await _context.LoaiPhongs.AsNoTracking()
                .FirstOrDefaultAsync(x => x.Id == id);

            if (model == null) return NotFound();
            return View(model);
        }
    }
}
