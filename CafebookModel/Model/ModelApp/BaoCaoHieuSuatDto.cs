using System;
using System.Collections.Generic;

namespace CafebookModel.Model.ModelApp
{
    /// <summary>
    /// DTO để gửi yêu cầu lọc (filters) lên API
    /// </summary>
    public class BaoCaoHieuSuatRequestDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int? VaiTroId { get; set; }
        public string? SearchText { get; set; }
    }

    /// <summary>
    /// DTO chứa toàn bộ kết quả báo cáo
    /// </summary>
    public class BaoCaoHieuSuatTongHopDto
    {
        public BaoCaoHieuSuatKpiDto Kpi { get; set; } = new();
        public List<BaoCaoSalesDto> SalesPerformance { get; set; } = new();
        public List<BaoCaoOperationsDto> OperationalPerformance { get; set; } = new();
        public List<BaoCaoAttendanceDto> Attendance { get; set; } = new();
    }

    // -- Các lớp con cho báo cáo chi tiết --

    // Dùng cho KPI
    public class BaoCaoHieuSuatKpiDto
    {
        public decimal TongDoanhThu { get; set; }
        public decimal TongGioLam { get; set; } // SỬA TỪ double -> decimal
        public int TongSoCaLam { get; set; }
        public int TongLanHuyMon { get; set; }
    }

    // Dùng cho Tab 1: Sales
    public class BaoCaoSalesDto
    {
        public string HoTen { get; set; } = string.Empty;
        public string TenVaiTro { get; set; } = string.Empty;
        public decimal TongDoanhThu { get; set; }
        public int SoHoaDon { get; set; }
        public decimal DoanhThuTrungBinh { get; set; }
        public int SoLanHuyMon { get; set; }
    }

    // Dùng cho Tab 2: Operations
    public class BaoCaoOperationsDto
    {
        public string HoTen { get; set; } = string.Empty;
        public string TenVaiTro { get; set; } = string.Empty;
        public int PhieuNhap { get; set; }
        public int PhieuKiem { get; set; }
        public int PhieuHuy { get; set; }
        public int DonDuyet { get; set; }
    }

    // Dùng cho Tab 3: Attendance
    public class BaoCaoAttendanceDto
    {
        public string HoTen { get; set; } = string.Empty;
        public int TongSoCaLam { get; set; }
        public decimal TongGioLam { get; set; } // SỬA TỪ double -> decimal
        public int SoDonXinNghi { get; set; }
        public int SoDonDaDuyet { get; set; }
        public int SoDonChoDuyet { get; set; }
    }
}