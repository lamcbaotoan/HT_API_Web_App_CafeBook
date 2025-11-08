// Tập tin: CafebookModel/Model/ModelApp/SachDto.cs
using System.Collections.Generic;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http;

namespace CafebookModel.Model.ModelApp
{
    /// <summary>
    /// DTO dùng để hiển thị Sách trên DataGrid (Đã sửa)
    /// </summary>
    public class SachDto
    {
        public int IdSach { get; set; }
        public string TenSach { get; set; } = string.Empty;
        public string? TenTacGia { get; set; } // Sẽ được nối chuỗi từ API (vd: "A, B, C")
        public string? TenTheLoai { get; set; } // Sẽ được nối chuỗi từ API
        public string? ViTri { get; set; }
        public int SoLuongTong { get; set; }
        public int SoLuongHienCo { get; set; }
        public int SoLuongDangMuon => SoLuongTong - SoLuongHienCo;
        public string? AnhBiaUrl { get; set; }
    }

    /// <summary>
    /// DTO (SỬA) cho Form Chi Tiết (Khi GetDetails)
    /// </summary>
    public class SachDetailDto
    {
        public int IdSach { get; set; }
        public string TenSach { get; set; } = string.Empty;

        // SỬA: Chuyển sang List<int>
        public List<int> IdTheLoais { get; set; } = new();
        public List<int> IdTacGias { get; set; } = new();
        public List<int> IdNhaXuatBans { get; set; } = new();

        public int? NamXuatBan { get; set; }
        public string? MoTa { get; set; }
        public int SoLuongTong { get; set; }
        public string? AnhBiaUrl { get; set; }
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

        // SỬA: Chuyển sang List<int>
        public List<int> IdTheLoais { get; set; } = new();
        public List<int> IdTacGias { get; set; } = new();
        public List<int> IdNhaXuatBans { get; set; } = new();

        public int? NamXuatBan { get; set; }
        public string? MoTa { get; set; }
        public int SoLuongTong { get; set; }
        public decimal? GiaBia { get; set; }
        public string? ViTri { get; set; }

        [JsonIgnore]
        public IFormFile? AnhBiaUpload { get; set; }
        [JsonIgnore]
        public bool XoaAnhBia { get; set; } = false;
    }

    // (ĐÃ XÓA FilterLookupDto và BaoCaoSachTreHanDto khỏi đây)

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