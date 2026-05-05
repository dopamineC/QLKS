/**
 * Module: AdminUserCreateVM
 * Chức năng: ViewModel chứa dữ liệu form tạo người dùng mới
 * Người phụ trách: An
 */
using System.ComponentModel.DataAnnotations;
using QLKS.Models;

namespace QLKS.ViewModels
{
    // ViewModel dùng để nhận dữ liệu từ form tạo mới người dùng trong khu vực Admin
    public class AdminUserCreateVM
    {
        // Tên đăng nhập của tài khoản, bắt buộc và có độ dài tối đa 50 ký tự
        [Display(Name = "Tên đăng nhập")]
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
        [StringLength(50, ErrorMessage = "Tên đăng nhập tối đa {1} ký tự.")]
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

        // Vai trò của tài khoản (VD: Admin, Manager, Customer), mặc định là Customer
        [Display(Name = "Vai trò")]
        public VaiTro VaiTro { get; set; } = VaiTro.Customer;

        // Trạng thái hoạt động của tài khoản (true = Kích hoạt, false = Khóa)
        [Display(Name = "Trạng thái")]
        public bool TrangThai { get; set; } = true;

        // Mật khẩu cho tài khoản mới, yêu cầu tối thiểu 6 ký tự
        [Display(Name = "Mật khẩu")]
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất {1} ký tự.")]
        [DataType(DataType.Password)]
        public string MatKhau { get; set; } = string.Empty;

        // Xác nhận lại mật khẩu, phải khớp với trường MatKhau
        [Display(Name = "Xác nhận mật khẩu")]
        [Required(ErrorMessage = "Vui lòng nhập xác nhận mật khẩu.")]
        [Compare(nameof(MatKhau), ErrorMessage = "Xác nhận mật khẩu không khớp.")]
        [DataType(DataType.Password)]
        public string XacNhanMatKhau { get; set; } = string.Empty;
    }
}
