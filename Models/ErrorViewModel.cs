namespace QLKS.Models
{
    // Model chứa thông tin lỗi để hiển thị trên giao diện Error.
    public class ErrorViewModel
    {
        // ID của request gây ra lỗi
        public string? RequestId { get; set; }

        // Kiểm tra xem có RequestId để hiển thị hay không
        public bool ShowRequestId => !string.IsNullOrEmpty(RequestId);
    }
}
