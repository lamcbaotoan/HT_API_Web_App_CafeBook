using System;

namespace CafebookModel.Model.ModelApp.NhanVien
{
    public class GiaoHangDto
    {
        public int IdHoaDon { get; set; }
        public string? TenKhachHang { get; set; }
        public string? SDTKhachHang { get; set; }
        public string? DiaChiGiaoHang { get; set; }
        public DateTime ThoiGianTao { get; set; }
        public string TrangThaiGiaoHang { get; set; } = string.Empty;
        public decimal ThanhTien { get; set; }
        public string GhiChu { get; set; } = string.Empty;
    }
}