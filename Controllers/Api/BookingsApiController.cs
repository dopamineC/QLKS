using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLKS.Data;
using QLKS.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace QLKS.Controllers.Api
{
    /// <summary>API Đặt phòng</summary>
    [ApiController]
    [Route("api/bookings")]
    [Authorize(Roles = "Admin")]
    public class BookingsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public BookingsApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        private int? CurrentUserId()
        {
            var s = User.FindFirstValue(ClaimTypes.NameIdentifier);
            if (int.TryParse(s, out var id)) return id;
            return null;
        }

        public class CreateBookingRequest
        {
            /// <summary>Nếu API không auth cookie/jwt thì bắt buộc truyền</summary>
            public int? KhachHangId { get; set; }

            [Required]
            public int PhongId { get; set; }

            [Required]
            public DateTime NgayNhan { get; set; }

            [Required]
            public DateTime NgayTra { get; set; }
        }

        /// <summary>Danh sách đặt phòng (lọc)</summary>
        [HttpGet]
        public async Task<IActionResult> GetAll(
            [FromQuery] int? khachHangId,
            [FromQuery] int? phongId,
            [FromQuery] TrangThaiDatPhong? trangThai
        )
        {
            var q = _context.DatPhongs
                .AsNoTracking()
                .Include(dp => dp.KhachHang)
                .Include(dp => dp.Phong)!.ThenInclude(p => p.LoaiPhong)
                .Include(dp => dp.HoaDon)
                .AsQueryable();

            if (khachHangId.HasValue) q = q.Where(x => x.KhachHangId == khachHangId.Value);
            if (phongId.HasValue) q = q.Where(x => x.PhongId == phongId.Value);
            if (trangThai.HasValue) q = q.Where(x => x.TrangThai == trangThai.Value);

            var data = await q
                .OrderByDescending(x => x.NgayDat)
                .Select(x => new
                {
                    x.Id,
                    x.KhachHangId,
                    KhachHang = new { x.KhachHang!.Id, x.KhachHang.HoTen, x.KhachHang.TenDangNhap },
                    x.PhongId,
                    Phong = new
                    {
                        x.Phong!.Id,
                        x.Phong.SoPhong,
                        x.Phong.Tang,
                        TrangThaiPhong = x.Phong.TrangThai.ToString(),
                        LoaiPhong = new
                        {
                            x.Phong.LoaiPhong!.Id,
                            x.Phong.LoaiPhong.TenLoai,
                            x.Phong.LoaiPhong.GiaMoiDem,
                            x.Phong.LoaiPhong.SoNguoi
                        }
                    },
                    x.NgayNhanPhong,
                    x.NgayTraPhong,
                    x.SoDem,
                    x.TongTien,
                    TrangThai = x.TrangThai.ToString(),
                    x.NgayDat,
                    HoaDon = x.HoaDon == null ? null : new
                    {
                        x.HoaDon.Id,
                        x.HoaDon.SoTien,
                        TrangThai = x.HoaDon.TrangThai.ToString(),
                        PhuongThuc = x.HoaDon.PhuongThuc.ToString(),
                        x.HoaDon.NgayThanhToan
                    }
                })
                .ToListAsync();

            return Ok(data);
        }

        /// <summary>Chi tiết đặt phòng</summary>
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var x = await _context.DatPhongs
                .AsNoTracking()
                .Include(dp => dp.KhachHang)
                .Include(dp => dp.Phong)!.ThenInclude(p => p.LoaiPhong)
                .Include(dp => dp.HoaDon)
                .Where(dp => dp.Id == id)
                .Select(dp => new
                {
                    dp.Id,
                    dp.KhachHangId,
                    KhachHang = new { dp.KhachHang!.Id, dp.KhachHang.HoTen, dp.KhachHang.TenDangNhap },
                    dp.PhongId,
                    Phong = new
                    {
                        dp.Phong!.Id,
                        dp.Phong.SoPhong,
                        dp.Phong.Tang,
                        TrangThaiPhong = dp.Phong.TrangThai.ToString(),
                        LoaiPhong = new
                        {
                            dp.Phong.LoaiPhong!.Id,
                            dp.Phong.LoaiPhong.TenLoai,
                            dp.Phong.LoaiPhong.GiaMoiDem,
                            dp.Phong.LoaiPhong.SoNguoi
                        }
                    },
                    dp.NgayNhanPhong,
                    dp.NgayTraPhong,
                    dp.SoDem,
                    dp.TongTien,
                    TrangThai = dp.TrangThai.ToString(),
                    dp.NgayDat,
                    HoaDon = dp.HoaDon == null ? null : new
                    {
                        dp.HoaDon.Id,
                        dp.HoaDon.SoTien,
                        TrangThai = dp.HoaDon.TrangThai.ToString(),
                        PhuongThuc = dp.HoaDon.PhuongThuc.ToString(),
                        dp.HoaDon.NgayThanhToan
                    }
                })
                .FirstOrDefaultAsync();

            if (x == null) return NotFound(new { message = "Không tìm thấy đặt phòng." });
            return Ok(x);
        }

        /// <summary>Tạo đặt phòng + tự tạo hóa đơn (Chưa thanh toán)</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] CreateBookingRequest req)
        {
            var uid = CurrentUserId();
            int? khachHangId = req.KhachHangId ?? uid;

            if (!khachHangId.HasValue)
                return BadRequest(new { message = "Thiếu KhachHangId (hoặc chưa đăng nhập)." });

            if (req.NgayNhan == default || req.NgayTra == default)
                return BadRequest(new { message = "Thiếu ngày nhận hoặc ngày trả." });

            var ngayNhan = req.NgayNhan.Date;
            var ngayTra = req.NgayTra.Date;

            if (ngayNhan < DateTime.Today)
                return BadRequest(new { message = "Ngày nhận phòng phải >= hôm nay." });

            if (ngayTra <= ngayNhan)
                return BadRequest(new { message = "Ngày trả phòng phải > ngày nhận phòng." });

            var kh = await _context.NguoiDungs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == khachHangId.Value);
            if (kh == null) return NotFound(new { message = "Không tìm thấy khách hàng." });

            var room = await _context.Phongs
                .Include(p => p.LoaiPhong)
                .FirstOrDefaultAsync(p => p.Id == req.PhongId);

            if (room == null) return NotFound(new { message = "Không tìm thấy phòng." });

            if (room.TrangThai == TrangThaiPhong.BaoTri)
                return BadRequest(new { message = "Phòng đang bảo trì." });

            bool hasOverlap = await _context.DatPhongs.AnyAsync(dp =>
                dp.PhongId == req.PhongId
                && (dp.TrangThai == TrangThaiDatPhong.DaDat || dp.TrangThai == TrangThaiDatPhong.DaNhanPhong)
                && ngayNhan < dp.NgayTraPhong
                && ngayTra > dp.NgayNhanPhong
            );

            if (hasOverlap)
                return Conflict(new { message = "Phòng đã được đặt trong khoảng thời gian này." });

            int soDem = (ngayTra - ngayNhan).Days;
            decimal tongTien = soDem * room.LoaiPhong!.GiaMoiDem;

            var dpNew = new DatPhong
            {
                KhachHangId = khachHangId.Value,
                PhongId = req.PhongId,
                NgayNhanPhong = ngayNhan,
                NgayTraPhong = ngayTra,
                SoDem = soDem,
                TongTien = tongTien,
                TrangThai = TrangThaiDatPhong.DaDat,
                NgayDat = DateTime.UtcNow
            };

            _context.DatPhongs.Add(dpNew);
            await _context.SaveChangesAsync();

            var hd = new HoaDon
            {
                DatPhongId = dpNew.Id,
                SoTien = dpNew.TongTien,
                TrangThai = TrangThaiHoaDon.ChuaThanhToan,
                PhuongThuc = PhuongThucThanhToan.TienMat
            };

            _context.HoaDons.Add(hd);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = dpNew.Id }, new
            {
                dpNew.Id,
                dpNew.KhachHangId,
                dpNew.PhongId,
                dpNew.NgayNhanPhong,
                dpNew.NgayTraPhong,
                dpNew.SoDem,
                dpNew.TongTien,
                TrangThai = dpNew.TrangThai.ToString(),
                dpNew.NgayDat,
                HoaDon = new
                {
                    hd.Id,
                    hd.SoTien,
                    TrangThai = hd.TrangThai.ToString(),
                    PhuongThuc = hd.PhuongThuc.ToString(),
                    hd.NgayThanhToan
                }
            });
        }

        /// <summary>Hủy đặt phòng (chỉ khi trạng thái Đã đặt + trước ngày nhận >= 1 ngày)</summary>
        [HttpPut("{id:int}/cancel")]
        public async Task<IActionResult> Cancel(int id)
        {
            var dp = await _context.DatPhongs.FirstOrDefaultAsync(x => x.Id == id);
            if (dp == null) return NotFound(new { message = "Không tìm thấy đặt phòng." });

            if (dp.TrangThai != TrangThaiDatPhong.DaDat)
                return BadRequest(new { message = "Chỉ hủy được đơn ở trạng thái 'Đã đặt'." });

            if ((dp.NgayNhanPhong.Date - DateTime.Today).Days < 1)
                return BadRequest(new { message = "Chỉ được hủy trước ngày nhận phòng ít nhất 1 ngày." });

            dp.TrangThai = TrangThaiDatPhong.DaHuy;

            var hd = await _context.HoaDons.FirstOrDefaultAsync(x => x.DatPhongId == dp.Id);
            if (hd != null) hd.TrangThai = TrangThaiHoaDon.DaHuy;

            await _context.SaveChangesAsync();

            return Ok(new { message = $"Đã hủy đặt phòng #{dp.Id}.", id = dp.Id });
        }
    }
}