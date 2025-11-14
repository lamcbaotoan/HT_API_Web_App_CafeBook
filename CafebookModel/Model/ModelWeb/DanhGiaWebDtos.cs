using System.ComponentModel.DataAnnotations;

namespace CafebookModel.Model.ModelWeb
{
    // DTO dùng khi khách hàng GỬI một đánh giá mới
    public class TaoDanhGiaDto
    {
        [Required]
        public int idHoaDon { get; set; }

        [Required] // Sửa: Chuyển thành bắt buộc
        public int idSanPham { get; set; }

        // idSach đã bị xóa

        [Required]
        [Range(1, 5)]
        public int SoSao { get; set; }
        public string? BinhLuan { get; set; }
        // File ảnh (IFormFile) sẽ được xử lý riêng tại Controller
    }

    // DTO dùng để HIỂN THỊ SP trên trang đánh giá (THÊM MỚI)
    public class SanPhamChoDanhGiaDto
    {
        public int IdSanPham { get; set; }
        public string TenSanPham { get; set; } = "";
        public string? HinhAnhUrl { get; set; }
        public bool DaDanhGia { get; set; }
    }

    // DTO dùng để HIỂN THỊ đánh giá trên trang chi tiết
    public class DanhGiaWebDto
    {
        public int IdDanhGia { get; set; }
        public string TenKhachHang { get; set; } = "";
        public string? AvatarKhachHang { get; set; }
        public int SoSao { get; set; }
        public string? BinhLuan { get; set; }
        public string? HinhAnhUrl { get; set; }
        public DateTime NgayTao { get; set; }
        public PhanHoiWebDto? PhanHoi { get; set; }
    }

    // DTO dùng để HIỂN THỊ phản hồi của nhân viên
    public class PhanHoiWebDto
    {
        public string TenNhanVien { get; set; } = "";
        public string NoiDung { get; set; } = "";
        public DateTime NgayTao { get; set; }
    }
}