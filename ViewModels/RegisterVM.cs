/**
 * Module: RegisterVM
 * Chức năng: ViewModel chứa dữ liệu đầu vào cho form đăng ký
 * Người phụ trách: An
 */
using System.ComponentModel.DataAnnotations;

namespace QLKS.ViewModels
{
    // ViewModel dùng để nhận và xác thực dữ liệu từ form đăng ký tài khoản khách hàng mới
    public class RegisterVM
    {
        // Tên đăng nhập mong muốn, bắt buộc, tối đa 50 ký tự
        [Display(Name = "Tên đăng nhập")]
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
        [StringLength(50, ErrorMessage = "Tên đăng nhập tối đa {1} ký tự.")]
        public string TenDangNhap { get; set; } = string.Empty;

        // Họ và tên đầy đủ của người đăng ký
        [Display(Name = "Họ tên")]
        [Required(ErrorMessage = "Vui lòng nhập họ tên.")]
        [StringLength(100, ErrorMessage = "Họ tên tối đa {1} ký tự.")]
        public string HoTen { get; set; } = string.Empty;

        // Mật khẩu cho tài khoản, yêu cầu tối thiểu 6 ký tự
        [Display(Name = "Mật khẩu")]
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [MinLength(6, ErrorMessage = "Mật khẩu phải có ít nhất {1} ký tự.")]
        public string MatKhau { get; set; } = string.Empty;

        // Xác nhận lại mật khẩu để đảm bảo người dùng nhập đúng
        [Display(Name = "Xác nhận mật khẩu")]
        [Required(ErrorMessage = "Vui lòng nhập xác nhận mật khẩu.")]
        [Compare(nameof(MatKhau), ErrorMessage = "Xác nhận mật khẩu không khớp.")]
        public string XacNhanMatKhau { get; set; } = string.Empty;

        // Số điện thoại liên lạc
        [Display(Name = "Số điện thoại")]
        [StringLength(20, ErrorMessage = "Số điện thoại tối đa {1} ký tự.")]
        public string? SoDienThoai { get; set; }

        // Địa chỉ email hợp lệ
        [Display(Name = "Email")]
        [EmailAddress(ErrorMessage = "Email không đúng định dạng.")]
        [StringLength(100, ErrorMessage = "Email tối đa {1} ký tự.")]
        public string? Email { get; set; }

        // Số căn cước công dân hoặc CMND
        [Display(Name = "CCCD")]
        [StringLength(20, ErrorMessage = "CCCD tối đa {1} ký tự.")]
        public string? CCCD { get; set; }
    }
}