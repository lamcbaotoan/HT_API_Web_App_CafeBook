using System.Collections.Generic;
using System.Linq;

namespace CafebookModel.Model.ModelWeb
{
    /// <summary>
    /// DTO cơ bản đại diện cho một món hàng trong giỏ.
    /// Đây là đối tượng SẼ ĐƯỢC LƯU trong Session.
    /// </summary>
    public class CartItemDto
    {
        public int Id { get; set; } // Có thể là IdSach hoặc IdSanPham

        // Phân biệt 2 loại: "Sach" hoặc "SanPham"
        public string Loai { get; set; } = string.Empty;

        public int SoLuong { get; set; }
    }

    /// <summary>
    /// Model đầy đủ thông tin của một món hàng ĐỂ HIỂN THỊ ra View.
    /// Model này được xây dựng lúc tải trang giỏ hàng.
    /// </summary>
    public class GioHangItemViewModel
    {
        public int Id { get; set; }
        public string Loai { get; set; } = string.Empty;
        public string TenHienThi { get; set; } = string.Empty;
        public string? HinhAnhUrl { get; set; }
        public decimal DonGia { get; set; }
        public int SoLuong { get; set; }
        public decimal ThanhTien => DonGia * SoLuong;
    }

    /// <summary>
    /// ViewModel chính cho trang GioHangView, chứa danh sách các món
    /// và tổng tiền.
    /// </summary>
    public class GioHangViewModel
    {
        public List<GioHangItemViewModel> Items { get; set; } = new();
        public decimal TongTien => Items.Sum(item => item.ThanhTien);
    }
}