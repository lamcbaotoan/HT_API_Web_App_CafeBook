// Tệp: CafebookModel/Model/ModelApp/NhanVien/ChamCongDto.cs
using System;
using System.Collections.Generic; // THÊM USING NÀY

namespace CafebookModel.Model.ModelApp.NhanVien
{
    /// <summary>
    /// DTO trả về trạng thái tổng hợp cho Bảng điều khiển Chấm công
    /// </summary>
    public class ChamCongDashboardDto
    {
        /// <summary>
        /// Trạng thái chính: 
        /// "KhongCoCa", "ChuaChamCong", "DaChamCong", "DaTraCa", "NghiPhep"
        /// </summary>
        public string TrangThai { get; set; } = "KhongCoCa";

        // Thông tin ca làm
        public string? TenCa { get; set; }
        public TimeSpan? GioBatDauCa { get; set; }
        public TimeSpan? GioKetThucCa { get; set; }

        // Thông tin thực tế
        public DateTime? GioVao { get; set; }
        public DateTime? GioRa { get; set; }
        public decimal? SoGioLam { get; set; }

        // Thông tin nhân viên
        public string TenNhanVien { get; set; } = string.Empty;

        // === CẢI TIẾN MỚI ===

        /// <summary>
        /// Trạng thái đơn nghỉ: null (không có), "Chờ duyệt", "Đã duyệt", "Đã từ chối"
        /// </summary>
        public string? TrangThaiDonNghi { get; set; }

        /// <summary>
        /// Số lần chấm công trễ trong tháng này
        /// </summary>
        public int SoLanDiTreThangNay { get; set; } = 0;
    }


    // === THÊM CÁC LỚP MỚI CHO TRANG LỊCH SỬ ===

    public class LichSuItemDto
    {
        public string Ngay { get; set; } = string.Empty;
        public string CaLamViec { get; set; } = string.Empty;
        public string GioVao { get; set; } = string.Empty;
        public string GioRa { get; set; } = string.Empty;
        public string DiTre { get; set; } = string.Empty; // (ví dụ: "15 phút")
        public decimal SoGioLam { get; set; }
    }

    public class DonNghiItemDto
    {
        public int IdDonXinNghi { get; set; }
        public string LoaiDon { get; set; } = string.Empty;
        public string ThoiGian { get; set; } = string.Empty; // (ví dụ: "01/11 - 02/11")
        public string LyDo { get; set; } = string.Empty;
        public string TrangThai { get; set; } = string.Empty;
        public string? PheDuyet { get; set; } // (Ghi chú của quản lý)
    }

    public class ThongKeChamCongDto
    {
        public decimal TongGioLam { get; set; }
        public int SoLanDiTre { get; set; }
        public int SoNgayNghiPhep { get; set; }
    }

    public class LichSuChamCongPageDto
    {
        public List<LichSuItemDto> LichSuChamCong { get; set; } = new List<LichSuItemDto>();
        public List<DonNghiItemDto> DanhSachDonNghi { get; set; } = new List<DonNghiItemDto>();
        public ThongKeChamCongDto ThongKe { get; set; } = new ThongKeChamCongDto();
    }

}