using CafebookModel.Model.Data; // Cho NhanVienDto
using CafebookModel.Model.ModelWeb; // Cho KhachHangDto

namespace CafebookModel.Model.ModelApi
{
    public class WebLoginResponseModel
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;

        // Dùng một trong hai
        public KhachHangDto? KhachHangData { get; set; }
        public NhanVienDto? NhanVienData { get; set; } // Tận dụng NhanVienDto từ WPF
    }
}