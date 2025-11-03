using System;
using System.Collections.Generic;

namespace CafebookModel.Model.ModelApp
{
    /// <summary>
    /// DTO cho DataGrid Khuyến mãi
    /// </summary>
    public class KhuyenMaiDto
    {
        public int IdKhuyenMai { get; set; }
        public string MaKhuyenMai { get; set; } = string.Empty; // <-- THÊM MỚI
        public string TenKhuyenMai { get; set; } = string.Empty;
        public string GiaTriGiam { get; set; } = string.Empty;
        public decimal? GiamToiDa { get; set; }
        public string? DieuKienApDung { get; set; }
        public DateTime NgayBatDau { get; set; }
        public DateTime NgayKetThuc { get; set; }
        public string TrangThai { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO để tải chi tiết và cập nhật Khuyến mãi
    /// </summary>
    public class KhuyenMaiUpdateRequestDto
    {
        public int IdKhuyenMai { get; set; }
        public string MaKhuyenMai { get; set; } = string.Empty; // <-- THÊM MỚI
        public string TenChuongTrinh { get; set; } = string.Empty;
        public string? MoTa { get; set; }
        public string LoaiGiamGia { get; set; } = string.Empty;
        public decimal GiaTriGiam { get; set; }
        public decimal? GiamToiDa { get; set; }
        public decimal? HoaDonToiThieu { get; set; }
        public int? IdSanPhamApDung { get; set; }
        public DateTime NgayBatDau { get; set; }
        public DateTime NgayKetThuc { get; set; }
        public string? NgayTrongTuan { get; set; }
        public string? GioBatDau { get; set; }
        public string? GioKetThuc { get; set; }
        public string? DieuKienApDung { get; set; }
        public string TrangThai { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO lấy các bộ lọc cho Form Khuyến mãi
    /// </summary>
    public class KhuyenMaiFiltersDto
    {
        public List<FilterLookupDto> SanPhams { get; set; } = new();
    }
}