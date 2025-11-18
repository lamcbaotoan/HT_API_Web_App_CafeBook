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
using System.Text.Encodings.Web;
using System.Text.Json.Serialization;

namespace CafebookApi.Services
{
    public class AiService
    {
        private readonly IHttpClientFactory _httpClientFactory;
        private readonly IServiceProvider _serviceProvider;
        private readonly AiToolService _toolService;
        private static readonly JsonSerializerOptions _jsonOptions;

        static AiService()
        {
            _jsonOptions = new JsonSerializerOptions
            {
                Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping,
                PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
                DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
            };
        }

        public AiService(IServiceProvider serviceProvider, IHttpClientFactory httpClientFactory, AiToolService toolService)
        {
            _httpClientFactory = httpClientFactory;
            _serviceProvider = serviceProvider;
            _toolService = toolService;
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

        public async Task<string?> GetAnswerAsync(string userQuestion, int? idKhachHang, List<object> chatHistory)
        {
            var (apiKey, apiEndpoint) = await GetAiSettingsAsync();
            if (string.IsNullOrEmpty(apiKey) || string.IsNullOrEmpty(apiEndpoint))
            {
                return "[NEEDS_SUPPORT]";
            }

            var client = _httpClientFactory.CreateClient();
            var requestUrl = $"{apiEndpoint}?key={apiKey}";

            string systemPrompt = BuildSystemPrompt(idKhachHang);

            var payloadContents = new List<object>();
            payloadContents.Add(new { role = "user", parts = new[] { new { text = systemPrompt } } });
            payloadContents.Add(new { role = "model", parts = new[] { new { text = "Đã hiểu. Tôi là trợ lý ảo Cafebook. Tôi đã sẵn sàng." } } });

            if (chatHistory != null)
            {
                payloadContents.AddRange(chatHistory);
            }

            payloadContents.Add(new { role = "user", parts = new[] { new { text = userQuestion } } });

            var functionDeclarations = GetToolDefinitions(idKhachHang);

            var payload = new
            {
                contents = payloadContents,
                tools = new[] { new { functionDeclarations = functionDeclarations } },
                toolConfig = new { functionCallingConfig = new { mode = "AUTO" } }
            };

            var aiResponse = await CallGeminiApiAsync(client, requestUrl, payload);
            if (aiResponse == null) return null;

            var (responseText, functionCall) = ParseAiResponse(aiResponse.Value);

            if (!string.IsNullOrEmpty(responseText) && functionCall == null)
            {
                return responseText;
            }

            if (functionCall != null)
            {
                var (toolResult, toolName) = await ExecuteToolCallAsync(functionCall, idKhachHang);

                payloadContents.Add(new { role = "model", parts = new[] { new { functionCall = functionCall } } });

                payloadContents.Add(new
                {
                    role = "function",
                    parts = new[] { new { functionResponse = new { name = toolName, response = toolResult } } }
                });

                var finalPayload = new
                {
                    contents = payloadContents,
                    tools = new[] { new { functionDeclarations = functionDeclarations } }
                };

                var finalAiResponse = await CallGeminiApiAsync(client, requestUrl, finalPayload);
                if (finalAiResponse == null) return null;

                var (finalResponseText, _) = ParseAiResponse(finalAiResponse.Value);
                return finalResponseText;
            }

            return null;
        }

        private async Task<JsonElement?> CallGeminiApiAsync(HttpClient client, string url, object payload)
        {
            try
            {
                var jsonPayload = JsonSerializer.Serialize(payload, _jsonOptions);
                var httpContent = new StringContent(jsonPayload, System.Text.Encoding.UTF8, "application/json");

                var response = await client.PostAsync(url, httpContent);

                if (!response.IsSuccessStatusCode)
                {
                    // Tắt log sau khi debug
                    // var errorBody = await response.Content.ReadAsStringAsync();
                    // Console.WriteLine("============ API ERROR ============");
                    // Console.WriteLine(errorBody);
                    // Console.WriteLine("============ PAYLOAD ============");
                    // Console.WriteLine(jsonPayload);
                    return null;
                }

                var jsonResponse = await response.Content.ReadFromJsonAsync<JsonElement>();
                return jsonResponse;
            }
            catch (Exception)
            {
                return null;
            }
        }

        private (string? text, object? functionCall) ParseAiResponse(JsonElement aiResponse)
        {
            try
            {
                var candidates = aiResponse.GetProperty("candidates");
                if (candidates.GetArrayLength() == 0) return (null, null);

                var content = candidates[0].GetProperty("content");
                var parts = content.GetProperty("parts");
                if (parts.GetArrayLength() == 0) return (null, null);

                var firstPart = parts[0];

                if (firstPart.TryGetProperty("functionCall", out var funcCall))
                {
                    return (null, funcCall);
                }

                if (firstPart.TryGetProperty("text", out var text))
                {
                    return (text.GetString(), null);
                }
            }
            catch (Exception) { /* Lỗi parsing */ }
            return (null, null);
        }

        // Cập nhật: Thêm các case mới
        private async Task<(object? toolResult, string toolName)> ExecuteToolCallAsync(object functionCall, int? idKhachHang)
        {
            var callElement = (JsonElement)functionCall;
            string toolName = callElement.GetProperty("name").GetString() ?? "";
            var args = callElement.GetProperty("args");

            // Bảo vệ các tool chỉ dành cho khách đăng nhập
            if (toolName == "GET_TONG_QUAN_TAI_KHOAN" || toolName == "THEO_DOI_DON_HANG")
            {
                if (!idKhachHang.HasValue || idKhachHang == 0)
                {
                    return (new { TrangThai = "Error", Message = "Tool này yêu cầu khách hàng đăng nhập." }, toolName);
                }
            }

            try
            {
                switch (toolName)
                {
                    case "GET_THONG_TIN_CHUNG":
                        return (await _toolService.GetThongTinChungAsync(), toolName);

                    case "KIEM_TRA_BAN":
                        int soNguoi = args.TryGetProperty("soNguoi", out var soNguoiEl) ? soNguoiEl.GetInt32() : 1;
                        return (await _toolService.KiemTraBanTrongAsync(soNguoi), toolName);

                    case "KIEM_TRA_SAN_PHAM":
                        string tenSP = args.TryGetProperty("tenSanPham", out var tenSPEl) ? tenSPEl.GetString() ?? "" : "";
                        return (await _toolService.KiemTraSanPhamAsync(tenSP), toolName);

                    case "KIEM_TRA_SACH":
                        string tenSach = args.TryGetProperty("tenSach", out var tenSachEl) ? tenSachEl.GetString() ?? "" : "";
                        return (await _toolService.KiemTraSachAsync(tenSach), toolName);

                    // === NÂNG CẤP MỚI ===
                    case "GET_TONG_QUAN_TAI_KHOAN":
                        return (await _toolService.GetTongQuanTaiKhoanAsync(idKhachHang.Value), toolName);

                    case "THEO_DOI_DON_HANG":
                        int idHoaDon = args.TryGetProperty("idHoaDon", out var idHdEl) ? idHdEl.GetInt32() : 0;
                        return (await _toolService.TheoDoiDonHangAsync(idHoaDon, idKhachHang.Value), toolName);
                    // ===================

                    case "DAT_BAN_THUC_SU":
                        int idBan = args.TryGetProperty("idBan", out var idBanEl) ? idBanEl.GetInt32() : 0;
                        int soNguoiDat = args.TryGetProperty("soNguoi", out var soNguoiDatEl) ? soNguoiDatEl.GetInt32() : 0;
                        string thoiGianDatStr = args.TryGetProperty("thoiGianDat", out var tgEl) ? tgEl.GetString() ?? "" : "";
                        string hoTen = args.TryGetProperty("hoTen", out var htEl) ? htEl.GetString() ?? "" : "";
                        string sdt = args.TryGetProperty("soDienThoai", out var sdtEl) ? sdtEl.GetString() ?? "" : "";
                        string email = args.TryGetProperty("email", out var eEl) ? eEl.GetString() ?? "" : "";
                        string? ghiChu = args.TryGetProperty("ghiChu", out var gcEl) ? gcEl.GetString() : null;

                        if (idBan == 0 || soNguoiDat == 0 || string.IsNullOrEmpty(thoiGianDatStr) || string.IsNullOrEmpty(hoTen) || string.IsNullOrEmpty(sdt) || string.IsNullOrEmpty(email))
                        {
                            return (new { TrangThai = "Error", Message = "AI quên hỏi thông tin (bàn, giờ, tên, sđt, email)." }, toolName);
                        }

                        if (!DateTime.TryParse(thoiGianDatStr, System.Globalization.CultureInfo.InvariantCulture, System.Globalization.DateTimeStyles.AssumeUniversal, out DateTime thoiGianDat))
                        {
                            return (new { TrangThai = "Error", Message = "AI gửi sai định dạng thời gian." }, toolName);
                        }

                        return (await _toolService.DatBanThucSuAsync(idBan, soNguoiDat, thoiGianDat, hoTen, sdt, email, ghiChu), toolName);

                    case "GET_GOI_Y_SACH_NGAU_NHIEN":
                        return (await _toolService.GetGoiYSachNgauNhienAsync(), toolName);

                    default:
                        return (new { Error = $"Tool '{toolName}' không tồn tại." }, toolName);
                }
            }
            catch (Exception ex)
            {
                return (new { Error = $"Lỗi khi thực thi tool '{toolName}': {ex.Message}" }, toolName);
            }
        }

        // === SỬA LỖI: Cập nhật PROMPT ===
        private string BuildSystemPrompt(int? idKhachHang)
        {
            string personaPrompt = @"
# GIỌNG ĐIỆU CỦA BẠN (TRỢ LÝ ẢO CAFEBOOK)
Bạn là trợ lý ảo của Cafebook, không phải AI của Google.
- **Phong cách:** Thân thiện, chuyên nghiệp, tự nhiên như người thật, và tinh tế.
- **Giọng điệu:** Dùng các từ như ""Dạ vâng"", ""mình kiểm tra giúp bạn ngay nha"", ""Bạn yên tâm nhé"".
- **Mô hình trả lời:** Luôn theo cấu trúc: [Kết quả] -> [Gợi ý] -> [Hỏi hành động tiếp theo].
";

            string rules = $@"
# QUY TẮC PHẢN HỒI
(Hôm nay là {DateTime.Now:dd/MM/yyyy}. Giờ hiện tại là {DateTime.Now:HH:mm})
1.  **[NEEDS_SUPPORT]:** Nếu khách hàng báo cáo 'sự cố', 'bị lỗi', 'hỏng', 'than phiền' hoặc yêu cầu 'gặp nhân viên', bạn CHỈ được phép phản hồi duy nhất bằng mã: [NEEDS_SUPPORT]
2.  **[HỎI LẠI THÔNG MINH]:** Nếu tool yêu cầu tham số mà khách chưa cung cấp, bạn PHẢI hỏi lại khách.
3.  **[DIỄN GIẢI KẾT QUẢ]:** Khi nhận được kết quả từ Tool (dưới dạng JSON), bạn KHÔNG được hiển thị JSON. Bạn phải dùng giọng điệu của mình để diễn giải kết quả đó một cách tự nhiên.
    - Ví dụ Tool trả về: {{ TrangThai: ""Error"", Message: ""Giờ đặt quá gần. Vui lòng chọn thời gian sau 15:20."" }}
    - Bạn phải nói: ""Dạ, giờ bạn chọn (15:10) gần quá, bạn vui lòng chọn giờ sau 15:20 hôm nay giúp mình nhé."" (Diễn giải lại message lỗi)
    - Ví dụ Tool trả về: {{ TrangThai: ""Success"", IdPhieuDatBan: 123, ... }}
    - Bạn phải nói: ""Em đã đặt bàn thành công! Mã phiếu của bạn là 123. Cảm ơn bạn!""

# QUY TRÌNH NGHIỆP VỤ (RẤT QUAN TRỌNG)
- **Quy trình Đặt Bàn (Bắt buộc):**
    1. Khách hỏi đặt bàn (ví dụ: 'đặt bàn 4 người').
    2. Bạn BẮT BUỘC hỏi `soNguoi` (nếu khách chưa cung cấp).
    3. Bạn gọi tool `KIEM_TRA_BAN`. (Tool này sẽ trả về JSON, ví dụ: {{ banTimThay: [{{ ""idBan"": 8, ""soBan"": ""Bệt G01"", ""soGhe"": 4 }}, ...] }})
    4. Bạn trình bày kết quả cho khách, chỉ nói Tên Bàn (ví dụ: ""Bệt G01""). **Bạn phải GHI NHỚ `idBan` (ví dụ: 8) tương ứng với tên bàn đó.**
    5. Khách chọn 1 bàn (ví dụ: ""G01 nhé"").
    6. Bạn BẮT BUỘC hỏi khách 3 thông tin: `hoTen`, `soDienThoai`, `email`. (Nếu là khách đã đăng nhập, bạn gọi `GET_TONG_QUAN_TAI_KHOAN` để lấy SĐT/Email và chỉ hỏi tên).
    7. Bạn BẮT BUỘC hỏi khách `thoiGianDat` (Ngày và Giờ).
    8. **QUAN TRỌNG (SỬA LỖI):** Khi khách cung cấp giờ (ví dụ: '15:10 hôm nay'), bạn phải hiểu hôm nay là {DateTime.Now:yyyy-MM-dd} và chuyển nó thành định dạng ISO 8601 đầy đủ (ví dụ: '{DateTime.Now:yyyy-MM-dd}T15:10:00').
    9. **TUYỆT ĐỐI KHÔNG** được tự ý từ chối giờ (ví dụ: nói 'giờ quá gần'). Cứ gửi giờ cho tool `DAT_BAN_THUC_SU`. Tool C# sẽ tự kiểm tra logic 10 phút.
    10. Sau khi có ĐỦ 7 thông tin (`idBan` (là SỐ, ví dụ: 8), `soNguoi`, `thoiGianDat` (ISO 8601), `hoTen`, `soDienThoai`, `email`, `ghiChu`), bạn gọi tool `DAT_BAN_THUC_SU`.
- **Quy trình Gợi ý Sách (Đơn giản):**
    1. Khách hỏi 'gợi ý sách', 'sách hay', 'sách đọc giải trí'.
    2. Bạn gọi tool `GET_GOI_Y_SACH_NGAU_NHIEN`.
    3. Bạn trình bày 3 cuốn sách ngẫu nhiên đó.
- **Quy trình Lấy Thông Tin (Đã Đăng Nhập):**
    1. Khách hỏi 'điểm của tôi', 'tôi đã tiêu bao nhiêu', 'lịch sử thuê sách'.
    2. Bạn gọi tool `GET_TONG_QUAN_TAI_KHOAN`.
    3. Bạn diễn giải kết quả cho khách (ví dụ: 'Dạ, anh Toàn đang có 150 điểm. Đơn hàng gần nhất của anh là ...').
- **Quy trình Theo Dõi Đơn Hàng:**
    1. Khách hỏi 'đơn hàng 123 của tôi đâu rồi?'.
    2. Bạn BẮT BUỘC hỏi `idHoaDon` (nếu khách chưa cung cấp, bạn có thể tìm nó bằng `GET_TONG_QUAN_TAI_KHOAN`).
    3. Bạn gọi tool `THEO_DOI_DON_HANG` với `idHoaDon` đó.
- **Quy trình Thêm Giỏ Hàng (QUAN TRỌNG):**
    1. Nếu khách yêu cầu 'thêm vào giỏ hàng'.
    2. Bạn PHẢI trả lời: 'Dạ, em rất tiếc chưa thể thêm món vào giỏ hàng giúp anh/chị. Anh/chị vui lòng chọn món trên website để thêm vào giỏ ạ.'
";

            if (idKhachHang.HasValue && idKhachHang > 0)
            {
                return personaPrompt + $"# KHÁCH HÀNG HIỆN TẠI\nID Khách hàng: {idKhachHang}\n" + rules;
            }
            else
            {
                return personaPrompt + "# KHÁCH HÀNG HIỆN TẠI\nKhách vãng lai (Chưa đăng nhập)\n" + rules +
                    "\n# QUY TẮC KHÁCH VÃNG LAI\nNếu khách vãng lai hỏi thông tin cá nhân (tool GET_TONG_QUAN_TAI_KHOAN), bạn PHẢI trả lời: \"Dạ, để xem thông tin này bạn vui lòng đăng nhập tài khoản giúp mình nhé!\"";
            }
        }

        // Cập nhật: Sửa tool gợi ý và thêm tool mới
        private List<object> GetToolDefinitions(int? idKhachHang)
        {
            var tools = new List<object>();

            // --- Tool 1: GET_THONG_TIN_CHUNG ---
            tools.Add(new
            {
                name = "GET_THONG_TIN_CHUNG",
                description = "Lấy thông tin chung (giờ mở cửa, địa chỉ, wifi) của quán.",
                parameters = new { type = "object", properties = new { }, required = new string[] { } }
            });

            // --- Tool 2: KIEM_TRA_BAN (Chỉ kiểm tra) ---
            tools.Add(new
            {
                name = "KIEM_TRA_BAN",
                description = "Kiểm tra bàn trống theo số lượng người.",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        soNguoi = new { type = "integer", description = "Số lượng người cần đặt bàn" }
                    },
                    required = new[] { "soNguoi" }
                }
            });

            // --- Tool 3: KIEM_TRA_SAN_PHAM ---
            tools.Add(new
            {
                name = "KIEM_TRA_SAN_PHAM",
                description = "Kiểm tra tình trạng (còn/hết/giá) của một món ăn hoặc thức uống.",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        tenSanPham = new { type = "string", description = "Tên món cần kiểm tra, có thể không chính xác tuyệt đối" }
                    },
                    required = new[] { "tenSanPham" }
                }
            });

            // --- Tool 4: KIEM_TRA_SACH ---
            tools.Add(new
            {
                name = "KIEM_TRA_SACH",
                description = "Kiểm tra tình trạng (còn/hết/vị trí/idSach) của một đầu sách trong thư viện.",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        tenSach = new { type = "string", description = "Tên sách cần tìm" }
                    },
                    required = new[] { "tenSach" }
                }
            });

            // --- Tool 6: DAT_BAN_THUC_SU (Tool ghi) ---
            tools.Add(new
            {
                name = "DAT_BAN_THUC_SU",
                description = "Chỉ gọi tool này sau khi đã thu thập ĐỦ 7 thông tin: idBan, soNguoi, thoiGianDat, hoTen, soDienThoai, email, ghiChu.",
                parameters = new
                {
                    type = "object",
                    properties = new
                    {
                        idBan = new { type = "integer", description = "ID (con số) của bàn khách đã chọn, KHÔNG phải tên bàn" },
                        soNguoi = new { type = "integer", description = "Số lượng khách" },
                        thoiGianDat = new { type = "string", description = "Thời gian đặt (định dạng ISO 8601: YYYY-MM-DDTHH:MM:SS)" },
                        hoTen = new { type = "string", description = "Họ tên người đặt" },
                        soDienThoai = new { type = "string", description = "SĐT người đặt" },
                        email = new { type = "string", description = "Email người đặt" },
                        ghiChu = new { type = "string", description = "Ghi chú của khách (nếu có)" }
                    },
                    required = new[] { "idBan", "soNguoi", "thoiGianDat", "hoTen", "soDienThoai", "email" }
                }
            });

            // --- Tool 7: GET_GOI_Y_SACH_NGAU_NHIEN (Sửa lỗi theo yêu cầu) ---
            tools.Add(new
            {
                name = "GET_GOI_Y_SACH_NGAU_NHIEN",
                description = "Gợi ý 3 sách ngẫu nhiên từ bảng đề xuất (không cần tham số).",
                parameters = new { type = "object", properties = new { }, required = new string[] { } }
            });


            // --- CÁC TOOL CHỈ DÀNH CHO KHÁCH ĐĂNG NHẬP ---
            if (idKhachHang.HasValue && idKhachHang > 0)
            {
                // (Tool 5)
                tools.Add(new
                {
                    name = "GET_TONG_QUAN_TAI_KHOAN",
                    description = "Lấy tổng quan tài khoản cho khách hàng đã đăng nhập (điểm, tổng chi tiêu, lịch sử đơn hàng/thuê/đặt bàn gần nhất, SĐT, Email).",
                    parameters = new { type = "object", properties = new { }, required = new string[] { } }
                });

                // (Tool 8)
                tools.Add(new
                {
                    name = "THEO_DOI_DON_HANG",
                    description = "Theo dõi trạng thái chi tiết của một đơn hàng giao đi (chỉ dành cho khách đã đăng nhập).",
                    parameters = new
                    {
                        type = "object",
                        properties = new
                        {
                            idHoaDon = new { type = "integer", description = "ID của hóa đơn cần theo dõi" }
                        },
                        required = new[] { "idHoaDon" }
                    }
                });
            }

            return tools;
        }
    }
}