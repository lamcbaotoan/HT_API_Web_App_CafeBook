using System.Collections.Generic;

namespace CafebookModel.Model.ModelApp
{
    // (Class SanPhamDto giữ nguyên)
    public class SanPhamDto
    {
        public int IdSanPham { get; set; }
        public string TenSanPham { get; set; } = string.Empty;
        public decimal GiaBan { get; set; }
        public string? TenDanhMuc { get; set; }
        public bool TrangThaiKinhDoanh { get; set; }
    }

    // (Class SanPhamUpdateRequestDto giữ nguyên)
    public class SanPhamUpdateRequestDto
    {
        public int IdSanPham { get; set; }
        public string TenSanPham { get; set; } = string.Empty;
        public int? IdDanhMuc { get; set; }
        public decimal GiaBan { get; set; }
        public string? MoTa { get; set; }
        public bool TrangThaiKinhDoanh { get; set; } = true;
        public string? NhomIn { get; set; }
        public string? HinhAnhBase64 { get; set; }
    }

    // --- SỬA ĐỔI DTO NÀY ---
    public class SanPhamFiltersDto
    {
        public List<FilterLookupDto> DanhMucs { get; set; } = new();
        public List<FilterLookupDto> NguyenLieus { get; set; } = new();
        // Thêm: List tất cả các đơn vị
        public List<DonViChuyenDoiDto> DonViTinhs { get; set; } = new();
    }

    // --- THÊM DTO MỚI ---
    public class DonViChuyenDoiDto
    {
        public int Id { get; set; }
        public int IdNguyenLieu { get; set; }
        public string Ten { get; set; } = string.Empty;
    }

    // --- SỬA ĐỔI DTO NÀY ---
    public class DinhLuongDto
    {
        public int IdNguyenLieu { get; set; }
        public string TenNguyenLieu { get; set; } = string.Empty;
        public decimal SoLuong { get; set; } // (Tên giữ nguyên là SoLuong)
        public int IdDonViSuDung { get; set; } // (Id của ĐVT, vd: Id 'gram')
        public string TenDonViSuDung { get; set; } = string.Empty; // (Tên ĐVT, vd: 'gram')
    }

    // --- SỬA ĐỔI DTO NÀY ---
    public class DinhLuongUpdateRequestDto
    {
        public int IdSanPham { get; set; }
        public int IdNguyenLieu { get; set; }
        public decimal SoLuong { get; set; }
        public int IdDonViSuDung { get; set; } // Thêm (Id của 'gram')
    }
}