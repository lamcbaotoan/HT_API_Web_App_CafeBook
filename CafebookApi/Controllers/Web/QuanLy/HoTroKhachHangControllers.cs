// Tập tin: CafebookApi/Controllers/Web/QuanLy/HoTroKhachHangControllers.cs
using CafebookApi.Data;
using CafebookModel.Model.Entities;
using CafebookModel.Model.ModelWeb;
using CafebookModel.Model.ModelWeb.QuanLy;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.SignalR;
using CafebookApi.Hubs;

namespace CafebookApi.Controllers.Web.QuanLy
{
    [Route("api/web/quanly/hotro")]
    //[ApiController]
    // [Authorize(Roles = "NhanVien,QuanLy")]
    public class HoTroKhachHangController : ControllerBase
    {
        private readonly CafebookDbContext _context;
        private readonly IHubContext<ChatHub> _chatHubContext; // <-- BẮT BUỘC CÓ

        // CẬP NHẬT CONSTRUCTOR
        public HoTroKhachHangController(CafebookDbContext context, IHubContext<ChatHub> chatHubContext)
        {
            _context = context;
            _chatHubContext = chatHubContext; // <-- BẮT BUỘC CÓ
        }

        private int GetCurrentUserId()
        {
            var idClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            int.TryParse(idClaim?.Value, out int id);
            return id;
        }

        /// <summary>
        /// API lấy danh sách tất cả các phiếu hỗ trợ (Giữ nguyên)
        /// </summary>
        [HttpGet("tickets")]
        public async Task<IActionResult> GetTickets()
        {
            var tickets = await _context.ThongBaoHoTros
                .Include(t => t.KhachHang) // Include an toàn
                .OrderByDescending(t => t.ThoiGianTao)
                .Select(t => new HoTroKhachHangListDto
                {
                    IdThongBao = t.IdThongBao,
                    TenKhachHang = t.KhachHang != null ? t.KhachHang.HoTen : $"Khách vãng lai ({t.GuestSessionId})", // Sửa lại logic an toàn
                    NoiDungYeuCau = t.NoiDungYeuCau,
                    ThoiGianTao = t.ThoiGianTao,
                    TrangThai = t.TrangThai,
                    GhiChuTuAI = t.GhiChu
                })
                .ToListAsync();
            return Ok(tickets);
        }

        /// <summary>
        /// API lấy chi tiết 1 phiếu hỗ trợ (bao gồm lịch sử chat)
        /// </summary>
        [HttpGet("ticket/{id}")]
        public async Task<IActionResult> GetTicketDetail(int id)
        {
            var ticket = await _context.ThongBaoHoTros
                .Include(t => t.KhachHang)
                .FirstOrDefaultAsync(t => t.IdThongBao == id);

            if (ticket == null) return NotFound("Không tìm thấy phiếu hỗ trợ.");

            // === LOGIC TRUY VẤN LỊCH SỬ CHAT (ĐÃ SỬA VÀ ĐƠN GIẢN HÓA) ===
            // 'id' chính là IdThongBaoHoTro.
            // Chúng ta chỉ cần truy vấn chính xác theo ID vé này.
            var query = _context.ChatLichSus
                .Where(c => c.IdThongBaoHoTro == id);
            // ======================================

            var chatHistory = await query
                .OrderBy(c => c.ThoiGian)
                .Select(c => new ChatMessageDto // Tái sử dụng DTO
                {
                    IdChat = c.IdChat,
                    NoiDung = c.NoiDungTraLoi,
                    ThoiGian = c.ThoiGian,
                    LoaiTinNhan = c.LoaiTinNhan ?? "AI"
                })
                .ToListAsync();

            var dto = new HoTroKhachHangDetailDto
            {
                IdThongBao = ticket.IdThongBao,
                IdKhachHang = ticket.IdKhachHang,
                GuestSessionId = ticket.GuestSessionId,
                TenKhachHang = ticket.KhachHang != null ? ticket.KhachHang.HoTen : $"Khách vãng lai ({ticket.GuestSessionId})",
                NoiDungYeuCau = ticket.NoiDungYeuCau,
                ThoiGianTao = ticket.ThoiGianTao,
                TrangThai = ticket.TrangThai,
                GhiChuTuAI = ticket.GhiChu,
                LichSuChat = chatHistory // Gán danh sách (đã tải chính xác)
            };

            return Ok(dto);
        }

        // === XÓA BỎ [HttpPost("reply")] ===
        // Logic này đã được chuyển sang ChatHub.SendMessageFromStaff

        /// <summary>
        /// API để đánh dấu phiếu đã xử lý (đóng ticket)
        /// </summary>
        [HttpPost("resolve/{id}")]
        public async Task<IActionResult> ResolveTicket(int id)
        {
            var ticket = await _context.ThongBaoHoTros.FindAsync(id);
            if (ticket == null) return NotFound("Không tìm thấy phiếu hỗ trợ.");

            ticket.TrangThai = "Đã xử lý";
            await _context.SaveChangesAsync();

            // Gửi tín hiệu reload
            await _chatHubContext.Clients.All.SendAsync("ReloadTicketList");

            return Ok();
        }
    }
}