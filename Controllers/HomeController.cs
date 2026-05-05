/**
 * Module: HomeController
 * Chức năng: Xử lý và hiển thị thông tin chung tại trang chủ (Public)
 * Người phụ trách: Đức
 */
using System.Diagnostics;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLKS.Data;
using QLKS.Models;
using QLKS.ViewModels;

namespace QLKS.Controllers
{
    // Controller xử lý các trang công khai mặc định của hệ thống
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly ApplicationDbContext _context;

        public HomeController(ILogger<HomeController> logger, ApplicationDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // Trang chủ (Trang chính của khách sạn)
        public async Task<IActionResult> Index()
        {
            var vm = new HomeIndexVM
            {
                // Lấy danh sách các loại phòng sắp xếp theo giá
                LoaiPhongs = await _context.LoaiPhongs
                    .AsNoTracking()
                    .OrderBy(x => x.GiaMoiDem)
                    .ToListAsync()
            };

            return View(vm);
        }

        // Trang chính sách bảo mật
        public IActionResult Privacy() => View();

        // Xử lý và hiển thị lỗi hệ thống
        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel { RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier });
        }
    }
}
