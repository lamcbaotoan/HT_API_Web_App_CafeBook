using Microsoft.AspNetCore.Http;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CafebookModel.Model.ModelApp
{
    /// <summary>
    /// DTO cho DataGrid (Tìm kiếm)
    /// </summary>
    public class SanPhamDto
    {
        public int IdSanPham { get; set; }
        public string TenSanPham { get; set; } = string.Empty;
        public decimal GiaBan { get; set; }
        public string? TenDanhMuc { get; set; }
        public bool TrangThaiKinhDoanh { get; set; }
        public string? HinhAnhUrl { get; set; } // SỬA: Dùng URL
    }

    /// <summary>
    /// DTO (MỚI) cho Form Chi Tiết (Khi GetDetails)
    /// </summary>
    public class SanPhamDetailDto
    {
        public int IdSanPham { get; set; }
        public string TenSanPham { get; set; } = string.Empty;
        public int? IdDanhMuc { get; set; }
        public decimal GiaBan { get; set; }
        public string? MoTa { get; set; }
        public bool TrangThaiKinhDoanh { get; set; }
        public string? NhomIn { get; set; }
        public string? HinhAnhUrl { get; set; } // DÙNG ĐỂ HIỂN THỊ
    }

    /// <summary>
    /// DTO (SỬA) chỉ dùng để Tải Lên (Create/Update)
    /// </summary>
    public class SanPhamUpdateRequestDto
    {
        public int IdSanPham { get; set; } // Sẽ = 0 nếu là Tạo mới
        public string TenSanPham { get; set; } = string.Empty;
        public int? IdDanhMuc { get; set; }
        public decimal GiaBan { get; set; }
        public string? MoTa { get; set; }
        public bool TrangThaiKinhDoanh { get; set; }
        public string? NhomIn { get; set; }

        // SỬA: Thêm 2 thuộc tính này để Swagger nhận diện
        [JsonIgnore]
        public IFormFile? HinhAnhUpload { get; set; }
        [JsonIgnore]
        public bool XoaHinhAnh { get; set; } = false;
    }

    // (Các DTO còn lại giữ nguyên)
    public class SanPhamFiltersDto
    {
        public List<FilterLookupDto> DanhMucs { get; set; } = new();
        public List<FilterLookupDto> NguyenLieus { get; set; } = new();
        public List<DonViChuyenDoiDto> DonViTinhs { get; set; } = new();
    }
    public class DonViChuyenDoiDto
    {
        public int Id { get; set; }
        public int IdNguyenLieu { get; set; }
        public string Ten { get; set; } = string.Empty;
    }
    public class DinhLuongDto
    {
        public int IdNguyenLieu { get; set; }
        public string TenNguyenLieu { get; set; } = string.Empty;
        public decimal SoLuong { get; set; }
        public int IdDonViSuDung { get; set; }
        public string TenDonViSuDung { get; set; } = string.Empty;
    }
    public class DinhLuongUpdateRequestDto
    {
        public int IdSanPham { get; set; }
        public int IdNguyenLieu { get; set; }
        public decimal SoLuong { get; set; }
        public int IdDonViSuDung { get; set; }
    }
}