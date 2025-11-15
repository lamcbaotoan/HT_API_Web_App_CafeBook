using AppCafebookApi.Services;
using CafebookModel.Model.ModelApp;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Globalization;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Threading; // THÊM DÒNG NÀY

namespace AppCafebookApi.View.quanly.pages
{
    public partial class QuanLyDanhGiaView : Page
    {
        private readonly HttpClient _httpClient;
        private int _currentPage = 1;
        private int _totalPages = 1;

        private DispatcherTimer _searchTimer; // Timer cho tìm kiếm

        public QuanLyDanhGiaView()
        {
            InitializeComponent();

            // SỬA LỖI: Dùng HttpClient từ ApiClient Singleton
            _httpClient = ApiClient.Instance; // <-- Dòng này ĐÚNG

            // Khởi tạo timer
            _searchTimer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(500) // Chờ 0.5 giây sau khi gõ
            };
            _searchTimer.Tick += SearchTimer_Tick;
        }

        // === LOGIC CHO TAB 1 (QUẢN LÝ ĐÁNH GIÁ) ===

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            cbFilterSao.SelectedIndex = 0;
            cbFilterTrangThai.SelectedIndex = 0;
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            try
            {
                int? filterSao = (cbFilterSao.SelectedItem as ComboBoxItem)?.Tag as int?;
                string? trangThai = (cbFilterTrangThai.SelectedItem as ComboBoxItem)?.Tag as string;

                var result = await GetReviewsAsync(_currentPage, filterSao, trangThai);

                if (result != null)
                {
                    lvDanhGia.ItemsSource = result.Items;
                    _totalPages = result.TotalPages;
                    _currentPage = result.CurrentPage;

                    tbCurrentPage.Text = $"{_currentPage} / {_totalPages}";
                    btnPrevPage.IsEnabled = _currentPage > 1;
                    btnNextPage.IsEnabled = _currentPage < _totalPages;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải danh sách đánh giá: {ex.Message}", "Lỗi API");
            }
        }

        private async void BtnFilter_Click(object sender, RoutedEventArgs e)
        {
            _currentPage = 1;
            await LoadDataAsync();
        }

        private async void BtnPrevPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage > 1)
            {
                _currentPage--;
                await LoadDataAsync();
            }
        }

        private async void BtnNextPage_Click(object sender, RoutedEventArgs e)
        {
            if (_currentPage < _totalPages)
            {
                _currentPage++;
                await LoadDataAsync();
            }
        }

        private async void BtnToggleStatus_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is int idDanhGia)
            {
                try
                {
                    bool success = await ToggleReviewStatusAsync(idDanhGia);
                    if (success)
                    {
                        await LoadDataAsync();
                    }
                    else
                    {
                        MessageBox.Show("Không thể cập nhật trạng thái.", "Lỗi");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi API");
                }
            }
        }

        private void BtnReply_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is DanhGiaQuanLyDto review)
            {
                popupReply.DataContext = review;
                tbReviewInfo.Text = $"Đang trả lời: \"{review.BinhLuan?.Substring(0, Math.Min(50, review.BinhLuan.Length))}...\"";
                txtReplyInput.Text = review.PhanHoi?.NoiDung ?? "";
                popupReply.IsOpen = true;
                txtReplyInput.Focus();
            }
        }

        private async void BtnSendReply_Click(object sender, RoutedEventArgs e)
        {
            if (popupReply.DataContext is DanhGiaQuanLyDto review)
            {
                string noiDung = txtReplyInput.Text;
                if (string.IsNullOrWhiteSpace(noiDung))
                {
                    MessageBox.Show("Vui lòng nhập nội dung phản hồi.", "Thiếu thông tin");
                    return;
                }

                try
                {
                    bool success = await ReplyToReviewAsync(review.IdDanhGia, noiDung);
                    if (success)
                    {
                        MessageBox.Show("Gửi phản hồi thành công!");
                        popupReply.IsOpen = false;
                        await LoadDataAsync();
                    }
                    else
                    {
                        MessageBox.Show("Không thể gửi phản hồi.", "Lỗi");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi API");
                }
            }
        }

        private void BtnCancelReply_Click(object sender, RoutedEventArgs e)
        {
            popupReply.IsOpen = false;
        }

        // === CÁC HÀM HELPER GỌI API (Tab 1) ===

        private async Task<PaginatedResult<DanhGiaQuanLyDto>?> GetReviewsAsync(int page = 1, int? filterSao = null, string? trangThai = null)
        {
            try
            {
                var queryParams = new List<string> { $"page={page}", "pageSize=10" };
                if (filterSao.HasValue) { queryParams.Add($"filterSao={filterSao.Value}"); }
                if (!string.IsNullOrEmpty(trangThai)) { queryParams.Add($"trangThai={Uri.EscapeDataString(trangThai)}"); }
                string url = $"api/app/quanlydanhgia?{string.Join("&", queryParams)}";

                var response = await _httpClient.GetAsync(url);

                // NẾU LÀ 401 (Chưa đăng nhập) THÌ SẼ BÁO LỖI
                if (!response.IsSuccessStatusCode)
                {
                    var errorContent = await response.Content.ReadAsStringAsync();
                    throw new Exception($"API Error: {response.StatusCode} - {errorContent}");
                }

                return await response.Content.ReadFromJsonAsync<PaginatedResult<DanhGiaQuanLyDto>>();
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi GetReviewsAsync: {ex.Message}");
                throw;
            }
        }

        private async Task<bool> ReplyToReviewAsync(int idDanhGia, string noiDung)
        {
            try
            {
                var input = new PhanHoiInputDto { NoiDung = noiDung };
                var response = await _httpClient.PostAsJsonAsync($"api/app/quanlydanhgia/{idDanhGia}/reply", input);
                return response.IsSuccessStatusCode; // Sẽ là true nếu API chạy thành công
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi ReplyToReviewAsync: {ex.Message}");
                return false;
            }
        }

        private async Task<bool> ToggleReviewStatusAsync(int idDanhGia)
        {
            try
            {
                var response = await _httpClient.PutAsync($"api/app/quanlydanhgia/{idDanhGia}/toggle-status", null);
                return response.IsSuccessStatusCode;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi ToggleReviewStatusAsync: {ex.Message}");
                return false;
            }
        }

        // === LOGIC MỚI CHO TAB 2 (TỔNG QUAN) ===

        private void TxtProductSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Reset timer mỗi khi gõ phím
            _searchTimer.Stop();
            _searchTimer.Start();
        }

        private async void SearchTimer_Tick(object? sender, EventArgs e)
        {
            _searchTimer.Stop(); // Dừng timer
            string query = txtProductSearch.Text;

            if (string.IsNullOrWhiteSpace(query))
            {
                lbSearchResults.ItemsSource = null;
                return;
            }

            try
            {
                var products = await SearchProductsAsync(query);
                lbSearchResults.ItemsSource = products;
            }
            catch (Exception ex)
            {
                Debug.WriteLine($"Lỗi tìm kiếm sản phẩm: {ex.Message}");
            }
        }

        // === BƯỚC 1: TÁI CẤU TRÚC LOGIC RA HÀM RIÊNG ===
        private async Task UpdateProductStats(int? productId)
        {
            // Nếu không có Id sản phẩm (ví dụ: đánh giá chung), thì reset
            if (!productId.HasValue)
            {
                tbAvgRating.Text = "0.0";
                tbTotalReviews.Text = "0";
                return;
            }

            try
            {
                // Gọi API lấy thống kê (hàm này đã có)
                var stats = await GetProductStatsAsync(productId.Value);
                if (stats != null)
                {
                    tbAvgRating.Text = stats.AverageRating.ToString("0.0");
                    tbTotalReviews.Text = stats.TotalReviews.ToString();
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi lấy thống kê: {ex.Message}");
                tbAvgRating.Text = "Lỗi";
                tbTotalReviews.Text = "Lỗi";
            }
        }

        // === BƯỚC 2: CẬP NHẬT HÀM CŨ ===
        private async void LbSearchResults_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lbSearchResults.SelectedItem is ProductSearchResultDto product)
            {
                // Gọi hàm helper mới
                await UpdateProductStats(product.IdSanPham);
            }
        }

        // === BƯỚC 3: THÊM HÀM MỚI CHO LISTVIEW ===
        private async void LvDanhGia_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // Lấy đánh giá được chọn từ ListView
            if (lvDanhGia.SelectedItem is DanhGiaQuanLyDto review)
            {
                // Gọi hàm helper mới với IdSanPham từ đánh giá
                await UpdateProductStats(review.IdSanPham);
            }
        }

        // === CÁC HÀM HELPER GỌI API (Tab 2) ===

        private async Task<List<ProductSearchResultDto>?> SearchProductsAsync(string query)
        {
            var response = await _httpClient.GetAsync($"api/app/quanlydanhgia/search-products?query={Uri.EscapeDataString(query)}");
            response.EnsureSuccessStatusCode(); // Sẽ báo lỗi nếu API thất bại
            return await response.Content.ReadFromJsonAsync<List<ProductSearchResultDto>>();
        }

        private async Task<ProductStatsDto?> GetProductStatsAsync(int productId)
        {
            var response = await _httpClient.GetAsync($"api/app/quanlydanhgia/product-stats?productId={productId}");
            response.EnsureSuccessStatusCode();
            return await response.Content.ReadFromJsonAsync<ProductStatsDto>();
        }
    }

    // --- CONVERTER (Giữ nguyên) ---
    public class StatusToButtonStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string trangThai = value as string ?? "";
            Style? style;
            if (trangThai == "Hiển thị")
            {
                style = Application.Current.TryFindResource("WarningButton") as Style;
            }
            else
            {
                style = Application.Current.TryFindResource("SecondaryButton") as Style;
            }

            if (style != null)
            {
                Style newStyle = new Style(typeof(Button), style);
                newStyle.Setters.Add(new Setter(ContentControl.ContentProperty, trangThai == "Hiển thị" ? "Ẩn" : "Hiện"));
                return newStyle;
            }
            return Application.Current.TryFindResource("BaseButton");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
    // BƯỚC 1: THAY THẾ 'StatusToButtonStyleConverter' BẰNG CONVERTER NÀY
    public class StatusToStyleConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string trangThai = value as string ?? "";
            Style? style;
            if (trangThai == "Hiển thị")
            {
                style = Application.Current.TryFindResource("WarningButton") as Style;
            }
            else
            {
                style = Application.Current.TryFindResource("SecondaryButton") as Style;
            }
            return style ?? Application.Current.TryFindResource("BaseButton");
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // BƯỚC 2: THÊM CONVERTER MỚI NÀY VÀO
    public class StatusToContentConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            string trangThai = value as string ?? "";
            return trangThai == "Hiển thị" ? "Ẩn" : "Hiện";
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }

    // --- KẾT THÚC SỬA LỖI ---
    // --- CONVERTER (Giữ nguyên) ---
    public class NullOrEmptyToVisibilityConverter : IValueConverter
    {
        public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
        {
            if (value == null)
                return Visibility.Collapsed;
            if (value is string str)
            {
                return string.IsNullOrWhiteSpace(str) ? Visibility.Collapsed : Visibility.Visible;
            }
            return Visibility.Visible;
        }

        public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
        {
            throw new NotImplementedException();
        }
    }
}