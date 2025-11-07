using System.Net.Http;
using System.Net.Http.Headers;
using System; // Thêm
using AppCafebookApi.Services; // Thêm

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
                        // SỬA DÒNG NÀY:
                        BaseAddress = new System.Uri("http://127.0.0.1:5166")
                    };
                    _instance.DefaultRequestHeaders.Accept.Clear();
                    _instance.DefaultRequestHeaders.Accept.Add(
                        new MediaTypeWithQualityHeaderValue("application/json"));
                }

                if (!string.IsNullOrEmpty(AuthService.AuthToken))
                {
                    _instance.DefaultRequestHeaders.Authorization =
                        new AuthenticationHeaderValue("Bearer", AuthService.AuthToken);
                    Console.WriteLine($"[ApiClient] Authorization header set: {AuthService.AuthToken.Substring(0, 20)}...");
                }
                else
                {
                    _instance.DefaultRequestHeaders.Authorization = null;
                    Console.WriteLine("[ApiClient] AuthToken trống, không thêm Authorization header.");
                }

                return _instance;
            }
        }
    }
}