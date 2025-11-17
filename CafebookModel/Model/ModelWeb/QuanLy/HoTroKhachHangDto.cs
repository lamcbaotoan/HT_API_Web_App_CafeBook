// Tập tin: CafebookModel/Model/ModelWeb/QuanLy/HoTroKhachHangDto.cs
using CafebookModel.Model.ModelWeb; // Để tái sử dụng ChatMessageDto
using System;
using System.Collections.Generic;

namespace CafebookModel.Model.ModelWeb.QuanLy
{
    /// <summary>
    /// DTO cho danh sách phiếu hỗ trợ (dùng cho trang dashboard của nhân viên)
    /// </summary>
    public class HoTroKhachHangListDto
    {
        public int IdThongBao { get; set; }
        public string TenKhachHang { get; set; } = string.Empty; // Sẽ là "Khách vãng lai (GuestId)" nếu chưa đăng nhập
        public string? NoiDungYeuCau { get; set; }
        public DateTime ThoiGianTao { get; set; }
        public string TrangThai { get; set; } = string.Empty;
        public string? GhiChuTuAI { get; set; }
    }

    /// <summary>
    /// DTO chi tiết cho một phiếu hỗ trợ (dùng cho trang trả lời)
    /// </summary>
    public class HoTroKhachHangDetailDto
    {
        public int IdThongBao { get; set; }
        public string TenKhachHang { get; set; } = string.Empty;
        public int? IdKhachHang { get; set; }
        public string? GuestSessionId { get; set; }
        public string? NoiDungYeuCau { get; set; }
        public DateTime ThoiGianTao { get; set; }
        public string TrangThai { get; set; } = string.Empty;
        public string? GhiChuTuAI { get; set; }

        // Tái sử dụng DTO từ HoTroDto.cs
        public List<ChatMessageDto> LichSuChat { get; set; } = new List<ChatMessageDto>();
    }

    /// <summary>
    /// DTO khi nhân viên gửi phản hồi
    /// </summary>
    public class HoTroKhachHangReplyDto
    {
        public int IdThongBao { get; set; }
        public string NoiDungTraLoi { get; set; } = string.Empty;
    }
}