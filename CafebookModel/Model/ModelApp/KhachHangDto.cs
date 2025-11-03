using System;
using System.Collections.Generic;

namespace CafebookModel.Model.ModelApp
{
    /// <summary>
    /// DTO cho DataGrid Khách hàng
    /// </summary>
    public class KhachHangDto
    {
        public int IdKhachHang { get; set; }
        public string HoTen { get; set; } = string.Empty;
        public string? SoDienThoai { get; set; }
        public string? Email { get; set; }
        public DateTime NgayTao { get; set; }
        public bool BiKhoa { get; set; }
        public string TrangThai => BiKhoa ? "Bị khóa" : "Hoạt động";
    }

    /// <summary>
    /// DTO cho Form (gồm Lịch sử)
    /// </summary>
    public class KhachHangDetailDto
    {
        public int IdKhachHang { get; set; }
        public string HoTen { get; set; } = string.Empty;
        public string? SoDienThoai { get; set; }
        public string? Email { get; set; }
        public string? DiaChi { get; set; }
        public int DiemTichLuy { get; set; }
        public string? TenDangNhap { get; set; }
        public bool BiKhoa { get; set; }
        public string? AnhDaiDienBase64 { get; set; }
        public List<LichSuDonHangDto> LichSuDonHang { get; set; } = new();
        public List<LichSuThueSachDto> LichSuThueSach { get; set; } = new();
    }

    /// <summary>
    /// DTO để Gửi (Thêm/Sửa)
    /// </summary>
    public class KhachHangUpdateRequestDto
    {
        public int IdKhachHang { get; set; }
        public string HoTen { get; set; } = string.Empty;
        public string? SoDienThoai { get; set; }
        public string? Email { get; set; }
        public string? DiaChi { get; set; }
        public int DiemTichLuy { get; set; }
        public string? TenDangNhap { get; set; }
        public string? AnhDaiDienBase64 { get; set; }
        // (Không bao gồm mật khẩu, mật khẩu chỉ đổi ở web)
    }

    // --- DTOs con cho Lịch sử ---
    public class LichSuDonHangDto
    {
        public int IdHoaDon { get; set; }
        public DateTime ThoiGianTao { get; set; }
        public decimal ThanhTien { get; set; }
        public string TrangThai { get; set; } = string.Empty;
    }

    public class LichSuThueSachDto
    {
        public int IdPhieuThue { get; set; }
        public string TieuDeSach { get; set; } = string.Empty;
        public DateTime NgayThue { get; set; }
        public string TrangThai { get; set; } = string.Empty;
    }
}