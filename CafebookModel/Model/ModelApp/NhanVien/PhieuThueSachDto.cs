using System;
using System.Collections.Generic;
using CafebookModel.Model.ModelApp;

namespace CafebookModel.Model.ModelApp.NhanVien
{
    /// <summary>
    /// DTO chứa các cài đặt cấu hình phí thuê sách
    /// </summary>
    public class CaiDatThueSachDto
    {
        public decimal PhiThue { get; set; }
        public decimal PhiTraTreMoiNgay { get; set; }
        public int SoNgayMuonToiDa { get; set; }
        public int DiemPhieuThue { get; set; } // Thay thế cho VNDToPoint
        public decimal PointToVND { get; set; }
    }

    /// <summary>
    /// DTO an toàn cho tìm kiếm Khách Hàng
    /// </summary>
    public class KhachHangSearchDto
    {
        public int IdKhachHang { get; set; }
        public string HoTen { get; set; } = string.Empty;
        public string? SoDienThoai { get; set; }
        public int DiemTichLuy { get; set; }
        public string? Email { get; set; }
    }

    /// <summary>
    /// DTO thông tin nhân viên nhập vào
    /// </summary>
    public class KhachHangInfoDto
    {
        public string HoTen { get; set; } = string.Empty;
        public string? SoDienThoai { get; set; }
        public string? Email { get; set; }
    }

    /// <summary>
    /// DTO hiển thị trên DataGrid chính (danh sách đang thuê)
    /// </summary>
    public class PhieuThueGridDto
    {
        public int IdPhieuThueSach { get; set; }
        public string HoTenKH { get; set; } = string.Empty;
        public string? SoDienThoaiKH { get; set; }
        public DateTime NgayThue { get; set; }
        public DateTime NgayHenTra { get; set; }
        public int SoLuongSach { get; set; }
        public decimal TongTienCoc { get; set; }
        public string TrangThai { get; set; } = string.Empty;
        public string TinhTrang { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO chi tiết một cuốn sách trong phiếu thuê
    /// </summary>
    public class ChiTietSachThueDto
    {
        public int IdPhieuThueSach { get; set; }
        public int IdSach { get; set; }
        public string TenSach { get; set; } = string.Empty;
        public DateTime NgayHenTra { get; set; }
        public decimal TienCoc { get; set; }
        public string TinhTrang { get; set; } = string.Empty;
        public decimal TienPhat { get; set; } // Tính toán
    }

    /// <summary>
    /// DTO chứa đầy đủ chi tiết của 1 phiếu (khi click vào grid)
    /// </summary>
    public class PhieuThueChiTietDto
    {
        public int IdPhieuThueSach { get; set; }
        public string HoTenKH { get; set; } = string.Empty;
        public string? SoDienThoaiKH { get; set; }
        public string? EmailKH { get; set; }
        public int DiemTichLuyKH { get; set; }
        public DateTime NgayThue { get; set; }
        public string TrangThaiPhieu { get; set; } = string.Empty;
        public List<ChiTietSachThueDto> SachDaThue { get; set; } = new();
    }

    /// <summary>
    /// DTO cho form tìm kiếm sách
    /// </summary>
    public class SachTimKiemDto
    {
        public int IdSach { get; set; }
        public string TenSach { get; set; } = string.Empty;
        public string TacGia { get; set; } = string.Empty;
        public int SoLuongHienCo { get; set; }
        public decimal GiaBia { get; set; }
    }

    // --- DTOs cho Yêu Cầu (Request) ---

    /// <summary>
    /// DTO để tạo phiếu thuê mới
    /// </summary>
    public class PhieuThueRequestDto
    {
        public KhachHangInfoDto KhachHangInfo { get; set; } = new();
        public List<SachThueRequestDto> SachCanThue { get; set; } = new();
        public DateTime NgayHenTra { get; set; }
        public int IdNhanVien { get; set; }
    }

    public class SachThueRequestDto
    {
        public int IdSach { get; set; }
        public decimal TienCoc { get; set; }
    }

    /// <summary>
    /// DTO để thực hiện trả sách
    /// </summary>
    public class TraSachRequestDto
    {
        public int IdPhieuThueSach { get; set; }
        public List<int> IdSachs { get; set; } = new();
        public int IdNhanVien { get; set; }
    }

    /// <summary>
    /// DTO kết quả trả về sau khi trả sách
    /// </summary>
    public class TraSachResponseDto
    {
        public int IdPhieuTra { get; set; }
        public int SoSachDaTra { get; set; }
        public decimal TongPhiThue { get; set; }
        public decimal TongTienPhat { get; set; }
        public decimal TongTienCoc { get; set; }
        public decimal TongHoanTra { get; set; }
        public int DiemTichLuy { get; set; }
    }

    /// <summary>
    /// DTO Dữ liệu để in phiếu (Thuê)
    /// </summary>
    public class PhieuThuePrintDto
    {
        public string IdPhieu { get; set; } = string.Empty;
        public string TenQuan { get; set; } = string.Empty;
        public string DiaChiQuan { get; set; } = string.Empty;
        public string SdtQuan { get; set; } = string.Empty;
        public DateTime NgayTao { get; set; }
        public string TenNhanVien { get; set; } = string.Empty;
        public string TenKhachHang { get; set; } = string.Empty;
        public string SdtKhachHang { get; set; } = string.Empty;
        public DateTime NgayHenTra { get; set; }
        public List<ChiTietPrintDto> ChiTiet { get; set; } = new();
        public decimal TongPhiThue { get; set; }
        public decimal TongTienCoc { get; set; }
    }

    public class ChiTietPrintDto
    {
        public string TenSach { get; set; } = string.Empty;
        public decimal TienCoc { get; set; }
    }

    /// <summary>
    /// DTO cho Tab Lịch Sử Trả Sách
    /// </summary>
    public class PhieuTraGridDto
    {
        public int IdPhieuTra { get; set; }
        public int IdPhieuThueSach { get; set; }
        public DateTime NgayTra { get; set; }
        public string TenNhanVien { get; set; } = string.Empty;
        public decimal TongHoanTra { get; set; }
    }

    /// <summary>
    /// DTO Dữ liệu để in phiếu (Trả)
    /// </summary>
    public class PhieuTraPrintDto
    {
        public string IdPhieuTra { get; set; } = string.Empty;
        public string IdPhieuThue { get; set; } = string.Empty;
        public string TenQuan { get; set; } = string.Empty;
        public string DiaChiQuan { get; set; } = string.Empty;
        public string SdtQuan { get; set; } = string.Empty;
        public DateTime NgayTra { get; set; }
        public string TenNhanVien { get; set; } = string.Empty;
        public string TenKhachHang { get; set; } = string.Empty;
        public string SdtKhachHang { get; set; } = string.Empty;
        public int DiemTichLuy { get; set; }

        public List<ChiTietTraPrintDto> ChiTiet { get; set; } = new();

        public decimal TongTienCoc { get; set; }
        public decimal TongPhiThue { get; set; }
        public decimal TongTienPhat { get; set; }
        public decimal TongHoanTra { get; set; }
    }

    /// <summary>
    /// DTO chi tiết cho phiếu trả
    /// </summary>
    public class ChiTietTraPrintDto
    {
        public string TenSach { get; set; } = string.Empty;
        public decimal TienCoc { get; set; }
        public decimal TienPhat { get; set; }
    }
}