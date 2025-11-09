using System;
using System.ComponentModel.DataAnnotations;

namespace CafebookModel.Model.ModelWeb
{
    /// <summary>
    /// DTO dùng để tìm kiếm bàn trống
    /// </summary>
    public class TimBanRequestDto
    {
        [Required]
        public DateTime NgayDat { get; set; }

        [Required]
        public TimeSpan GioDat { get; set; }

        [Range(1, 50, ErrorMessage = "Số lượng khách phải từ 1 đến 50")]
        public int SoNguoi { get; set; }
    }

    /// <summary>
    /// DTO hiển thị kết quả bàn trống
    /// </summary>
    public class BanTrongDto
    {
        public int IdBan { get; set; }
        public string SoBan { get; set; } = string.Empty;
        public int SoGhe { get; set; }
        public string KhuVuc { get; set; } = string.Empty;
        public string? MoTa { get; set; }
    }

    /// <summary>
    /// DTO gửi yêu cầu đặt bàn từ Web
    /// </summary>
    public class DatBanWebRequestDto
    {
        // Thông tin khách (nếu chưa đăng nhập)
        public string? HoTen { get; set; }
        public string? SoDienThoai { get; set; }
        public string? Email { get; set; } // <-- SỬA ĐỔI: Thêm trường Email
        [Required]
        public int IdBan { get; set; }

        [Required]
        public DateTime NgayDat { get; set; }

        [Required]
        public TimeSpan GioDat { get; set; }

        [Required]
        public int SoLuongKhach { get; set; }

        public string? GhiChu { get; set; }
    }
}