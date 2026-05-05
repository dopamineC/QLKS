/**
 * Module: NguoiDung
 * Chức năng: Model đại diện cho thực thể tài khoản người dùng
 * Người phụ trách: An
 */
using System.ComponentModel.DataAnnotations;

namespace QLKS.Models
{
    // Model đại diện cho tài khoản người dùng đăng nhập hệ thống (bao gồm cả Admin và Khách hàng).
    public class NguoiDung
    {
        // Mã định danh người dùng (Khoá chính)
        public int Id { get; set; }

        // Tên đăng nhập tài khoản
        [Required, StringLength(50)]
        public string TenDangNhap { get; set; } = string.Empty;

        // Mật khẩu đã được mã hóa bảo mật (băm - hash)
        [Required]
        public string MatKhauHash { get; set; } = string.Empty;

        // Họ và tên đầy đủ
        [Required, StringLength(100)]
        public string HoTen { get; set; } = string.Empty;

        // Số điện thoại liên lạc
        [StringLength(20)]
        public string? SoDienThoai { get; set; }

        // Địa chỉ email
        [EmailAddress, StringLength(100)]
        public string? Email { get; set; }

        // Số Căn cước công dân hoặc Chứng minh nhân dân
        [StringLength(20)]
        public string? CCCD { get; set; }

        // Quyền hạn của người dùng (vd: Quản trị viên, Khách hàng)
        public VaiTro VaiTro { get; set; } = VaiTro.Customer;

        // Trạng thái hoạt động của tài khoản (true = Cho phép đăng nhập, false = Khoá)
        public bool TrangThai { get; set; } = true;

        // Ngày giờ tài khoản được tạo trên hệ thống
        public DateTime NgayTao { get; set; } = DateTime.UtcNow;

        // Danh sách các đơn đặt phòng thuộc về người dùng này (Navigation property)
        public ICollection<DatPhong> DatPhongs { get; set; } = new List<DatPhong>();
    }
}
