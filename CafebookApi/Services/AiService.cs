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
using Microsoft.Extensions.DependencyInjection;

namespace CafebookApi.Services
{
    public class AiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IServiceProvider _serviceProvider;

        public AiService(IServiceProvider serviceProvider, IHttpClientFactory httpClientFactory)
        {
            _httpClientFactory = httpClientFactory;
            _serviceProvider = serviceProvider;
        }

        private async Task<(string ApiKey, string ApiEndpoint)> GetAiSettingsAsync()
        {
            using (var scope = _serviceProvider.CreateScope())
            {
                var context = scope.ServiceProvider.GetRequiredService<CafebookDbContext>();
                var settings = await context.CaiDats.AsNoTracking().ToListAsync();

                string apiKey = settings.FirstOrDefault(c => c.TenCaiDat == "AI_Chat_API_Key")?.GiaTri ?? "";
                string apiEndpoint = settings.FirstOrDefault(c => c.TenCaiDat == "AI_Chat_Endpoint")?.GiaTri ?? "";

                return (apiKey, apiEndpoint);
            }
        }

        // ==================================================================
        // === NÂNG CẤP TRIỆT ĐỂ HÀM GetAnswerAsync VÀ BuildSystemPrompt ===
        // ==================================================================

        /// <summary>
        /// Gửi câu hỏi đến AI và nhận câu trả lời (Hàm điều phối nâng cao)
        /// </summary>
        /// <param name="userQuestion">Câu hỏi mới của người dùng</param>
        /// <param name="idKhachHang">ID khách hàng (nếu có)</param>
        /// <param name="chatHistory">Lịch sử chat (để AI hiểu ngữ cảnh)</param>
        /// <param name="toolResult">Kết quả từ việc gọi Tool (nếu có)</param>
        /// <returns>Câu trả lời của AI (có thể là văn bản hoặc mã lệnh)</returns>
        public async Task<string?> GetAnswerAsync(string userQuestion, int? idKhachHang, List<dynamic> chatHistory, string? toolResult = null)
        {
            var (apiKey, apiEndpoint) = await GetAiSettingsAsync();
            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiEndpoint))
            {
                return "[NEEDS_SUPPORT]"; // AI không được cấu hình
            }

            // 1. Xây dựng System Prompt (Mồi) dựa trên yêu cầu mới
            string systemPrompt = BuildSystemPrompt(idKhachHang);

            var client = _httpClientFactory.CreateClient();
            var requestUrl = $"{apiEndpoint}?key={apiKey}";

            // 2. Xây dựng Payload cho Gemini (bao gồm Lịch sử chat)
            var payloadContents = new List<dynamic>();

            // 2.1. Thêm System Prompt
            payloadContents.Add(new { role = "user", parts = new[] { new { text = systemPrompt } } });
            payloadContents.Add(new { role = "model", parts = new[] { new { text = "Đã hiểu. Tôi là trợ lý ảo Cafebook. Tôi đã sẵn sàng." } } });

            // 2.2. Thêm lịch sử chat (nếu có)
            if (chatHistory != null)
            {
                payloadContents.AddRange(chatHistory);
            }

            // 2.3. Nếu có kết quả từ Tool, thêm vào
            if (!string.IsNullOrEmpty(toolResult))
            {
                // Thêm câu hỏi gốc đã kích hoạt tool (giả định nó là câu cuối trong history)
                // Thêm phản hồi của AI (là TOOL_CALL - đã được lưu ở Controller)
                // Thêm kết quả Tool
                payloadContents.Add(new { role = "user", parts = new[] { new { text = $"[TOOL_RESULT]\n{toolResult}" } } });
            }
            else
            {
                // 2.4. Nếu không, thêm câu hỏi mới của khách
                payloadContents.Add(new { role = "user", parts = new[] { new { text = userQuestion } } });
            }

            var payload = new { contents = payloadContents };

            try
            {
                var response = await client.PostAsJsonAsync(requestUrl, payload);
                if (!response.IsSuccessStatusCode)
                {
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
                        return aiText; // Đây có thể là văn bản hoặc [TOOL_CALL]
                    }
                }
                return null;
            }
            catch (Exception)
            {
                return null;
            }
        }

        /// <summary>
        /// Xây dựng mồi (System Prompt) thông minh theo yêu cầu mới
        /// </summary>
        private string BuildSystemPrompt(int? idKhachHang)
        {
            // === PHẦN B: PHONG CÁCH GIỌNG ĐIỆU ===
            string personaPrompt = @"
# GIỌNG ĐIỆU CỦA BẠN (TRỢ LÝ ẢO CAFEBOOK)
Bạn là trợ lý ảo của Cafebook, không phải AI của Google.
- **Phong cách:** Thân thiện, chuyên nghiệp, tự nhiên như người thật, và tinh tế.
- **Giọng điệu:** Dùng các từ như ""Dạ vâng"", ""mình kiểm tra giúp bạn ngay nha"", ""Bạn yên tâm nhé"".
- **Mô hình trả lời:** Luôn theo cấu trúc: [Kết quả] -> [Gợi ý] -> [Hỏi hành động tiếp theo].
- **Ví dụ:** ""Dạ món Trà Đào Cam Sả hiện vẫn còn đầy đủ ạ. Món này vị thanh mát hợp với mùa hè lắm. Bạn muốn mình đặt trước 1 ly cho bạn không?""
- **Xử lý lỗi (Fallback):** KHÔNG dùng ""Xin lỗi"", ""Rất tiếc"". Thay bằng: ""Thông tin món này hiện mình chưa cập nhật theo thời gian thực, nhưng mình có thể kiểm tra ngay cho bạn nếu bạn muốn.""
- **Ngữ cảnh:** Luôn đọc kỹ lịch sử chat để trả lời liền mạch. Nếu khách nói ""4 người, 7h"" thì phải hiểu là họ đang trả lời câu hỏi đặt bàn trước đó.
";

            // === PHẦN A: CHỨC NĂNG (TOOLS) ===
            string baseTools = @"
# CÔNG CỤ CỦA BẠN
Bạn có các công cụ để truy vấn DB. Khi cần thông tin, bạn KHÔNG được tự bịa ra. Bạn PHẢI yêu cầu gọi công cụ bằng cách trả lời ĐÚNG theo định dạng: [TOOL_CALL: TênTool, ThamSo1: GiaTri1, ...]

## CÔNG CỤ CHUNG (LUỒNG 1: KHÁCH VÃNG LAI)
Bạn được phép dùng các công cụ này cho MỌI khách hàng.

1.  **[TOOL_CALL: GET_THONG_TIN_CHUNG]**
    - **Mô tả:** Lấy thông tin chung (giờ mở cửa, địa chỉ, wifi).
    - **Khi nào dùng:** Khi khách hỏi ""Quán mở cửa lúc mấy giờ?"", ""Địa chỉ ở đâu?"", ""Wifi là gì?"".
    - **Tham số:** Không có.

2.  **[TOOL_CALL: KIEM_TRA_BAN, SoNguoi: <số>]**
    - **Mô tả:** Kiểm tra bàn trống theo số lượng người.
    - **Khi nào dùng:** Khi khách hỏi ""Còn bàn 4 người không?"", ""Đặt bàn 10 người"".
    - **Tham số:** SoNguoi (Bắt buộc).
    - **Hỏi lại (Smart Question):** Nếu khách chỉ hỏi ""Còn bàn không?"", bạn phải hỏi lại: ""Bạn cho mình xin số lượng người và giờ bạn muốn đến để mình kiểm tra bàn trống nhé?""

3.  **[TOOL_CALL: KIEM_TRA_SAN_PHAM, TenSanPham: '<tên>']**
    - **Mô tả:** Kiểm tra tình trạng (còn/hết) của 1 món.
    - **Khi nào dùng:** ""Món trà đào còn không?"", ""cà phê sữa hết rồi hả?"".
    - **Tham số:** TenSanPham (Bắt buộc).

4.  **[TOOL_CALL: KIEM_TRA_SACH, TenSach: '<tên>']**
    - **Mô tả:** Kiểm tra sách (còn/hết) trong thư viện.
    - **Khi nào dùng:** ""Cuốn Đắc Nhân Tâm còn không?"", ""Tìm sách 1984"".
    - **Tham số:** TenSach (Bắt buộc).
";

            // === CÔNG CỤ CHO KHÁCH ĐÃ ĐĂNG NHẬP (LUỒNG 2) ===
            string loggedInTools = @"
## CÔNG CỤ NÂNG CAO (LUỒNG 2: KHÁCH HÀNG ĐÃ ĐĂNG NHẬP)
Bạn CHỈ được dùng công cụ này khi `idKhachHang` tồn tại (lớn hơn 0).

5.  **[TOOL_CALL: GET_THONG_TIN_KHACH_HANG]**
    - **Mô tả:** Lấy thông tin cá nhân (điểm tích lũy, hóa đơn gần nhất, sách đang mượn).
    - **Khi nào dùng:** ""Tôi còn bao nhiêu điểm?"", ""Xem hóa đơn gần nhất"", ""Kiểm tra sách đang mượn"".
    - **Tham số:** Không có (API sẽ tự lấy IdKhachHang).
";

            // === QUY TẮC PHẢN HỒI ===
            string rules = @"
# QUY TẮC PHẢN HỒI
1.  **[NEEDS_SUPPORT]:** Nếu khách hàng báo cáo 'sự cố', 'bị lỗi', 'hỏng', 'than phiền' hoặc yêu cầu 'gặp nhân viên', bạn CHỈ được phép phản hồi duy nhất bằng mã: [NEEDS_SUPPORT]
2.  **[TOOL_CALL]:** Nếu cần dữ liệu, trả lời bằng [TOOL_CALL:...].
3.  **[VĂN BẢN]:** Nếu không cần công cụ, trả lời bình thường theo giọng điệu đã định.
4.  **[XỬ LÝ KẾT QUẢ TOOL]:** Khi nhận được [TOOL_RESULT], bạn KHÔNG được hiển thị mã đó cho khách. Bạn phải dùng giọng điệu của mình để diễn giải kết quả đó một cách tự nhiên.
";

            if (idKhachHang.HasValue && idKhachHang > 0)
            {
                // Đã đăng nhập
                return personaPrompt + $"# KHÁCH HÀNG HIỆN TẠI\nID Khách hàng: {idKhachHang}\n" + baseTools + loggedInTools + rules;
            }
            // Tập tin: AiService.cs

            else
            {
                // Khách vãng lai
                return personaPrompt + "# KHÁCH HÀNG HIỆN TẠI\nKhách vãng lai (Chưa đăng nhập)\n" + baseTools + rules +
                    "\n# QUY TẮC KHÁCH VÃNG LAI\nNếu khách vãng lai hỏi thông tin cá nhân (điểm, hóa đơn, sách đang mượn), bạn PHẢI trả lời: \"Dạ, để xem thông tin này bạn vui lòng đăng nhập tài khoản giúp mình nhé!\"";
            }
        }
    }
}