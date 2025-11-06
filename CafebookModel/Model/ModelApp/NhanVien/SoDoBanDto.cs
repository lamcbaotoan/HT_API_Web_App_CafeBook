using System.Collections.Generic;

namespace CafebookModel.Model.ModelApp.NhanVien
{
    /// <summary>
    /// DTO để hiển thị một thẻ 'Bàn' trên sơ đồ
    /// </summary>
    public class BanSoDoDto
    {
        public int IdBan { get; set; }
        public string SoBan { get; set; } = string.Empty;
        public string TrangThai { get; set; } = string.Empty;
        public string? GhiChu { get; set; }
        public decimal TongTienHienTai { get; set; }
        public int? IdHoaDonHienTai { get; set; }

        // --- ĐÃ THÊM ---
        public int? IdKhuVuc { get; set; }
    }

    /// <summary>
    /// DTO dùng để báo cáo sự cố (Khóa bàn)
    /// </summary>
    public class BaoCaoSuCoRequestDto
    {
        public string GhiChuSuCo { get; set; } = string.Empty;
    }

    /// <summary>
    // DTO cho các thao tác phức tạp (Chuyển/Gộp bàn)
    /// </summary>
    public class BanActionRequestDto
    {
        public int IdHoaDonNguon { get; set; } // Hóa đơn/Bàn gốc
        public int IdBanDich { get; set; }     // Bàn chuyển đến
        public int? IdHoaDonDich { get; set; } // Hóa đơn (nếu gộp)
    }
}