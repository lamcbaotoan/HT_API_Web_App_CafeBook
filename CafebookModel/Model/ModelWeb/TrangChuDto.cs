namespace CafebookModel.Model.ModelWeb
{
    // Lớp cha chứa tất cả
    public class TrangChuDto
    {
        public ThongTinChungDto? Info { get; set; }
        public List<KhuyenMaiDto> Promotions { get; set; } = new();
        public List<SanPhamDto> MonNoiBat { get; set; } = new();
        public List<SachDto> SachNoiBat { get; set; } = new();
    }

    // Lớp cho Model.Info
    public class ThongTinChungDto
    {
        public string TenQuan { get; set; } = "Cafebook";
        public string? GioiThieu { get; set; }
        public string? BannerImageUrl { get; set; }
        public int SoBanTrong { get; set; }
        public int SoSachDangDuocThue { get; set; }
        public string? DiaChi { get; set; }
        public string? SoDienThoai { get; set; }
        public string? EmailLienHe { get; set; }
        public string? GioMoCua { get; set; }
    }

    // Lớp cho Model.Promotions
    public class KhuyenMaiDto
    {
        public string TenKhuyenMai { get; set; } = string.Empty;
        public string? MoTa { get; set; }
        public string? dieuKienApDung { get; set; }
    }

    // Lớp cho Model.MonNoiBat
    public class SanPhamDto
    {
        public string TenSanPham { get; set; } = string.Empty;
        public string? AnhSanPhamBase64 { get; set; }
        public decimal DonGia { get; set; }
    }

    // Lớp cho Model.SachNoiBat
    public class SachDto
    {
        public int IdSach { get; set; }
        public string TieuDe { get; set; } = string.Empty;
        public string? TacGia { get; set; }
        public string? AnhBia { get; set; } // Base64
    }
}