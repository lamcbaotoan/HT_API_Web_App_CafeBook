using System;
using System.Collections.Generic;

namespace CafebookModel.Model.ModelApp
{
    /// <summary>
    /// DTO cho một bàn (Node con)
    /// </summary>
    public class BanDto
    {
        public int IdBan { get; set; }
        public string SoBan { get; set; } = string.Empty;
        public int SoGhe { get; set; }
        public string TrangThai { get; set; } = string.Empty;
        public string? GhiChu { get; set; }
        public int IdKhuVuc { get; set; }
    }

    /// <summary>
    /// DTO cho một khu vực (Node cha), chứa danh sách các bàn
    /// </summary>
    public class KhuVucDto
    {
        public int IdKhuVuc { get; set; }
        public string TenKhuVuc { get; set; } = string.Empty;
        public string? MoTa { get; set; }
        public List<BanDto> Bans { get; set; } = new List<BanDto>();
    }

    /// <summary>
    /// DTO cho chức năng "Xem Lịch sử" (Advanced Function 3)
    /// </summary>
    public class BanHistoryDto
    {
        public int SoLuotPhucVu { get; set; }
        public decimal TongDoanhThu { get; set; }
        public int SoLuotDatTruoc { get; set; }
    }

    /// <summary>
    /// DTO dùng để gửi dữ liệu CẬP NHẬT/THÊM MỚI Bàn
    /// </summary>
    public class BanUpdateRequestDto
    {
        public string SoBan { get; set; } = string.Empty;
        public int SoGhe { get; set; }
        public int IdKhuVuc { get; set; }
        public string TrangThai { get; set; } = string.Empty;
        public string? GhiChu { get; set; }
    }

    /// <summary>
    /// DTO dùng để gửi dữ liệu CẬP NHẬT/THÊM MỚI Khu Vực
    /// </summary>
    public class KhuVucUpdateRequestDto
    {
        public string TenKhuVuc { get; set; } = string.Empty;
        public string? MoTa { get; set; }
    }
}