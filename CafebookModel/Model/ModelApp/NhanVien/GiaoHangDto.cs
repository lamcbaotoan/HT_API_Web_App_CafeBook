// Tệp: CafebookModel/Model/ModelApp/NhanVien/GiaoHangDto.cs
using System;
using System.Collections.Generic;

namespace CafebookModel.Model.ModelApp.NhanVien
{
    /// <summary>
    /// DTO chính để tải trang Giao Hàng
    /// </summary>
    public class GiaoHangViewDto
    {
        public List<GiaoHangItemDto> DonGiaoHang { get; set; } = new List<GiaoHangItemDto>();
        public List<NguoiGiaoHangDto> NguoiGiaoHangSanSang { get; set; } = new List<NguoiGiaoHangDto>();
    }

    /// <summary>
    /// DTO đại diện cho một dòng (hóa đơn) trong danh sách giao hàng
    /// </summary>
    public class GiaoHangItemDto
    {
        public int IdHoaDon { get; set; }
        public DateTime ThoiGianTao { get; set; }
        public string? TenKhachHang { get; set; }
        public string? SoDienThoaiGiaoHang { get; set; }
        public string? DiaChiGiaoHang { get; set; }
        public decimal ThanhTien { get; set; }

        /// <summary>
        /// Trạng thái thanh toán (ví dụ: "Chưa thanh toán", "Đã thanh toán")
        /// </summary>
        public string TrangThaiThanhToan { get; set; } = string.Empty;

        /// <summary>
        /// Trạng thái giao hàng (ví dụ: "Chờ xác nhận", "Đang giao", "Hoàn thành")
        /// </summary>
        public string? TrangThaiGiaoHang { get; set; }

        public int? IdNguoiGiaoHang { get; set; }
        public string? TenNguoiGiaoHang { get; set; }
    }

    /// <summary>
    /// DTO cho ComboBox chọn người giao hàng
    /// </summary>
    public class NguoiGiaoHangDto
    {
        public int IdNguoiGiaoHang { get; set; }
        public string TenNguoiGiaoHang { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO để gửi cập nhật từ DataGrid
    /// </summary>
    public class GiaoHangUpdateRequestDto
    {
        public string? TrangThaiGiaoHang { get; set; }
        public int? IdNguoiGiaoHang { get; set; }
    }
}