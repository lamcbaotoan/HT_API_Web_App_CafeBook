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
    [Route("api/web/datban")]
    [ApiController]
    public class DatBanWebController : ControllerBase
    {
        private readonly CafebookDbContext _context;
        private const int SlotDurationHours = 2; // Mỗi lượt đặt kéo dài 2 tiếng

        public DatBanWebController(CafebookDbContext context)
        {
            _context = context;
        }

        /// <summary>
        /// Tìm kiếm các bàn còn trống vào ngày giờ cụ thể
        /// </summary>
        [HttpPost("tim-ban")]
        public async Task<IActionResult> TimBanTrong([FromBody] TimBanRequestDto req)
        {
            // 1. Xác định khung giờ khách muốn đặt
            DateTime requestedStart = req.NgayDat.Date.Add(req.GioDat);
            DateTime requestedEnd = requestedStart.AddHours(SlotDurationHours);

            if (requestedStart < DateTime.Now.AddMinutes(30))
            {
                return BadRequest("Vui lòng đặt bàn trước ít nhất 30 phút.");
            }

            // 2. Lấy danh sách tất cả các bàn phù hợp với số người
            var suitableTables = await _context.Bans
                .Include(b => b.KhuVuc)
                .Where(b => b.SoGhe >= req.SoNguoi && b.TrangThai != "Bảo trì" && b.TrangThai != "Tạm ngưng")
                .ToListAsync();

            // 3. Lấy danh sách các phiếu đặt bàn ĐÃ CONFIRM hoặc ĐANG CHỜ trong ngày đó để kiểm tra trùng lặp
            // (Lọc sơ bộ theo ngày để giảm tải)
            var existingBookings = await _context.PhieuDatBans
                .Where(p => p.ThoiGianDat.Date == req.NgayDat.Date &&
                            (p.TrangThai == "Đã xác nhận" || p.TrangThai == "Chờ xác nhận" || p.TrangThai == "Khách đã đến"))
                .ToListAsync();

            var availableTables = new List<BanTrongDto>();

            foreach (var table in suitableTables)
            {
                // Kiểm tra xem bàn này có bị trùng lịch không
                bool isBusy = existingBookings.Any(booking =>
                {
                    if (booking.IdBan != table.IdBan) return false;

                    DateTime bookingStart = booking.ThoiGianDat;
                    DateTime bookingEnd = bookingStart.AddHours(SlotDurationHours);

                    // Logic trùng lặp: (StartA < EndB) và (StartB < EndA)
                    return requestedStart < bookingEnd && bookingStart < requestedEnd;
                });

                if (!isBusy)
                {
                    availableTables.Add(new BanTrongDto
                    {
                        IdBan = table.IdBan,
                        SoBan = table.SoBan,
                        SoGhe = table.SoGhe,
                        KhuVuc = table.KhuVuc?.TenKhuVuc ?? "Chung",
                        MoTa = table.GhiChu
                    });
                }
            }

            return Ok(availableTables.OrderBy(t => t.SoGhe).ThenBy(t => t.SoBan));
        }

        /// <summary>
        /// Tạo yêu cầu đặt bàn mới (Trạng thái: Chờ xác nhận)
        /// </summary>
        [HttpPost("tao-yeu-cau")]
        public async Task<IActionResult> TaoYeuCauDatBan([FromBody] DatBanWebRequestDto req)
        {
            // 1. Xác định khách hàng (từ Token nếu đã đăng nhập, hoặc từ form nếu chưa)
            int? idKhachHang = null;
            string tenKhach = req.HoTen ?? "Khách";
            string sdtKhach = req.SoDienThoai ?? "";

            if (User.Identity?.IsAuthenticated == true)
            {
                var claimId = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (int.TryParse(claimId, out int id))
                {
                    idKhachHang = id;
                    var kh = await _context.KhachHangs.FindAsync(id);
                    if (kh != null)
                    {
                        tenKhach = kh.HoTen;
                        sdtKhach = kh.SoDienThoai ?? "";
                    }
                }
            }

            if (string.IsNullOrEmpty(tenKhach) || string.IsNullOrEmpty(sdtKhach))
            {
                return BadRequest("Vui lòng cung cấp Họ tên và Số điện thoại liên hệ.");
            }

            // 2. Kiểm tra lại tình trạng bàn (đề phòng race condition)
            DateTime requestedStart = req.NgayDat.Date.Add(req.GioDat);
            DateTime requestedEnd = requestedStart.AddHours(SlotDurationHours);

            bool isConflict = await _context.PhieuDatBans.AnyAsync(p =>
                p.IdBan == req.IdBan &&
                (p.TrangThai == "Đã xác nhận" || p.TrangThai == "Chờ xác nhận" || p.TrangThai == "Khách đã đến") &&
                requestedStart < p.ThoiGianDat.AddHours(SlotDurationHours) &&
                p.ThoiGianDat < requestedEnd
            );

            if (isConflict)
            {
                return Conflict("Rất tiếc, bàn này vừa có người đặt vào khung giờ bạn chọn. Vui lòng chọn bàn khác.");
            }

            // 3. Tạo phiếu đặt bàn
            var phieuMoi = new PhieuDatBan
            {
                IdBan = req.IdBan,
                IdKhachHang = idKhachHang,
                HoTenKhach = tenKhach,
                SdtKhach = sdtKhach,
                ThoiGianDat = requestedStart,
                SoLuongKhach = req.SoLuongKhach,
                GhiChu = req.GhiChu,
                TrangThai = "Chờ xác nhận" // QUAN TRỌNG: Trạng thái chờ
            };

            _context.PhieuDatBans.Add(phieuMoi);
            await _context.SaveChangesAsync(); // Lưu để lấy ID phiếu

            // 4. Tạo thông báo cho nhân viên
            var banInfo = await _context.Bans.FindAsync(req.IdBan);
            var thongBao = new ThongBao
            {
                NoiDung = $"Đơn đặt bàn mới: {tenKhach} - Bàn {banInfo?.SoBan} lúc {requestedStart:HH:mm dd/MM}",
                LoaiThongBao = "DatBan", // Loại để app nhân viên nhận biết
                IdLienQuan = phieuMoi.IdPhieuDatBan, // Liên kết để mở nhanh phiếu
                ThoiGianTao = DateTime.Now,
                DaXem = false,
                IdNhanVienTao = null // Hệ thống tạo
            };
            _context.ThongBaos.Add(thongBao);
            await _context.SaveChangesAsync();

            return Ok(new { success = true, message = "Yêu cầu đặt bàn của bạn đã được gửi. Chúng tôi sẽ sớm xác nhận!", idPhieu = phieuMoi.IdPhieuDatBan });
        }
    }
}