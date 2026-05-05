using System.ComponentModel.DataAnnotations;

namespace QLKS.Models
{
    // Phân quyền các vai trò của người dùng trong hệ thống
    public enum VaiTro
    {
        [Display(Name = "Quản trị")] Admin = 1,
        [Display(Name = "Khách hàng")] Customer = 2
    }

    // Tình trạng hiện tại của phòng vật lý
    public enum TrangThaiPhong
    {
        [Display(Name = "Trống")] Trong = 1,
        [Display(Name = "Đang sử dụng")] DangSuDung = 2,
        [Display(Name = "Bảo trì")] BaoTri = 3
    }

    // Các trạng thái tiến trình của một đơn đặt phòng
    public enum TrangThaiDatPhong
    {
        [Display(Name = "Đã đặt")] DaDat = 1,
        [Display(Name = "Đã nhận phòng")] DaNhanPhong = 2,
        [Display(Name = "Đã trả phòng")] DaTraPhong = 3,
        [Display(Name = "Đã huỷ")] DaHuy = 4
    }

    // Các trạng thái thanh toán của một hoá đơn
    public enum TrangThaiHoaDon
    {
        [Display(Name = "Chưa thanh toán")] ChuaThanhToan = 1,
        [Display(Name = "Đã thanh toán")] DaThanhToan = 2,
        [Display(Name = "Đã huỷ")] DaHuy = 3
    }

    // Các phương thức thanh toán được hỗ trợ
    public enum PhuongThucThanhToan
    {
        [Display(Name = "Tiền mặt")] TienMat = 1,
        [Display(Name = "Chuyển khoản")] ChuyenKhoan = 2
    }
}
