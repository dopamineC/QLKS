/**
 * Module: AdminResetPasswordVM
 * Chức năng: ViewModel chứa dữ liệu đặt lại mật khẩu cho người dùng
 * Người phụ trách: An
 */
using System.ComponentModel.DataAnnotations;

namespace QLKS.ViewModels
{
    // ViewModel dùng để xử lý yêu cầu đặt lại mật khẩu cho người dùng từ phía Admin
    public class AdminResetPasswordVM
    {
        // ID của người dùng cần đặt lại mật khẩu
        public int Id { get; set; }

        // Mật khẩu mới do Admin thiết lập (Yêu cầu độ dài tối thiểu 6 ký tự)
        [Required, MinLength(6)]
        [Display(Name = "Mật khẩu mới")]
        public string MatKhauMoi { get; set; } = string.Empty;

        // Xác nhận lại mật khẩu mới (Phải khớp với trường MatKhauMoi)
        [Required, Compare(nameof(MatKhauMoi))]
        [Display(Name = "Xác nhận mật khẩu")]
        public string XacNhanMatKhauMoi { get; set; } = string.Empty;
    }
}
