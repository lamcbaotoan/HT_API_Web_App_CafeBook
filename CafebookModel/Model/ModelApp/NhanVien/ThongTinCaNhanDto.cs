using System;
using System.Collections.Generic;

namespace CafebookModel.Model.ModelApp.NhanVien
{
    // Dùng để hiển thị thông tin chung
    public class ThongTinCaNhanViewDto
    {
        public NhanVienInfoDto NhanVien { get; set; } = null!;
        public LichLamViecDto? LichLamViecHomNay { get; set; }

        // SỬA: Thay thế List bằng 2 thuộc tính mới
        public int SoLanXinNghiThangNay { get; set; }
        public List<LichLamViecChiTietDto> LichLamViecThangNay { get; set; } = new List<LichLamViecChiTietDto>();
    }

    // THÊM MỚI: DTO cho bảng Lịch Làm Việc
    public class LichLamViecChiTietDto
    {
        public DateTime NgayLam { get; set; }
        public string TenCa { get; set; } = string.Empty;
        public TimeSpan GioBatDau { get; set; }
        public TimeSpan GioKetThuc { get; set; }
    }

    // Thông tin cơ bản của nhân viên
    public class NhanVienInfoDto
    {
        public int IdNhanVien { get; set; }
        public string HoTen { get; set; } = string.Empty;
        public string SoDienThoai { get; set; } = string.Empty;
        public string? Email { get; set; }
        public string? DiaChi { get; set; }
        public string? AnhDaiDien { get; set; }
    }

    // Thông tin lịch làm việc
    public class LichLamViecDto
    {
        public string TenCa { get; set; } = string.Empty; // Khởi tạo
        public TimeSpan GioBatDau { get; set; }
        public TimeSpan GioKetThuc { get; set; }
    }

    // Thông tin đơn xin nghỉ
    public class DonXinNghiDto
    {
        public int IdDonXinNghi { get; set; }
        public string LoaiDon { get; set; } = string.Empty; // Khởi tạo
        public string LyDo { get; set; } = string.Empty; // Khởi tạo
        public DateTime NgayBatDau { get; set; }
        public DateTime NgayKetThuc { get; set; }
        public string TrangThai { get; set; } = string.Empty; // Khởi tạo
        public string? GhiChuPheDuyet { get; set; } // Cho phép null
    }

    /// <summary>
    /// Dùng để cập nhật thông tin
    /// </summary>
    public class CapNhatThongTinDto
    {
        public string HoTen { get; set; } = string.Empty; // Khởi tạo
        public string SoDienThoai { get; set; } = string.Empty; // Khởi tạo
        public string? Email { get; set; } // Cho phép null
        public string? DiaChi { get; set; } // Cho phép null
    }

    // Dùng để đổi mật khẩu
    public class DoiMatKhauRequestDto
    {
        public string MatKhauCu { get; set; } = string.Empty; // Khởi tạo
        public string MatKhauMoi { get; set; } = string.Empty; // Khởi tạo
    }

    // Dùng để tạo đơn xin nghỉ
    public class DonXinNghiRequestDto
    {
        public string LoaiDon { get; set; } = string.Empty; // Khởi tạo
        public string LyDo { get; set; } = string.Empty; // Khởi tạo
        public DateTime NgayBatDau { get; set; }
        public DateTime NgayKetThuc { get; set; }
    }
}