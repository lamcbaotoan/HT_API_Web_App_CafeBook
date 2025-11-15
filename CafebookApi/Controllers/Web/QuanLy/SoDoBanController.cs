// Tập tin: CafebookApi/Controllers/Web/QuanLy/SoDoBanController.cs

using CafebookApi.Data;
using CafebookModel.Model.Entities;
using CafebookModel.Model.ModelWeb.QuanLy;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore; // <-- SỬA LỖI CS1061: Thêm using này
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace CafebookApi.Controllers.Web.QuanLy
{
    [Route("api/web/quanly/sodoban")]
    [ApiController]
    public class SoDoBanController : ControllerBase
    {
        private readonly CafebookDbContext _context;

        public SoDoBanController(CafebookDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// API lấy tất cả Khu Vực và Bàn (ĐÃ SỬA LỖI CS8072)
        /// </summary>
        [HttpGet]
        public async Task<IActionResult> GetSoDoBan()
        {
            var thoiGianHienTai = DateTime.Now;

            // 1. Lấy thông tin Hóa đơn (vẫn làm trước, chạy 1 lần)
            var hoaDonChuaThanhToan = await _context.HoaDons
                .Where(h => h.TrangThai == "Chưa thanh toán" && h.IdBan != null)
                .GroupBy(h => h.IdBan)
                .Select(g => new
                {
                    IdBan = g.Key,
                    IdHoaDonHienTai = g.OrderByDescending(h => h.ThoiGianTao).Select(h => h.IdHoaDon).FirstOrDefault(),
                    TongTien = g.Sum(h => h.ThanhTien) // Dùng cột tính toán 'ThanhTien'
                })
                .ToDictionaryAsync(h => (int)h.IdBan!, h => h); // <-- Lỗi CS1061 được fix bởi using

            // 2. Lấy thông tin Đặt bàn (vẫn làm trước, chạy 1 lần)
            var phieuDatBanData = await _context.PhieuDatBans
                            .Where(p => p.TrangThai == "Đã xác nhận" && p.ThoiGianDat.Date == thoiGianHienTai.Date && p.ThoiGianDat.TimeOfDay >= thoiGianHienTai.TimeOfDay)
                            .Select(p => new
                            {
                                p.IdBan,
                                // Format thông tin đặt bàn
                                ThongTinDatBan = $"Đặt: {p.ThoiGianDat:HH:mm} - {p.HoTenKhach ?? p.SdtKhach}"
                            })
                            .ToListAsync(); // <-- 1. Tải về bộ nhớ (thay vì ToLookupAsync)

            // 2.b. Chuyển đổi sang Lookup (chạy trong bộ nhớ, không dùng await)
            var phieuDatBanHienTai = phieuDatBanData
                .ToLookup(p => p.IdBan, p => p.ThongTinDatBan);

            // 3. Lấy dữ liệu thô từ DB (SỬA LỖI CS8072)
            // Chỉ lấy dữ liệu cơ bản, không dùng Dictionary/Lookup tại đây
            var khuVucList = await _context.KhuVucs
                .Include(k => k.Bans)
                .OrderBy(k => k.IdKhuVuc)
                .Select(k => new KhuVucDto
                {
                    IdKhuVuc = k.IdKhuVuc,
                    TenKhuVuc = k.TenKhuVuc,
                    Bans = k.Bans.OrderBy(b => b.IdBan).Select(b => new BanDto
                    {
                        IdBan = b.IdBan,
                        SoBan = b.SoBan,
                        TrangThai = b.TrangThai,
                        GhiChu = b.GhiChu,
                        IdKhuVuc = b.IdKhuVuc ?? 0
                        // Xóa các trường logic phức tạp (IdHoaDonHienTai, TongTien, ThongTinDatBan)
                    }).ToList()
                })
                .ToListAsync(); // <-- Tải dữ liệu về bộ nhớ (in-memory)

            // 4. Xử lý logic phức tạp (gán IdHoaDon, TongTien...) TRONG BỘ NHỚ
            // Vòng lặp này chạy sau khi đã lấy dữ liệu từ DB, nên không còn lỗi Expression Tree
            foreach (var kv in khuVucList)
            {
                foreach (var ban in kv.Bans)
                {
                    // Gán thông tin Hóa đơn (nếu có)
                    if (ban.TrangThai == "Có khách" && hoaDonChuaThanhToan.TryGetValue(ban.IdBan, out var hoaDonInfo))
                    {
                        ban.IdHoaDonHienTai = hoaDonInfo.IdHoaDonHienTai;
                        ban.TongTien = hoaDonInfo.TongTien;
                    }

                    // Gán thông tin Đặt bàn (nếu có)
                    if ((ban.TrangThai == "Trống" || ban.TrangThai == "Đã đặt") && phieuDatBanHienTai.Contains(ban.IdBan))
                    {
                        ban.ThongTinDatBan = phieuDatBanHienTai[ban.IdBan].FirstOrDefault();
                    }
                }
            }

            return Ok(khuVucList);
        }

        // --- CÁC HÀM POST (Giữ nguyên, không có lỗi) ---

        [HttpPost("createorder")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequestDto request)
        {
            var ban = await _context.Bans.FindAsync(request.IdBan);
            if (ban == null) return NotFound("Không tìm thấy bàn.");

            // Kiểm tra xem bàn có phiếu đặt không
            var phieuDat = await _context.PhieuDatBans
                .FirstOrDefaultAsync(p => p.IdBan == request.IdBan && p.TrangThai == "Đã xác nhận" && p.ThoiGianDat.Date == DateTime.Now.Date);

            if (ban.TrangThai == "Trống" || ban.TrangThai == "Đã đặt")
            {
                var hoaDon = new HoaDon
                {
                    IdBan = request.IdBan,
                    IdNhanVien = request.IdNhanVien,
                    ThoiGianTao = DateTime.Now,
                    TrangThai = "Chưa thanh toán",
                    LoaiHoaDon = "Tại quán",
                    TongTienGoc = 0,
                    GiamGia = 0,
                    TongPhuThu = 0,
                    // Nếu khách đến từ phiếu đặt, gán IdKhachHang
                    IdKhachHang = phieuDat?.IdKhachHang
                };

                _context.HoaDons.Add(hoaDon);
                ban.TrangThai = "Có khách";
                ban.GhiChu = null;

                // Nếu có phiếu đặt, cập nhật trạng thái phiếu
                if (phieuDat != null)
                {
                    phieuDat.TrangThai = "Khách đã đến";
                }

                await _context.SaveChangesAsync();
                return Ok(new CreateOrderResponseDto { IdHoaDon = hoaDon.IdHoaDon });
            }

            return BadRequest("Bàn đang có khách hoặc đang bảo trì.");
        }

        [HttpPost("reportproblem")]
        public async Task<IActionResult> ReportProblem([FromBody] ReportProblemRequestDto request)
        {
            var ban = await _context.Bans.FindAsync(request.IdBan);
            if (ban == null) return NotFound("Không tìm thấy bàn.");

            if (ban.TrangThai == "Có khách")
            {
                return BadRequest("Không thể báo cáo sự cố cho bàn đang có khách.");
            }

            // --- SỬA ĐỔI LOGIC ---
            if (string.IsNullOrEmpty(request.GhiChu))
            {
                // Nếu GhiChu rỗng -> Client muốn HỦY BẢO TRÌ
                if (ban.TrangThai == "Bảo trì")
                {
                    ban.TrangThai = "Trống"; // Chuyển về trạng thái Trống
                    ban.GhiChu = null;
                    await _context.SaveChangesAsync();
                    return Ok(new { message = "Đã hủy bảo trì bàn." });
                }
                return Ok(new { message = "Không có gì thay đổi." });
            }
            // --- Kết thúc sửa đổi ---

            // Logic báo cáo sự cố như cũ
            ban.TrangThai = "Bảo trì";
            ban.GhiChu = request.GhiChu;

            var thongBao = new ThongBao
            {
                IdNhanVienTao = request.IdNhanVien,
                NoiDung = $"Bàn {ban.SoBan} vừa được báo cáo sự cố: {request.GhiChu}",
                ThoiGianTao = DateTime.Now,
                LoaiThongBao = "SuCoBan",
                IdLienQuan = ban.IdBan,
                DaXem = false
            };
            _context.ThongBaos.Add(thongBao);

            await _context.SaveChangesAsync();
            return Ok(new { message = "Báo cáo sự cố thành công." });
        }

        [HttpPost("move-table")]
        public async Task<IActionResult> MoveTable([FromBody] BanActionRequestDto request)
        {
            var hoaDonNguon = await _context.HoaDons.FindAsync(request.IdHoaDonNguon);
            if (hoaDonNguon == null) return NotFound("Không tìm thấy hóa đơn nguồn.");

            var banNguon = await _context.Bans.FindAsync(hoaDonNguon.IdBan);
            var banDich = await _context.Bans.FindAsync(request.IdBanDich);

            if (banNguon == null) return NotFound("Không tìm thấy bàn nguồn.");
            if (banDich == null) return NotFound("Không tìm thấy bàn đích.");

            if (banDich.TrangThai != "Trống")
            {
                return BadRequest("Bàn đích phải là bàn trống.");
            }

            hoaDonNguon.IdBan = banDich.IdBan;
            banNguon.TrangThai = "Trống";
            banDich.TrangThai = "Có khách";
            banDich.GhiChu = null; // Xóa ghi chú (nếu có) của bàn đích

            await _context.SaveChangesAsync();
            return Ok(new { message = "Chuyển bàn thành công." });
        }

        [HttpPost("merge-table")]
        public async Task<IActionResult> MergeTable([FromBody] BanActionRequestDto request)
        {
            if (!request.IdHoaDonDich.HasValue)
            {
                return BadRequest("Phải có hóa đơn đích để gộp.");
            }

            var hoaDonNguon = await _context.HoaDons
                .Include(h => h.ChiTietHoaDons)
                .FirstOrDefaultAsync(h => h.IdHoaDon == request.IdHoaDonNguon);

            var hoaDonDich = await _context.HoaDons.FindAsync(request.IdHoaDonDich.Value);

            if (hoaDonNguon == null) return NotFound("Không tìm thấy hóa đơn nguồn.");
            if (hoaDonDich == null) return NotFound("Không tìm thấy hóa marshall.");

            var banNguon = await _context.Bans.FindAsync(hoaDonNguon.IdBan);
            if (banNguon == null) return NotFound("Không tìm thấy bàn nguồn.");

            // 1. Chuyển tất cả chi tiết từ HĐ nguồn sang HĐ đích
            foreach (var ct in hoaDonNguon.ChiTietHoaDons.ToList()) // Dùng ToList() để tránh lỗi collection modified
            {
                var ctDich = await _context.ChiTietHoaDons
                    .FirstOrDefaultAsync(c => c.IdHoaDon == hoaDonDich.IdHoaDon &&
                                              c.IdSanPham == ct.IdSanPham &&
                                              c.GhiChu == ct.GhiChu &&
                                              c.DonGia == ct.DonGia); // Thêm kiểm tra đơn giá

                if (ctDich != null)
                {
                    ctDich.SoLuong += ct.SoLuong;
                    _context.ChiTietHoaDons.Remove(ct); // Xóa CTHD gốc
                }
                else
                {
                    ct.IdHoaDon = hoaDonDich.IdHoaDon;
                    _context.ChiTietHoaDons.Update(ct);
                }
            }

            // 2. Chuyển phiếu giảm giá (nếu có)
            var kmNguon = await _context.HoaDonKhuyenMais.Where(k => k.IdHoaDon == hoaDonNguon.IdHoaDon).ToListAsync();
            foreach (var km in kmNguon)
            {
                var kmDichExits = await _context.HoaDonKhuyenMais
                    .AnyAsync(k => k.IdHoaDon == hoaDonDich.IdHoaDon && k.IdKhuyenMai == km.IdKhuyenMai);
                if (!kmDichExits)
                {
                    km.IdHoaDon = hoaDonDich.IdHoaDon;
                    _context.HoaDonKhuyenMais.Update(km);
                }
                else
                {
                    _context.HoaDonKhuyenMais.Remove(km);
                }
            }

            // 3. Hủy hóa đơn nguồn và cập nhật bàn nguồn
            hoaDonNguon.TrangThai = "Đã hủy";
            hoaDonNguon.GhiChu = $"Đã gộp vào HĐ #{hoaDonDich.IdHoaDon}";
            hoaDonNguon.TongTienGoc = 0; // Reset tiền
            hoaDonNguon.GiamGia = 0;
            hoaDonNguon.TongPhuThu = 0;

            banNguon.TrangThai = "Trống";

            // 4. Tính toán lại tổng tiền cho hóa đơn đích
            await _context.SaveChangesAsync(); // Lưu thay đổi CTHD trước

            var chiTietMoi = await _context.ChiTietHoaDons
                .Where(c => c.IdHoaDon == hoaDonDich.IdHoaDon)
                .ToListAsync();

            hoaDonDich.TongTienGoc = chiTietMoi.Sum(c => c.ThanhTien);
            // (Tạm thời chưa tính lại giảm giá/phụ thu phức tạp, chỉ gộp CTHD)

            await _context.SaveChangesAsync();
            return Ok(new { message = "Gộp bàn thành công." });
        }
    }
}