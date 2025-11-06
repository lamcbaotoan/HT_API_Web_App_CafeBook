// Tập tin: CafebookModel/Model/ModelWeb/ThuVienSachDto.cs
using CafebookModel.Model.ModelApp; // Dùng chung FilterLookupDto
using System.Collections.Generic;

namespace CafebookModel.Model.ModelWeb
{
    /// <summary>
    /// DTO cho một thẻ Sách (Book Card) trên lưới
    /// </summary>
    public class SachCardDto
    {
        public int IdSach { get; set; }
        public string TieuDe { get; set; } = string.Empty;
        public string? TacGia { get; set; }
        public decimal GiaBia { get; set; } // Dùng làm tiền cọc
        public int SoLuongCoSan { get; set; }
        public string? AnhBiaUrl { get; set; } // Đã đổi sang URL
    }

    /// <summary>
    /// DTO chứa kết quả trả về (bao gồm phân trang)
    /// </summary>
    public class SachPhanTrangDto
    {
        public List<SachCardDto> Items { get; set; } = new();
        public int TotalPages { get; set; }
        public int CurrentPage { get; set; }
    }

    /// <summary>
    /// DTO cho trang Chi Tiết Sách
    /// </summary>
    public class SachChiTietDto
    {
        public int IdSach { get; set; }
        public string TieuDe { get; set; } = string.Empty;
        public string? TacGia { get; set; }
        public string? TheLoai { get; set; }
        public decimal GiaBia { get; set; }
        public string? AnhBiaUrl { get; set; } // Đã đổi sang URL
        public string? MoTa { get; set; }
        public string? ViTri { get; set; }
        public int TongSoLuong { get; set; }
        public int SoLuongCoSan { get; set; }

        // Danh sách "Có thể bạn cũng thích"
        public List<SachCardDto> GoiY { get; set; } = new();
    }

    /// <summary>
    /// DTO chứa các bộ lọc (filters) cho trang thư viện
    /// </summary>
    public class SachFiltersDto
    {
        public List<FilterLookupDto> TheLoais { get; set; } = new();
    }
}