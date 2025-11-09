// Tệp: CafebookModel/Model/ModelApp/NhanVien/PhieuLuongDto.cs
using System;
using System.Collections.Generic;

namespace CafebookModel.Model.ModelApp.NhanVien
{
    /// <summary>
    /// DTO tổng quan, chứa danh sách các phiếu lương
    /// </summary>
    public class PhieuLuongViewDto
    {
        public List<PhieuLuongItemDto> DanhSachPhieuLuong { get; set; } = new List<PhieuLuongItemDto>();
    }

    /// <summary>
    /// DTO cho một mục trong danh sách (cột bên trái)
    /// </summary>
    public class PhieuLuongItemDto
    {
        public int IdPhieuLuong { get; set; }
        public int Thang { get; set; }
        public int Nam { get; set; }
        public decimal ThucLanh { get; set; }
        public string TrangThai { get; set; } = string.Empty;
        public string TieuDe => $"Phiếu lương tháng {Thang}/{Nam}";
    }

    /// <summary>
    /// DTO chi tiết cho một phiếu lương (cột bên phải)
    /// </summary>
    public class PhieuLuongChiTietDto
    {
        public int IdPhieuLuong { get; set; }
        public int Thang { get; set; }
        public int Nam { get; set; }
        public decimal LuongCoBan { get; set; }
        public decimal TongGioLam { get; set; }
        public decimal TienLuongTheoGio { get; set; } // (LuongCoBan * TongGioLam)
        public decimal TongTienThuong { get; set; }
        public decimal TongKhauTru { get; set; }
        public decimal ThucLanh { get; set; }
        public string TrangThai { get; set; } = string.Empty;
        public DateTime? NgayPhatLuong { get; set; }
        public string? TenNguoiPhat { get; set; }

        public List<PhieuThuongPhatItemDto> DanhSachThuong { get; set; } = new List<PhieuThuongPhatItemDto>();
        public List<PhieuThuongPhatItemDto> DanhSachPhat { get; set; } = new List<PhieuThuongPhatItemDto>();
    }

    /// <summary>
    /// DTO cho một dòng thưởng/phạt chi tiết
    /// </summary>
    public class PhieuThuongPhatItemDto
    {
        public DateTime NgayTao { get; set; }
        public decimal SoTien { get; set; }
        public string LyDo { get; set; } = string.Empty;
        public string TenNguoiTao { get; set; } = string.Empty;
    }
}