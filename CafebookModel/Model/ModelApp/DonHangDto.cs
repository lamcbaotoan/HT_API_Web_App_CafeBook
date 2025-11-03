using System;
using System.Collections.Generic;

namespace CafebookModel.Model.ModelApp
{
    /// <summary>
    /// DTO cho DataGrid chính (Danh sách Đơn hàng)
    /// </summary>
    public class DonHangDto
    {
        public int IdHoaDon { get; set; }
        public DateTime ThoiGianTao { get; set; }
        public string? TenNhanVien { get; set; }
        public string? TenKhachHang { get; set; } // Thêm
        public string? SoBan { get; set; }
        public decimal ThanhTien { get; set; }
        public string TrangThai { get; set; } = string.Empty;
        public string LoaiHoaDon { get; set; } = string.Empty; // Thêm
    }

    /// <summary>
    /// DTO cho DataGrid chi tiết
    /// </summary>
    public class DonHangChiTietDto
    {
        public string TenSanPham { get; set; } = string.Empty;
        public int SoLuong { get; set; }
        public decimal ThanhTien { get; set; }
    }

    /// <summary>
    /// DTO để lấy dữ liệu cho các ComboBox lọc
    /// </summary>
    public class DonHangFiltersDto
    {
        // FilterLookupDto đã được định nghĩa trong SachDto.cs
        public List<FilterLookupDto> NhanViens { get; set; } = new();
        public List<FilterLookupDto> KhachHangs { get; set; } = new();
    }
}