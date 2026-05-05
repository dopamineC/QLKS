/**
 * Module: HoaDon
 * Chức năng: Model đại diện cho hoá đơn thanh toán của khách hàng
 * Người phụ trách: Đức
 */
using System.ComponentModel.DataAnnotations;

namespace QLKS.Models
{
    // Model đại diện cho hoá đơn thanh toán của khách hàng.
    public class HoaDon
    {
        // Mã hoá đơn (Khoá chính)
        public int Id { get; set; }

        // Mã đơn đặt phòng tương ứng (Khoá ngoại liên kết tới bảng DatPhong)
        [Display(Name = "Đặt phòng")]
        [Required(ErrorMessage = "Vui lòng chọn đơn đặt phòng.")]
        public int DatPhongId { get; set; }

        // Đối tượng đơn đặt phòng tương ứng (Navigation property)
        public DatPhong? DatPhong { get; set; }

        // Tổng số tiền khách hàng phải thanh toán
        [Display(Name = "Số tiền")]
        [Range(0, 1000000000000, ErrorMessage = "Số tiền phải từ 0đ đến 1.000.000.000.000đ.")]
        public decimal SoTien { get; set; }

        // Trạng thái của hoá đơn (Chưa thanh toán, Đã thanh toán, Đã huỷ)
        [Display(Name = "Trạng thái")]
        public TrangThaiHoaDon TrangThai { get; set; } = TrangThaiHoaDon.ChuaThanhToan;

        // Hình thức thanh toán khách hàng lựa chọn (Tiền mặt, Chuyển khoản)
        [Display(Name = "Phương thức")]
        public PhuongThucThanhToan PhuongThuc { get; set; } = PhuongThucThanhToan.TienMat;

        // Thời điểm hoàn tất việc thanh toán
        [Display(Name = "Ngày thanh toán")]
        public DateTime? NgayThanhToan { get; set; }
    }
}
