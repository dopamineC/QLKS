/**
 * Module: AdminUserEditVM
 * Chức năng: ViewModel chứa dữ liệu form cập nhật người dùng
 * Người phụ trách: An
 */
using System.ComponentModel.DataAnnotations;
using QLKS.Models;

namespace QLKS.ViewModels
{
    // ViewModel dùng để nhận và hiển thị dữ liệu cho form chỉnh sửa thông tin người dùng
    public class AdminUserEditVM
    {
        // ID của người dùng cần chỉnh sửa
        public int Id { get; set; }

        // Tên đăng nhập (thường không cho phép sửa, chỉ để hiển thị)
        [Display(Name = "Tên đăng nhập")]
        public string TenDangNhap { get; set; } = string.Empty;

        // Họ và tên đầy đủ của người dùng, bắt buộc
        [Display(Name = "Họ tên")]
        [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        [StringLength(100, ErrorMessage = "Họ tên tối đa {1} ký tự.")]
        public string HoTen { get; set; } = string.Empty;

        // Số điện thoại liên lạc của người dùng
        [Display(Name = "Số điện thoại")]
        [StringLength(20, ErrorMessage = "Số điện thoại tối đa {1} ký tự.")]
        public string? SoDienThoai { get; set; }

        // Địa chỉ email của người dùng, phải đúng định dạng
        [Display(Name = "Email")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        [StringLength(100, ErrorMessage = "Email tối đa {1} ký tự.")]
        public string? Email { get; set; }

        // Căn cước công dân hoặc chứng minh nhân dân
        [Display(Name = "CCCD")]
        [StringLength(20, ErrorMessage = "CCCD tối đa {1} ký tự.")]
        public string? CCCD { get; set; }

        // Vai trò của tài khoản (VD: Admin, Manager, Customer)
        [Display(Name = "Vai trò")]
        public VaiTro VaiTro { get; set; } = VaiTro.Customer;

        // Trạng thái hoạt động của tài khoản (true = Kích hoạt, false = Khóa)
        [Display(Name = "Trạng thái")]
        public bool TrangThai { get; set; } = true;
    }
}