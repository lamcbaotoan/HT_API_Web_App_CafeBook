namespace CafebookModel.Model.ModelWeb
{
    public class DangKyRequestModel
    {
        public string HoTen { get; set; } = string.Empty;
        public string SoDienThoai { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string? TenDangNhap { get; set; }
        public string Password { get; set; } = string.Empty;
    }
}