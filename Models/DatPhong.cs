/**
 * Module: DatPhong
 * Chức năng: Model đại diện cho thông tin đặt phòng của khách hàng
 * Người phụ trách: Sơn
 */
using System.ComponentModel.DataAnnotations;

namespace QLKS.Models
{
    // Model đại diện cho thông tin đặt phòng của khách hàng.
    public class DatPhong
    {
        // Mã đặt phòng (Khoá chính)
        public int Id { get; set; }

        // Mã khách hàng đặt phòng (Khoá ngoại liên kết tới bảng NguoiDung)
        [Display(Name = "Khách hàng")]
        [Required(ErrorMessage = "Vui lòng chọn khách hàng.")]
        public int KhachHangId { get; set; }
        
        // Đối tượng khách hàng đặt phòng (Navigation property)
        public NguoiDung? KhachHang { get; set; }

        // Mã phòng được đặt (Khoá ngoại liên kết tới bảng Phong)
        [Display(Name = "Phòng")]
        [Required(ErrorMessage = "Vui lòng chọn phòng.")]
        public int PhongId { get; set; }
        
        // Đối tượng phòng được đặt (Navigation property)
        public Phong? Phong { get; set; }

        // Ngày dự kiến nhận phòng
        [Display(Name = "Ngày nhận phòng")]
        [DataType(DataType.Date)]
        [Required(ErrorMessage = "Vui lòng chọn ngày nhận phòng.")]
        public DateTime NgayNhanPhong { get; set; }

        // Ngày dự kiến trả phòng
        [Display(Name = "Ngày trả phòng")]
        [DataType(DataType.Date)]
        [Required(ErrorMessage = "Vui lòng chọn ngày trả phòng.")]
        public DateTime NgayTraPhong { get; set; }

        // Tổng số đêm lưu trú (Dùng để tính tiền phòng)
        [Display(Name = "Số đêm")]
        [Range(1, 365, ErrorMessage = "Số đêm phải từ {1} đến {2}.")]
        public int SoDem { get; set; }

        // Tổng số tiền dự kiến hoặc đã tính cho đơn đặt phòng
        [Display(Name = "Tổng tiền")]
        [Range(0, 100000000000, ErrorMessage = "Tổng tiền phải từ 0đ đến 100.000.000.000đ.")]
        public decimal TongTien { get; set; }

        // Trạng thái hiện tại của đơn đặt phòng (Đã đặt, Đã nhận phòng, Đã trả phòng, Đã huỷ)
        [Display(Name = "Trạng thái")]
        public TrangThaiDatPhong TrangThai { get; set; } = TrangThaiDatPhong.DaDat;

        // Thời điểm tạo đơn đặt phòng trên hệ thống
        [Display(Name = "Ngày đặt")]
        public DateTime NgayDat { get; set; } = DateTime.UtcNow;

        // Hoá đơn liên kết với đơn đặt phòng này (Navigation property)
        public HoaDon? HoaDon { get; set; }
    }
}
