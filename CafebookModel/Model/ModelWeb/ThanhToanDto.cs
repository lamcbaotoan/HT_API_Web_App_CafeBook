using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace CafebookModel.Model.ModelWeb
{
    /// <summary>
    /// ViewModel cho trang ThanhToanView, chứa thông tin
    /// các mục và tổng tiền để hiển thị.
    /// </summary>
    public class ThanhToanViewModel
    {
        // Dùng lại GioHangItemViewModel từ DTO giỏ hàng
        public List<GioHangItemViewModel> Items { get; set; } = new();
        public decimal TongTien { get; set; }

        // Thông tin khách hàng (nếu đã đăng nhập)
        public string? TenKhachHang { get; set; }
        public string? SoDienThoai { get; set; }
        public string? DiaChi { get; set; }
    }

    /// <summary>
    /// Model đầu vào, bind dữ liệu từ form thanh toán.
    /// </summary>
    public class ThanhToanInputModel
    {
        [Required(ErrorMessage = "Vui lòng nhập họ tên người nhận")]
        public string TenNguoiNhan { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập số điện thoại")]
        [Phone]
        public string SoDienThoai { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập địa chỉ nhận hàng")]
        public string DiaChi { get; set; } = string.Empty;

        public string? GhiChu { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn phương thức thanh toán")]
        public string PhuongThucThanhToan { get; set; } = string.Empty; // "COD", "BankTransfer"
    }

    /// <summary>
    /// DTO đặc biệt để gửi từ Web (Frontend) lên API (Backend)
    /// sau khi người dùng xác nhận.
    /// </summary>
    public class DatHangRequestDto
    {
        public ThanhToanInputModel ThongTinNhanHang { get; set; } = new();
        public List<CartItemDto> Items { get; set; } = new();
    }
}