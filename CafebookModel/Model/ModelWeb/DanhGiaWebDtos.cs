using System.ComponentModel.DataAnnotations;

namespace CafebookModel.Model.ModelWeb
{
    // DTO dùng khi khách hàng GỬI một đánh giá mới
    public class TaoDanhGiaDto
    {
        [Required]
        public int idHoaDon { get; set; }

        [Required]
        public int idSanPham { get; set; }

        [Required(ErrorMessage = "Vui lòng chọn số sao.")]
        [Range(1, 5, ErrorMessage = "Vui lòng chọn số sao.")]
        public int SoSao { get; set; }

        // <<< SỬA LỖI TẠI ĐÂY >>>
        [Required(ErrorMessage = "Vui lòng nhập bình luận của bạn.")]
        [MinLength(10, ErrorMessage = "Bình luận cần ít nhất 10 ký tự.")]
        public string BinhLuan { get; set; } = string.Empty; // Xóa '?' để
    }

    // DTO dùng để HIỂN THỊ SP trên trang đánh giá
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

    // DTO cho Phản Hồi
    public class PhanHoiWebDto
    {
        public string TenNhanVien { get; set; } = "";
        public string NoiDung { get; set; } = "";
        public DateTime NgayTao { get; set; }
    }
}