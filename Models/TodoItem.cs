/**
 * Module: TodoItem
 * Chức năng: Model quản lý công việc (Todo) của Admin
 * Người phụ trách: Đức
 */
using System.ComponentModel.DataAnnotations;

namespace QLKS.Models
{
    // Model quản lý công việc (Todo) của người dùng/Admin.
    public class TodoItem
    {
        // Mã công việc (Khoá chính)
        public int Id { get; set; }

        // Mã người dùng sở hữu công việc này (Khoá ngoại liên kết tới bảng NguoiDung)
        public int NguoiDungId { get; set; }
        
        // Đối tượng người dùng sở hữu (Navigation property)
        public NguoiDung? NguoiDung { get; set; }

        // Tiêu đề hoặc nội dung tóm tắt của công việc
        [Required, StringLength(200)]
        public string TieuDe { get; set; } = string.Empty;

        // Đánh dấu công việc đã hoàn thành hay chưa
        public bool DaXong { get; set; } = false;

        // Thời hạn hoàn thành công việc (Tuỳ chọn)
        public DateTime? Han { get; set; }

        // Thời điểm tạo công việc
        public DateTime NgayTao { get; set; } = DateTime.UtcNow;
        
        // Thời điểm cập nhật công việc lần cuối
        public DateTime? NgayCapNhat { get; set; }
    }
}
