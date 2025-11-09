// Tập tin: CafebookModel/Model/ModelWeb/LichSuPhieuThueDto.cs
using System;

namespace CafebookModel.Model.ModelWeb
{
    public class LichSuPhieuThueDto
    {
        public int IdPhieuThueSach { get; set; }
        public DateTime NgayThue { get; set; }
        public string TrangThai { get; set; } = string.Empty;
        public int SoLuongSach { get; set; }
        public decimal TongTienCoc { get; set; }

        // Thông tin từ PhieuTraSach (nếu đã trả)
        public DateTime? NgayTra { get; set; }
        public decimal? TongPhiThue { get; set; }
        public decimal? TongTienPhat { get; set; }
        public decimal? TongTienCocHoan { get; set; }
    }
}