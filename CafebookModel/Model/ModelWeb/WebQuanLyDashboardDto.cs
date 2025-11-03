namespace CafebookModel.Model.ModelWeb
{
    /// <summary>
    /// DTO chứa dữ liệu cho trang Dashboard của Nhân viên trên Web
    /// </summary>
    public class WebQuanLyDashboardDto
    {
        public string? HoTen { get; set; }
        public string? VaiTro { get; set; }
        public string? AnhDaiDien { get; set; } // Base64
        public string? CaHienTai { get; set; }
        public int TongBanDangPhucVu { get; set; }
        public int TongDonDangXuLy { get; set; }
    }
}