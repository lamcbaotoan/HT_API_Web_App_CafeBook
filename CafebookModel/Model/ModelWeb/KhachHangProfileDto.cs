using System.ComponentModel.DataAnnotations;

namespace CafebookModel.Model.ModelWeb
{
    /// <summary>
    /// DTO cho trang TỔNG QUAN
    /// </summary>
    public class KhachHangTongQuanDto
    {
        public int DiemTichLuy { get; set; }
        public int TongHoaDon { get; set; }
        public decimal TongChiTieu { get; set; }
        public DateTime NgayTao { get; set; }
    }

    /// <summary>
    /// DTO dùng để TẢI thông tin trang HỒ SƠ
    /// </summary>
    public class KhachHangProfileDto
    {
        public int IdKhachHang { get; set; }
        public string HoTen { get; set; } = string.Empty;
        public string? SoDienThoai { get; set; }
        public string? Email { get; set; }
        public string? DiaChi { get; set; }
        public string? TenDangNhap { get; set; }
        public string? AnhDaiDienUrl { get; set; }
    }

    /// <summary>
    /// Model dùng để CẬP NHẬT thông tin hồ sơ
    /// </summary>
    public class ProfileUpdateModel
    {
        [Required(ErrorMessage = "Họ tên không được để trống")]
        public string HoTen { get; set; } = string.Empty;

        [Phone(ErrorMessage = "Số điện thoại không hợp lệ")]
        public string? SoDienThoai { get; set; }

        [EmailAddress(ErrorMessage = "Email không hợp lệ")]
        public string? Email { get; set; }

        public string? DiaChi { get; set; }

        // ==================================
        // === THÊM MỚI ===
        // ==================================
        [Required(ErrorMessage = "Tên đăng nhập không được để trống")]
        [StringLength(100, ErrorMessage = "Tên đăng nhập phải dài từ 6 đến 100 ký tự.", MinimumLength = 6)]
        public string TenDangNhap { get; set; } = string.Empty;
    }

    /// <summary>
    /// Model dùng để ĐỔI MẬT KHẨU
    /// </summary>
    public class PasswordChangeModel
    {
        [Required(ErrorMessage = "Vui lòng nhập mật khẩu cũ")]
        [DataType(DataType.Password)]
        public string MatKhauCu { get; set; } = string.Empty;

        [Required(ErrorMessage = "Vui lòng nhập mật khẩu mới")]
        [StringLength(100, ErrorMessage = "Mật khẩu mới phải dài ít nhất 6 ký tự.", MinimumLength = 6)]
        [DataType(DataType.Password)]
        public string MatKhauMoi { get; set; } = string.Empty;

        [DataType(DataType.Password)]
        [Compare("MatKhauMoi", ErrorMessage = "Mật khẩu mới và mật khẩu xác nhận không khớp.")]
        public string XacNhanMatKhauMoi { get; set; } = string.Empty;
    }
}