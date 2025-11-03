using System;

namespace CafebookModel.Model.ModelApp
{
    public class ThongBaoDto
    {
        public int IdThongBao { get; set; }
        public string NoiDung { get; set; } = string.Empty;
        public DateTime ThoiGianTao { get; set; }
        public string? LoaiThongBao { get; set; }
        public int? IdLienQuan { get; set; } // idBan
        public bool DaXem { get; set; }
        public string? TenNhanVienTao { get; set; }
    }

    public class ThongBaoCountDto
    {
        public int UnreadCount { get; set; }
    }
}