using System;
using System.Collections.Generic;

namespace CafebookModel.Model.ModelApp.NhanVien
{
    // DTO chính chứa toàn bộ dữ liệu cho trang GoiMonView
    public class GoiMonViewDto
    {
        public HoaDonInfoDto HoaDonInfo { get; set; } = default!;
        public List<ChiTietDto> ChiTietItems { get; set; } = default!;
        public List<SanPhamDto> SanPhams { get; set; } = default!;
        public List<DanhMucDto> DanhMucs { get; set; } = default!;
        public List<KhuyenMaiDto> KhuyenMais { get; set; } = default!;
    }

    // Thông tin cơ bản của hóa đơn (hiển thị ở panel bên phải)
    public class HoaDonInfoDto
    {
        public int IdHoaDon { get; set; }
        public string SoBan { get; set; } = default!;
        public string LoaiHoaDon { get; set; } = default!;
        public decimal TongTienGoc { get; set; }
        public decimal GiamGia { get; set; }
        public decimal ThanhTien { get; set; }
        public int? IdKhuyenMai { get; set; }
    }

    // DTO cho một dòng trong DataGrid (ChiTietHoaDon)
    public class ChiTietDto
    {
        public int IdChiTietHoaDon { get; set; }
        public int IdSanPham { get; set; }
        public string TenSanPham { get; set; } = default!;
        public int SoLuong { get; set; }
        public decimal DonGia { get; set; }
        public decimal ThanhTien { get; set; }
    }

    // DTO cho một thẻ Sản Phẩm
    public class SanPhamDto
    {
        public int IdSanPham { get; set; }
        public string TenSanPham { get; set; } = default!;
        public decimal DonGia { get; set; }
        public string HinhAnh { get; set; } = default!;
        public int IdDanhMuc { get; set; }
    }

    // DTO cho Danh Mục (Sidebar trái)
    public class DanhMucDto
    {
        public int IdDanhMuc { get; set; }
        public string TenLoaiSP { get; set; } = default!;
    }

    // DTO cho ComboBox Khuyến mãi
    public class KhuyenMaiDto
    {
        public int IdKhuyenMai { get; set; }
        public string TenKhuyenMai { get; set; } = default!;
        public string LoaiGiamGia { get; set; } = default!;
        public decimal GiaTriGiam { get; set; }
    }

    // === CÁC DTO DÙNG ĐỂ GỬI YÊU CẦU LÊN API ===

    public class AddItemRequest
    {
        public int IdHoaDon { get; set; }
        public int IdSanPham { get; set; }
        public int SoLuong { get; set; }
    }

    public class UpdateSoLuongRequest
    {
        public int IdChiTietHoaDon { get; set; }
        public int SoLuongMoi { get; set; }
    }

    public class ThanhToanRequest
    {
        public int IdHoaDon { get; set; }
        public int? IdKhuyenMai { get; set; }
        public string PhuongThucThanhToan { get; set; } = default!;
    }

    public class ApplyPromotionRequest
    {
        public int IdHoaDon { get; set; }
        public int? IdKhuyenMai { get; set; }
    }

    // DTO này dùng chung cho cả Phiếu Tạm Tính và Phiếu Bếp
    public class PhieuGoiMonPrintDto
    {
        public string IdPhieu { get; set; } = string.Empty;
        public string TenQuan { get; set; } = string.Empty;
        public string DiaChiQuan { get; set; } = string.Empty;
        public string SdtQuan { get; set; } = string.Empty;
        public DateTime NgayTao { get; set; }
        public string TenNhanVien { get; set; } = string.Empty;
        public string SoBan { get; set; } = string.Empty;
        public List<ChiTietDto> ChiTiet { get; set; } = new List<ChiTietDto>();
        public decimal TongTienGoc { get; set; }
        public decimal GiamGia { get; set; }
        public decimal ThanhTien { get; set; }
    }
}