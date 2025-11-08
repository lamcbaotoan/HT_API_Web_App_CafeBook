using System;
using System.Collections.Generic;

namespace CafebookModel.Model.ModelApp
{
    /// <summary>
    /// DTO để gửi yêu cầu lọc (filters) lên API
    /// </summary>
    public class BaoCaoSachRequestDto
    {
        public string? SearchText { get; set; }
        public int? TheLoaiId { get; set; }
        public int? TacGiaId { get; set; }
    }

    /// <summary>
    /// DTO chứa dữ liệu cho các ComboBox lọc
    /// </summary>
    public class FilterLookupDto
    {
        public int Id { get; set; }
        public string Ten { get; set; } = string.Empty;
        public string? MoTa { get; set; } // <--- THÊM DÒNG NÀY
    }

    /// <summary>
    /// DTO chứa toàn bộ kết quả báo cáo Sách
    /// </summary>
    public class BaoCaoSachTongHopDto
    {
        public BaoCaoSachKpiDto Kpi { get; set; } = new();
        public List<BaoCaoSachChiTietDto> ChiTietTonKho { get; set; } = new();
        public List<BaoCaoSachTreHanDto> SachTreHan { get; set; } = new();
        public List<TopSachDuocThueDto> TopSachThue { get; set; } = new();
    }

    // -- Các lớp con cho báo cáo chi tiết --

    // Dùng cho KPI
    public class BaoCaoSachKpiDto
    {
        public int TongDauSach { get; set; }
        public int TongSoLuong { get; set; }
        public int DangChoThue { get; set; }
        public int SanSang { get; set; }
    }

    // Dùng cho Tab 1
    public class BaoCaoSachChiTietDto
    {
        public string TenSach { get; set; } = string.Empty;
        public string? TenTacGia { get; set; }
        public string? TenTheLoai { get; set; }
        public int SoLuongTong { get; set; }
        public int SoLuongDangMuon { get; set; }
        public int SoLuongConLai { get; set; }
    }

    // Dùng cho Tab 2
    public class BaoCaoSachTreHanDto
    {
        public string TenSach { get; set; } = string.Empty;
        public string HoTen { get; set; } = string.Empty;
        public string? SoDienThoai { get; set; }
        public DateTime NgayThue { get; set; }
        public DateTime NgayHenTra { get; set; }
        public string TinhTrang { get; set; } = string.Empty;
    }

    // Dùng cho Tab 3
    public class TopSachDuocThueDto
    {
        public string TenSach { get; set; } = string.Empty;
        public string? TenTacGia { get; set; }
        public int TongLuotThue { get; set; }
    }
}