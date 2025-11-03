using System;
using System.Collections.Generic;

namespace CafebookModel.Model.ModelApp
{
    /// <summary>
    /// DTO để gửi yêu cầu lọc (filters) lên API
    /// </summary>
    public class BaoCaoTonKhoRequestDto
    {
        public string? SearchText { get; set; }
        public int? NhaCungCapId { get; set; }
        public bool ShowLowStockOnly { get; set; }
    }

    /// <summary>
    /// DTO chứa toàn bộ kết quả báo cáo Kho
    /// </summary>
    public class BaoCaoTonKhoTongHopDto
    {
        public BaoCaoTonKhoKpiDto Kpi { get; set; } = new();
        public List<BaoCaoTonKhoChiTietDto> ChiTietTonKho { get; set; } = new();
        public List<BaoCaoKiemKeDto> LichSuKiemKe { get; set; } = new();
        public List<BaoCaoHuyHangDto> LichSuHuyHang { get; set; } = new();
    }

    // -- Các lớp con cho báo cáo chi tiết --

    // Dùng cho KPI
    public class BaoCaoTonKhoKpiDto
    {
        public decimal TongGiaTriTonKho { get; set; }
        public int SoLuongSPSapHet { get; set; }
        public decimal TongGiaTriDaHuy { get; set; }
    }

    // Dùng cho Tab 1
    public class BaoCaoTonKhoChiTietDto
    {
        public string TenNguyenLieu { get; set; } = string.Empty;
        public string DonViTinh { get; set; } = string.Empty; 
        public decimal TonKho { get; set; }
        public decimal TonKhoToiThieu { get; set; }
        public string TinhTrang { get; set; } = string.Empty;
    }

    // Dùng cho Tab 2
    public class BaoCaoKiemKeDto
    {
        public DateTime NgayKiem { get; set; }
        public string TenNguyenLieu { get; set; } = string.Empty; 
        public decimal TonKhoHeThong { get; set; }
        public decimal TonKhoThucTe { get; set;}
        public decimal ChenhLech { get; set; }
        public string? LyDoChenhLech { get; set; }
    }

    // Dùng cho Tab 3
    public class BaoCaoHuyHangDto
    {
        public DateTime NgayHuy { get; set; }
        public string TenNguyenLieu { get; set; } = string.Empty; 
        public decimal SoLuongHuy { get; set; }
        public decimal GiaTriHuy { get; set; }
        public string LyDoHuy { get; set; } = string.Empty; 
    }
}