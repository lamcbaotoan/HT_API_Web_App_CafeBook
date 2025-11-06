using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http; // <-- THÊM

namespace CafebookModel.Model.ModelApp
{
    /// <summary>
    /// DTO dùng để hiển thị Sách trên DataGrid (Đã sửa)
    /// </summary>
    public class SachDto
    {
        public int IdSach { get; set; }
        public string TenSach { get; set; } = string.Empty;
        public string? TenTacGia { get; set; }
        public string? TenTheLoai { get; set; }
        public string? ViTri { get; set; }
        public int SoLuongTong { get; set; }
        public int SoLuongHienCo { get; set; }
        public int SoLuongDangMuon => SoLuongTong - SoLuongHienCo;
        public string? AnhBiaUrl { get; set; } // <-- SỬA: Dùng URL
    }

    /// <summary>
    /// DTO (MỚI) cho Form Chi Tiết (Khi GetDetails)
    /// </summary>
    public class SachDetailDto
    {
        public int IdSach { get; set; }
        public string TenSach { get; set; } = string.Empty;
        public int? IdTheLoai { get; set; }
        public int? IdTacGia { get; set; }
        public int? IdNhaXuatBan { get; set; }
        public int? NamXuatBan { get; set; }
        public string? MoTa { get; set; }
        public int SoLuongTong { get; set; }
        public string? AnhBiaUrl { get; set; } // <-- SỬA: Dùng URL
        public decimal? GiaBia { get; set; }
        public string? ViTri { get; set; }
    }

    /// <summary>
    /// DTO (SỬA) chỉ dùng để Tải Lên (Create/Update)
    /// </summary>
    public class SachUpdateRequestDto
    {
        public int IdSach { get; set; }
        public string TenSach { get; set; } = string.Empty;
        public int? IdTheLoai { get; set; }
        public int? IdTacGia { get; set; }
        public int? IdNhaXuatBan { get; set; }
        public int? NamXuatBan { get; set; }
        public string? MoTa { get; set; }
        public int SoLuongTong { get; set; }
        public decimal? GiaBia { get; set; }
        public string? ViTri { get; set; }

        // SỬA: Thêm 2 thuộc tính này để Swagger nhận diện
        // Chúng sẽ bị bỏ qua khi đọc/ghi JSON, chỉ dùng cho [FromForm]
        [JsonIgnore]
        public IFormFile? AnhBiaUpload { get; set; }
        [JsonIgnore]
        public bool XoaAnhBia { get; set; } = false;
    }

    // (Các DTO còn lại giữ nguyên)
    public class SachFiltersDto
    {
        public List<FilterLookupDto> TheLoais { get; set; } = new();
        public List<FilterLookupDto> TacGias { get; set; } = new();
        public List<FilterLookupDto> NhaXuatBans { get; set; } = new();
    }

    public class SachRentalsDto
    {
        public List<BaoCaoSachTreHanDto> SachQuaHan { get; set; } = new();
        public List<LichSuThueDto> LichSuThue { get; set; } = new();
    }

    public class LichSuThueDto
    {
        public string TenSach { get; set; } = string.Empty;
        public string TenKhachHang { get; set; } = string.Empty;
        public DateTime NgayThue { get; set; }
        public DateTime NgayHenTra { get; set; }
        public DateTime? NgayTraThucTe { get; set; }
        public decimal TienPhat { get; set; }
        public string TrangThai { get; set; } = string.Empty;
    }
}