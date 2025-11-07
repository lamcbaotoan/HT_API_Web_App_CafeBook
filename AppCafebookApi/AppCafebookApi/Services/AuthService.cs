using CafebookModel.Model.Data;
using CafebookModel.Model.ModelApi;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;

namespace AppCafebookApi.Services
{
    public static class AuthService
    {
        // 1. Lưu trữ thông tin người dùng hiện tại
        public static NhanVienDto? CurrentUser { get; private set; }
        public static string? AuthToken { get; private set; }

        // 2. Cấu hình HttpClient
        private static readonly HttpClient _httpClient;

        static AuthService()
        {
            _httpClient = new HttpClient
            {
                // Lấy từ file launchSettings.json của API
                BaseAddress = new System.Uri("http://localhost:5166")
            };
            _httpClient.DefaultRequestHeaders.Accept.Clear();
            _httpClient.DefaultRequestHeaders.Accept.Add(
                new System.Net.Http.Headers.MediaTypeWithQualityHeaderValue("application/json"));
        }

        // 3. Đăng nhập bằng API
        public static async Task<LoginResponseModel> LoginAsync(LoginRequestModel model)
        {
            try
            {
                var response = await _httpClient.PostAsJsonAsync("/api/app/taikhoan/login", model);

                if (response.IsSuccessStatusCode)
                {
                    var loginResponse = await response.Content.ReadFromJsonAsync<LoginResponseModel>();
                    if (loginResponse != null && loginResponse.Success)
                    {
                        CurrentUser = loginResponse.UserData; // Lưu người dùng
                        AuthToken = loginResponse.Token ?? string.Empty; // Nếu API trả về token thì lưu lại để dùng cho các request sau
                    }
                    return loginResponse ?? new LoginResponseModel { Success = false, Message = "Không thể đọc phản hồi." };
                }

                // Thử đọc nội dung lỗi nếu có
                var errorResponse = await response.Content.ReadFromJsonAsync<LoginResponseModel>();
                return errorResponse ?? new LoginResponseModel { Success = false, Message = $"Lỗi: {response.StatusCode}" };
            }
            catch (HttpRequestException ex)
            {
                return new LoginResponseModel { Success = false, Message = $"Lỗi kết nối API: {ex.Message}" };
            }
        }

        // 4. Kiểm tra tài khoản Backdoor
        public static LoginResponseModel LoginBackdoor(LoginRequestModel model)
        {
            if (model.TenDangNhap == "admin@cafebook.com" && model.MatKhau == "123456")
            {
                var user = new NhanVienDto
                {
                    HoTen = "Tài Khoản QL Siêu Quản Trị",
                    TenVaiTro = "Quản trị viên",
                    DanhSachQuyen = new List<string> { "FULL" }
                };
                CurrentUser = user; // Lưu người dùng
                return new LoginResponseModel { Success = true, UserData = user };
            }

            if (model.TenDangNhap == "nhanvien@cafebook.com" && model.MatKhau == "123456")
            {
                var user = new NhanVienDto
                {
                    HoTen = "Tài Khoản NV Siêu Quản Trị",
                    TenVaiTro = "Quản Trị Vien",
                    DanhSachQuyen = new List<string> { "FULL" }
                };
                CurrentUser = user; // Lưu người dùng
                return new LoginResponseModel { Success = true, UserData = user };
            }

            // Nếu không phải backdoor
            return new LoginResponseModel { Success = false };
        }

        // 1. HÀM ĐĂNG XUẤT (XÓA DỮ LIỆU SESSION)
        public static void Logout()
        {
            CurrentUser = null;
            AuthToken = null;//mới
        }


        // 2. HÀM KIỂM TRA MỘT QUYỀN
        public static bool CoQuyen(string idQuyen)
        {
            if (CurrentUser == null || CurrentUser.DanhSachQuyen == null)
            {
                return false;
            }

            // Quản trị viên (từ CSDL) hoặc backdoor "FULL" luôn có quyền
            if (CurrentUser.TenVaiTro == "Quản trị viên" || CurrentUser.DanhSachQuyen.Contains("FULL"))
            {
                return true;
            }

            // Kiểm tra quyền cụ thể
            return CurrentUser.DanhSachQuyen.Contains(idQuyen);
        }

        // 3. HÀM KIỂM TRA NHIỀU QUYỀN (Logic "OR")
        // (Rất hữu ích để kiểm tra 1 trong nhiều quyền, ví dụ: QL Kho)
        public static bool CoQuyen(params string[] quyenIds)
        {
            if (CurrentUser == null || CurrentUser.DanhSachQuyen == null)
            {
                return false;
            }

            if (CurrentUser.TenVaiTro == "Quản trị viên" || CurrentUser.DanhSachQuyen.Contains("FULL"))
            {
                return true;
            }

            // Kiểm tra xem người dùng có BẤT KỲ quyền nào trong danh sách không
            return quyenIds.Any(id => CurrentUser.DanhSachQuyen.Contains(id));
        }
    }
}