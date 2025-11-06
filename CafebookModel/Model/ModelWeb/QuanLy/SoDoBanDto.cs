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
        public string? GhiChu { get; set; } // <-- THÊM MỚI
        public int IdKhuVuc { get; set; }
        public decimal TongTien { get; set; } // Vẫn giữ TongTien (dùng cho WPF, web ẩn đi)
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
}