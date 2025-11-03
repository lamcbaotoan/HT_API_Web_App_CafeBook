using System.Collections.Generic;

namespace CafebookModel.Model.ModelApp
{
    /// <summary>
    /// DTO để gửi yêu cầu (Từ ngày/Đến ngày) lên API
    /// </summary>
    public class BaoCaoRequestDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
    }

    /// <summary>
    /// DTO chứa toàn bộ kết quả báo cáo
    /// </summary>
    public class BaoCaoTongHopDto
    {
        public BaoCaoKpiDto Kpi { get; set; } = new();
        public BaoCaoChiTietDoanhThuDto ChiTietDoanhThu { get; set; } = new();
        public BaoCaoChiPhiDto ChiTietChiPhi { get; set; } = new();
        public List<TopSanPhamDto> TopSanPham { get; set; } = new();
    }

    // -- Các lớp con cho báo cáo chi tiết --

    public class BaoCaoKpiDto
    {
        public decimal DoanhThuRong { get; set; }
        public decimal TongGiaVon { get; set; }
        public decimal LoiNhuanGop { get; set; }
        public decimal ChiPhiOpex { get; set; }
        public decimal LoiNhuanRong { get; set; }
    }

    // Kết quả của truy vấn 3.1
    public class BaoCaoChiTietDoanhThuDto
    {
        public decimal TongDoanhThuGoc { get; set; }
        public decimal TongGiamGia { get; set; }
        public decimal TongPhuThu { get; set; }
        public decimal DoanhThuRong { get; set; }
        public int SoLuongHoaDon { get; set; }
        public decimal GiaTriTrungBinhHD { get; set; }
    }

    // Kết quả của truy vấn 3.2b
    public class OpexDto
    {
        public decimal TongChiPhiLuong { get; set; }
        public decimal TongChiPhiHuyHang { get; set; }
    }

    // DTO cho Tab 2
    public class BaoCaoChiPhiDto
    {
        public decimal TongGiaVon_COGS { get; set; }
        public decimal TongChiPhiLuong { get; set; }
        public decimal TongChiPhiHuyHang { get; set; }
    }

    // Kết quả của truy vấn 3.3
    public class TopSanPhamDto
    {
        public string TenSanPham { get; set; } = string.Empty;
        public int TongSoLuongBan { get; set; }
        public decimal TongDoanhThu { get; set; }
    }
}