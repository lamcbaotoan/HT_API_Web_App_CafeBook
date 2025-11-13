// Tập tin: CafebookModel/Model/ModelWeb/ThanhToanDto.cs
// (*** KHÔNG CẦN THAY ĐỔI - MÃ NGUỒN ĐÃ CHÍNH XÁC ***)

using System;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CafebookModel.Model.ModelWeb
{
    // --- DTO 1: DÙNG ĐỂ TẢI DỮ LIỆU LÊN TRANG (API -> RAZOR) ---

    public class ThanhToanLoadDto
    {
        public KhachHangThanhToanDto KhachHang { get; set; } = new();
        public List<KhuyenMaiThanhToanDto> KhuyenMaisHopLe { get; set; } = new();
        public decimal TiLeDoiDiemVND { get; set; } = 1000;

        [System.Text.Json.Serialization.JsonIgnore]
        public List<GioHangItemViewModel> SanPhamItems { get; set; } = new();
        [System.Text.Json.Serialization.JsonIgnore]
        public List<GioHangItemViewModel> SachItems { get; set; } = new();
        [System.Text.Json.Serialization.JsonIgnore]
        public decimal TongTienHang { get; set; }
    }

    public class KhachHangThanhToanDto
    {
        public int IdKhachHang { get; set; }
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        public string HoTen { get; set; } = string.Empty;
        [Required(ErrorMessage = "Vui lòng nhập SĐT")]
        public string SoDienThoai { get; set; } = string.Empty;
        [Required(ErrorMessage = "Vui lòng nhập Email")]
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng")]
        public string DiaChi { get; set; } = string.Empty;
        public int DiemTichLuy { get; set; }
    }

    /// <summary>
    /// SỬA: Thêm các trường kiểm tra thời gian
    /// </summary>
    public class KhuyenMaiThanhToanDto
    {
        public int IdKhuyenMai { get; set; }
        public string TenChuongTrinh { get; set; } = string.Empty;
        public string MaKhuyenMai { get; set; } = string.Empty;
        public string LoaiGiamGia { get; set; } = string.Empty;
        public decimal GiaTriGiam { get; set; }
        public decimal? GiamToiDa { get; set; }
        public int? IdSanPhamApDung { get; set; }
        public string? DieuKienApDung { get; set; }
        public decimal? HoaDonToiThieu { get; set; }

        // =======================================
        // === THÊM MỚI 3 TRƯỜNG TẠI ĐÂY ===
        // =======================================
        public string? NgayTrongTuan { get; set; }
        public TimeSpan? GioBatDau { get; set; }
        public TimeSpan? GioKetThuc { get; set; }
    }


    // --- DTO 2: DÙNG ĐỂ GỬI ĐƠN HÀNG (RAZOR -> API) ---

    public class ThanhToanSubmitDto
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên")]
        public string HoTen { get; set; } = string.Empty;
        [Required(ErrorMessage = "Vui lòng nhập SĐT")]
        public string SoDienThoai { get; set; } = string.Empty;
        [Required(ErrorMessage = "Vui lòng nhập địa chỉ giao hàng")]
        public string DiaChiGiaoHang { get; set; } = string.Empty;
        [EmailAddress]
        public string Email { get; set; } = string.Empty;
        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
        public string PhuongThucThanhToan { get; set; } = "COD";
        public int? IdKhuyenMai { get; set; }
        [Range(0, int.MaxValue, ErrorMessage = "Điểm sử dụng không hợp lệ")]
        public int DiemSuDung { get; set; } = 0;
        public string? GhiChu { get; set; }
        public List<CartItemDto> ItemsToPurchase { get; set; } = new();
    }


    // --- DTO 3: PHẢN HỒI SAU KHI TẠO ĐƠN (API -> RAZOR) ---

    public class ThanhToanResponseDto
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public int? IdHoaDonMoi { get; set; }
        public int? IdPhieuThueMoi { get; set; }
    }

    /// <summary>
    /// DTO tóm tắt đơn hàng cho trang ThanhToanThanhCongView
    /// </summary>
    public class ThanhToanThanhCongDto
    {
        public int IdHoaDonMoi { get; set; }
        public DateTime ThoiGianTao { get; set; }
        public string TrangThai { get; set; } = string.Empty;
        public string PhuongThucThanhToan { get; set; } = string.Empty;
        public string DiaChiGiaoHang { get; set; } = string.Empty;
        public string SoDienThoai { get; set; } = string.Empty;

        public List<GioHangItemViewModel> Items { get; set; } = new();

        public decimal TongTienHang { get; set; }
        public decimal GiamGia { get; set; }
        public decimal ThanhTien { get; set; }
    }
}