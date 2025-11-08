using CafebookApi.Data;
using CafebookModel.Model.Entities;
using CafebookModel.Model.ModelApp.NhanVien;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail; // THÊM MỚI để kiểm tra email
using System.Threading.Tasks;

namespace CafebookApi.Controllers.App.NhanVien
{
    [Route("api/app/nhanvien/thanhtoan")]
    [ApiController]
    public class ThanhToanController : ControllerBase
    {
        private readonly CafebookDbContext _context;
        private decimal _tiLeDoiDiem = 1000m;
        private decimal _tiLeNhanDiem = 10000m;
        private Dictionary<string, string> _settings = new Dictionary<string, string>();

        public ThanhToanController(CafebookDbContext context)
        {
            _context = context;
        }

        // Hàm helper tải cài đặt
        private async Task LoadCaiDat()
        {
            _settings = await _context.CaiDats
                .Where(c =>
                    c.TenCaiDat == "DiemTichLuy_DoiVND" ||
                    c.TenCaiDat == "DiemTichLuy_NhanVND" ||
                    c.TenCaiDat == "TenQuan" ||
                    c.TenCaiDat == "DiaChi" ||
                    c.TenCaiDat == "SoDienThoai" ||
                    c.TenCaiDat == "Wifi_MatKhau")
                .AsNoTracking()
                .ToDictionaryAsync(c => c.TenCaiDat, c => c.GiaTri);

            decimal.TryParse(_settings.GetValueOrDefault("DiemTichLuy_DoiVND", "1000"), out _tiLeDoiDiem);
            decimal.TryParse(_settings.GetValueOrDefault("DiemTichLuy_NhanVND", "10000"), out _tiLeNhanDiem);
            if (_tiLeNhanDiem == 0) _tiLeNhanDiem = 10000;
            if (_tiLeDoiDiem == 0) _tiLeDoiDiem = 1000;
        }

        /// <summary>
        /// Tải dữ liệu cho màn hình ThanhToanView
        /// </summary>
        [HttpGet("load/{idHoaDon}")]
        public async Task<IActionResult> LoadThanhToanData(int idHoaDon)
        {
            await LoadCaiDat(); // Tải cài đặt

            var hoaDon = await _context.HoaDons
                .Include(h => h.Ban)
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.IdHoaDon == idHoaDon);

            if (hoaDon == null) return NotFound("Không tìm thấy hóa đơn.");

            var hoaDonInfo = new HoaDonInfoDto
            {
                IdHoaDon = hoaDon.IdHoaDon,
                SoBan = hoaDon.Ban?.SoBan ?? hoaDon.LoaiHoaDon,
                LoaiHoaDon = hoaDon.LoaiHoaDon,
                TongTienGoc = hoaDon.TongTienGoc,
                GiamGia = hoaDon.GiamGia,
                ThanhTien = hoaDon.ThanhTien,
            };

            var chiTietItems = await _context.ChiTietHoaDons
                .Where(c => c.IdHoaDon == idHoaDon)
                .AsNoTracking()
                .Select(c => new ChiTietDto
                {
                    IdChiTietHoaDon = c.IdChiTietHoaDon,
                    IdSanPham = c.IdSanPham,
                    TenSanPham = c.SanPham.TenSanPham,
                    SoLuong = c.SoLuong,
                    DonGia = c.DonGia,
                    ThanhTien = c.ThanhTien
                }).ToListAsync();

            var phuThusDaApDung = await _context.ChiTietPhuThuHoaDons
                .Where(pt => pt.IdHoaDon == idHoaDon)
                .AsNoTracking()
                .Select(pt => new PhuThuDto
                {
                    IdPhuThu = pt.IdPhuThu,
                    TenPhuThu = pt.PhuThu.TenPhuThu,
                    SoTien = pt.SoTien,
                    LoaiGiaTri = pt.PhuThu.LoaiGiaTri,
                    GiaTri = pt.PhuThu.GiaTri
                }).ToListAsync();

            var idPhuThuDaApDung = phuThusDaApDung.Select(p => p.IdPhuThu);
            var phuThusKhaDung = await _context.PhuThus
                .Where(p => !idPhuThuDaApDung.Contains(p.IdPhuThu))
                .AsNoTracking()
                .ToListAsync();

            var khuyenMaiLink = await _context.HoaDonKhuyenMais
                .AsNoTracking()
                .FirstOrDefaultAsync(hkm => hkm.IdHoaDon == idHoaDon);

            KhachHang? khachHang = null;
            if (hoaDon.IdKhachHang.HasValue)
            {
                khachHang = await _context.KhachHangs.AsNoTracking().FirstOrDefaultAsync(kh => kh.IdKhachHang == hoaDon.IdKhachHang.Value);
            }

            var khachHangsList = await _context.KhachHangs
                .AsNoTracking()
                .Select(kh => new KhachHangTimKiemDto
                {
                    IdKhachHang = kh.IdKhachHang,
                    // SỬA: Chỉ dùng HoTen và SoDienThoai
                    DisplayText = kh.HoTen + (kh.SoDienThoai != null ? $" - {kh.SoDienThoai}" : ""),
                    KhachHangData = kh
                }).ToListAsync();

            return Ok(new ThanhToanViewDto
            {
                HoaDonInfo = hoaDonInfo,
                ChiTietItems = chiTietItems,
                IdKhuyenMaiDaApDung = khuyenMaiLink?.IdKhuyenMai, // Trả về ID
                PhuThusDaApDung = phuThusDaApDung,
                PhuThusKhaDung = phuThusKhaDung,
                KhachHang = khachHang,
                KhachHangsList = khachHangsList, // Trả về DS Khách hàng
                DiemTichLuy_DoiVND = _tiLeDoiDiem,
                DiemTichLuy_NhanVND = _tiLeNhanDiem,

                TenQuan = _settings.GetValueOrDefault("TenQuan", "CafeBook"),
                DiaChi = _settings.GetValueOrDefault("DiaChi", "N/A"),
                SoDienThoai = _settings.GetValueOrDefault("SoDienThoai", "N/A"),
                WifiMatKhau = _settings.GetValueOrDefault("Wifi_MatKhau", "N/A")
            });
        }

        // Trong file: ThanhToanController.cs
        // THAY THẾ HÀM CŨ BẰNG HÀM NÀY:

        [HttpPost("find-or-create-customer")]
        public async Task<IActionResult> FindOrCreateCustomer([FromBody] string query)
        {
            if (string.IsNullOrWhiteSpace(query))
            {
                return Ok(null); // Không nhập -> Khách vãng lai
            }

            // 1. Chỉ tìm kiếm theo SĐT
            var khachHang = await _context.KhachHangs
                .FirstOrDefaultAsync(kh => kh.SoDienThoai == query);

            if (khachHang != null)
            {
                // 1.1. Nếu tìm thấy, trả về
                var khDto = new KhachHangTimKiemDto
                {
                    IdKhachHang = khachHang.IdKhachHang,
                    DisplayText = khachHang.HoTen + $" - {khachHang.SoDienThoai}",
                    KhachHangData = khachHang,
                    IsNew = false
                };
                return Ok(khDto);
            }

            // 2. Không tìm thấy -> Kiểm tra xem có phải SĐT hợp lệ không
            if (IsValidPhone(query))
            {
                // 2.1. Nếu là SĐT hợp lệ -> Tạo tài khoản mới
                var newKhachHang = new KhachHang
                {
                    HoTen = $"Khách SĐT {query}",
                    SoDienThoai = query,

                    // === SỬA LỖI DATABASE (UNIQUE KEY) ===
                    // Cung cấp giá trị mặc định duy nhất thay vì NULL
                    Email = $"{query}@temp.cafebook.com",
                    TenDangNhap = query,
                    // === KẾT THÚC SỬA LỖI ===
                    MatKhau = "123456", // Mật khẩu tạm thời
                    TaiKhoanTam = true,
                    NgayTao = DateTime.Now,
                    DiemTichLuy = 0,
                    BiKhoa = false
                };

                _context.KhachHangs.Add(newKhachHang);
                await _context.SaveChangesAsync(); // Dòng 185 (dòng gây lỗi) giờ sẽ chạy được

                // 3. Đóng gói DTO cho khách hàng vừa tạo
                var newKhachHangDto = new KhachHangTimKiemDto
                {
                    IdKhachHang = newKhachHang.IdKhachHang,
                    DisplayText = newKhachHang.HoTen + $" - {newKhachHang.SoDienThoai}",
                    KhachHangData = newKhachHang,
                    IsNew = true
                };
                return Ok(newKhachHangDto);
            }
            else
            {
                // 2.3. Nếu là Tên hoặc Email -> KHÔNG TẠO, coi như khách vãng lai
                return Ok(null);
            }
        }

        [HttpPost("pay")]
        public async Task<IActionResult> ProcessPayment([FromBody] ThanhToanRequestDto req)
        {
            await LoadCaiDat();

            var hoaDonGoc = await _context.HoaDons
                .Include(h => h.Ban)
                .Include(h => h.ChiTietHoaDons)
                .Include(h => h.ChiTietPhuThuHoaDons)
                .FirstOrDefaultAsync(h => h.IdHoaDon == req.IdHoaDonGoc);

            if (hoaDonGoc == null) return NotFound("Không tìm thấy hóa đơn gốc.");
            if (hoaDonGoc.TrangThai != "Chưa thanh toán") return Conflict("Hóa đơn này đã được xử lý.");

            var allItemsGocIds = hoaDonGoc.ChiTietHoaDons.Select(c => c.IdChiTietHoaDon).ToList();
            bool isFullPayment = allItemsGocIds.Count == req.IdChiTietTach.Count && allItemsGocIds.All(req.IdChiTietTach.Contains);

            HoaDon hoaDonThanhToan;

            var chiTietTach = hoaDonGoc.ChiTietHoaDons
                .Where(c => req.IdChiTietTach.Contains(c.IdChiTietHoaDon)).ToList();

            var phuThuDaApDungGoc = hoaDonGoc.ChiTietPhuThuHoaDons
                .Where(pt => req.IdPhuThuTach.Contains(pt.IdPhuThu)).ToList();
            var idPhuThuMoi = req.IdPhuThuTach.Where(id => !phuThuDaApDungGoc.Any(pt => pt.IdPhuThu == id)).ToList();
            var phuThuMoi = await _context.PhuThus.Where(p => idPhuThuMoi.Contains(p.IdPhuThu)).ToListAsync();

            if (isFullPayment)
            {
                hoaDonThanhToan = hoaDonGoc;
                if (req.IdKhachHang.HasValue)
                    hoaDonThanhToan.IdKhachHang = req.IdKhachHang;

                decimal tongTienGocMoi = chiTietTach.Sum(c => c.ThanhTien);
                hoaDonThanhToan.TongTienGoc = tongTienGocMoi;

                decimal tongPhuThuMoi = 0;

                foreach (var phuThu in phuThuDaApDungGoc)
                {
                    tongPhuThuMoi += phuThu.SoTien;
                }

                foreach (var ptMoi in phuThuMoi)
                {
                    decimal soTienPT = string.Equals(ptMoi.LoaiGiaTri, "%", StringComparison.OrdinalIgnoreCase)
                        ? (tongTienGocMoi * (ptMoi.GiaTri / 100))
                        : ptMoi.GiaTri;

                    _context.ChiTietPhuThuHoaDons.Add(new ChiTietPhuThuHoaDon
                    {
                        IdHoaDon = hoaDonThanhToan.IdHoaDon,
                        IdPhuThu = ptMoi.IdPhuThu,
                        SoTien = soTienPT
                    });
                    tongPhuThuMoi += soTienPT;
                }

                hoaDonThanhToan.TongPhuThu = tongPhuThuMoi;
            }
            else
            {
                hoaDonThanhToan = new HoaDon
                {
                    IdBan = hoaDonGoc.IdBan,
                    IdNhanVien = hoaDonGoc.IdNhanVien,
                    IdKhachHang = req.IdKhachHang,
                    ThoiGianTao = hoaDonGoc.ThoiGianTao,
                    LoaiHoaDon = hoaDonGoc.LoaiHoaDon,
                    GhiChu = $"Tách từ HĐ #{hoaDonGoc.IdHoaDon}"
                };
                _context.HoaDons.Add(hoaDonThanhToan);
                await _context.SaveChangesAsync();

                decimal tongTienTach = 0;
                foreach (var chiTiet in chiTietTach)
                {
                    chiTiet.IdHoaDon = hoaDonThanhToan.IdHoaDon;
                    tongTienTach += chiTiet.ThanhTien;
                    hoaDonGoc.ChiTietHoaDons.Remove(chiTiet);
                }
                hoaDonThanhToan.TongTienGoc = tongTienTach;

                decimal tongPhuThuTach = 0;
                foreach (var phuThu in phuThuDaApDungGoc)
                {
                    phuThu.IdHoaDon = hoaDonThanhToan.IdHoaDon;
                    tongPhuThuTach += phuThu.SoTien;
                    hoaDonGoc.ChiTietPhuThuHoaDons.Remove(phuThu);
                }
                foreach (var ptMoi in phuThuMoi)
                {
                    decimal soTienPT = string.Equals(ptMoi.LoaiGiaTri, "%", StringComparison.OrdinalIgnoreCase)
                        ? (tongTienTach * (ptMoi.GiaTri / 100))
                        : ptMoi.GiaTri;
                    _context.ChiTietPhuThuHoaDons.Add(new ChiTietPhuThuHoaDon
                    {
                        IdHoaDon = hoaDonThanhToan.IdHoaDon,
                        IdPhuThu = ptMoi.IdPhuThu,
                        SoTien = soTienPT
                    });
                    tongPhuThuTach += soTienPT;
                }
                hoaDonThanhToan.TongPhuThu = tongPhuThuTach;

                hoaDonGoc.TongTienGoc = hoaDonGoc.ChiTietHoaDons.Sum(c => c.ThanhTien);
                hoaDonGoc.TongPhuThu = hoaDonGoc.ChiTietPhuThuHoaDons.Sum(pt => pt.SoTien);
            }

            decimal giamGiaKM = 0;
            decimal giamGiaDiem = 0;

            if (req.IdKhuyenMai.HasValue && req.IdKhuyenMai > 0)
            {
                var km = await _context.KhuyenMais.FindAsync(req.IdKhuyenMai.Value);
                if (km != null)
                {
                    giamGiaKM = await CalculateDiscount(km, hoaDonThanhToan.TongTienGoc, chiTietTach);

                    var existingLink = await _context.HoaDonKhuyenMais
                        .AsNoTracking()
                        .FirstOrDefaultAsync(hkm => hkm.IdHoaDon == hoaDonThanhToan.IdHoaDon && hkm.IdKhuyenMai == km.IdKhuyenMai);

                    if (existingLink == null)
                    {
                        _context.HoaDonKhuyenMais.Add(new HoaDon_KhuyenMai { IdHoaDon = hoaDonThanhToan.IdHoaDon, IdKhuyenMai = km.IdKhuyenMai });
                        if (km.SoLuongConLai.HasValue && km.SoLuongConLai > 0) km.SoLuongConLai -= 1;
                    }
                }
            }

            KhachHang? khachHang = null;
            if (req.IdKhachHang.HasValue)
            {
                khachHang = await _context.KhachHangs.FindAsync(req.IdKhachHang.Value);
            }

            if (khachHang != null && req.DiemSuDung > 0)
            {
                if (khachHang.DiemTichLuy < req.DiemSuDung)
                    return BadRequest("Khách hàng không đủ điểm tích lũy.");

                giamGiaDiem = req.DiemSuDung * _tiLeDoiDiem;

                decimal tongTruocDiem = hoaDonThanhToan.TongTienGoc + hoaDonThanhToan.TongPhuThu - giamGiaKM;

                if (giamGiaDiem > tongTruocDiem)
                {
                    giamGiaDiem = tongTruocDiem;
                    req.DiemSuDung = (int)Math.Ceiling(giamGiaDiem / _tiLeDoiDiem);
                }

                khachHang.DiemTichLuy -= req.DiemSuDung;
                hoaDonThanhToan.IdKhachHang = khachHang.IdKhachHang;
            }

            hoaDonThanhToan.GiamGia = giamGiaKM + giamGiaDiem;

            await TruKho(hoaDonThanhToan.IdNhanVien, chiTietTach);

            hoaDonThanhToan.TrangThai = "Đã thanh toán";
            hoaDonThanhToan.ThoiGianThanhToan = DateTime.Now;
            hoaDonThanhToan.PhuongThucThanhToan = req.PhuongThucThanhToan;

            await _context.SaveChangesAsync();

            _context.GiaoDichThanhToans.Add(new GiaoDichThanhToan
            {
                IdHoaDon = hoaDonThanhToan.IdHoaDon,
                MaGiaoDichNgoai = $"HD_{hoaDonThanhToan.IdHoaDon}_{DateTime.Now:HHmmss}",
                CongThanhToan = req.PhuongThucThanhToan,
                SoTien = hoaDonThanhToan.ThanhTien,
                ThoiGianGiaoDich = (DateTime)hoaDonThanhToan.ThoiGianThanhToan,
                TrangThai = "Thành công"
            });

            if (khachHang != null && _tiLeNhanDiem > 0)
            {
                if (req.DiemSuDung == 0)
                {
                    int diemMoi = (int)Math.Floor(hoaDonThanhToan.ThanhTien / _tiLeNhanDiem);
                    if (diemMoi > 0)
                    {
                        khachHang.DiemTichLuy += diemMoi;
                    }
                }
            }

            bool hoaDonGocDaThanhToanHet = false;

            if (isFullPayment || (hoaDonGoc.TongTienGoc == 0 && hoaDonGoc.TongPhuThu == 0))
            {
                hoaDonGocDaThanhToanHet = true;
                if (hoaDonGoc.Ban != null)
                {
                    hoaDonGoc.Ban.TrangThai = "Trống";
                }
                if (!isFullPayment)
                {
                    hoaDonGoc.TrangThai = "Đã hủy";
                }
            }

            await _context.SaveChangesAsync();
            return Ok(new
            {
                message = "Thanh toán thành công!",
                isFullPayment = hoaDonGocDaThanhToanHet, // Trả về cờ này
                idHoaDonDaThanhToan = hoaDonThanhToan.IdHoaDon
            });
        }

        #region Hàm Helper (Trừ kho, Tính KM, Validate)

        private async Task TruKho(int idNhanVien, ICollection<ChiTietHoaDon> chiTietList)
        {
            foreach (var chiTiet in chiTietList)
            {
                var dinhLuongList = await _context.DinhLuongs
                    .Include(d => d.NguyenLieu)
                    .Include(d => d.DonViSuDung)
                    .Where(d => d.IdSanPham == chiTiet.IdSanPham)
                    .ToListAsync();

                foreach (var dl in dinhLuongList)
                {
                    if (dl.NguyenLieu != null && dl.DonViSuDung != null)
                    {
                        var nguyenLieu = dl.NguyenLieu;
                        decimal luongCanTru = (dl.SoLuongSuDung * dl.DonViSuDung.GiaTriQuyDoi) * chiTiet.SoLuong;
                        nguyenLieu.TonKho -= luongCanTru;

                        if (nguyenLieu.TonKho <= nguyenLieu.TonKhoToiThieu)
                        {
                            _context.ThongBaos.Add(new ThongBao
                            {
                                IdNhanVienTao = idNhanVien,
                                NoiDung = $"Cảnh báo: Tồn kho '{nguyenLieu.TenNguyenLieu}' chỉ còn {nguyenLieu.TonKho:N2} {nguyenLieu.DonViTinh}.",
                                ThoiGianTao = DateTime.Now,
                                LoaiThongBao = "CanhBaoKho",
                                IdLienQuan = nguyenLieu.IdNguyenLieu
                            });
                        }
                    }
                }
            }
        }

        private async Task<decimal> CalculateDiscount(KhuyenMai km, decimal tongTienGoc, ICollection<ChiTietHoaDon> chiTietList)
        {
            decimal tongTienGocChoKM = tongTienGoc;
            if (km.IdSanPhamApDung.HasValue)
            {
                tongTienGocChoKM = chiTietList
                    .Where(c => c.IdSanPham == km.IdSanPhamApDung.Value)
                    .Sum(c => c.ThanhTien);
            }

            decimal giamGia = 0;
            if (string.Equals(km.LoaiGiamGia, "PhanTram", StringComparison.OrdinalIgnoreCase))
            {
                giamGia = tongTienGocChoKM * (km.GiaTriGiam / 100);
                if (km.GiamToiDa.HasValue && giamGia > km.GiamToiDa.Value)
                {
                    giamGia = km.GiamToiDa.Value;
                }
            }
            else // SoTien
            {
                giamGia = km.GiaTriGiam;
            }
            return await Task.FromResult(giamGia);
        }

        // --- HÀM HELPER MỚI ---
        private bool IsValidEmail(string email)
        {
            try
            {
                var addr = new MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        private bool IsValidPhone(string phone)
        {
            return phone.All(char.IsDigit) && phone.Length >= 9 && phone.Length <= 11;
        }

        #endregion
    }
}