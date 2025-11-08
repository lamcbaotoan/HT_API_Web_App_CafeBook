using CafebookApi.Data;
using CafebookModel.Model.Entities;
using CafebookModel.Model.ModelApp.NhanVien;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System.IO;

namespace CafebookApi.Controllers.App.NhanVien
{
    // DTO này đã có trong file GoiMonDto.cs
    // public class ApplyPromotionRequest { ... } 

    [Route("api/app/nhanvien/goimon")]
    [ApiController]
    public class GoiMonController : ControllerBase
    {
        private readonly CafebookDbContext _context;
        private readonly string _baseUrl;

        public GoiMonController(CafebookDbContext context, IConfiguration config)
        {
            _context = context;
            _baseUrl = config.GetValue<string>("Kestrel:Endpoints:Http:Url")
                       ?? "http://127.0.0.1:5166";
        }

        private string GetFullImageUrl(string? relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                return $"{_baseUrl}/images/default-food-icon.png";
            }
            return $"{_baseUrl}{relativePath.Replace(Path.DirectorySeparatorChar, '/')}";
        }

        [HttpGet("load/{idHoaDon}")]
        public async Task<IActionResult> LoadGoiMonData(int idHoaDon)
        {
            var hoaDon = await _context.HoaDons
                .Include(h => h.Ban)
                .FirstOrDefaultAsync(h => h.IdHoaDon == idHoaDon);

            if (hoaDon == null) return NotFound("Không tìm thấy hóa đơn.");

            var currentPromotion = await _context.HoaDonKhuyenMais
                .FirstOrDefaultAsync(hk => hk.IdHoaDon == idHoaDon);

            var hoaDonInfo = new HoaDonInfoDto
            {
                IdHoaDon = hoaDon.IdHoaDon,
                SoBan = hoaDon.Ban?.SoBan ?? hoaDon.LoaiHoaDon,
                LoaiHoaDon = hoaDon.LoaiHoaDon,
                TongTienGoc = hoaDon.TongTienGoc,
                GiamGia = hoaDon.GiamGia,
                ThanhTien = hoaDon.ThanhTien,
                IdKhuyenMai = currentPromotion?.IdKhuyenMai
            };

            var chiTietItems = await _context.ChiTietHoaDons
                .Where(c => c.IdHoaDon == idHoaDon)
                .Select(c => new ChiTietDto
                {
                    IdChiTietHoaDon = c.IdChiTietHoaDon,
                    IdSanPham = c.IdSanPham,
                    TenSanPham = c.SanPham.TenSanPham,
                    SoLuong = c.SoLuong,
                    DonGia = c.DonGia,
                    ThanhTien = c.ThanhTien
                }).ToListAsync();

            var rawSanPhams = await _context.SanPhams
                .Where(s => s.TrangThaiKinhDoanh == true)
                .Select(s => new { s.IdSanPham, s.TenSanPham, s.GiaBan, s.HinhAnh, s.IdDanhMuc })
                .ToListAsync();

            var sanPhams = rawSanPhams
                .Select(s => new SanPhamDto
                {
                    IdSanPham = s.IdSanPham,
                    TenSanPham = s.TenSanPham,
                    DonGia = s.GiaBan,
                    HinhAnh = GetFullImageUrl(s.HinhAnh),
                    IdDanhMuc = s.IdDanhMuc
                }).ToList();

            var danhMucs = await _context.DanhMucs
                .OrderBy(d => d.TenDanhMuc)
                .Select(d => new DanhMucDto { IdDanhMuc = d.IdDanhMuc, TenLoaiSP = d.TenDanhMuc })
                .ToListAsync();

            // === SỬA LỖI LỌC KHUYẾN MÃI (THEO YÊU CẦU CỦA BẠN) ===
            var now = DateTime.Now;

            // 1. Tải về bộ nhớ
            var allKms = await _context.KhuyenMais.ToListAsync();

            // 2. Dùng C# (LINQ to Objects) để lọc
            var khuyenMais = allKms
                .Where(k =>
                    string.Equals(k.TrangThai, "Hoạt động", StringComparison.OrdinalIgnoreCase) &&
                    k.NgayBatDau <= now &&
                    k.NgayKetThuc >= now &&
                    (k.SoLuongConLai == null || k.SoLuongConLai > 0) &&
                    (k.HoaDonToiThieu == null || k.HoaDonToiThieu == 0 || hoaDon.TongTienGoc >= k.HoaDonToiThieu) &&
                    (k.GioBatDau == null || k.GioKetThuc == null || (now.TimeOfDay >= k.GioBatDau && now.TimeOfDay <= k.GioKetThuc))
                )
                .Select(k => new KhuyenMaiDto
                {
                    IdKhuyenMai = k.IdKhuyenMai,
                    TenKhuyenMai = k.TenChuongTrinh,
                    LoaiGiamGia = k.LoaiGiamGia,
                    GiaTriGiam = k.GiaTriGiam
                }).ToList();
            // === KẾT THÚC SỬA LỖI ===

            khuyenMais.Insert(0, new KhuyenMaiDto { IdKhuyenMai = 0, TenKhuyenMai = "-- Không áp dụng --" });

            var dto = new GoiMonViewDto
            {
                HoaDonInfo = hoaDonInfo,
                ChiTietItems = chiTietItems,
                SanPhams = sanPhams,
                DanhMucs = danhMucs,
                KhuyenMais = khuyenMais
            };

            return Ok(dto);
        }

        // HÀM HELPER MỚI: Cập nhật TongTienGoc
        private async Task UpdateHoaDonTotals(HoaDon hoaDon)
        {
            if (hoaDon != null)
            {
                // Tính tổng tiền từ các chi tiết (ChiTietHoaDon.ThanhTien là cột computed)
                var tongGocMoi = await _context.ChiTietHoaDons
                    .Where(c => c.IdHoaDon == hoaDon.IdHoaDon)
                    .SumAsync(c => c.ThanhTien);

                hoaDon.TongTienGoc = tongGocMoi;
                // Không lưu ở đây, để hàm gọi tự lưu
            }
        }

        // HÀM HELPER MỚI: Tính toán lại giảm giá (quan trọng)
        private async Task ReApplyPromotion(HoaDon hoaDon)
        {
            var currentPromoLink = await _context.HoaDonKhuyenMais
                .FirstOrDefaultAsync(hk => hk.IdHoaDon == hoaDon.IdHoaDon);

            if (currentPromoLink == null)
            {
                hoaDon.GiamGia = 0; // Không có KM
                return;
            }

            var km = await _context.KhuyenMais.FindAsync(currentPromoLink.IdKhuyenMai);
            if (km == null)
            {
                hoaDon.GiamGia = 0;
                _context.HoaDonKhuyenMais.Remove(currentPromoLink); // Xóa link hỏng
                return;
            }

            // === TÍNH TOÁN LẠI GIẢM GIÁ ===

            // 1. Kiểm tra điều kiện tối thiểu
            if (km.HoaDonToiThieu.HasValue && hoaDon.TongTienGoc < km.HoaDonToiThieu.Value)
            {
                hoaDon.GiamGia = 0;
                _context.HoaDonKhuyenMais.Remove(currentPromoLink); // Hủy KM vì không đủ ĐK
                return;
            }

            // 2. Tính toán lại
            decimal tongTienGocChoKM = hoaDon.TongTienGoc;
            // 2a. Nếu KM chỉ áp dụng cho 1 SP
            if (km.IdSanPhamApDung.HasValue)
            {
                tongTienGocChoKM = await _context.ChiTietHoaDons
                    .Where(c => c.IdHoaDon == hoaDon.IdHoaDon && c.IdSanPham == km.IdSanPhamApDung)
                    .SumAsync(c => c.ThanhTien);
            }

            // 3. Áp dụng giảm giá (SỬA LỖI 10% = 10đ)
            if (string.Equals(km.LoaiGiamGia, "PhanTram", StringComparison.OrdinalIgnoreCase))
            {
                decimal giamGia = tongTienGocChoKM * (km.GiaTriGiam / 100);
                if (km.GiamToiDa.HasValue && giamGia > km.GiamToiDa.Value)
                {
                    giamGia = km.GiamToiDa.Value;
                }
                hoaDon.GiamGia = giamGia;
            }
            else // "SoTien"
            {
                hoaDon.GiamGia = km.GiaTriGiam;
            }
        }


        [HttpPost("add-item")]
        public async Task<IActionResult> AddItem([FromBody] AddItemRequest req)
        {
            var hoaDon = await _context.HoaDons.FindAsync(req.IdHoaDon);
            if (hoaDon == null) return NotFound("Hóa đơn không tồn tại.");
            if (hoaDon.TrangThai == "Đã thanh toán") return Conflict("Hóa đơn đã thanh toán.");
            var sanPham = await _context.SanPhams.FindAsync(req.IdSanPham);
            if (sanPham == null) return NotFound("Sản phẩm không tồn tại.");

            // === NÂNG CẤP: KIỂM TRA TỒN KHO ===
            var dinhLuongList = await _context.DinhLuongs
                .Include(d => d.NguyenLieu)
                .Include(d => d.DonViSuDung)
                .Where(d => d.IdSanPham == req.IdSanPham)
                .ToListAsync();

            if (dinhLuongList.Any())
            {
                var existingItemQty = (await _context.ChiTietHoaDons.FirstOrDefaultAsync(c => c.IdHoaDon == req.IdHoaDon && c.IdSanPham == req.IdSanPham))?.SoLuong ?? 0;

                foreach (var dl in dinhLuongList)
                {
                    decimal luongCanDungMotSP = dl.SoLuongSuDung * dl.DonViSuDung.GiaTriQuyDoi;
                    decimal luongCanDungTong = luongCanDungMotSP * (existingItemQty + req.SoLuong); // SL mới + SL cũ

                    if (dl.NguyenLieu.TonKho < luongCanDungTong)
                    {
                        var thongBao = new ThongBao
                        {
                            IdNhanVienTao = hoaDon.IdNhanVien,
                            NoiDung = $"Nguyên liệu '{dl.NguyenLieu.TenNguyenLieu}' đã hết hàng khi cố gắng bán món '{sanPham.TenSanPham}'. Chỉ còn {dl.NguyenLieu.TonKho:N2} {dl.NguyenLieu.DonViTinh}.",
                            ThoiGianTao = DateTime.Now,
                            LoaiThongBao = "HetHang",
                            IdLienQuan = dl.IdNguyenLieu,
                            DaXem = false
                        };
                        _context.ThongBaos.Add(thongBao);
                        await _context.SaveChangesAsync();

                        return Conflict($"Hết hàng: '{dl.NguyenLieu.TenNguyenLieu}'. Không thể thêm món.");
                    }
                }
            }
            // === KẾT THÚC NÂNG CẤP ===

            ChiTietDto resultDto;
            var existingItemDb = await _context.ChiTietHoaDons.FirstOrDefaultAsync(c => c.IdHoaDon == req.IdHoaDon && c.IdSanPham == req.IdSanPham);

            if (existingItemDb != null)
            {
                existingItemDb.SoLuong += req.SoLuong;
                await _context.SaveChangesAsync();
                resultDto = new ChiTietDto { IdChiTietHoaDon = existingItemDb.IdChiTietHoaDon, IdSanPham = existingItemDb.IdSanPham, TenSanPham = sanPham.TenSanPham, SoLuong = existingItemDb.SoLuong, DonGia = existingItemDb.DonGia, ThanhTien = existingItemDb.SoLuong * existingItemDb.DonGia };
            }
            else
            {
                var newItem = new ChiTietHoaDon { IdHoaDon = req.IdHoaDon, IdSanPham = req.IdSanPham, SoLuong = req.SoLuong, DonGia = sanPham.GiaBan };
                _context.ChiTietHoaDons.Add(newItem);
                await _context.SaveChangesAsync();
                resultDto = new ChiTietDto { IdChiTietHoaDon = newItem.IdChiTietHoaDon, IdSanPham = newItem.IdSanPham, TenSanPham = sanPham.TenSanPham, SoLuong = newItem.SoLuong, DonGia = newItem.DonGia, ThanhTien = newItem.SoLuong * newItem.DonGia };
            }

            // SỬA LỖI TÍNH TOÁN: Cập nhật tổng và khuyến mãi
            await UpdateHoaDonTotals(hoaDon);
            await ReApplyPromotion(hoaDon);
            await _context.SaveChangesAsync(); // Lưu Hóa đơn

            var updatedHoaDonInfo = await GetHoaDonInfo(req.IdHoaDon);
            return Ok(new { updatedHoaDonInfo, newItem = resultDto });
        }

        [HttpPut("update-quantity")]
        public async Task<IActionResult> UpdateQuantity([FromBody] UpdateSoLuongRequest req)
        {
            var item = await _context.ChiTietHoaDons.Include(c => c.HoaDon).FirstOrDefaultAsync(c => c.IdChiTietHoaDon == req.IdChiTietHoaDon);
            if (item == null) return NotFound("Không tìm thấy món.");
            var hoaDon = item.HoaDon;
            if (hoaDon.TrangThai == "Đã thanh toán") return Conflict("Hóa đơn đã thanh toán.");

            if (req.SoLuongMoi <= 0)
                _context.ChiTietHoaDons.Remove(item);
            else
                item.SoLuong = req.SoLuongMoi;

            await _context.SaveChangesAsync();

            // SỬA LỖI TÍNH TOÁN: Cập nhật tổng và khuyến mãi
            await UpdateHoaDonTotals(hoaDon);
            await ReApplyPromotion(hoaDon);
            await _context.SaveChangesAsync(); // Lưu Hóa đơn

            var updatedHoaDonInfo = await GetHoaDonInfo(hoaDon.IdHoaDon);
            return Ok(updatedHoaDonInfo);
        }

        [HttpDelete("delete-item/{idChiTiet}")]
        public async Task<IActionResult> DeleteItem(int idChiTiet)
        {
            var item = await _context.ChiTietHoaDons.Include(c => c.HoaDon).FirstOrDefaultAsync(c => c.IdChiTietHoaDon == idChiTiet);
            if (item == null) return NotFound("Không tìm thấy món.");
            var hoaDon = item.HoaDon;
            if (hoaDon.TrangThai == "Đã thanh toán") return Conflict("Hóa đơn đã thanh toán.");

            int idHoaDon = item.IdHoaDon;
            _context.ChiTietHoaDons.Remove(item);
            await _context.SaveChangesAsync();

            // SỬA LỖI TÍNH TOÁN: Cập nhật tổng và khuyến mãi
            await UpdateHoaDonTotals(hoaDon);
            await ReApplyPromotion(hoaDon);
            await _context.SaveChangesAsync(); // Lưu Hóa đơn

            var updatedHoaDonInfo = await GetHoaDonInfo(idHoaDon);
            return Ok(updatedHoaDonInfo);
        }

        [HttpPut("apply-promotion")]
        public async Task<IActionResult> ApplyPromotion([FromBody] ApplyPromotionRequest req)
        {
            var hoaDon = await _context.HoaDons.FindAsync(req.IdHoaDon);
            if (hoaDon == null) return NotFound("Hóa đơn không tồn tại.");

            var existingPromos = _context.HoaDonKhuyenMais.Where(hk => hk.IdHoaDon == req.IdHoaDon);
            _context.HoaDonKhuyenMais.RemoveRange(existingPromos);

            if (req.IdKhuyenMai == null || req.IdKhuyenMai == 0)
            {
                hoaDon.GiamGia = 0;
            }
            else
            {
                var km = await _context.KhuyenMais.FindAsync(req.IdKhuyenMai);
                if (km == null) return NotFound("Khuyến mãi không tồn tại.");

                _context.HoaDonKhuyenMais.Add(new HoaDon_KhuyenMai
                {
                    IdHoaDon = req.IdHoaDon,
                    IdKhuyenMai = km.IdKhuyenMai
                });

                // === SỬA LỖI 10% = 10đ ===
                if (string.Equals(km.LoaiGiamGia, "PhanTram", StringComparison.OrdinalIgnoreCase))
                {
                    decimal tongTienGocChoKM = hoaDon.TongTienGoc;
                    if (km.IdSanPhamApDung.HasValue)
                    {
                        tongTienGocChoKM = await _context.ChiTietHoaDons
                            .Where(c => c.IdHoaDon == req.IdHoaDon && c.IdSanPham == km.IdSanPhamApDung)
                            .SumAsync(c => c.ThanhTien);
                    }

                    decimal giamGia = tongTienGocChoKM * (km.GiaTriGiam / 100);
                    if (km.GiamToiDa.HasValue && giamGia > km.GiamToiDa.Value)
                    {
                        giamGia = km.GiamToiDa.Value;
                    }
                    hoaDon.GiamGia = giamGia;
                }
                else // "SoTien"
                {
                    hoaDon.GiamGia = km.GiaTriGiam;
                }
            }

            await _context.SaveChangesAsync();
            var updatedHoaDonInfo = await GetHoaDonInfo(req.IdHoaDon);
            return Ok(updatedHoaDonInfo);
        }

        // (Hàm ThanhToan đã bị comment out trong file gốc, giữ nguyên)
        /*
        [HttpPost("thanh-toan")]
        public async Task<IActionResult> ThanhToan([FromBody] ThanhToanRequest req)
        { ... }
        */

        private async Task<HoaDonInfoDto> GetHoaDonInfo(int idHoaDon)
        {
            var updatedHoaDon = await _context.HoaDons
                .Include(h => h.Ban)
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.IdHoaDon == idHoaDon);

            if (updatedHoaDon == null)
                throw new Exception("Không thể tìm thấy hóa đơn sau khi cập nhật.");

            var currentPromotion = await _context.HoaDonKhuyenMais
                .AsNoTracking()
                .FirstOrDefaultAsync(hk => hk.IdHoaDon == idHoaDon);

            return new HoaDonInfoDto
            {
                IdHoaDon = updatedHoaDon.IdHoaDon,
                SoBan = updatedHoaDon.Ban?.SoBan ?? updatedHoaDon.LoaiHoaDon,
                LoaiHoaDon = updatedHoaDon.LoaiHoaDon,
                TongTienGoc = updatedHoaDon.TongTienGoc,
                GiamGia = updatedHoaDon.GiamGia,
                ThanhTien = updatedHoaDon.ThanhTien,
                IdKhuyenMai = currentPromotion?.IdKhuyenMai
            };
        }

        // === API MỚI CHO LOGIC BẾP (YÊU CẦU MỚI) ===

        /// <summary>
        /// Helper: Tìm các món trong CTHD và đẩy vào Bảng Chế Biến
        /// </summary>
        private async Task<int> CreateOrUpdateCheBienItems(int idHoaDon)
        {
            var hoaDon = await _context.HoaDons
                .Include(h => h.Ban)
                .FirstOrDefaultAsync(h => h.IdHoaDon == idHoaDon);

            if (hoaDon == null) return 0;

            var chiTietItems = await _context.ChiTietHoaDons
                .Include(c => c.SanPham)
                .Where(c => c.IdHoaDon == idHoaDon)
                .ToListAsync();

            string soBan = hoaDon.Ban?.SoBan ?? hoaDon.LoaiHoaDon;
            int itemsAdded = 0;
            var now = DateTime.Now;

            foreach (var item in chiTietItems)
            {
                // Kiểm tra xem món này đã được gửi bếp chưa
                bool daTonTai = await _context.TrangThaiCheBiens
                    .AnyAsync(cb => cb.IdChiTietHoaDon == item.IdChiTietHoaDon);

                if (!daTonTai)
                {
                    // Tạo một bản ghi chờ chế biến
                    var newItem = new TrangThaiCheBien
                    {
                        IdChiTietHoaDon = item.IdChiTietHoaDon,
                        IdHoaDon = item.IdHoaDon,
                        IdSanPham = item.IdSanPham,
                        TenMon = item.SanPham.TenSanPham,
                        SoBan = soBan,
                        SoLuong = item.SoLuong,
                        GhiChu = item.GhiChu,
                        NhomIn = item.SanPham.NhomIn,
                        TrangThai = "Chờ làm", // Trạng thái mặc định
                        ThoiGianGoi = now
                    };
                    _context.TrangThaiCheBiens.Add(newItem);
                    itemsAdded++;
                }
            }

            if (itemsAdded > 0)
            {
                await _context.SaveChangesAsync();
            }
            return itemsAdded;
        }

        /// <summary>
        /// API cho nút "Lưu" (Chỉ gửi bếp)
        /// </summary>
        [HttpPost("send-to-kitchen/{idHoaDon}")]
        public async Task<IActionResult> SendToKitchen(int idHoaDon)
        {
            try
            {
                int itemsAdded = await CreateOrUpdateCheBienItems(idHoaDon);
                return Ok(new { message = $"Đã gửi {itemsAdded} món mới vào bếp." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi gửi bếp: {ex.Message}");
            }
        }

        /// <summary>
        /// API cho nút "In Phiếu Gọi Món" (Gửi bếp + Tạo thông báo)
        /// </summary>
        [HttpPost("print-and-notify-kitchen/{idHoaDon}/{idNhanVien}")]
        public async Task<IActionResult> PrintAndNotifyKitchen(int idHoaDon, int idNhanVien)
        {
            try
            {
                // 1. Gửi các món mới vào bếp
                await CreateOrUpdateCheBienItems(idHoaDon);

                // 2. Tạo thông báo
                var hoaDon = await _context.HoaDons.Include(h => h.Ban).FirstOrDefaultAsync(h => h.IdHoaDon == idHoaDon);
                string soBan = hoaDon?.Ban?.SoBan ?? hoaDon?.LoaiHoaDon ?? "Hóa đơn";

                var thongBao = new ThongBao
                {
                    IdNhanVienTao = idNhanVien,
                    NoiDung = $"Phiếu gọi món mới cho [{soBan}].",
                    ThoiGianTao = DateTime.Now,
                    LoaiThongBao = "PhieuGoiMon", // Theo yêu cầu
                    IdLienQuan = idHoaDon,
                    DaXem = false
                };
                _context.ThongBaos.Add(thongBao);

                await _context.SaveChangesAsync();

                return Ok(new { message = "Đã gửi phiếu gọi món và thông báo cho bếp." });
            }
            catch (Exception ex)
            {
                return StatusCode(500, $"Lỗi khi in và gửi bếp: {ex.Message}");
            }
        }

        /// <summary>
        /// API MỚI: Lấy dữ liệu cho cửa sổ In Tạm Tính
        /// </summary>
        [HttpGet("print-data/{idHoaDon}")]
        public async Task<IActionResult> GetPrintData(int idHoaDon)
        {
            var hoaDon = await _context.HoaDons
                .Include(h => h.Ban)
                .Include(h => h.NhanVien)
                .Include(h => h.ChiTietHoaDons).ThenInclude(ct => ct.SanPham)
                .AsNoTracking()
                .FirstOrDefaultAsync(h => h.IdHoaDon == idHoaDon);

            if (hoaDon == null) return NotFound();

            var settings = await _context.CaiDats.ToListAsync();

            var dto = new PhieuGoiMonPrintDto
            {
                IdPhieu = $"HD{hoaDon.IdHoaDon:D6}",
                TenQuan = settings.FirstOrDefault(c => c.TenCaiDat == "TenQuan")?.GiaTri ?? "Cafebook",
                DiaChiQuan = settings.FirstOrDefault(c => c.TenCaiDat == "DiaChi")?.GiaTri ?? "N/A",
                SdtQuan = settings.FirstOrDefault(c => c.TenCaiDat == "SoDienThoai")?.GiaTri ?? "N/A",
                NgayTao = hoaDon.ThoiGianTao,
                TenNhanVien = hoaDon.NhanVien.HoTen,
                SoBan = hoaDon.Ban?.SoBan ?? hoaDon.LoaiHoaDon,
                ChiTiet = hoaDon.ChiTietHoaDons.Select(ct => new ChiTietDto
                {
                    TenSanPham = ct.SanPham.TenSanPham,
                    SoLuong = ct.SoLuong,
                    DonGia = ct.DonGia,
                    ThanhTien = ct.ThanhTien
                }).ToList(),
                TongTienGoc = hoaDon.TongTienGoc,
                GiamGia = hoaDon.GiamGia,
                ThanhTien = hoaDon.ThanhTien
            };

            return Ok(dto);
        }
    }
}