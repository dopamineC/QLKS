using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using QLKS.Data;
using QLKS.Models;
using System.ComponentModel.DataAnnotations;

namespace QLKS.Controllers.Api
{
    /// <summary>API Loại phòng</summary>
    [ApiController]
    [Route("api/room-types")]
    [Authorize(Roles = "Admin")]
    public class RoomTypesApiController : ControllerBase
    {
        private readonly ApplicationDbContext _context;

        public RoomTypesApiController(ApplicationDbContext context)
        {
            _context = context;
        }

        public class RoomTypeUpsertRequest
        {
            [Required, StringLength(100)]
            public string TenLoai { get; set; } = string.Empty;

            [Range(1000, 1000000000)]
            public decimal GiaMoiDem { get; set; }

            [Range(1, 20)]
            public int SoNguoi { get; set; }

            [StringLength(1000)]
            public string? MoTa { get; set; }

            [StringLength(255)]
            public string? HinhAnh { get; set; }
        }

        /// <summary>Lấy danh sách loại phòng</summary>
        [AllowAnonymous]
        [HttpGet]
        public async Task<IActionResult> GetAll()
        {
            var data = await _context.LoaiPhongs
                .AsNoTracking()
                .OrderBy(x => x.Id)
                .Select(x => new
                {
                    x.Id,
                    x.TenLoai,
                    x.GiaMoiDem,
                    x.SoNguoi,
                    x.MoTa,
                    x.HinhAnh
                })
                .ToListAsync();

            return Ok(data);
        }

        /// <summary>Lấy chi tiết loại phòng theo id</summary>
        [AllowAnonymous]
        [HttpGet("{id:int}")]
        public async Task<IActionResult> GetById(int id)
        {
            var x = await _context.LoaiPhongs
                .AsNoTracking()
                .Where(t => t.Id == id)
                .Select(t => new
                {
                    t.Id,
                    t.TenLoai,
                    t.GiaMoiDem,
                    t.SoNguoi,
                    t.MoTa,
                    t.HinhAnh
                })
                .FirstOrDefaultAsync();

            if (x == null) return NotFound(new { message = "Không tìm thấy loại phòng." });
            return Ok(x);
        }

        /// <summary>Thêm loại phòng</summary>
        [HttpPost]
        public async Task<IActionResult> Create([FromBody] RoomTypeUpsertRequest req)
        {
            var entity = new LoaiPhong
            {
                TenLoai = req.TenLoai.Trim(),
                GiaMoiDem = req.GiaMoiDem,
                SoNguoi = req.SoNguoi,
                MoTa = req.MoTa,
                HinhAnh = req.HinhAnh
            };

            _context.LoaiPhongs.Add(entity);
            await _context.SaveChangesAsync();

            return CreatedAtAction(nameof(GetById), new { id = entity.Id }, new
            {
                entity.Id,
                entity.TenLoai,
                entity.GiaMoiDem,
                entity.SoNguoi,
                entity.MoTa,
                entity.HinhAnh
            });
        }

        /// <summary>Sửa loại phòng</summary>
        [HttpPut("{id:int}")]
        public async Task<IActionResult> Update(int id, [FromBody] RoomTypeUpsertRequest req)
        {
            var entity = await _context.LoaiPhongs.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null) return NotFound(new { message = "Không tìm thấy loại phòng." });

            entity.TenLoai = req.TenLoai.Trim();
            entity.GiaMoiDem = req.GiaMoiDem;
            entity.SoNguoi = req.SoNguoi;
            entity.MoTa = req.MoTa;
            entity.HinhAnh = req.HinhAnh;

            await _context.SaveChangesAsync();

            return Ok(new
            {
                entity.Id,
                entity.TenLoai,
                entity.GiaMoiDem,
                entity.SoNguoi,
                entity.MoTa,
                entity.HinhAnh
            });
        }

        /// <summary>Xóa loại phòng</summary>
        [HttpDelete("{id:int}")]
        public async Task<IActionResult> Delete(int id)
        {
            var entity = await _context.LoaiPhongs.FirstOrDefaultAsync(x => x.Id == id);
            if (entity == null) return NotFound(new { message = "Không tìm thấy loại phòng." });

            bool hasRooms = await _context.Phongs.AnyAsync(p => p.LoaiPhongId == id);
            if (hasRooms)
                return Conflict(new { message = "Không thể xóa loại phòng vì đang có phòng thuộc loại này." });

            _context.LoaiPhongs.Remove(entity);
            await _context.SaveChangesAsync();

            return Ok(new { message = "Đã xóa loại phòng.", id });
        }
    }
}