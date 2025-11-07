using System.Net.Http;
using System.Net.Http.Headers;

namespace AppCafebookApi.Services
{
    public static class ApiClient
    {
        private static HttpClient? _instance;

        public static HttpClient Instance
        {
            get
            {
                if (_instance == null)
                {
                    _instance = new HttpClient
                    {
                        BaseAddress = new System.Uri("http://localhost:5166")
                    };
                    _instance.DefaultRequestHeaders.Accept.Clear();
                    _instance.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("application/json"));
                }

                // Luôn cập nhật token mới nhất từ AuthService
                // (Giả định AuthService.AuthToken chứa token sau khi đăng nhập)
                if (!string.IsNullOrEmpty(AuthService.AuthToken))
                {
                    _instance.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", AuthService.AuthToken);
                }

                return _instance;
            }
        }
    }
}