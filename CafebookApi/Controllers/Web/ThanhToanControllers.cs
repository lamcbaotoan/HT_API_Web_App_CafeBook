// Tập tin: CafebookApi/Controllers/Web/ThanhToanController.cs
using CafebookApi.Data;
using CafebookModel.Model.Entities;
using CafebookModel.Model.ModelWeb;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CafebookApi.Controllers.Web
{
    [Route("api/web/thanhtoan")]
    [ApiController]
    [Authorize(Roles = "KhachHang")]
    public class ThanhToanController : ControllerBase
    {
        private readonly CafebookDbContext _context;

        public ThanhToanController(CafebookDbContext context)
        {
            _context = context;
        }

        private int GetCurrentUserId()
        {
            var idClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            int.TryParse(idClaim?.Value, out int id);
            return id;
        }

        [HttpGet("load")]
        public async Task<IActionResult> LoadCheckoutData()
        {
            var idKhachHang = GetCurrentUserId();
            if (idKhachHang == 0) return Unauthorized();

            var khachHang = await _context.KhachHangs.FindAsync(idKhachHang);
            if (khachHang == null) return NotFound("Không tìm thấy khách hàng.");

            var settings = await _context.CaiDats.ToDictionaryAsync(c => c.TenCaiDat, c => c.GiaTri);
            decimal.TryParse(settings.GetValueOrDefault("DiemTichLuy_DoiVND", "1000"), out var tiLeDoiDiem);

            var khuyenMais = await _context.KhuyenMais
                .Where(km => km.TrangThai == "Hoạt động" && km.NgayBatDau <= DateTime.Now && km.NgayKetThuc >= DateTime.Now)
                .Select(km => new KhuyenMaiThanhToanDto
                {
                    IdKhuyenMai = km.IdKhuyenMai,
                    TenChuongTrinh = km.TenChuongTrinh,
                    MaKhuyenMai = km.MaKhuyenMai,
                    LoaiGiamGia = km.LoaiGiamGia,
                    GiaTriGiam = km.GiaTriGiam,
                    GiamToiDa = km.GiamToiDa,
                    IdSanPhamApDung = km.IdSanPhamApDung,
                    DieuKienApDung = km.DieuKienApDung,
                    HoaDonToiThieu = km.HoaDonToiThieu,

                    // =======================================
                    // === THÊM 3 TRƯỜNG MỚI TẠI ĐÂY ===
                    // =======================================
                    NgayTrongTuan = km.NgayTrongTuan,
                    GioBatDau = km.GioBatDau,
                    GioKetThuc = km.GioKetThuc
                })
                .ToListAsync();

            var dto = new ThanhToanLoadDto
            {
                KhachHang = new KhachHangThanhToanDto
                {
                    IdKhachHang = khachHang.IdKhachHang,
                    HoTen = khachHang.HoTen,
                    SoDienThoai = khachHang.SoDienThoai ?? "",
                    Email = khachHang.Email ?? "",
                    DiaChi = khachHang.DiaChi ?? "",
                    DiemTichLuy = khachHang.DiemTichLuy
                },
                KhuyenMaisHopLe = khuyenMais,
                TiLeDoiDiemVND = tiLeDoiDiem
            };

            return Ok(dto);
        }

        // ... (Hàm SubmitOrder, CalculateDiscount, GetOrderSummary giữ nguyên) ...
        [HttpPost("submit")]
        public async Task<IActionResult> SubmitOrder([FromBody] ThanhToanSubmitDto dto)
        {
            var idKhachHang = GetCurrentUserId();
            if (idKhachHang == 0) return Unauthorized();

            var khachHang = await _context.KhachHangs.FindAsync(idKhachHang);
            if (khachHang == null) return NotFound("Không tìm thấy khách hàng.");

            var sanPhamCartItems = dto.ItemsToPurchase; // Sửa: Đã xóa Loai
            if (!sanPhamCartItems.Any())
            {
                return BadRequest(new ThanhToanResponseDto { Success = false, Message = "Không có sản phẩm nào trong giỏ hàng để thanh toán." });
            }

            var settings = await _context.CaiDats.ToDictionaryAsync(c => c.TenCaiDat, c => c.GiaTri);
            decimal.TryParse(settings.GetValueOrDefault("DiemTichLuy_DoiVND", "1000"), out var tiLeDoiDiem);
            decimal.TryParse(settings.GetValueOrDefault("DiemTichLuy_NhanVND", "10000"), out var tiLeNhanDiem);

            await using var transaction = await _context.Database.BeginTransactionAsync();
            try
            {
                var productIds = sanPhamCartItems.Select(i => i.Id).ToList();
                var productsInDb = await _context.SanPhams
                    .Where(p => productIds.Contains(p.IdSanPham))
                    .ToDictionaryAsync(p => p.IdSanPham);

                var hoaDon = new HoaDon
                {
                    IdKhachHang = idKhachHang,
                    IdNhanVien = null,
                    ThoiGianTao = DateTime.Now,
                    // SỬA: Logic thanh toán
                    TrangThai = (dto.PhuongThucThanhToan == "COD") ? "Chưa thanh toán" : "Đã thanh toán",
                    LoaiHoaDon = "Giao hàng",
                    DiaChiGiaoHang = dto.DiaChiGiaoHang,
                    SoDienThoaiGiaoHang = dto.SoDienThoai,
                    GhiChu = dto.GhiChu,
                    PhuongThucThanhToan = dto.PhuongThucThanhToan,
                    // SỬA: Gán trạng thái giao hàng
                    TrangThaiGiaoHang = "Chờ xác nhận",
                    TongTienGoc = 0,
                    GiamGia = 0
                };

                _context.HoaDons.Add(hoaDon);
                await _context.SaveChangesAsync();

                decimal tongTienGoc = 0;
                var chiTietTach = new List<ChiTietHoaDon>();

                foreach (var item in sanPhamCartItems)
                {
                    if (productsInDb.TryGetValue(item.Id, out var sanPhamDb))
                    {
                        var chiTiet = new ChiTietHoaDon
                        {
                            IdHoaDon = hoaDon.IdHoaDon,
                            IdSanPham = item.Id,
                            SoLuong = item.SoLuong,
                            DonGia = sanPhamDb.GiaBan,
                            SanPham = sanPhamDb
                        };
                        _context.ChiTietHoaDons.Add(chiTiet);
                        chiTietTach.Add(chiTiet);
                        tongTienGoc += (sanPhamDb.GiaBan * item.SoLuong);
                    }
                }
                hoaDon.TongTienGoc = tongTienGoc;

                decimal giamGiaKM = 0;
                decimal giamGiaDiem = 0;

                if (dto.IdKhuyenMai.HasValue && dto.IdKhuyenMai > 0)
                {
                    var km = await _context.KhuyenMais.FindAsync(dto.IdKhuyenMai.Value);
                    if (km != null)
                    {
                        giamGiaKM = await CalculateDiscount(km, hoaDon.TongTienGoc, chiTietTach);
                        _context.HoaDonKhuyenMais.Add(new HoaDon_KhuyenMai { IdHoaDon = hoaDon.IdHoaDon, IdKhuyenMai = km.IdKhuyenMai });
                        if (km.SoLuongConLai.HasValue && km.SoLuongConLai > 0) km.SoLuongConLai -= 1;
                    }
                }

                if (dto.DiemSuDung > 0)
                {
                    if (khachHang.DiemTichLuy < dto.DiemSuDung)
                    {
                        await transaction.RollbackAsync();
                        return BadRequest(new ThanhToanResponseDto { Success = false, Message = "Bạn không đủ điểm tích lũy." });
                    }
                    giamGiaDiem = dto.DiemSuDung * tiLeDoiDiem;
                    decimal tongTruocDiem = hoaDon.TongTienGoc - giamGiaKM;

                    // Logic 50%
                    decimal maxAllowedPointDiscount = tongTruocDiem * 0.5m;
                    if (giamGiaDiem > maxAllowedPointDiscount)
                    {
                        giamGiaDiem = maxAllowedPointDiscount;
                    }
                    int diemBiTru = (int)Math.Ceiling(giamGiaDiem / tiLeDoiDiem);
                    if (diemBiTru > khachHang.DiemTichLuy) diemBiTru = khachHang.DiemTichLuy;

                    khachHang.DiemTichLuy -= diemBiTru;
                }

                hoaDon.GiamGia = giamGiaKM + giamGiaDiem;

                await _context.SaveChangesAsync();
                await _context.Entry(hoaDon).ReloadAsync();

                if (dto.DiemSuDung == 0 && tiLeNhanDiem > 0)
                {
                    int diemNhan = (int)Math.Floor(hoaDon.ThanhTien / tiLeNhanDiem);
                    if (diemNhan > 0)
                    {
                        khachHang.DiemTichLuy += diemNhan;
                    }
                }

                var giaoDich = new GiaoDichThanhToan
                {
                    IdHoaDon = hoaDon.IdHoaDon,
                    MaGiaoDichNgoai = $"WEB_{hoaDon.IdHoaDon}",
                    CongThanhToan = dto.PhuongThucThanhToan,
                    SoTien = hoaDon.ThanhTien,
                    ThoiGianGiaoDich = DateTime.Now,
                    TrangThai = (dto.PhuongThucThanhToan == "COD") ? "Chờ thanh toán" : "Thành công"
                };
                _context.GiaoDichThanhToans.Add(giaoDich);

                await _context.SaveChangesAsync();
                await transaction.CommitAsync();

                var thongBao = new ThongBao
                {
                    NoiDung = $"Đơn hàng Giao hàng mới #{hoaDon.IdHoaDon} từ {khachHang.HoTen} đang chờ xác nhận.",
                    LoaiThongBao = "DonHangMoi",
                    IdLienQuan = hoaDon.IdHoaDon,
                    ThoiGianTao = DateTime.Now,
                    DaXem = false
                };
                _context.ThongBaos.Add(thongBao);
                await _context.SaveChangesAsync();

                return Ok(new ThanhToanResponseDto { Success = true, Message = "Đặt hàng thành công!", IdHoaDonMoi = hoaDon.IdHoaDon });
            }
            catch (Exception ex)
            {
                await transaction.RollbackAsync();
                return StatusCode(500, new ThanhToanResponseDto { Success = false, Message = $"Lỗi máy chủ: {ex.Message}" });
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
            else
            {
                giamGia = Math.Min(km.GiaTriGiam, tongTienGocChoKM);
            }
            return await Task.FromResult(giamGia);
        }

        [HttpGet("order-summary/{id}")]
        public async Task<IActionResult> GetOrderSummary(int id)
        {
            var idKhachHang = GetCurrentUserId();
            if (idKhachHang == 0) return Unauthorized();

            var hoaDon = await _context.HoaDons
                .Include(h => h.ChiTietHoaDons).ThenInclude(ct => ct.SanPham)
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.IdHoaDon == id && h.IdKhachHang == idKhachHang);

            if (hoaDon == null)
            {
                return NotFound("Không tìm thấy đơn hàng hoặc bạn không có quyền xem.");
            }

            var dto = new ThanhToanThanhCongDto
            {
                IdHoaDonMoi = hoaDon.IdHoaDon,
                ThoiGianTao = hoaDon.ThoiGianTao,
                TrangThai = hoaDon.TrangThai,
                PhuongThucThanhToan = hoaDon.PhuongThucThanhToan ?? "N/A",
                DiaChiGiaoHang = hoaDon.DiaChiGiaoHang ?? "N/A",
                SoDienThoai = hoaDon.SoDienThoaiGiaoHang ?? "N/A",
                TongTienHang = hoaDon.TongTienGoc,
                GiamGia = hoaDon.GiamGia,
                ThanhTien = hoaDon.ThanhTien,
                Items = hoaDon.ChiTietHoaDons.Select(ct => new GioHangItemViewModel
                {
                    Id = ct.IdSanPham,
                    // SỬA: Xóa 'Loai'
                    TenHienThi = ct.SanPham.TenSanPham,
                    DonGia = ct.DonGia,
                    SoLuong = ct.SoLuong
                }).ToList()
            };

            return Ok(dto);
        }
    }
}