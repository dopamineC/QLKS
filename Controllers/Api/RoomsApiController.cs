using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLKS.Data;
using QLKS.Models;
using System.ComponentModel.DataAnnotations;

namespace QLKS.Controllers.Api
{
    /// <summary>API Phòng</summary>
    [ApiController]
    [Route("api/rooms")]
    [Authorize(Roles = "Admin")]
    public class RoomsApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RoomsApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        public class RoomUpsertRequest
        {
            [Required, StringLength(20)]
            public string SoPhong { get; set; } = string.Empty;

            [Range(1, 200)]
            public int Tang { get; set; } = 1;

            public TrangThaiPhong? TrangThai { get; set; }

            [Required]
            public int LoaiPhongId { get; set; }
        }

        /// <summary>Danh sách phòng (lọc theo loại phòng / trạng thái)</summary>
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetAll([FromQuery] int? loaiPhongId, [FromQuery] TrangThaiPhong? trangThai)
        {
            var q = _context.Phongs.AsNoTracking().Include(x => x.LoaiPhong).AsQueryable();

            if (loaiPhongId.HasValue) q = q.Where(x => x.LoaiPhongId == loaiPhongId.Value);
            if (trangThai.HasValue) q = q.Where(x => x.TrangThai == trangThai.Value);

            var data = await q.OrderBy(x => x.SoPhong)
                .Select(x => new
                {
                    x.Id,
                    x.SoPhong,
                    x.Tang,
                    TrangThai = x.TrangThai.ToString(),
                    LoaiPhong = new
                    {
                        x.LoaiPhong!.Id,
                        x.LoaiPhong.TenLoai,
                        x.LoaiPhong.GiaMoiDem,
                        x.LoaiPhong.SoNguoi
                    }
                })
                .ToListAsync();

            return Ok(data);
        }

        /// <summary>Chi tiết phòng</summary>
        [AllowAnonymous]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var x = await _context.Phongs
                .AsNoTracking()
                .Include(p => p.LoaiPhong)
                .Where(p => p.Id == id)
                .Select(p => new
                {
                    p.Id,
                    p.SoPhong,
                    p.Tang,
                    TrangThai = p.TrangThai.ToString(),
                    LoaiPhong = new
                    {
                        p.LoaiPhong!.Id,
                        p.LoaiPhong.TenLoai,
                        p.LoaiPhong.GiaMoiDem,
                        p.LoaiPhong.SoNguoi
                    }
                })
                .FirstOrDefaultAsync();

            if (x == null) return NotFound(new { message = "Không tìm thấy phòng." });
            return Ok(x);
        }

        /// <summary>Tạo phòng</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RoomUpsertRequest req)
        {
            req.SoPhong = req.SoPhong.Trim();

            bool soPhongTrung = await _context.Phongs.AnyAsync(p => p.SoPhong == req.SoPhong);
            if (soPhongTrung) return Conflict(new { message = "Số phòng đã tồn tại." });

            bool loaiTonTai = await _context.LoaiPhongs.AnyAsync(lp => lp.Id == req.LoaiPhongId);
            if (!loaiTonTai) return NotFound(new { message = "Không tìm thấy loại phòng." });

            var entity = new Phong
            {
                SoPhong = req.SoPhong,
                Tang = req.Tang,
                TrangThai = req.TrangThai ?? TrangThaiPhong.Trong,
                LoaiPhongId = req.LoaiPhongId
            };

            _context.Phongs.Add(entity);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, new
            {
                entity.Id,
                entity.SoPhong,
                entity.Tang,
                TrangThai = entity.TrangThai.ToString(),
                entity.LoaiPhongId
            });
        }

        /// <summary>Sửa phòng</summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] RoomUpsertRequest req)
        {
            req.SoPhong = req.SoPhong.Trim();

            var entity = await _context.Phongs.FirstOrDefaultAsync(p => p.Id == id);
            if (entity == null) return NotFound(new { message = "Không tìm thấy phòng." });

            bool soPhongTrung = await _context.Phongs.AnyAsync(p => p.SoPhong == req.SoPhong && p.Id != id);
            if (soPhongTrung) return Conflict(new { message = "Số phòng đã tồn tại." });

            bool loaiTonTai = await _context.LoaiPhongs.AnyAsync(lp => lp.Id == req.LoaiPhongId);
            if (!loaiTonTai) return NotFound(new { message = "Không tìm thấy loại phòng." });

            entity.SoPhong = req.SoPhong;
            entity.Tang = req.Tang;
            entity.TrangThai = req.TrangThai ?? entity.TrangThai;
            entity.LoaiPhongId = req.LoaiPhongId;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                entity.Id,
                entity.SoPhong,
                entity.Tang,
                TrangThai = entity.TrangThai.ToString(),
                entity.LoaiPhongId
            });
        }

        /// <summary>Xóa phòng</summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.Phongs.FirstOrDefaultAsync(p => p.Id == id);
            if (entity == null) return NotFound(new { message = "Không tìm thấy phòng." });

            bool hasBookings = await _context.DatPhongs.AnyAsync(dp => dp.PhongId == id);
            if (hasBookings)
                return Conflict(new { message = "Không thể xóa phòng vì đã có đơn đặt phòng liên quan." });

            _context.Phongs.Remove(entity);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa phòng.", id });
        }

        /// <summary>Tìm phòng trống theo ngày</summary>
        [AllowAnonymous]
        [HttpGet("available")]
        public async Task<IActionResult> GetAvailable(
            [FromQuery] DateTime ngayNhan,
            [FromQuery] DateTime ngayTra,
            [FromQuery] int? loaiPhongId,
            [FromQuery] int? soNguoi
        )
        {
            if (ngayNhan == default || ngayTra == default)
                return BadRequest(new { message = "Thiếu ngày nhận hoặc ngày trả." });

            if (ngayNhan.Date < DateTime.Today)
                return BadRequest(new { message = "Ngày nhận phòng phải >= hôm nay." });

            if (ngayTra.Date <= ngayNhan.Date)
                return BadRequest(new { message = "Ngày trả phòng phải > ngày nhận phòng." });

            if (soNguoi.HasValue && soNguoi.Value <= 0)
                return BadRequest(new { message = "Số người phải >= 1." });

            if (loaiPhongId.HasValue)
            {
                var lp = await _context.LoaiPhongs.AsNoTracking().FirstOrDefaultAsync(x => x.Id == loaiPhongId.Value);
                if (lp == null) return NotFound(new { message = "Không tìm thấy loại phòng." });

                if (soNguoi.HasValue && soNguoi.Value > lp.SoNguoi)
                {
                    return BadRequest(new
                    {
                        message = $"Loại phòng '{lp.TenLoai}' tối đa {lp.SoNguoi} người.",
                        loaiPhongId = lp.Id,
                        tenLoai = lp.TenLoai,
                        sucChuaToiDa = lp.SoNguoi,
                        soNguoiYeuCau = soNguoi.Value
                    });
                }
            }

            var query = _context.Phongs
                .AsNoTracking()
                .Include(p => p.LoaiPhong)
                .Where(p => p.TrangThai != TrangThaiPhong.BaoTri);

            if (loaiPhongId.HasValue)
                query = query.Where(p => p.LoaiPhongId == loaiPhongId.Value);

            if (soNguoi.HasValue)
                query = query.Where(p => p.LoaiPhong!.SoNguoi >= soNguoi.Value);

            query = query.Where(p =>
                !_context.DatPhongs.Any(dp =>
                    dp.PhongId == p.Id
                    && (dp.TrangThai == TrangThaiDatPhong.DaDat || dp.TrangThai == TrangThaiDatPhong.DaNhanPhong)
                    && ngayNhan < dp.NgayTraPhong
                    && ngayTra > dp.NgayNhanPhong
                ));

            var rooms = await query.OrderBy(p => p.SoPhong)
                .Select(p => new
                {
                    p.Id,
                    p.SoPhong,
                    p.Tang,
                    LoaiPhong = new
                    {
                        p.LoaiPhong!.Id,
                        p.LoaiPhong.TenLoai,
                        p.LoaiPhong.GiaMoiDem,
                        p.LoaiPhong.SoNguoi
                    }
                })
                .ToListAsync();

            return Ok(new
            {
                NgayNhan = ngayNhan.Date,
                NgayTra = ngayTra.Date,
                SoDem = (ngayTra.Date - ngayNhan.Date).Days,
                SoPhongTrong = rooms.Count,
                Rooms = rooms
            });
        }
    }
}