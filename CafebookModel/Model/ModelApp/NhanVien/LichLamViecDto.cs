// Tệp: CafebookModel/Model/ModelApp/NhanVien/LichLamViecDto.cs
using System;
using System.Collections.Generic;

namespace CafebookModel.Model.ModelApp.NhanVien
{
    /// <summary>
    /// DTO cho chế độ xem Ngày (Day) và Tuần (Week)
    /// </summary>
    public class LichLamViecViewDto
    {
        public DateTime NgayBatDauTuan { get; set; }
        public DateTime NgayKetThucTuan { get; set; }
        public List<LichLamViecItemDto> LichLamViecTrongTuan { get; set; } = new List<LichLamViecItemDto>();
        public List<LichNghiItemDto> DonNghiTrongTuan { get; set; } = new List<LichNghiItemDto>();
    }

    /// <summary>
    /// DTO cho 1 ca làm (event có giờ)
    /// </summary>
    public class LichLamViecItemDto
    {
        public int IdLichLamViec { get; set; }
        public string TenCa { get; set; } = string.Empty;
        public DateTime NgayLam { get; set; }
        public TimeSpan GioBatDau { get; set; }
        public TimeSpan GioKetThuc { get; set; }
    }

    /// <summary>
    /// DTO cho 1 đơn nghỉ (event cả ngày)
    /// </summary>
    public class LichNghiItemDto
    {
        public string LoaiDon { get; set; } = string.Empty;
        public DateTime NgayBatDau { get; set; }
        public DateTime NgayKetThuc { get; set; }
    }


    // === THÊM MỚI CHO CHẾ ĐỘ XEM THÁNG ===

    /// <summary>
    /// DTO cho chế độ xem Tháng (Month)
    /// </summary>
    public class LichLamViecThangDto
    {
        // Danh sách các ngày trong tháng có sự kiện
        public List<LichLamViecNgayDto> NgayCoSuKien { get; set; } = new List<LichLamViecNgayDto>();
    }

    /// <summary>
    /// Đại diện cho 1 ngày trong ô lịch tháng
    /// </summary>
    public class LichLamViecNgayDto
    {
        public DateTime Ngay { get; set; }
        // Danh sách tên các sự kiện, ví dụ: "Ca Sáng (8h-12h)", "Nghỉ phép"
        public List<string> SuKien { get; set; } = new List<string>();
    }
}