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
using System.Text.RegularExpressions;
using System.Collections.Generic;
using Microsoft.AspNetCore.SignalR; // <-- THÊM MỚI
using CafebookApi.Hubs; // <-- THÊM MỚI

namespace CafebookApi.Controllers.Web
{
    [Route("api/web/hotro")]
    [ApiController]
    [AllowAnonymous]
    public class HoTroController : ControllerBase
    {
        private readonly CafebookDbContext _context;
        private readonly AiService _aiService;
        private readonly AiToolService _aiToolService;
        private readonly IHubContext<ChatHub> _chatHubContext; // <-- THÊM MỚI

        // CẬP NHẬT CONSTRUCTOR
        public HoTroController(
            CafebookDbContext context,
            AiService aiService,
            AiToolService aiToolService,
            IHubContext<ChatHub> chatHubContext) // <-- THÊM MỚI
        {
            _context = context;
            _aiService = aiService;
            _aiToolService = aiToolService;
            _chatHubContext = chatHubContext; // <-- THÊM MỚI
        }

        private int GetCurrentUserId()
        {
            var idClaim = User.Claims.FirstOrDefault(c => c.Type == ClaimTypes.NameIdentifier);
            int.TryParse(idClaim?.Value, out int id);
            return id;
        }

        [HttpPost("send")]
        public async Task<IActionResult> SendMessage([FromBody] SendChatRequestDto request)
        {
            var idKhachHang = GetCurrentUserId();
            string? guestSessionId = (idKhachHang == 0) ? request.GuestSessionId : null;

            if (idKhachHang == 0 && string.IsNullOrEmpty(guestSessionId))
            {
                return Unauthorized("Không thể xác định phiên làm việc.");
            }

            var responseDto = new SendChatResponseDto();

            // 1. Lưu tin nhắn của khách
            var lichSuKhach = await SaveChatHistoryAsync(idKhachHang, guestSessionId, request.NoiDung, request.NoiDung, "KhachHang", null);
            responseDto.TinNhanCuaKhach = MapToChatDto(lichSuKhach);

            // 2. Tải lịch sử chat (cho AI)
            var chatHistory = await GetChatHistoryPayloadAsync(idKhachHang, guestSessionId);

            // 3. Gửi cho AI (Vòng 1)
            string? aiResponse = await _aiService.GetAnswerAsync(request.NoiDung, (idKhachHang > 0) ? idKhachHang : null, chatHistory);

            string phanHoiCuoiCung;
            string loaiTinNhanPhanHoi = "AI";
            ThongBaoHoTro? thongBaoHoTro = null;

            // 4. Phân tích phản hồi của AI
            if (aiResponse == null)
            {
                phanHoiCuoiCung = "Hiện tại trợ lý AI đang bận. Câu hỏi của bạn đã được chuyển đến nhân viên hỗ trợ.";
                responseDto.DaChuyenNhanVien = true;
                thongBaoHoTro = await CreateThongBaoHoTro(idKhachHang, guestSessionId, request.NoiDung, "AI Service lỗi");

                // === BẮT ĐẦU SỬA LỖI ===
                await UpdateChatHistoryWithTicketId(idKhachHang, guestSessionId, thongBaoHoTro.IdThongBao);
                // === KẾT THÚC SỬA LỖI ===
            }
            else if (aiResponse == "[NEEDS_SUPPORT]")
            {
                phanHoiCuoiCung = "Câu hỏi của bạn đã được ghi nhận. Chúng tôi sẽ chuyển yêu cầu này đến nhân viên hỗ trợ và phản hồi bạn sớm nhất.";
                responseDto.DaChuyenNhanVien = true;
                thongBaoHoTro = await CreateThongBaoHoTro(idKhachHang, guestSessionId, request.NoiDung, "AI phát hiện cần hỗ trợ");

                // === BẮT ĐẦU SỬA LỖI ===
                await UpdateChatHistoryWithTicketId(idKhachHang, guestSessionId, thongBaoHoTro.IdThongBao);
                // === KẾT THÚC SỬA LỖI ===
            }
            else if (aiResponse.StartsWith("[TOOL_CALL:"))
            {
                await SaveChatHistoryAsync(idKhachHang, guestSessionId, request.NoiDung, aiResponse, "AI_ToolCall", null);
                string toolResult = await ExecuteToolAsync(aiResponse, idKhachHang);
                var chatHistoryV2 = await GetChatHistoryPayloadAsync(idKhachHang, guestSessionId);
                string? finalAiResponse = await _aiService.GetAnswerAsync(request.NoiDung, (idKhachHang > 0) ? idKhachHang : null, chatHistoryV2, toolResult);
                phanHoiCuoiCung = finalAiResponse ?? "Dịch vụ AI đang gặp sự cố khi diễn giải kết quả.";
            }
            else
            {
                phanHoiCuoiCung = aiResponse;
            }

            // 5. Lưu phản hồi cuối cùng
            var lichSuPhanHoi = await SaveChatHistoryAsync(
                idKhachHang, guestSessionId, request.NoiDung, phanHoiCuoiCung, loaiTinNhanPhanHoi,
                thongBaoHoTro?.IdThongBao // Gán IdThongBao (nếu có)
            );
            responseDto.TinNhanPhanHoi = MapToChatDto(lichSuPhanHoi);

            // 6. Trả về IdThongBao để JS biết và kích hoạt SignalR
            if (thongBaoHoTro != null)
            {
                responseDto.IdThongBaoHoTro = thongBaoHoTro.IdThongBao;
            }

            return Ok(responseDto);
        }

        // === HÀM CREATETHONGBAO ĐƯỢC NÂNG CẤP ===
        private async Task<ThongBaoHoTro> CreateThongBaoHoTro(int? idKhachHang, string? guestSessionId, string noiDungYeuCau, string ghiChu)
        {
            int? finalKhachHangId = (idKhachHang.HasValue && idKhachHang > 0) ? idKhachHang : null;
            var thongBaoHoTro = new ThongBaoHoTro
            {
                IdKhachHang = finalKhachHangId,
                GuestSessionId = guestSessionId,
                NoiDungYeuCau = noiDungYeuCau,
                ThoiGianTao = DateTime.Now,
                TrangThai = "Chờ xử lý",
                GhiChu = ghiChu
            };
            _context.ThongBaoHoTros.Add(thongBaoHoTro);
            await _context.SaveChangesAsync();

            // Logic tạo ThongBao chung
            string idHienThi = finalKhachHangId?.ToString() ?? $"KVL ({guestSessionId?.Substring(0, 6)}...)";
            string noiDungThongBao = $"Người dùng ({idHienThi}) cần hỗ trợ: '{noiDungYeuCau.Substring(0, Math.Min(noiDungYeuCau.Length, 100))}'";
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

            // === NÂNG CẤP SIGNALR ===
            // Gửi tín hiệu cho TẤT CẢ nhân viên đang mở trang dashboard
            await _chatHubContext.Clients.All.SendAsync("ReloadTicketList");
            // ======================

            return thongBaoHoTro; // Trả về entity để lấy ID
        }

        // === CÁC HÀM HELPER (Giữ nguyên) ===

        private async Task<ChatLichSu> SaveChatHistoryAsync(int? idKhachHang, string? guestSessionId, string hoi, string traLoi, string loaiTinNhan, int? idThongBao)
        {
            var lichSu = new ChatLichSu
            {
                IdKhachHang = (idKhachHang > 0) ? idKhachHang : null,
                GuestSessionId = guestSessionId,
                NoiDungHoi = hoi,
                NoiDungTraLoi = traLoi,
                ThoiGian = DateTime.Now,
                LoaiChat = "Web_HTTP", // Đánh dấu đây là chat qua HTTP
                LoaiTinNhan = loaiTinNhan,
                IdThongBaoHoTro = idThongBao
            };
            _context.ChatLichSus.Add(lichSu);
            await _context.SaveChangesAsync();
            return lichSu;
        }

        private async Task<string> ExecuteToolAsync(string toolCall, int? idKhachHang)
        {
            try
            {
                var toolMatch = Regex.Match(toolCall, @"\[TOOL_CALL: ([\w_]+)(.*)\]");
                if (!toolMatch.Success) return "[TOOL_ERROR: Định dạng tool call không hợp lệ]";
                string toolName = toolMatch.Groups[1].Value;
                string paramString = toolMatch.Groups[2].Value.Trim();
                Func<string, string, string> getParam = (name, defaultValue) =>
                {
                    var match = Regex.Match(paramString, $@"{name}:\s*'([^']*)'");
                    if (!match.Success) match = Regex.Match(paramString, $@"{name}:\s*([\d]+)");
                    return match.Success ? match.Groups[1].Value : defaultValue;
                };

                switch (toolName)
                {
                    case "GET_THONG_TIN_CHUNG":
                        return await _aiToolService.GetThongTinChungAsync();
                    case "KIEM_TRA_BAN":
                        int.TryParse(getParam("SoNguoi", "0"), out int soNguoi);
                        if (soNguoi == 0) return "[TOOL_ERROR: AI quên hỏi số lượng người]";
                        return await _aiToolService.KiemTraBanTrongAsync(soNguoi);
                    case "KIEM_TRA_SAN_PHAM":
                        string tenSP = getParam("TenSanPham", "");
                        if (string.IsNullOrEmpty(tenSP)) return "[TOOL_ERROR: AI quên hỏi tên sản phẩm]";
                        return await _aiToolService.KiemTraSanPhamAsync(tenSP);
                    case "KIEM_TRA_SACH":
                        string tenSach = getParam("TenSach", "");
                        if (string.IsNullOrEmpty(tenSach)) return "[TOOL_ERROR: AI quên hỏi tên sách]";
                        return await _aiToolService.KiemTraSachAsync(tenSach);
                    case "GET_THONG_TIN_KHACH_HANG":
                        if (idKhachHang == null || idKhachHang == 0) return "[TOOL_ERROR: AI gọi nhầm tool (Khách chưa đăng nhập)]";
                        return await _aiToolService.GetThongTinKhachHangAsync(idKhachHang.Value);
                    default:
                        return "[TOOL_ERROR: Tên tool không tồn tại]";
                }
            }
            catch (Exception ex)
            {
                return $"[TOOL_EXCEPTION: {ex.Message}]";
            }
        }

        private async Task<List<dynamic>> GetChatHistoryPayloadAsync(int? idKhachHang, string? guestSessionId)
        {
            var query = (idKhachHang > 0)
                ? _context.ChatLichSus.Where(c => c.IdKhachHang == idKhachHang)
                : _context.ChatLichSus.Where(c => c.GuestSessionId == guestSessionId);

            var history = await query
                .OrderByDescending(c => c.ThoiGian)
                .Take(10)
                .OrderBy(c => c.ThoiGian)
                .ToListAsync();

            var payload = new List<dynamic>();
            foreach (var msg in history)
            {
                if (msg.LoaiTinNhan == "KhachHang") { payload.Add(new { role = "user", parts = new[] { new { text = msg.NoiDungTraLoi } } }); }
                else { payload.Add(new { role = "model", parts = new[] { new { text = msg.NoiDungTraLoi } } }); }
            }
            return payload;
        }

        private ChatMessageDto MapToChatDto(ChatLichSu entity)
        {
            return new ChatMessageDto
            {
                IdChat = entity.IdChat,
                NoiDung = entity.NoiDungTraLoi,
                ThoiGian = entity.ThoiGian,
                LoaiTinNhan = entity.LoaiTinNhan ?? "AI"
            };
        }

        /// <summary>
        /// Hàm helper để cập nhật các tin nhắn AI (chưa có vé) vào ID vé mới
        /// </summary>
        private async Task UpdateChatHistoryWithTicketId(int? idKhachHang, string? guestSessionId, int newTicketId)
        {
            // Tìm tất cả tin nhắn của phiên chat này
            // 1. Cùng IdKhachHang (nếu có) HOẶC cùng GuestSessionId (nếu có)
            // 2. Và CHƯA được gán vào phiếu nào (IdThongBaoHoTro == null)
            var messagesToUpdate = _context.ChatLichSus.Where(
                c => c.IdThongBaoHoTro == null &&
                ((idKhachHang > 0 && c.IdKhachHang == idKhachHang) ||
                  (!string.IsNullOrEmpty(guestSessionId) && c.GuestSessionId == guestSessionId))
            );

            // Dùng ToListAsync để thực thi truy vấn trước khi cập nhật
            var messagesList = await messagesToUpdate.ToListAsync();

            foreach (var msg in messagesList)
            {
                msg.IdThongBaoHoTro = newTicketId;
            }

            await _context.SaveChangesAsync();
        }
    }
}