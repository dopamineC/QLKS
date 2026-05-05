/**
 * Module: LoaiPhong
 * Chức năng: Model đại diện cho danh mục các loại phòng trong khách sạn
 * Người phụ trách: Việt
 */
using System.ComponentModel.DataAnnotations;

namespace QLKS.Models
{
    // Model đại diện cho danh mục các loại phòng trong khách sạn (vd: Phòng Đơn, Phòng Đôi, VIP).
    public class LoaiPhong
    {
        // Mã loại phòng (Khoá chính)
        public int Id { get; set; }

        // Tên gọi của loại phòng
        [Display(Name = "Tên loại")]
        [Required(ErrorMessage = "Vui lòng nhập tên loại phòng.")]
        [StringLength(100, ErrorMessage = "Tên loại tối đa {1} ký tự.")]
        public string TenLoai { get; set; } = string.Empty;

        // Giá niêm yết khi thuê qua đêm
        [Display(Name = "Giá/đêm (VND)")]
        [Range(1000, 1000000000, ErrorMessage = "Giá/đêm phải từ 1.000đ đến 1.000.000.000đ.")]
        public decimal GiaMoiDem { get; set; }

        // Số lượng khách tối đa được phép ở trong phòng
        [Display(Name = "Số người")]
        [Range(1, 20, ErrorMessage = "Số người phải từ 1 đến 20.")]
        public int SoNguoi { get; set; }

        // Đoạn văn bản mô tả các tiện ích, đặc điểm của loại phòng
        [Display(Name = "Mô tả")]
        [StringLength(1000, ErrorMessage = "Mô tả tối đa {1} ký tự.")]
        public string? MoTa { get; set; }

        // Tên file hoặc đường dẫn đến hình ảnh minh hoạ cho loại phòng
        [StringLength(255)]
        public string? HinhAnh { get; set; }

        // Danh sách các phòng thực tế thuộc loại phòng này (Navigation property)
        public ICollection<Phong> Phongs { get; set; } = new List<Phong>();
    }
}
