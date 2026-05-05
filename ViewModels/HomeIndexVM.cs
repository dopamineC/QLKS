/**
 * Module: HomeIndexVM
 * Chức năng: ViewModel chứa dữ liệu cần thiết để hiển thị trang chủ
 * Người phụ trách: Đức
 */
using QLKS.Models;

namespace QLKS.ViewModels
{
    // ViewModel dùng để cung cấp dữ liệu cho trang chủ (Home/Index)
    public class HomeIndexVM
    {
        // Form tìm kiếm phòng trống với các tiêu chí cơ bản
        public TimPhongTrongVM TimPhong { get; set; } = new();
        
        // Danh sách các loại phòng nổi bật hoặc có sẵn để hiển thị cho khách hàng xem
        public List<LoaiPhong> LoaiPhongs { get; set; } = new();
    }
}
