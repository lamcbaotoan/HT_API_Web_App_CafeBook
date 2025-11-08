using System;
using System.Collections.Generic; // Thêm

namespace CafebookModel.Model.ModelApp.NhanVien
{
    // DTO cho một món ăn hiển thị trên màn hình bếp
    public class CheBienItemDto
    {
        public int IdTrangThaiCheBien { get; set; }
        public int IdSanPham { get; set; } // <-- THÊM MỚI
        public string TenMon { get; set; } = string.Empty;
        public int SoLuong { get; set; }
        public string SoBan { get; set; } = string.Empty;
        public string? GhiChu { get; set; }
        public string TrangThai { get; set; } = string.Empty; // Chờ làm, Đang làm
        public DateTime ThoiGianGoi { get; set; }
        public string NhomIn { get; set; } = string.Empty;

        // === SỬA LỖI XamlParseException TẠI ĐÂY ===
        // Chuyển đổi các thuộc tính expression-bodied (=>) thành các getter đầy đủ.

        /// <summary>
        /// Thuộc tính tính toán cho WPF
        /// </summary>
        public string ThoiGianGoiDisplay
        {
            get { return ThoiGianGoi.ToString("HH:mm:ss"); }
        }

        public string ThoiGianCho
        {
            get
            {
                var thoiGian = (DateTime.Now - ThoiGianGoi).TotalMinutes;
                return $"({Math.Floor(thoiGian)} phút)";
            }
        }

        public bool IsChoLam
        {
            get { return TrangThai == "Chờ làm"; }
        }

        public bool IsDangLam
        {
            get { return TrangThai == "Đang làm"; }
        }
    }

    // === THÊM MỚI DTO CHO CÔNG THỨC ===
    public class CongThucItemDto
    {
        public string TenNguyenLieu { get; set; } = string.Empty;
        public decimal SoLuongSuDung { get; set; }
        public string TenDonVi { get; set; } = string.Empty;
    }
}