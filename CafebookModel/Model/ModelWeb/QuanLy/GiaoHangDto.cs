using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CafebookModel.Model.ModelWeb.QuanLy
{
    public class GiaoHangViewDto
    {
        public List<GiaoHangItemDto> DonGiaoHang { get; set; } = new List<GiaoHangItemDto>();
        public List<NguoiGiaoHangDto> NguoiGiaoHangSanSang { get; set; } = new List<NguoiGiaoHangDto>();
    }

    public class GiaoHangItemDto
    {
        public int IdHoaDon { get; set; }
        public DateTime ThoiGianTao { get; set; }
        public string? TenKhachHang { get; set; }
        public string? SoDienThoaiGiaoHang { get; set; }
        public string? DiaChiGiaoHang { get; set; }
        public decimal ThanhTien { get; set; }
        public string TrangThaiThanhToan { get; set; } = string.Empty;
        public string? TrangThaiGiaoHang { get; set; }
        public int? IdNguoiGiaoHang { get; set; }
        public string? TenNguoiGiaoHang { get; set; }
        public string? GhiChu { get; set; }

        // --- THÊM MỚI: Để biết đơn này của nhân viên nào ---
        public int? IdNhanVien { get; set; }
    }

    public class NguoiGiaoHangDto
    {
        public int IdNguoiGiaoHang { get; set; }
        public string TenNguoiGiaoHang { get; set; } = string.Empty;
    }

    public class GiaoHangUpdateRequestDto
    {
        public string? TrangThaiGiaoHang { get; set; }
        public int? IdNguoiGiaoHang { get; set; }

        // --- THÊM MỚI: Để lưu ID Shipper ---
        public int? IdNhanVien { get; set; }

        [JsonIgnore]
        public IFormFile? HinhAnhXacNhan { get; set; }
    }

    /// <summary>
    /// DTO thống kê lịch sử chạy đơn trong ngày của Shipper
    /// </summary>
    public class ShipperHistorySummaryDto
    {
        public decimal TongTienMatCam { get; set; } // Chỉ tính đơn COD
        public int TongDonHoanThanh { get; set; }
        public int TongDonHuy { get; set; }
        public List<GiaoHangItemDto> LichSuDonHang { get; set; } = new List<GiaoHangItemDto>();
    }
}