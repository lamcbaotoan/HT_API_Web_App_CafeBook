// Tập tin: CafebookApi/Controllers/Web/HoTroController.cs
using CafebookApi.Data;
using CafebookApi.Services;
using CafebookModel.Model.Entities;
using CafebookModel.Model.ModelWeb;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;

namespace CafebookApi.Controllers.Web
{
    [Route("api/web/hotro")]
    [ApiController]
    [AllowAnonymous]
    public class HoTroController : ControllerBase
    {
        private readonly CafebookDbContext _context;
        private readonly AiService _aiService;

        public HoTroController(CafebookDbContext context, AiService aiService)
        {
            _context = context;
            _aiService = aiService;
        }

        private int GetCurrentUserId()
        {
            var idClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            int.TryParse(idClaim?.Value, out int id);
            return id;
        }

        [HttpGet("history")]
        public async Task<IActionResult> GetChatHistory([FromQuery] string? guestSessionId)
        {
            var idKhachHang = GetCurrentUserId();
            IQueryable<ChatLichSu> query;

            if (idKhachHang > 0)
            {
                query = _context.ChatLichSus.Where(c => c.IdKhachHang == idKhachHang);
            }
            else if (!string.IsNullOrEmpty(guestSessionId))
            {
                query = _context.ChatLichSus.Where(c => c.GuestSessionId == guestSessionId);
            }
            else
            {
                return Ok(new List<ChatMessageDto>());
            }

            var history = await query
                .OrderBy(c => c.ThoiGian)
                .Select(c => new ChatMessageDto
                {
                    IdChat = c.IdChat,
                    NoiDung = c.NoiDungTraLoi,
                    ThoiGian = c.ThoiGian,
                    LoaiTinNhan = c.LoaiTinNhan ?? "AI"
                })
                .ToListAsync();

            return Ok(history);
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] SendChatRequestDto request)
        {
            var idKhachHang = GetCurrentUserId();
            string? guestSessionId = (idKhachHang == 0) ? request.GuestSessionId : null;

            if (idKhachHang == 0 && string.IsNullOrEmpty(guestSessionId))
            {
                return Unauthorized("Không thể xác định phiên làm việc của khách vãng lai.");
            }

            if (request == null || string.IsNullOrWhiteSpace(request.NoiDung))
            {
                return BadRequest("Nội dung không được để trống.");
            }
            string noiDungHoi = request.NoiDung;

            var thoiGianGui = DateTime.Now;
            var responseDto = new SendChatResponseDto();

            // 1. Lưu tin nhắn của khách hàng
            var lichSuKhach = new ChatLichSu
            {
                IdKhachHang = (idKhachHang > 0) ? idKhachHang : null,
                GuestSessionId = guestSessionId,
                NoiDungHoi = noiDungHoi,
                NoiDungTraLoi = noiDungHoi,
                ThoiGian = thoiGianGui,
                LoaiChat = "Web",
                LoaiTinNhan = "KhachHang"
            };
            _context.ChatLichSus.Add(lichSuKhach);
            await _context.SaveChangesAsync();
            responseDto.TinNhanCuaKhach = new ChatMessageDto
            {
                IdChat = lichSuKhach.IdChat,
                NoiDung = lichSuKhach.NoiDungTraLoi,
                ThoiGian = lichSuKhach.ThoiGian,
                LoaiTinNhan = lichSuKhach.LoaiTinNhan
            };

            // 2. Gửi cho AI xử lý
            string? aiResponse = await _aiService.GetAnswerAsync(noiDungHoi, (idKhachHang > 0) ? idKhachHang : null);
            string phanHoiCuoiCung;
            string loaiTinNhanPhanHoi = "AI";

            // 3. Xử lý phản hồi
            if (aiResponse == "[NEEDS_SUPPORT]")
            {
                phanHoiCuoiCung = "Câu hỏi của bạn đã được ghi nhận. Chúng tôi sẽ chuyển yêu cầu này đến nhân viên hỗ trợ và phản hồi bạn sớm nhất.";
                responseDto.DaChuyenNhanVien = true;
                await CreateThongBaoHoTro(idKhachHang, guestSessionId, noiDungHoi, "AI phát hiện cần hỗ trợ");
            }
            else if (aiResponse == null)
            {
                phanHoiCuoiCung = "Hiện tại trợ lý AI đang bận. Câu hỏi của bạn đã được chuyển đến nhân viên hỗ trợ.";
                responseDto.DaChuyenNhanVien = true;
                await CreateThongBaoHoTro(idKhachHang, guestSessionId, noiDungHoi, "AI Service lỗi");
            }
            else
            {
                phanHoiCuoiCung = aiResponse;
            }

            // 4. Lưu phản hồi
            var lichSuPhanHoi = new ChatLichSu
            {
                IdKhachHang = (idKhachHang > 0) ? idKhachHang : null,
                GuestSessionId = guestSessionId,
                NoiDungHoi = noiDungHoi,
                NoiDungTraLoi = phanHoiCuoiCung,
                ThoiGian = DateTime.Now,
                LoaiChat = "Web",
                LoaiTinNhan = loaiTinNhanPhanHoi
            };
            _context.ChatLichSus.Add(lichSuPhanHoi);
            await _context.SaveChangesAsync();

            responseDto.TinNhanPhanHoi = new ChatMessageDto
            {
                IdChat = lichSuPhanHoi.IdChat,
                NoiDung = lichSuPhanHoi.NoiDungTraLoi,
                ThoiGian = lichSuPhanHoi.ThoiGian,
                LoaiTinNhan = lichSuPhanHoi.LoaiTinNhan
            };

            // === SỬA LỖI 204 TẠI ĐÂY ===
            // Trả về 200 OK với nội dung JSON
            return Ok(responseDto);
        }

        private async Task CreateThongBaoHoTro(int? idKhachHang, string? guestSessionId, string noiDungYeuCau, string ghiChu)
        {
            int? finalKhachHangId = (idKhachHang.HasValue && idKhachHang > 0) ? idKhachHang : null;

            // Logic tạo KVL tạm đã bị XÓA (theo yêu cầu của bạn)

            var thongBaoHoTro = new ThongBaoHoTro
            {
                IdKhachHang = finalKhachHangId, // Sẽ là NULL nếu là KVL
                GuestSessionId = guestSessionId, // Sẽ có giá trị nếu là KVL
                NoiDungYeuCau = noiDungYeuCau,
                ThoiGianTao = DateTime.Now,
                TrangThai = "Chờ xử lý",
                GhiChu = ghiChu
            };
            _context.ThongBaoHoTros.Add(thongBaoHoTro);
            await _context.SaveChangesAsync();

            string idHienThi = finalKhachHangId?.ToString() ?? $"KVL ({guestSessionId?.Substring(0, 6)}...)";
            string noiDungThongBao = $"Người dùng ({idHienThi}) cần hỗ trợ.";
            if (!string.IsNullOrEmpty(noiDungYeuCau))
            {
                noiDungThongBao = $"Người dùng ({idHienThi}) cần hỗ trợ: '{noiDungYeuCau.Substring(0, Math.Min(noiDungYeuCau.Length, 100))}'";
            }

            var thongBao = new ThongBao
            {
                NoiDung = noiDungThongBao,
                ThoiGianTao = DateTime.Now,
                LoaiThongBao = "HoTroKhachHang",
                IdLienQuan = thongBaoHoTro.IdThongBao,
                DaXem = false
            };
            _context.ThongBaos.Add(thongBao);
            await _context.SaveChangesAsync();
        }
    }
}