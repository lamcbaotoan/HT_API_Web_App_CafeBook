// Tập tin: CafebookModel/Model/ModelWeb/LichSuDatBanDto.cs
using System;

namespace CafebookModel.Model.ModelWeb
{
    public class LichSuDatBanDto
    {
        public int IdPhieuDatBan { get; set; }
        public string TenBan { get; set; } = string.Empty;
        public DateTime ThoiGianDat { get; set; }
        public int SoLuongKhach { get; set; }
        public string TrangThai { get; set; } = string.Empty;
        public string? GhiChu { get; set; }
    }
}