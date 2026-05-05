/**
 * Module: Phong
 * Chức năng: Model đại diện cho một căn phòng vật lý cụ thể trong khách sạn
 * Người phụ trách: Việt
 */
using System.ComponentModel.DataAnnotations;

namespace QLKS.Models
{
    // Model đại diện cho một căn phòng vật lý cụ thể trong khách sạn.
    public class Phong
    {
        // Mã phòng (Khoá chính)
        public int Id { get; set; }

        // Mã số/Ký hiệu của phòng dùng để nhận diện (vd: P101, P102)
        [Display(Name = "Số phòng")]
        [Required(ErrorMessage = "Vui lòng nhập số phòng.")]
        [StringLength(20, ErrorMessage = "Số phòng tối đa {1} ký tự.")]
        public string SoPhong { get; set; } = string.Empty;

        // Tầng chứa phòng
        [Display(Name = "Tầng")]
        [Range(1, 200, ErrorMessage = "Tầng phải từ {1} đến {2}.")]
        public int Tang { get; set; } = 1;

        // Hiện trạng thực tế của phòng (Trống, Đang sử dụng, Đang bảo trì sửa chữa)
        [Display(Name = "Trạng thái")]
        public TrangThaiPhong TrangThai { get; set; } = TrangThaiPhong.Trong;

        // ID loại phòng để xác định các đặc điểm và mức giá (Khoá ngoại liên kết tới bảng LoaiPhong)
        [Display(Name = "Loại phòng")]
        [Required(ErrorMessage = "Vui lòng chọn loại phòng.")]
        public int LoaiPhongId { get; set; }

        // Đối tượng loại phòng chứa thông tin chi tiết (Navigation property)
        public LoaiPhong? LoaiPhong { get; set; }

        // Danh sách lịch sử các lượt đặt phòng đối với phòng này (Navigation property)
        public ICollection<DatPhong> DatPhongs { get; set; } = new List<DatPhong>();
    }
}
