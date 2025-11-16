// Tập tin: CafebookApi/Services/AiService.cs
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.Configuration;
using System.Linq;
using System;
using CafebookApi.Data;
using Microsoft.EntityFrameworkCore;
using System.Collections.Generic;
using Microsoft.Extensions.DependencyInjection; // <-- THÊM MỚI

namespace CafebookApi.Services
{
    public class AiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        // SỬA: Dùng IServiceProvider để tránh lỗi DBContext/Caching trong Constructor
        private readonly IServiceProvider _serviceProvider;

        // SỬA: Xóa DbContext và Config khỏi Constructor
        public AiService(IServiceProvider serviceProvider, IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            _serviceProvider = serviceProvider;
            // XÓA BỎ HOÀN TOÀN LOGIC GỌI DB TRONG CONSTRUCTOR
        }

        /// <summary>
        /// Hàm helper để tải Cài đặt AI một cách an toàn (chống cache)
        /// </summary>
        private async Task<(string ApiKey, string ApiEndpoint)> GetAiSettingsAsync()
        {
            // Tạo một scope riêng để lấy DbContext (đọc giá trị mới nhất)
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<CafebookDbContext>();
                var settings = await context.CaiDats.AsNoTracking().ToListAsync();

                string apiKey = settings.FirstOrDefault(c => c.TenCaiDat == "AI_Chat_API_Key")?.GiaTri ?? "";
                string apiEndpoint = settings.FirstOrDefault(c => c.TenCaiDat == "AI_Chat_Endpoint")?.GiaTri ?? "";

                return (apiKey, apiEndpoint);
            }
        }

        /// <summary>
        /// Gửi câu hỏi đến AI và nhận câu trả lời
        /// </summary>
        public async Task<string?> GetAnswerAsync(string userQuestion, int? idKhachHang)
        {
            // 1. Tải cài đặt một cách an toàn (sẽ lấy giá trị MỚI NHẤT từ DB)
            var (apiKey, apiEndpoint) = await GetAiSettingsAsync();

            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiEndpoint))
            {
                return "[NEEDS_SUPPORT]"; // AI không được cấu hình
            }

            // 2. Xây dựng System Prompt (Mồi)
            string systemPrompt = BuildSystemPrompt(idKhachHang);

            var client = _httpClientFactory.CreateClient();
            var requestUrl = $"{apiEndpoint}?key={apiKey}";

            // 3. Tạo payload cho Gemini
            var payload = new
            {
                contents = new[]
                {
                    new { role = "user", parts = new[] { new { text = systemPrompt } } },
                    new { role = "model", parts = new[] { new { text = "Đã hiểu. Tôi là trợ lý ảo Cafebook. Tôi đã sẵn sàng." } } },
                    new { role = "user", parts = new[] { new { text = userQuestion } } }
                }
            };

            try
            {
                // 4. Gửi Request
                var response = await client.PostAsJsonAsync(requestUrl, payload);

                if (!response.IsSuccessStatusCode)
                {
                    // Nếu Google trả về 404 (do URL sai) hoặc 500, nó sẽ vào đây
                    return null; // API lỗi
                }

                var jsonResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
                var candidates = jsonResponse.GetProperty("candidates");

                if (candidates.GetArrayLength() > 0)
                {
                    var content = candidates[0].GetProperty("content");
                    var parts = content.GetProperty("parts");
                    if (parts.GetArrayLength() > 0)
                    {
                        string aiText = parts[0].GetProperty("text").GetString() ?? "";

                        if (aiText.Contains("[NEEDS_SUPPORT]"))
                        {
                            return "[NEEDS_SUPPORT]";
                        }
                        return aiText;
                    }
                }

                return null; // Không có câu trả lời hợp lệ
            }
            catch (Exception)
            {
                return null; // Lỗi mạng
            }
        }

        /// <summary>
        /// Xây dựng mồi cho AI dựa trên việc khách đã đăng nhập hay chưa
        /// </summary>
        private string BuildSystemPrompt(int? idKhachHang)
        {
            string basePrompt = "Bạn là trợ lý ảo của Cafebook. Nhiệm vụ của bạn là trả lời ngắn gọn, lịch sự, tập trung vào các câu hỏi của khách hàng. ";
            string guestRules = "KHÁCH VÃNG LAI: Bạn CHỈ được phép trả lời các câu hỏi chung về: địa chỉ, giờ mở cửa, cách đặt bàn, kiểm tra giờ đặt bàn. Nếu khách hỏi thông tin cá nhân (đơn hàng, điểm tích lũy, lịch sử thuê sách), bạn BẮT BUỘC phải yêu cầu họ đăng nhập. ";
            string userRules = $"KHÁCH HÀNG (ID: {idKhachHang}): Bạn được phép trả lời TẤT CẢ câu hỏi, bao gồm cả câu hỏi chung VÀ câu hỏi cá nhân. ";
            string supportRule = "QUAN TRỌN: Nếu khách hàng báo cáo 'sự cố', 'bị lỗi', 'hỏng', 'than phiền' hoặc dùng từ ngữ yêu cầu 'gặp nhân viên', bạn KHÔNG được trả lời, mà CHỈ được phép phản hồi duy nhất bằng mã: [NEEDS_SUPPORT]";

            if (idKhachHang.HasValue && idKhachHang > 0)
            {
                // Đã đăng nhập
                return basePrompt + userRules + supportRule;
            }
            else
            {
                // Khách vãng lai
                return basePrompt + guestRules + supportRule;
            }
        }
    }
}