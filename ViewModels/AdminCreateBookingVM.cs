/**
 * Module: AdminCreateBookingVM
 * Chức năng: ViewModel chứa dữ liệu tạo đơn đặt phòng mới
 * Người phụ trách: Sơn
 */
using System.ComponentModel.DataAnnotations;
using QLKS.Models;

namespace QLKS.ViewModels
{
    // ViewModel dùng để nhận dữ liệu từ form tạo đặt phòng mới trong khu vực Admin
    public class AdminCreateBookingVM
    {
        // ID của khách hàng đặt phòng (Bắt buộc)
        [Required]
        public int KhachHangId { get; set; }

        // ID của phòng được chọn (Bắt buộc)
        [Required]
        public int PhongId { get; set; }

        // Ngày dự kiến nhận phòng (Bắt buộc, kiểu ngày tháng)
        [Required, DataType(DataType.Date)]
        public DateTime NgayNhan { get; set; }

        // Ngày dự kiến trả phòng (Bắt buộc, kiểu ngày tháng)
        [Required, DataType(DataType.Date)]
        public DateTime NgayTra { get; set; }

        // Phương thức thanh toán được chọn, mặc định là Tiền mặt
        public PhuongThucThanhToan PhuongThuc { get; set; } = PhuongThucThanhToan.TienMat;
    }
}
