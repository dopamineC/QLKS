/**
 * Module: AdminLichDatPhongController
 * Chức năng: Quản lý hiển thị lịch đặt phòng trực quan trên FullCalendar (Admin)
 * Người phụ trách: Sơn
 */
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLKS.Data;
using QLKS.Models;
using System.ComponentModel.DataAnnotations;
using System.Globalization;
using System.Reflection;

namespace QLKS.Controllers
{
    [Authorize(Roles = "Admin")]
    public class AdminLichDatPhongController : Controller
    {
        private readonly ApplicationDbContext _context;

        public AdminLichDatPhongController(ApplicationDbContext context)
        {
            _context = context;
        }

        // Trang lịch (FullCalendar)
        public IActionResult Index()
        {
            return View();
        }

        // GET: /AdminLichDatPhong/Events?start=...&end=...&phongId=...&loaiPhongId=...&tang=...&trangThai=...&q=...
        [HttpGet]
        public async Task<IActionResult> Events(
            string start,
            string end,
            int? phongId = null,
            int? loaiPhongId = null,
            int? tang = null,
            TrangThaiDatPhong? trangThai = null,
            string? q = null)
        {
            if (!DateTime.TryParse(start, null, DateTimeStyles.RoundtripKind, out var startDt) ||
                !DateTime.TryParse(end, null, DateTimeStyles.RoundtripKind, out var endDt))
            {
                return BadRequest("Invalid start/end");
            }

            var startDate = startDt.Date;
            var endDateExclusive = endDt.Date;

            // Rooms filter
            var roomsQuery = _context.Phongs
                .AsNoTracking()
                .Include(p => p.LoaiPhong)
                .AsQueryable();

            if (phongId.HasValue)
            {
                roomsQuery = roomsQuery.Where(p => p.Id == phongId.Value);
            }
            else
            {
                if (loaiPhongId.HasValue)
                    roomsQuery = roomsQuery.Where(p => p.LoaiPhongId == loaiPhongId.Value);

                if (tang.HasValue)
                    roomsQuery = roomsQuery.Where(p => p.Tang == tang.Value);
            }

            var roomIds = await roomsQuery.Select(p => p.Id).ToListAsync();

            // Bookings overlap range
            var bookingQuery = _context.DatPhongs
                .AsNoTracking()
                .Include(dp => dp.Phong)
                .Include(dp => dp.KhachHang)
                .Where(dp =>
                    roomIds.Contains(dp.PhongId) &&
                    dp.NgayNhanPhong < endDateExclusive &&
                    dp.NgayTraPhong > startDate)
                .AsQueryable();

            if (trangThai.HasValue)
                bookingQuery = bookingQuery.Where(dp => dp.TrangThai == trangThai.Value);

            if (!string.IsNullOrWhiteSpace(q))
            {
                var k = q.Trim().ToLower();

                bookingQuery = bookingQuery.Where(dp =>
                    (dp.Phong != null && (dp.Phong.SoPhong ?? "").ToLower().Contains(k)) ||
                    (dp.KhachHang != null && (dp.KhachHang.HoTen ?? "").ToLower().Contains(k)));
            }

            var bookings = await bookingQuery.ToListAsync();

            // ===== Detect conflict chuẩn (overlap thật theo từng phòng) =====
            var conflictBookingIds = new HashSet<int>();

            foreach (var g in bookings.GroupBy(x => x.PhongId))
            {
                var list = g
                    .OrderBy(x => x.NgayNhanPhong.Date)
                    .ThenBy(x => x.NgayTraPhong.Date)
                    .ToList();

                for (int i = 0; i < list.Count - 1; i++)
                {
                    var aStart = list[i].NgayNhanPhong.Date;
                    var aEnd = list[i].NgayTraPhong.Date;

                    for (int j = i + 1; j < list.Count; j++)
                    {
                        var bStart = list[j].NgayNhanPhong.Date;
                        var bEnd = list[j].NgayTraPhong.Date;

                        if (bStart < aEnd && aStart < bEnd)
                        {
                            conflictBookingIds.Add(list[i].Id);
                            conflictBookingIds.Add(list[j].Id);
                        }

                        if (bStart >= aEnd) break;
                    }
                }
            }

            var events = bookings.Select(dp =>
            {
                var soPhong = dp.Phong?.SoPhong ?? "";
                var khach = dp.KhachHang?.HoTen ?? "Khách";

                var startStr = dp.NgayNhanPhong.Date.ToString("yyyy-MM-dd");

                // FullCalendar allDay: end là exclusive
                var endSafe = dp.NgayTraPhong.Date <= dp.NgayNhanPhong.Date
                    ? dp.NgayNhanPhong.Date.AddDays(1)
                    : dp.NgayTraPhong.Date;

                var endStr = endSafe.ToString("yyyy-MM-dd");

                var (color, key) = GetStatusStyle(dp.TrangThai);

                return new
                {
                    id = dp.Id.ToString(),
                    title = $"P{soPhong} - {khach}",
                    start = startStr,
                    end = endStr,
                    allDay = true,
                    backgroundColor = color,
                    borderColor = color,
                    textColor = "#fff",
                    extendedProps = new
                    {
                        soPhong,
                        khachHang = khach,
                        ngayNhan = dp.NgayNhanPhong.Date.ToString("dd/MM/yyyy"),
                        ngayTra = dp.NgayTraPhong.Date.ToString("dd/MM/yyyy"),
                        soDem = dp.SoDem,
                        tongTien = string.Format("{0:N0} ₫", dp.TongTien),
                        trangThaiText = GetDisplayText(dp.TrangThai),
                        trangThaiKey = key,

                        // conflict
                        isConflict = conflictBookingIds.Contains(dp.Id),

                        // NOTE: đổi đúng controller chi tiết/sửa đặt phòng của mày nếu khác
                        detailsUrl = Url.Action("Details", "AdminBooking", new { id = dp.Id }),
                        editUrl = Url.Action("Edit", "AdminBooking", new { id = dp.Id })
                    }
                };
            });

            return Json(events);
        }

        // GET: /AdminLichDatPhong/PhongOptions?loaiPhongId=...&tang=...
        [HttpGet]
        public async Task<IActionResult> PhongOptions(int? loaiPhongId = null, int? tang = null)
        {
            var q = _context.Phongs.AsNoTracking().AsQueryable();

            if (loaiPhongId.HasValue)
                q = q.Where(x => x.LoaiPhongId == loaiPhongId.Value);

            if (tang.HasValue)
                q = q.Where(x => x.Tang == tang.Value);

            var rooms = await q
                .OrderBy(x => x.Tang)
                .ThenBy(x => x.SoPhong)
                .Select(x => new
                {
                    id = x.Id,
                    text = $"Phòng {x.SoPhong} (Tầng {x.Tang})"
                })
                .ToListAsync();

            return Json(rooms);
        }

        [HttpGet]
        public async Task<IActionResult> LoaiPhongOptions()
        {
            var ls = await _context.LoaiPhongs
                .AsNoTracking()
                .OrderBy(x => x.TenLoai)
                .Select(x => new { id = x.Id, text = x.TenLoai })
                .ToListAsync();

            return Json(ls);
        }

        [HttpGet]
        public async Task<IActionResult> TangOptions()
        {
            var tangs = await _context.Phongs
                .AsNoTracking()
                .Select(x => x.Tang)
                .Distinct()
                .OrderBy(x => x)
                .ToListAsync();

            return Json(tangs.Select(t => new { value = t, text = $"Tầng {t}" }).ToList());
        }

        [HttpGet]
        public IActionResult TrangThaiOptions()
        {
            var sts = Enum.GetValues(typeof(TrangThaiDatPhong))
                .Cast<TrangThaiDatPhong>()
                .Select(x => new { value = (int)x, text = GetDisplayText(x) })
                .ToList();

            return Json(sts);
        }

        // ===== Helpers =====
        private static string GetDisplayText(TrangThaiDatPhong st)
        {
            var mem = st.GetType().GetMember(st.ToString()).FirstOrDefault();
            var display = mem?.GetCustomAttribute<DisplayAttribute>();
            return display?.Name ?? st.ToString();
        }

        private static (string color, string key) GetStatusStyle(TrangThaiDatPhong st)
        {
            var s = st.ToString().ToLower();

            if (s.Contains("huy") || s.Contains("cancel")) 
                return ("#dc3545", "red");

            if (s.Contains("nhan") || s.Contains("dang"))
                return ("#0d6efd", "blue");

            if (s.Contains("tra") || s.Contains("xong") || s.Contains("hoanthanh"))
                return ("#198754", "green");

            return ("#f0ad00", "yellow");
        }
    }
}
