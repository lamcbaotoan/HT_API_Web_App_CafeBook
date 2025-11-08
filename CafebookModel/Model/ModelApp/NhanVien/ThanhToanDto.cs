using CafebookModel.Model.Entities;
using System.Collections.Generic;
using System; // Thêm

namespace CafebookModel.Model.ModelApp.NhanVien
{
    // DTO cho một dòng Phụ Thu
    public class PhuThuDto
    {
        public int IdPhuThu { get; set; }
        public string TenPhuThu { get; set; } = string.Empty;
        public decimal SoTien { get; set; }
        public string LoaiGiaTri { get; set; } = "VND";
        public decimal GiaTri { get; set; }
    }

    // DTO cho ComboBox tìm kiếm khách hàng
    public class KhachHangTimKiemDto
    {
        public int IdKhachHang { get; set; }
        public string DisplayText { get; set; } = string.Empty;
        public KhachHang KhachHangData { get; set; } = null!;
        public bool IsNew { get; set; } // <-- THÊM DÒNG NÀY
    }

    /// <summary>
    /// DTO chứa dữ liệu cho cửa sổ In Hóa Đơn (Tạm tính hoặc Cuối cùng)
    /// </summary>
    public class HoaDonPreviewDto
    {
        // Thông tin Cửa hàng
        public string TenQuan { get; set; } = "CafeBook";
        public string DiaChi { get; set; } = "N/A";
        public string SoDienThoai { get; set; } = "N/A";
        public string WifiMatKhau { get; set; } = "N/A";

        // Thông tin Hóa đơn
        public int IdHoaDon { get; set; }
        public string SoBan { get; set; } = "Mang về";
        public DateTime ThoiGianTao { get; set; }
        public string TenNhanVien { get; set; } = "Nhân viên";
        public string TenKhachHang { get; set; } = "Khách vãng lai";
        public bool IsProvisional { get; set; } = true; // Là hóa đơn Tạm tính?

        // Chi tiết
        public List<ChiTietDto> Items { get; set; } = new List<ChiTietDto>();
        public List<PhuThuDto> Surcharges { get; set; } = new List<PhuThuDto>();

        // Tổng tiền
        public decimal TongTienGoc { get; set; }
        public decimal GiamGiaKM { get; set; }
        public decimal GiamGiaDiem { get; set; }
        public decimal TongPhuThu { get; set; }
        public decimal ThanhTien { get; set; }

        // ### THÊM MỚI (YÊU CẦU 06/11) ###
        public string PhuongThucThanhToan { get; set; } = "Tiền mặt";
        public decimal KhachDua { get; set; }
        public decimal TienThoi { get; set; }
    }

    /// <summary>
    /// DTO tải dữ liệu ban đầu cho trang ThanhToanView
    /// </summary>
    public class ThanhToanViewDto
    {
        public HoaDonInfoDto HoaDonInfo { get; set; } = null!;
        public List<ChiTietDto> ChiTietItems { get; set; } = new List<ChiTietDto>();
        public int? IdKhuyenMaiDaApDung { get; set; }
        public List<PhuThuDto> PhuThusDaApDung { get; set; } = new List<PhuThuDto>();
        public List<PhuThu> PhuThusKhaDung { get; set; } = new List<PhuThu>();
        public KhachHang? KhachHang { get; set; }
        public List<KhachHangTimKiemDto> KhachHangsList { get; set; } = new List<KhachHangTimKiemDto>();
        public decimal DiemTichLuy_DoiVND { get; set; }
        public decimal DiemTichLuy_NhanVND { get; set; }
        public string TenQuan { get; set; } = string.Empty;
        public string DiaChi { get; set; } = string.Empty;
        public string SoDienThoai { get; set; } = string.Empty;
        public string WifiMatKhau { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO gửi lên API để xác nhận thanh toán
    /// </summary>
    public class ThanhToanRequestDto
    {
        public int IdHoaDonGoc { get; set; }
        public List<int> IdChiTietTach { get; set; } = new List<int>();
        public List<int> IdPhuThuTach { get; set; } = new List<int>();
        public int? IdKhuyenMai { get; set; }
        public string PhuongThucThanhToan { get; set; } = string.Empty;
        public decimal KhachDua { get; set; }
        public int DiemSuDung { get; set; }
        public int? IdKhachHang { get; set; }
    }
}