using System;
using System.Collections.Generic;

namespace CafebookModel.Model.ModelApp
{
    // DTO chính để hiển thị một đánh giá trong ListView của quản lý
    public class DanhGiaQuanLyDto
    {
        public int IdDanhGia { get; set; }
        public string TenKhachHang { get; set; } = string.Empty;
        public string? TenSanPham { get; set; } // Có thể null nếu là đánh giá chung
        public int SoSao { get; set; }
        public string? BinhLuan { get; set; }
        public string? HinhAnhUrl { get; set; } // API sẽ trả về URL đầy đủ
        public DateTime NgayTao { get; set; }
        public string TrangThai { get; set; } = string.Empty; // "Hiển thị" hoặc "Đã ẩn"
        public PhanHoiQuanLyDto? PhanHoi { get; set; }
    }

    // DTO cho phản hồi của nhân viên (nếu có)
    public class PhanHoiQuanLyDto
    {
        public string TenNhanVien { get; set; } = string.Empty;
        public string NoiDung { get; set; } = string.Empty;
        public DateTime NgayTao { get; set; }
    }

    // DTO dùng khi Quản lý gửi một phản hồi
    public class PhanHoiInputDto
    {
        public string NoiDung { get; set; } = string.Empty;
    }

    // DTO để nhận kết quả phân trang từ API
    public class PaginatedResult<T>
    {
        public List<T> Items { get; set; } = new List<T>();
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
    }
    public class ProductSearchResultDto
    {
        public int IdSanPham { get; set; }
        public string TenSanPham { get; set; } = string.Empty;
    }

    public class ProductStatsDto
    {
        public int TotalReviews { get; set; }
        public double AverageRating { get; set; }
    }
}