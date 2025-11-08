using System;
using System.Collections.Generic;

namespace CafebookModel.Model.ModelApp.NhanVien.DatBan
{
    // DTO dùng để hiển thị trên DataGrid của nhân viên
    public class PhieuDatBanDto
    {
        public int IdPhieuDatBan { get; set; }
        public string? TenKhachHang { get; set; }
        public string? SoDienThoai { get; set; }
        public string? Email { get; set; }
        public int IdBan { get; set; }
        public string? SoBan { get; set; }
        public string? TenKhuVuc { get; set; }
        public DateTime ThoiGianDat { get; set; }
        public int SoLuongKhach { get; set; }
        public string? TrangThai { get; set; }
        public string? GhiChu { get; set; }
        public int? IdKhachHang { get; set; }
    }

    // DTO dùng khi nhân viên tạo/cập nhật phiếu
    public class PhieuDatBanCreateUpdateDto
    {
        public int? IdPhieuDatBan { get; set; }
        public string TenKhachHang { get; set; } = string.Empty;
        public string SoDienThoai { get; set; } = string.Empty;
        public string? Email { get; set; }
        public int IdBan { get; set; }
        public DateTime ThoiGianDat { get; set; }
        public int SoLuongKhach { get; set; }
        public string? GhiChu { get; set; }
        public string TrangThai { get; set; } = "Đã xác nhận";
        public int IdNhanVienTao { get; set; }
        // IsKhachVangLai đã được XÓA
    }

    // DTO dùng cho form đặt bàn của khách hàng qua web
    public class PhieuDatBanWebCreateDto
    {
        public string TenKhachHang { get; set; } = string.Empty;
        public string SoDienThoai { get; set; } = string.Empty;
        public string? Email { get; set; }
        public int IdBan { get; set; }
        public DateTime ThoiGianDat { get; set; }
        public int SoLuongKhach { get; set; }
        public string? GhiChu { get; set; }
    }

    // DTO khi nhân viên xác nhận khách đến
    public class XacNhanKhachDenRequestDto
    {
        public int IdPhieuDatBan { get; set; }
        public int IdNhanVien { get; set; }
    }

    // DTO trả về id hóa đơn mới tạo khi khách đến
    public class XacNhanKhachDenResponseDto
    {
        public int IdHoaDon { get; set; }
    }

    // DTO dùng cho ComboBox chọn bàn
    public class BanDatBanDto
    {
        public int IdBan { get; set; }
        public string? SoBan { get; set; }
        public string? TenKhuVuc { get; set; }
        // SỬA: Thêm SoGhe để kiểm tra sức chứa (Yêu cầu 7)
        public int SoGhe { get; set; }
        public int? IdKhuVuc { get; set; } // Thêm IdKhuVuc để lọc
        public string HienThi => $"{SoBan} ({TenKhuVuc})";
    }

    // DTO cho chuông thông báo
    public class ThongBaoDto
    {
        public int IdThongBao { get; set; }
        public string? NoiDung { get; set; }
        public string? LoaiThongBao { get; set; }
        public int? IdLienQuan { get; set; }
        public bool DaXem { get; set; }
        public DateTime ThoiGianTao { get; set; }
    }

    // THÊM MỚI: DTO cho tìm kiếm khách hàng (Yêu cầu 5)
    public class KhachHangLookupDto
    {
        public int IdKhachHang { get; set; }
        public string HoTen { get; set; } = string.Empty;
        public string SoDienThoai { get; set; } = string.Empty;
        public string? Email { get; set; }
        // Thuộc tính để hiển thị trong ComboBox
        public string DisplaySdt => $"{HoTen} ({SoDienThoai})";
        public string DisplayEmail => $"{HoTen} ({Email})";
    }
}