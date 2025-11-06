namespace CafebookModel.Model.ModelApp.NhanVien
{
    // DTO này được API sử dụng để gửi về cửa sổ ChonKhuyenMaiWindow
    public class KhuyenMaiHienThiDto
    {
        public int IdKhuyenMai { get; set; }
        public string MaKhuyenMai { get; set; } = string.Empty;
        public string TenChuongTrinh { get; set; } = string.Empty;
        public string? DieuKienApDung { get; set; } // Hiển thị cho nhân viên
        public string LoaiGiamGia { get; set; } = string.Empty; // PhanTram, SoTien
        public decimal GiaTriGiam { get; set; }
        public decimal? GiamToiDa { get; set; }

        // === Các trường tính toán từ API ===
        public bool IsEligible { get; set; } // Đủ điều kiện
        public string? IneligibilityReason { get; set; } // Lý do không đủ điều kiện

        // === THÊM DÒNG NÀY ĐỂ SỬA LỖI BUILD ===
        public decimal CalculatedDiscount { get; set; } // Giá trị giảm thực tế để sắp xếp
    }
}