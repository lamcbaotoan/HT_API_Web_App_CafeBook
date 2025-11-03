using System;
using System.Collections.Generic;

namespace CafebookModel.Model.ModelApp
{
    // --- CÁC DTO ĐÃ CÓ ---
    #region DTOs (Đã có)

    // DTO cho Tab 1: Tồn Kho
    public class NguyenLieuTonKhoDto
    {
        public int IdNguyenLieu { get; set; }
        public string TenNguyenLieu { get; set; } = string.Empty;
        public decimal TonKho { get; set; }
        public string DonViTinh { get; set; } = string.Empty;
        public decimal TonKhoToiThieu { get; set; }
        public string TinhTrang { get; set; } = string.Empty;
    }

    // DTO cho Tab 2: CRUD Nguyên Liệu (ĐÃ SỬA LỖI CS0117)
    public class NguyenLieuCrudDto
    {
        public int IdNguyenLieu { get; set; }
        public string TenNguyenLieu { get; set; } = string.Empty;
        public string DonViTinh { get; set; } = string.Empty;
        public decimal TonKhoToiThieu { get; set; }
        public decimal TonKho { get; set; } // <-- Thêm cột này
    }

    // DTO để Gửi (Thêm/Sửa) Nguyên Liệu
    public class NguyenLieuUpdateRequestDto
    {
        public string TenNguyenLieu { get; set; } = string.Empty;
        public string DonViTinh { get; set; } = string.Empty;
        public decimal TonKhoToiThieu { get; set; }
    }

    // DTO cho Tab 4: CRUD Nhà Cung Cấp
    public class NhaCungCapDto
    {
        public int IdNhaCungCap { get; set; }
        public string TenNhaCungCap { get; set; } = string.Empty;
        public string? SoDienThoai { get; set; }
        public string? DiaChi { get; set; }
        public string? Email { get; set; }
    }

    // DTO cho Tab 3: DataGrid Phiếu Nhập
    public class PhieuNhapDto
    {
        public int IdPhieuNhapKho { get; set; }
        public DateTime NgayNhap { get; set; }
        public string? TenNhaCungCap { get; set; }
        public string TenNhanVien { get; set; } = string.Empty;
        public decimal TongTien { get; set; }
        public string TrangThai { get; set; } = string.Empty;
    }

    // DTO cho Tab 3: DataGrid Chi Tiết Phiếu Nhập
    public class ChiTietPhieuNhapDto
    {
        public int IdNguyenLieu { get; set; }
        public string? TenNguyenLieu { get; set; }
        public decimal SoLuongNhap { get; set; }
        public decimal DonGiaNhap { get; set; }
        public decimal ThanhTien { get; set; }
    }

    // DTO để Gửi (POST) một Phiếu Nhập Mới
    public class PhieuNhapCreateDto
    {
        public int IdNhanVien { get; set; }
        public int? IdNhaCungCap { get; set; }
        public DateTime NgayNhap { get; set; }
        public string? GhiChu { get; set; }
        public List<ChiTietPhieuNhapCreateDto> ChiTiet { get; set; } = new();
    }

    // DTO con cho Phiếu Nhập Mới
    public class ChiTietPhieuNhapCreateDto
    {
        public int IdNguyenLieu { get; set; }
        public decimal SoLuongNhap { get; set; }
        public decimal DonGiaNhap { get; set; }
    }
    #endregion

    // --- THÊM CÁC DTO MỚI CHO XUẤT HỦY VÀ KIỂM KHO ---
    #region DTOs (Mới)

    // --- DTOs cho Chức năng Xuất Hủy ---

    public class PhieuXuatHuyDto
    {
        public int IdPhieuXuatHuy { get; set; }
        public DateTime NgayXuatHuy { get; set; }
        public string TenNhanVien { get; set; } = string.Empty;
        public string LyDoXuatHuy { get; set; } = string.Empty;
        public decimal TongGiaTriHuy { get; set; }
    }

    public class ChiTietPhieuXuatHuyDto
    {
        public int IdNguyenLieu { get; set; }
        public string? TenNguyenLieu { get; set; }
        public decimal SoLuong { get; set; }
        public decimal DonGiaVon { get; set; }
        public decimal ThanhTien { get; set; }
    }

    public class PhieuXuatHuyCreateDto
    {
        public int IdNhanVien { get; set; }
        public DateTime NgayXuatHuy { get; set; }
        public string LyDoXuatHuy { get; set; } = string.Empty;
        public List<ChiTietPhieuXuatHuyCreateDto> ChiTiet { get; set; } = new();
    }

    public class ChiTietPhieuXuatHuyCreateDto
    {
        public int IdNguyenLieu { get; set; }
        public decimal SoLuong { get; set; }
    }

    // --- DTOs cho Chức năng Kiểm Kho ---

    public class PhieuKiemKhoDto
    {
        public int IdPhieuKiemKho { get; set; }
        public DateTime NgayKiem { get; set; }
        public string TenNhanVienKiem { get; set; } = string.Empty;
        public string TrangThai { get; set; } = string.Empty;
    }

    public class ChiTietKiemKhoDto
    {
        public int IdNguyenLieu { get; set; }
        public string TenNguyenLieu { get; set; } = string.Empty;
        public string DonViTinh { get; set; } = string.Empty;
        public decimal TonKhoHeThong { get; set; }
        public decimal TonKhoThucTe { get; set; }
        public decimal ChenhLech => TonKhoThucTe - TonKhoHeThong;
        public string? LyDoChenhLech { get; set; }
        public decimal GiaTriChenhLech { get; set; } // Tính toán từ API
    }

    public class PhieuKiemKhoCreateDto
    {
        public int IdNhanVien { get; set; }
        public DateTime NgayKiem { get; set; }
        public string? GhiChu { get; set; }
        // Dùng lại ChiTietKiemKhoDto vì nó chứa đủ thông tin
        public List<ChiTietKiemKhoDto> ChiTiet { get; set; } = new();
    }

    #endregion
}