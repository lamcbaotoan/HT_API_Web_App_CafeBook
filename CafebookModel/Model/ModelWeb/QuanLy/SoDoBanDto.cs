// Tập tin: CafebookModel/Model/ModelWeb/QuanLy/SoDoBanDto.cs
using System.Collections.Generic;

namespace CafebookModel.Model.ModelWeb.QuanLy
{
    /// <summary>
    /// DTO cho một Bàn (hiển thị trên lưới)
    /// </summary>
    public class BanDto
    {
        public int IdBan { get; set; }
        public string SoBan { get; set; } = string.Empty;
        public string TrangThai { get; set; } = string.Empty;
        public string? GhiChu { get; set; }
        public int IdKhuVuc { get; set; }
        public decimal TongTien { get; set; }

        // --- BỔ SUNG MỚI (Từ logic WPF) ---
        public int? IdHoaDonHienTai { get; set; }
        public string? ThongTinDatBan { get; set; }
    }

    /// <summary>
    /// DTO cho một Khu Vực (hiển thị trên tab)
    /// </summary>
    public class KhuVucDto
    {
        public int IdKhuVuc { get; set; }
        public string TenKhuVuc { get; set; } = string.Empty;
        public List<BanDto> Bans { get; set; } = new();
    }

    // --- BỔ SUNG MỚI (DTO cho các hành động) ---

    /// <summary>
    /// DTO để tạo Hóa đơn / Mở bàn
    /// </summary>
    public class CreateOrderRequestDto
    {
        public int IdBan { get; set; }
        public int IdNhanVien { get; set; }
    }

    /// <summary>
    /// DTO trả về ID hóa đơn vừa tạo
    /// </summary>
    public class CreateOrderResponseDto
    {
        public int IdHoaDon { get; set; }
    }

    /// <summary>
    /// DTO để báo cáo sự cố
    /// </summary>
    public class ReportProblemRequestDto
    {
        public int IdBan { get; set; }
        public int IdNhanVien { get; set; }
        public string GhiChu { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO để Chuyển bàn hoặc Gộp bàn
    /// </summary>
    public class BanActionRequestDto
    {
        public int IdHoaDonNguon { get; set; }
        public int IdBanDich { get; set; } // Dùng cho Chuyển Bàn
        public int? IdHoaDonDich { get; set; } // Dùng cho Gộp Bàn
    }
}