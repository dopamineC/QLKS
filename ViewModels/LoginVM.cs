/**
 * Module: LoginVM
 * Chức năng: ViewModel chứa dữ liệu đầu vào cho form đăng nhập
 * Người phụ trách: An
 */
using System.ComponentModel.DataAnnotations;

namespace QLKS.ViewModels
{
    // ViewModel dùng để xử lý dữ liệu đăng nhập của người dùng
    public class LoginVM
    {
        // Tên đăng nhập của người dùng, bắt buộc
        [Display(Name = "Tên đăng nhập")]
        [Required(ErrorMessage = "Vui lòng nhập tên đăng nhập.")]
        public string TenDangNhap { get; set; } = string.Empty;

        // Mật khẩu của tài khoản, hiển thị dạng password
        [Display(Name = "Mật khẩu")]
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu.")]
        [DataType(DataType.Password)]
        public string MatKhau { get; set; } = string.Empty;

        // Cho phép lưu trạng thái đăng nhập (Remember Me) trên thiết bị
        [Display(Name = "Ghi nhớ đăng nhập")]
        public bool GhiNho { get; set; } = false;

        // Đường dẫn trả về sau khi đăng nhập thành công (nếu người dùng bị chuyển hướng từ một trang yêu cầu quyền truy cập)
        public string? ReturnUrl { get; set; }
    }
}