/**
 * Module: TimPhongTrongVM
 * Chức năng: ViewModel chứa bộ lọc tìm kiếm phòng trống
 * Người phụ trách: Sơn
 */
using System.ComponentModel.DataAnnotations;

namespace QLKS.ViewModels
{
    // ViewModel dùng để nhận tiêu chí tìm kiếm phòng trống từ người dùng
    public class TimPhongTrongVM
    {
        // Ngày mong muốn nhận phòng, mặc định là ngày mai
        [Required, DataType(DataType.Date)]
        public DateTime NgayNhan { get; set; } = DateTime.Today.AddDays(1);

        // Ngày mong muốn trả phòng, mặc định là ngày mốt (sau ngày nhận phòng 1 ngày)
        [Required, DataType(DataType.Date)]
        public DateTime NgayTra { get; set; } = DateTime.Today.AddDays(2);

        // ID loại phòng muốn tìm kiếm (Tùy chọn)
        public int? LoaiPhongId { get; set; }
        
        // Số lượng người dự kiến (Tùy chọn)
        public int? SoNguoi { get; set; }
    }
}
