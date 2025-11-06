// File: CafebookModel/Model/ModelApp/NhanSuDto.cs (Cập nhật)

using Microsoft.AspNetCore.Http;
using System;
using System.Collections.Generic;
using System.Text.Json.Serialization;

namespace CafebookModel.Model.ModelApp
{
    /// <summary>
    /// DTO cho DataGrid chính (Danh sách Nhân Viên) - ĐÃ ĐỔI TÊN
    /// </summary>
    public class NhanVienGridDto
    {
        public int IdNhanVien { get; set; }
        public string HoTen { get; set; } = string.Empty;
        public string TenVaiTro { get; set; } = string.Empty;
        public decimal LuongCoBan { get; set; }
        public string TrangThaiLamViec { get; set; } = string.Empty;
    }

    /// <summary>
    /// SỬA: DTO cho Form (GetDetails)
    /// </summary>
    public class NhanVienDetailDto
    {
        public int IdNhanVien { get; set; }
        public string HoTen { get; set; } = string.Empty;
        public string TenDangNhap { get; set; } = string.Empty;
        public int IdVaiTro { get; set; }
        public decimal LuongCoBan { get; set; }
        public string TrangThaiLamViec { get; set; } = string.Empty;
        public string? SoDienThoai { get; set; }
        public string? Email { get; set; }
        public string? DiaChi { get; set; }
        public DateTime NgayVaoLam { get; set; }
        public string? AnhDaiDienUrl { get; set; } // <-- SỬA: Dùng URL
    }

    /// <summary>
    /// SỬA: DTO cho Form (Thêm/Sửa)
    /// </summary>
    public class NhanVienUpdateRequestDto
    {
        public int IdNhanVien { get; set; }
        public string HoTen { get; set; } = string.Empty;
        public string TenDangNhap { get; set; } = string.Empty;
        public string? MatKhau { get; set; }
        public int IdVaiTro { get; set; }
        public decimal LuongCoBan { get; set; }
        public string TrangThaiLamViec { get; set; } = string.Empty;
        public string? SoDienThoai { get; set; }
        public string? Email { get; set; }
        public string? DiaChi { get; set; }
        public DateTime NgayVaoLam { get; set; }

        // SỬA: Xóa Base64, thêm 2 dòng này
        [JsonIgnore]
        public IFormFile? AnhDaiDienUpload { get; set; }
        [JsonIgnore]
        public bool XoaAnhDaiDien { get; set; } = false;
    }

    /// <summary>
    /// DTO cho ComboBox lọc - (Giữ nguyên)
    /// </summary>
    public class NhanSuFiltersDto
    {
        // Dùng FilterLookupDto (đã có)
        public List<FilterLookupDto> VaiTros { get; set; } = new();
    }

    /// <summary>
    /// DTO cho DataGrid Vai Trò - (Giữ nguyên)
    /// </summary>
    public class VaiTroDto
    {
        public int IdVaiTro { get; set; }
        public string TenVaiTro { get; set; } = string.Empty;
        public string? MoTa { get; set; }
    }

    /// <summary>
    /// DTO cho một Quyền, dùng để hiển thị trên UI (ListView)
    /// </summary>
    public class QuyenDto
    {
        public string IdQuyen { get; set; } = string.Empty;
        public string TenQuyen { get; set; } = string.Empty;
        public string NhomQuyen { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO dùng để gửi cấu hình quyền mới lên API
    /// </summary>
    public class PhanQuyenDto
    {
        public int IdVaiTro { get; set; }
        public List<string> DanhSachIdQuyen { get; set; } = new List<string>();
    }

    // === BẮT ĐẦU THÊM MỚI CHO YÊU CẦU #4 ===

    /// <summary>
    /// DTO cho Ca Làm Việc Mẫu (Bảng CaLamViec)
    /// </summary>
    public class CaLamViecDto
    {
        public int IdCa { get; set; }
        public string TenCa { get; set; } = string.Empty;

        // Dùng TimeSpan để API/WPF làm việc, CSDL sẽ lưu kiểu TIME
        public TimeSpan GioBatDau { get; set; }
        public TimeSpan GioKetThuc { get; set; }
    }

    /// <summary>
    /// DTO dùng để hiển thị 1 mục trong lịch làm việc
    /// (Kết hợp từ LichLamViec và DonXinNghi)
    /// </summary>
    public class LichLamViecDisplayDto
    {
        public int IdLich { get; set; } // Có thể là idLichLamViec hoặc idDonXinNghi
        public int IdNhanVien { get; set; }
        public string HoTenNhanVien { get; set; } = string.Empty;
        public DateTime Ngay { get; set; }

        // Loại lịch: "CaLam" hoặc "NghiPhep"
        public string LoaiLich { get; set; } = string.Empty;

        // Thông tin nếu là CaLam
        public string? TenCa { get; set; }
        public TimeSpan? GioBatDau { get; set; }
        public TimeSpan? GioKetThuc { get; set; }

        // Thông tin nếu là NghiPhep
        public string? LyDoNghi { get; set; }
    }

    /// <summary>
    /// DTO dùng để gán lịch làm việc hàng loạt
    /// </summary>
    public class LichLamViecCreateDto
    {
        public DateTime NgayGanLich { get; set; }
        public int IdCa { get; set; }
        public List<int> DanhSachIdNhanVien { get; set; } = new List<int>();
    }

    /// <summary>
    /// DTO đơn giản để chọn nhân viên gán lịch
    /// </summary>
    public class NhanVienLookupDto
    {
        public int IdNhanVien { get; set; }
        public string HoTen { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO cho kết quả trả về của API Gán Lịch
    /// </summary>
    public class LichLamViecAssignResponseDto
    {
        public string Message { get; set; } = string.Empty;
        public List<string> Failures { get; set; } = new List<string>();
    }

    // === BẮT ĐẦU THÊM MỚI CHO YÊU CẦU #5 ===

    /// <summary>
    /// DTO để hiển thị danh sách Đơn Xin Nghỉ trên DataGrid
    /// </summary>
    public class DonXinNghiDto
    {
        public int IdDonXinNghi { get; set; }
        public string TenNhanVien { get; set; } = string.Empty;
        public string LoaiDon { get; set; } = string.Empty;
        public DateTime NgayBatDau { get; set; }
        public DateTime NgayKetThuc { get; set; }
        public string LyDo { get; set; } = string.Empty;
        public string TrangThai { get; set; } = string.Empty;
        public string? TenNguoiDuyet { get; set; }
        public DateTime? NgayDuyet { get; set; }
        public string? GhiChuPheDuyet { get; set; }
    }

    /// <summary>
    /// DTO dùng để Thêm mới một đơn
    /// </summary>
    public class DonXinNghiCreateDto
    {
        // Quản lý có thể tạo đơn cho nhân viên
        public int IdNhanVien { get; set; }
        public string LoaiDon { get; set; } = string.Empty;
        public string LyDo { get; set; } = string.Empty;
        public DateTime NgayBatDau { get; set; }
        public DateTime NgayKetThuc { get; set; }
    }

    /// <summary>
    /// DTO dùng để Duyệt/Từ chối một đơn
    /// </summary>
    public class DonXinNghiActionDto
    {
        public int IdNguoiDuyet { get; set; }
        public string? GhiChuPheDuyet { get; set; }
    }

    // === BẮT ĐẦU THÊM MỚI CHO YÊU CẦU #6 ===

    /// <summary>
    /// DTO hiển thị Bảng Chấm Công (Module 6.1)
    /// </summary>
    public class ChamCongDto
    {
        public int IdChamCong { get; set; }
        public int IdLichLamViec { get; set; }
        public string HoTenNhanVien { get; set; } = string.Empty;
        public string TenCa { get; set; } = string.Empty;
        public DateTime NgayLam { get; set; }
        public TimeSpan GioCaBatDau { get; set; }
        public TimeSpan GioCaKetThuc { get; set; }
        public DateTime? GioVao { get; set; }
        public DateTime? GioRa { get; set; }
        public decimal SoGioLam { get; set; } // <-- SỬA: double -> decimal
        public string TrangThai { get; set; } = string.Empty; // "Đúng giờ", "Đi trễ", "Về sớm"
    }

    /// <summary>
    /// DTO cập nhật Chấm công thủ công (Module 6.1)
    /// </summary>
    public class ChamCongUpdateDto
    {
        public int IdChamCong { get; set; }
        public int IdLichLamViec { get; set; } // <-- THÊM DÒNG NÀY
        public DateTime? GioVaoMoi { get; set; }
        public DateTime? GioRaMoi { get; set; }
    }

    /// <summary>
    /// DTO hiển thị Thưởng/Phạt thủ công (Module 6.2.3)
    /// </summary>
    public class PhieuThuongPhatDto
    {
        public int IdPhieuThuongPhat { get; set; }
        public int IdNhanVien { get; set; }
        public string HoTenNhanVien { get; set; } = string.Empty;
        public DateTime NgayTao { get; set; }
        public decimal SoTien { get; set; }
        public string LyDo { get; set; } = string.Empty;
        public string TenNguoiTao { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO tạo Thưởng/Phạt thủ công (Module 6.2.3)
    /// </summary>
    public class PhieuThuongPhatCreateDto
    {
        public int IdNhanVien { get; set; }
        public int IdNguoiTao { get; set; }
        public decimal SoTien { get; set; }
        public string LyDo { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO Bảng kê lương (Trước khi chốt) (Module 6.3)
    /// </summary>
    public class LuongBangKeDto
    {
        public int IdNhanVien { get; set; }
        public string HoTenNhanVien { get; set; } = string.Empty;
        public decimal LuongCoBan { get; set; } // Lương/giờ
        public decimal TongGioLam { get; set; }
        public decimal TienLuongGio { get; set; }
        public decimal TongThuong { get; set; } // Auto (OT) + Manual
        public decimal TongPhat { get; set; }  // Auto (Late) + Manual
        public decimal ThucLanh { get; set; }
        public string ChiTiet { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO Chốt Lương (Module 6.4)
    /// </summary>
    public class LuongFinalizeDto
    {
        public int Thang { get; set; }
        public int Nam { get; set; }
        public int IdNguoiChot { get; set; }
        // Gửi toàn bộ bảng kê lên để lưu
        public List<LuongBangKeDto> DanhSachBangKe { get; set; } = new List<LuongBangKeDto>();
    }

    /// <summary>
    /// DTO để Tải và Lưu toàn bộ Cài Đặt Nhân Sự
    /// </summary>
    public class CaiDatNhanSuDto
    {
        // Giờ làm & OT
        public decimal GioLamChuan { get; set; }
        public decimal HeSoOT { get; set; }

        // Phạt đi trễ
        public int PhatDiTre_Phut { get; set; }
        public decimal PhatDiTre_HeSo { get; set; }

        // Thưởng chuyên cần
        public int ChuyenCan_SoNgay { get; set; }
        public decimal ChuyenCan_TienThuong { get; set; }

        // Chính sách nghỉ
        public int PhepNam_MacDinh { get; set; }
    }

    // === BẮT ĐẦU THÊM MỚI CHO YÊU CẦU #8 ===

    /// <summary>
    /// DTO Gửi yêu cầu lọc Báo cáo
    /// </summary>
    public class BaoCaoNhanSuRequestDto
    {
        public DateTime StartDate { get; set; }
        public DateTime EndDate { get; set; }
        public int IdNhanVien { get; set; } // 0 = Tất cả
        public int IdVaiTro { get; set; } // 0 = Tất cả
        public string TrangThaiNhanVien { get; set; } = "Đang làm việc"; // "Tất cả", "Đang làm việc", "Nghỉ việc"
    }

    /// <summary>
    /// DTO (Cha) chứa toàn bộ kết quả Báo cáo
    /// </summary>
    public class BaoCaoNhanSuDto
    {
        public BaoCaoNhanSu_KpiDto Kpi { get; set; } = new();
        public List<BaoCaoNhanSu_BangLuongDto> BangLuongChiTiet { get; set; } = new();
        public List<BaoCaoNhanSu_NghiPhepDto> ThongKeNghiPhep { get; set; } = new();
        public List<ChartDataPoint> LuongChartData { get; set; } = new(); // Tận dụng DTO từ Dashboard
    }

    /// <summary>
    /// DTO cho các thẻ KPI
    /// </summary>
    public class BaoCaoNhanSu_KpiDto
    {
        public int SoLuongNhanVien { get; set; }
        public decimal TongLuongDaTra { get; set; }
        public decimal TongGioLam { get; set; }
        public int TongSoNgayNghi { get; set; }
    }

    /// <summary>
    // DTO cho Tab 1: Báo cáo Lương (Tổng hợp từ PhieuLuong)
    /// </summary>
    public class BaoCaoNhanSu_BangLuongDto
    {
        public int IdNhanVien { get; set; }
        public string HoTenNhanVien { get; set; } = string.Empty;
        public string TenVaiTro { get; set; } = string.Empty;
        public decimal TongGioLam { get; set; }
        public decimal TongLuongCoBan { get; set; }
        public decimal TongThuong { get; set; }
        public decimal TongPhat { get; set; }
        public decimal ThucLanh { get; set; }
    }

    /// <summary>
    // DTO cho Tab 2: Báo cáo Nghỉ Phép (Tổng hợp từ DonXinNghi)
    /// </summary>
    public class BaoCaoNhanSu_NghiPhepDto
    {
        public int IdNhanVien { get; set; }
        public string HoTenNhanVien { get; set; } = string.Empty;
        public string TenVaiTro { get; set; } = string.Empty;
        public int SoDonDaDuyet { get; set; }
        public int TongSoNgayNghi { get; set; }
    }

    /// <summary>
    /// DTO chứa các bộ lọc cho Báo cáo
    /// </summary>
    public class BaoCaoNhanSu_FiltersDto
    {
        public List<NhanVienLookupDto> NhanViens { get; set; } = new();
        public List<FilterLookupDto> VaiTros { get; set; } = new();
    }

    // (Thêm vào cuối file NhanSuDto.cs)

    /// <summary>
    /// DTO cho DataGrid ở trang QuanLyPhatLuongView
    /// </summary>
    public class PhatLuongListItemDto
    {
        public int IdPhieuLuong { get; set; }
        public string HoTenNhanVien { get; set; } = string.Empty;
        public string KyLuong { get; set; } = string.Empty; // "Tháng 11/2025"
        public decimal TongGioLam { get; set; }
        public decimal ThucLanh { get; set; }
        public DateTime NgayChot { get; set; }
        public string TrangThai { get; set; } = string.Empty; // "Đã chốt", "Đã phát"
    }

    /// <summary>
    /// DTO chi tiết cho Popup PhieuLuongPreviewWindow (A5)
    /// </summary>
    public class PhatLuongDetailDto
    {
        public int IdPhieuLuong { get; set; }
        public string HoTenNhanVien { get; set; } = string.Empty;
        public string KyLuong { get; set; } = string.Empty; // "Tháng 11/2025"
        public DateTime NgayBatDauKy { get; set; }
        public DateTime NgayKetThucKy { get; set; }
        public decimal LuongCoBan { get; set; } // Lương/giờ
        public decimal TongGioLam { get; set; }
        public decimal TienThuong { get; set; }
        public decimal KhauTru { get; set; }
        public decimal ThucLanh { get; set; }

        public string TrangThai { get; set; } = string.Empty;
        public DateTime NgayChot { get; set; }
        public DateTime? NgayPhat { get; set; }
        public string TenNguoiPhat { get; set; } = string.Empty; // Tên kế toán

        // Thông tin cửa hàng (tải từ CaiDat)
        public string TenQuan { get; set; } = "CAFEBOOK";
        public string DiaChi { get; set; } = string.Empty;
        public string SoDienThoai { get; set; } = string.Empty;
    }

    /// <summary>
    /// DTO gửi lên khi xác nhận phát lương
    /// </summary>
    public class XacNhanPhatDto
    {
        public int IdNguoiPhat { get; set; } // ID của kế toán/người bấm nút
    }
}