// Tập tin: CafebookApi/Controllers/Web/QuanLy/GoiMonViewController.cs
using CafebookApi.Data;
using CafebookModel.Model.Entities;
using CafebookModel.Model.ModelWeb.QuanLy; // Sửa namespace DTO
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Linq;
using System.Threading.Tasks;
using System;
using System.Collections.Generic;
using Microsoft.Extensions.Configuration;
using System.IO;
using Microsoft.AspNetCore.Hosting; // Thêm
using Microsoft.AspNetCore.Authorization; // Thêm

namespace CafebookApi.Controllers.Web.QuanLy
{
    [Authorize] // Bảo vệ tất cả API trong này
    [Route("api/web/quanly/goimon")] // Đồng bộ với SoDoBan
    [ApiController]
    public class GoiMonViewController : ControllerBase // Đổi tên từ GoiMonController
    {
        private readonly CafebookDbContext _context;
        private readonly string _baseUrl;
        private readonly IWebHostEnvironment _env; // Thêm

        // Sửa constructor
        public GoiMonViewController(CafebookDbContext context, IConfiguration config, IWebHostEnvironment env)
        {
            _context = context;
            _env = env; // Thêm
            _baseUrl = config.GetValue<string>("Kestrel:Endpoints:Http:Url")
                       ?? "http://127.0.0.1:5166";
        }

        private string GetFullImageUrl(string? relativePath)
        {
            if (string.IsNullOrEmpty(relativePath))
            {
                // Cung cấp một ảnh default
                return $"{_baseUrl}/images/default-food-icon.png";
            }
            // Đảm bảo dùng '/'
            return $"{_baseUrl}{relativePath.Replace(Path.DirectorySeparatorChar, '/')}";
        }

        [HttpGet("load/{idHoaDon}")]
        public async Task<IActionResult> LoadGoiMonData(int idHoaDon)
        {
            // Logic giống hệt GoiMonController.cs
            var hoaDon = await _context.HoaDons
                .Include(h => h.Ban)
                .FirstOrDefaultAsync(h => h.IdHoaDon == idHoaDon);

            if (hoaDon == null) return NotFound("Không tìm thấy hóa đơn.");
            if (hoaDon.TrangThai == "Đã thanh toán" || hoaDon.TrangThai == "Đã hủy")
            {
                return Conflict($"Hóa đơn đã {hoaDon.TrangThai.ToLower()}. Không thể sửa.");
            }

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
                    ThanhTien = c.ThanhTien,
                    GhiChu = c.GhiChu
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
                    // SỬA LỖI CS0019: Xóa '?? 0' vì s.IdDanhMuc là kiểu 'int'
                    IdDanhMuc = s.IdDanhMuc
                }).ToList();

            var danhMucs = await _context.DanhMucs
                .OrderBy(d => d.TenDanhMuc)
                .Select(d => new DanhMucDto { IdDanhMuc = d.IdDanhMuc, TenLoaiSP = d.TenDanhMuc })
                .ToListAsync();

            // Logic lọc khuyến mãi (giống hệt)
            var now = DateTime.Now;
            var allKms = await _context.KhuyenMais.ToListAsync();
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

        // === CÁC HÀM HELPER TÍNH TOÁN (Giống hệt) ===
        //
        private async Task UpdateHoaDonTotals(HoaDon hoaDon)
        {
            if (hoaDon != null)
            {
                var tongGocMoi = await _context.ChiTietHoaDons
                    .Where(c => c.IdHoaDon == hoaDon.IdHoaDon)
                    .SumAsync(c => c.ThanhTien);
                hoaDon.TongTienGoc = tongGocMoi;
            }
        }

        //
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

        //
        private async Task<int> CreateOrUpdateCheBienItems(int idHoaDon, int? chiTietHoaDonId = null)
        {
            var hoaDon = await _context.HoaDons
                .Include(h => h.Ban)
                .FirstOrDefaultAsync(h => h.IdHoaDon == idHoaDon);
            if (hoaDon == null) return 0;

            // Chỉ lấy chi tiết món
            var query = _context.ChiTietHoaDons
                .Include(c => c.SanPham)
                .Where(c => c.IdHoaDon == idHoaDon);

            if (chiTietHoaDonId.HasValue)
            {
                query = query.Where(c => c.IdChiTietHoaDon == chiTietHoaDonId.Value);
            }

            var chiTietItems = await query.ToListAsync();
            string soBan = hoaDon.Ban?.SoBan ?? hoaDon.LoaiHoaDon;
            int itemsAdded = 0;
            var now = DateTime.Now;

            foreach (var item in chiTietItems)
            {
                // Tìm bản ghi bếp hiện có
                var daTonTai = await _context.TrangThaiCheBiens
                    .FirstOrDefaultAsync(cb => cb.IdChiTietHoaDon == item.IdChiTietHoaDon);

                if (daTonTai != null)
                {
                    // Cập nhật số lượng nếu chưa làm
                    if (daTonTai.TrangThai == "Chờ làm")
                    {
                        daTonTai.SoLuong = item.SoLuong;
                        itemsAdded++; // Vẫn tính là có cập nhật
                    }
                }
                else
                {
                    // Tạo mới
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
                        TrangThai = "Chờ làm",
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

        // === CÁC API THAO TÁC (ĐÃ SỬA) ===

        [HttpPost("add-item")]
        public async Task<IActionResult> AddItem([FromBody] AddItemRequest req)
        {
            var hoaDon = await _context.HoaDons.FindAsync(req.IdHoaDon);
            if (hoaDon == null) return NotFound("Hóa đơn không tồn tại.");
            if (hoaDon.TrangThai != "Chưa thanh toán") return Conflict("Hóa đơn đã đóng.");
            var sanPham = await _context.SanPhams.FindAsync(req.IdSanPham);
            if (sanPham == null) return NotFound("Sản phẩm không tồn tại.");

            // (Có thể thêm logic kiểm tra tồn kho ở đây)

            ChiTietDto resultDto;
            var existingItemDb = await _context.ChiTietHoaDons.FirstOrDefaultAsync(c =>
                c.IdHoaDon == req.IdHoaDon &&
                c.IdSanPham == req.IdSanPham &&
                c.GhiChu == req.GhiChu // Ghi chú khác -> món mới
            );

            if (existingItemDb != null)
            {
                existingItemDb.SoLuong += req.SoLuong;
                await _context.SaveChangesAsync();
                resultDto = new ChiTietDto { IdChiTietHoaDon = existingItemDb.IdChiTietHoaDon, IdSanPham = existingItemDb.IdSanPham, TenSanPham = sanPham.TenSanPham, SoLuong = existingItemDb.SoLuong, DonGia = existingItemDb.DonGia, ThanhTien = existingItemDb.SoLuong * existingItemDb.DonGia, GhiChu = existingItemDb.GhiChu };
            }
            else
            {
                var newItem = new ChiTietHoaDon { IdHoaDon = req.IdHoaDon, IdSanPham = req.IdSanPham, SoLuong = req.SoLuong, DonGia = sanPham.GiaBan, GhiChu = req.GhiChu };
                _context.ChiTietHoaDons.Add(newItem);
                await _context.SaveChangesAsync();
                resultDto = new ChiTietDto { IdChiTietHoaDon = newItem.IdChiTietHoaDon, IdSanPham = newItem.IdSanPham, TenSanPham = sanPham.TenSanPham, SoLuong = newItem.SoLuong, DonGia = newItem.DonGia, ThanhTien = newItem.SoLuong * newItem.DonGia, GhiChu = newItem.GhiChu };
            }

            // Cập nhật tổng và khuyến mãi
            await UpdateHoaDonTotals(hoaDon);
            await ReApplyPromotion(hoaDon);
            await _context.SaveChangesAsync();

            // YÊU CẦU MỚI: Tự động gửi bếp
            await CreateOrUpdateCheBienItems(hoaDon.IdHoaDon, resultDto.IdChiTietHoaDon);

            var updatedHoaDonInfo = await GetHoaDonInfo(req.IdHoaDon);
            return Ok(new AddItemResponseDto { updatedHoaDonInfo = updatedHoaDonInfo, newItem = resultDto });
        }

        [HttpPut("update-quantity")]
        public async Task<IActionResult> UpdateQuantity([FromBody] UpdateSoLuongRequest req)
        {
            var item = await _context.ChiTietHoaDons.Include(c => c.HoaDon).FirstOrDefaultAsync(c => c.IdChiTietHoaDon == req.IdChiTietHoaDon);
            if (item == null) return NotFound("Không tìm thấy món.");
            var hoaDon = item.HoaDon;
            if (hoaDon.TrangThai != "Chưa thanh toán") return Conflict("Hóa đơn đã đóng.");

            // YÊU CẦU MỚI: Tự động gửi bếp (Cập nhật hoặc Xóa)
            if (req.SoLuongMoi <= 0)
            {
                // Xóa khỏi bảng bếp trước
                var bepItem = await _context.TrangThaiCheBiens.FirstOrDefaultAsync(b => b.IdChiTietHoaDon == item.IdChiTietHoaDon);
                if (bepItem != null) _context.TrangThaiCheBiens.Remove(bepItem);

                _context.ChiTietHoaDons.Remove(item);
            }
            else
            {
                item.SoLuong = req.SoLuongMoi;
                // Gọi gửi bếp sau khi lưu
            }
            await _context.SaveChangesAsync();

            // Cập nhật tổng và khuyến mãi
            await UpdateHoaDonTotals(hoaDon);
            await ReApplyPromotion(hoaDon);
            await _context.SaveChangesAsync();

            // Gửi bếp sau khi đã lưu
            if (req.SoLuongMoi > 0)
            {
                await CreateOrUpdateCheBienItems(hoaDon.IdHoaDon, item.IdChiTietHoaDon);
            }

            var updatedHoaDonInfo = await GetHoaDonInfo(hoaDon.IdHoaDon);
            return Ok(updatedHoaDonInfo);
        }

        [HttpDelete("delete-item/{idChiTiet}")]
        public async Task<IActionResult> DeleteItem(int idChiTiet)
        {
            var item = await _context.ChiTietHoaDons.Include(c => c.HoaDon).FirstOrDefaultAsync(c => c.IdChiTietHoaDon == idChiTiet);
            if (item == null) return NotFound("Không tìm thấy món.");
            var hoaDon = item.HoaDon;
            if (hoaDon.TrangThai != "Chưa thanh toán") return Conflict("Hóa đơn đã đóng.");

            int idHoaDon = item.IdHoaDon;

            // YÊU CẦU MỚI: Tự động gửi bếp (Xóa)
            var bepItem = await _context.TrangThaiCheBiens.FirstOrDefaultAsync(b => b.IdChiTietHoaDon == item.IdChiTietHoaDon);
            if (bepItem != null) _context.TrangThaiCheBiens.Remove(bepItem);

            _context.ChiTietHoaDons.Remove(item);
            await _context.SaveChangesAsync();

            await UpdateHoaDonTotals(hoaDon);
            await ReApplyPromotion(hoaDon);
            await _context.SaveChangesAsync();

            var updatedHoaDonInfo = await GetHoaDonInfo(idHoaDon);
            return Ok(updatedHoaDonInfo);
        }

        [HttpPut("apply-promotion")]
        public async Task<IActionResult> ApplyPromotion([FromBody] ApplyPromotionRequest req)
        {
            // Logic giống hệt
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
                _context.HoaDonKhuyenMais.Add(new HoaDon_KhuyenMai { IdHoaDon = req.IdHoaDon, IdKhuyenMai = km.IdKhuyenMai });

                // Tính toán lại giảm giá
                if (string.Equals(km.LoaiGiamGia, "PhanTram", StringComparison.OrdinalIgnoreCase))
                {
                    decimal giamGia = hoaDon.TongTienGoc * (km.GiaTriGiam / 100);
                    // SỬA LỖI CS1061: Sửa 'GiaTriGia' thành 'GiamToiDa'
                    if (km.GiamToiDa.HasValue && giamGia > km.GiamToiDa.Value)
                    { giamGia = km.GiamToiDa.Value; }
                    hoaDon.GiamGia = giamGia;
                }
                else { hoaDon.GiamGia = km.GiaTriGiam; }
            }

            await _context.SaveChangesAsync();
            var updatedHoaDonInfo = await GetHoaDonInfo(req.IdHoaDon);
            return Ok(updatedHoaDonInfo);
        }

        [HttpPut("cancel-order/{idHoaDon}")]
        public async Task<IActionResult> CancelOrder(int idHoaDon)
        {
            var hoaDon = await _context.HoaDons.Include(h => h.Ban).FirstOrDefaultAsync(h => h.IdHoaDon == idHoaDon);
            if (hoaDon == null) return NotFound("Hóa đơn không tồn tại.");

            // 1. Trả lại trạng thái bàn
            if (hoaDon.Ban != null)
            {
                hoaDon.Ban.TrangThai = "Trống";
            }
            // 2. Xóa hết chi tiết
            var chiTietItems = _context.ChiTietHoaDons.Where(c => c.IdHoaDon == idHoaDon);
            _context.ChiTietHoaDons.RemoveRange(chiTietItems);
            // 3. Xóa hết khỏi bếp
            var bepItems = _context.TrangThaiCheBiens.Where(b => b.IdHoaDon == idHoaDon);
            _context.TrangThaiCheBiens.RemoveRange(bepItems);
            // 4. Đổi trạng thái hóa đơn
            hoaDon.TrangThai = "Đã hủy";
            hoaDon.GhiChu = "Hủy bởi nhân viên trên Web";

            await _context.SaveChangesAsync();
            return Ok(new { message = "Đã hủy hóa đơn thành công." });
        }

        // Helper
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
    }
}