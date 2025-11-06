using System;

namespace CafebookModel.Model.ModelApp
{
    /// <summary>
    /// DTO hiển thị danh sách phiếu lương đã chốt
    /// </summary>
    public class PhieuLuongDto
    {
        public int IdPhieuLuong { get; set; }
        public int IdNhanVien { get; set; }
        public string HoTenNhanVien { get; set; } = string.Empty;
        public int Thang { get; set; }
        public int Nam { get; set; }
        public decimal TongGioLam { get; set; }
        public decimal ThucLanh { get; set; }
        public DateTime NgayChot { get; set; }
        public string TrangThai { get; set; } = string.Empty; // "Đã chốt", "Đã phát"
    }


    /// <summary>
    /// DTO chi tiết phiếu lương để in hoặc xem
    /// </summary>
    public class PhieuLuongChiTietDto
    {
        public int IdPhieuLuong { get; set; }
        public string HoTenNhanVien { get; set; } = string.Empty;
        public int Thang { get; set; }
        public int Nam { get; set; }
        public decimal LuongCoBan { get; set; }
        public decimal TongGioLam { get; set; }
        public decimal TienThuong { get; set; }
        public decimal KhauTru { get; set; }
        public decimal ThucLanh { get; set; }
        public string TrangThai { get; set; } = string.Empty;
        public DateTime? NgayPhat { get; set; }
        public string? TenNguoiPhat { get; set; }

        public DateTime NgayBatDauKy => new DateTime(Nam, Thang, 1);
        public DateTime NgayKetThucKy => new DateTime(Nam, Thang, 1).AddMonths(1).AddDays(-1);
    }


    /// <summary>
    /// DTO gửi lên khi xác nhận phát lương
    /// </summary>
    public class PhatLuongXacNhanDto
    {
        public int IdNguoiPhat { get; set; }
    }
}