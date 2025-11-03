using System.Collections.Generic;

namespace CafebookModel.Model.ModelApp
{
    /// <summary>
    /// DTO dùng để hiển thị Sách trên DataGrid
    /// </summary>
    public class SachDto
    {
        public int IdSach { get; set; }
        public string TenSach { get; set; } = string.Empty;
        public string? TenTacGia { get; set; }
        public string? TenTheLoai { get; set; }
        public string? ViTri { get; set; } // <<< THÊM MỚI
        public int SoLuongTong { get; set; }
        public int SoLuongHienCo { get; set; } // Số lượng còn lại trong kho
        public int SoLuongDangMuon => SoLuongTong - SoLuongHienCo; // Số lượng đang được thuê
    }

    /// <summary>
    /// DTO dùng để tải, thêm mới, hoặc cập nhật chi tiết một cuốn sách
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

        // Dùng để GỬI và NHẬN ảnh
        public string? AnhBiaBase64 { get; set; }
        public decimal? GiaBia { get; set; } // <<< THÊM MỚI
        public string? ViTri { get; set; }  // <<< THÊM MỚI
    }

    /// <summary>
    /// DTO chứa các danh sách (TheLoai, TacGia, NXB) để điền vào ComboBox
    /// </summary>
    public class SachFiltersDto
    {
        public List<FilterLookupDto> TheLoais { get; set; } = new();
        public List<FilterLookupDto> TacGias { get; set; } = new();
        public List<FilterLookupDto> NhaXuatBans { get; set; } = new();
    }

    /// <summary>
    /// DTO chứa dữ liệu cho Tab "Lịch sử Thuê"
    /// </summary>
    public class SachRentalsDto
    {
        public List<BaoCaoSachTreHanDto> SachQuaHan { get; set; } = new();
        public List<LichSuThueDto> LichSuThue { get; set; } = new();
    }

    /// <summary>
    /// DTO con cho Lịch sử thuê
    /// </summary>
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