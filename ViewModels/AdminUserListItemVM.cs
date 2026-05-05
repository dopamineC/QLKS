/**
 * Module: AdminUserListItemVM
 * Chức năng: ViewModel hiển thị danh sách người dùng trong trang quản trị
 * Người phụ trách: An
 */
using QLKS.Models;

namespace QLKS.ViewModels
{
    // ViewModel dùng để hiển thị thông tin tóm tắt của một người dùng trên danh sách (dạng bảng)
    public class AdminUserListItemVM
    {
        // ID của người dùng
        public int Id { get; set; }
        // Tên đăng nhập của tài khoản
        public string TenDangNhap { get; set; } = string.Empty;
        // Họ và tên người dùng
        public string HoTen { get; set; } = string.Empty;
        // Số điện thoại liên hệ
        public string? SoDienThoai { get; set; }
        // Địa chỉ email
        public string? Email { get; set; }
        // Vai trò của tài khoản trong hệ thống
        public VaiTro VaiTro { get; set; }
        // Trạng thái hoạt động (true/false)
        public bool TrangThai { get; set; }
        // Ngày tạo tài khoản
        public DateTime NgayTao { get; set; }

        // Số lượng đơn đặt phòng mà người dùng này đã thực hiện
        public int SoDatPhong { get; set; }
    }
}
