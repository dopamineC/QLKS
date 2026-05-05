/**
 * Module: AdminLichDatPhongVM
 * Chức năng: ViewModel hỗ trợ hiển thị lịch đặt phòng dạng bảng lưới
 * Người phụ trách: Sơn
 */
using QLKS.Models;

namespace QLKS.ViewModels
{
    // ViewModel đại diện cho một ô (cell) trong bảng lịch đặt phòng của Admin
    public class AdminBookingCellVM
    {
        // ID của đơn đặt phòng tương ứng với ô này
        public int DatPhongId { get; set; }
        // Trạng thái hiện tại của đơn đặt phòng (Ví dụ: Đã xác nhận, Đã nhận phòng...)
        public TrangThaiDatPhong TrangThai { get; set; }
        // Tên khách hàng đặt phòng
        public string Khach { get; set; } = "";
        // Số điện thoại của khách hàng
        public string? SoDienThoai { get; set; }
        // Ngày nhận phòng
        public DateTime NgayNhan { get; set; }
        // Ngày trả phòng
        public DateTime NgayTra { get; set; }
        // Cờ đánh dấu nếu có sự trùng lặp (conflict) lịch đặt phòng tại ô này
        public bool IsConflict { get; set; } = false; // nếu bị trùng booking
    }

    // ViewModel dùng để hiển thị toàn bộ lịch đặt phòng dạng bảng (timeline)
    public class AdminLichDatPhongVM
    {
        // Ngày bắt đầu hiển thị trên lịch
        public DateTime Start { get; set; }
        // Số ngày hiển thị trên lịch (mặc định là 14 ngày)
        public int Days { get; set; } = 14;

        // Bộ lọc: ID loại phòng (nếu có)
        public int? LoaiPhongId { get; set; }
        // Bộ lọc: Số tầng (nếu có)
        public int? Tang { get; set; }
        // Bộ lọc: Trạng thái đặt phòng (nếu có)
        public TrangThaiDatPhong? TrangThai { get; set; }

        // Danh sách các ngày sẽ được hiển thị ở tiêu đề cột
        public List<DateTime> Dates { get; set; } = new();
        // Danh sách các loại phòng dùng cho bộ lọc hoặc nhóm dữ liệu
        public List<LoaiPhong> LoaiPhongs { get; set; } = new();
        // Danh sách các phòng hiển thị ở cột bên trái
        public List<Phong> Phongs { get; set; } = new();

        // Cấu trúc dữ liệu chứa các ô đặt phòng: roomId -> (ngày dạng yyyy-MM-dd -> thông tin ô)
        public Dictionary<int, Dictionary<string, AdminBookingCellVM>> Cells { get; set; } = new();
    }
}
