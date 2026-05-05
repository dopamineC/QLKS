/**
 * Module: AdminEditBookingVM
 * Chức năng: ViewModel chứa dữ liệu cập nhật đơn đặt phòng
 * Người phụ trách: Sơn
 */
using System;
using System.ComponentModel.DataAnnotations;

namespace QLKS.ViewModels
{
    // ViewModel dùng để nhận và hiển thị dữ liệu cho form chỉnh sửa đặt phòng
    public class AdminEditBookingVM
    {
        // ID của đơn đặt phòng cần chỉnh sửa
        public int Id { get; set; }

        // ID của phòng được chọn hoặc đổi sang (Bắt buộc)
        [Required]
        public int PhongId { get; set; }

        // Ngày nhận phòng mới (Bắt buộc)
        [DataType(DataType.Date)]
        [Required]
        public DateTime NgayNhanPhong { get; set; }

        // Ngày trả phòng mới (Bắt buộc)
        [DataType(DataType.Date)]
        [Required]
        public DateTime NgayTraPhong { get; set; }

        // Các thuộc tính dưới đây chỉ dùng để hiển thị trên View, không dùng để bind dữ liệu lưu trữ
        // Tên của khách hàng
        public string? TenKhach { get; set; }
        // Số phòng hiện tại
        public string? SoPhong { get; set; }
        // Tên loại phòng
        public string? TenLoaiPhong { get; set; }
        // Giá cho mỗi đêm của phòng
        public decimal GiaMoiDem { get; set; }
    }
}
