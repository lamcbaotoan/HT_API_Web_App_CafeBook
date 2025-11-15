// Tập tin: AppCafebookApi/View/nhanvien/pages/GoiMonView.xaml.cs (ĐÃ SỬA)
using AppCafebookApi.Services;
using AppCafebookApi.View.common;
using AppCafebookApi.View.Common;
using CafebookModel.Model.ModelApp.NhanVien;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;

namespace AppCafebookApi.View.nhanvien.pages
{
    public partial class GoiMonView : Page
    {
        // Lớp helper hứng kết quả AddItem
        private class AddItemResponseDto
        {
            [JsonPropertyName("updatedHoaDonInfo")]
            public HoaDonInfoDto? updatedHoaDonInfo { get; set; }
            [JsonPropertyName("newItem")]
            public ChiTietDto? newItem { get; set; }
        }

        private readonly int _idHoaDon;
        private static readonly HttpClient _httpClient;

        // Biến cục bộ để lưu trạng thái
        private List<SanPhamDto> _allSanPhams = new List<SanPhamDto>();
        private ObservableCollection<ChiTietDto> _chiTietItems = new ObservableCollection<ChiTietDto>();
        private List<KhuyenMaiDto> _availableKms = new List<KhuyenMaiDto>();
        private int? _currentKhuyenMaiId = null;
        private bool _isDataLoading = true;

        // NÂNG CẤP: Lưu trữ danh mục đang chọn
        private DanhMucDto _currentDanhMuc = new DanhMucDto { IdDanhMuc = 0, TenLoaiSP = "Tất cả" };

        static GoiMonView()
        {
            _httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5166") };
        }

        public GoiMonView(int idHoaDon)
        {
            InitializeComponent();
            _idHoaDon = idHoaDon;
            dgChiTietHoaDon.ItemsSource = _chiTietItems;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
        }

        private async Task LoadDataAsync()
        {
            _isDataLoading = true;
            try
            {
                var response = await _httpClient.GetFromJsonAsync<GoiMonViewDto>($"api/app/nhanvien/goimon/load/{_idHoaDon}");
                if (response == null)
                {
                    MessageBox.Show("Không thể tải dữ liệu hóa đơn.", "Lỗi API");
                    return;
                }

                _allSanPhams = response.SanPhams ?? new List<SanPhamDto>();

                var danhMucs = response.DanhMucs ?? new List<DanhMucDto>();
                danhMucs.Insert(0, new DanhMucDto { IdDanhMuc = 0, TenLoaiSP = "Tất cả" });
                lbLoaiSP.ItemsSource = danhMucs;

                _chiTietItems.Clear();
                response.ChiTietItems?.ForEach(item => _chiTietItems.Add(item));

                _availableKms = response.KhuyenMais ?? new List<KhuyenMaiDto>();

                UpdateBillUI(response.HoaDonInfo);

                // Kích hoạt bộ lọc "Tất cả"
                if (lbLoaiSP.Items.Count > 0)
                {
                    lbLoaiSP.UpdateLayout();
                    var container = lbLoaiSP.ItemContainerGenerator.ContainerFromIndex(0) as FrameworkElement;
                    var rb = FindVisualChild<RadioButton>(container);
                    if (rb != null)
                    {
                        rb.IsChecked = true;
                    }
                    UpdateProductFilter(); // Gọi hàm lọc chung
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải dữ liệu: {ex.Message}", "Lỗi nghiêm trọng");
            }
            _isDataLoading = false;
        }

        private T? FindVisualChild<T>(DependencyObject? obj) where T : DependencyObject
        {
            // ... (Hàm này giữ nguyên) ...
            if (obj == null) return null;
            for (int i = 0; i < VisualTreeHelper.GetChildrenCount(obj); i++)
            {
                DependencyObject child = VisualTreeHelper.GetChild(obj, i);
                if (child != null && child is T)
                    return (T)child;
                else
                {
                    T? childOfChild = FindVisualChild<T>(child);
                    if (childOfChild != null)
                        return childOfChild;
                }
            }
            return null;
        }

        private void UpdateBillUI(HoaDonInfoDto info)
        {
            // ... (Hàm này giữ nguyên) ...
            if (info == null) return;
            lblTieuDeHoaDon.Text = $"Hóa đơn - {info.SoBan}";
            lblTongTien.Text = info.TongTienGoc.ToString("N0");
            lblTienGiam.Text = info.GiamGia.ToString("N0");
            lblThanhTien.Text = info.ThanhTien.ToString("N0") + " VND";
            _currentKhuyenMaiId = info.IdKhuyenMai;
            if (_currentKhuyenMaiId.HasValue && _currentKhuyenMaiId != 0)
            {
                var km = _availableKms.FirstOrDefault(k => k.IdKhuyenMai == _currentKhuyenMaiId);
                btnChonKhuyenMai.Content = km?.TenKhuyenMai ?? "Đã chọn KM";
                btnHuyKhuyenMai.Visibility = Visibility.Visible;
            }
            else
            {
                btnChonKhuyenMai.Content = "-- Chọn Khuyến mãi --";
                btnHuyKhuyenMai.Visibility = Visibility.Collapsed;
            }
        }

        // --- NÂNG CẤP: Logic Lọc ---

        private void Category_Checked(object sender, RoutedEventArgs e)
        {
            var radioButton = sender as RadioButton;
            var danhMuc = radioButton?.DataContext as DanhMucDto;

            if (danhMuc != null)
            {
                _currentDanhMuc = danhMuc;
            }
            else if (radioButton != null && radioButton.Content.ToString() == "Tất cả")
            {
                _currentDanhMuc = (lbLoaiSP.Items[0] as DanhMucDto) ?? new DanhMucDto { IdDanhMuc = 0 };
            }

            UpdateProductFilter();
        }

        private void TxtTimKiem_TextChanged(object sender, TextChangedEventArgs e)
        {
            // Gọi hàm lọc chung mỗi khi gõ
            UpdateProductFilter();
        }

        /// <summary>
        /// Hàm lọc sản phẩm dựa trên Category và Ô tìm kiếm
        /// </summary>
        private void UpdateProductFilter()
        {
            if (_allSanPhams == null) return;

            IEnumerable<SanPhamDto> filteredList = _allSanPhams;

            // 1. Lọc theo Category
            if (_currentDanhMuc != null && _currentDanhMuc.IdDanhMuc != 0)
            {
                filteredList = filteredList.Where(s => s.IdDanhMuc == _currentDanhMuc.IdDanhMuc);
            }

            // 2. Lọc theo Từ khóa tìm kiếm
            string keyword = txtTimKiem.Text.Trim().ToLowerInvariant();
            if (!string.IsNullOrEmpty(keyword))
            {
                filteredList = filteredList.Where(s =>
                    s.TenSanPham.ToLowerInvariant().Contains(keyword)
                );
            }

            icSanPham.ItemsSource = filteredList.ToList();
        }

        // --- KẾT THÚC NÂNG CẤP LỌC ---


        private async void ProductButton_Click(object sender, RoutedEventArgs e)
        {
            // ... (Hàm này giữ nguyên như lần trước, đã có InputBoxWindow) ...
            var sanPham = (sender as Button)?.DataContext as SanPhamDto;
            if (sanPham == null) return;

            var inputBox = new InputBoxWindow(
                "Thêm ghi chú",
                $"Nhập ghi chú cho món [{sanPham.TenSanPham}]:",
                ""
            );

            string? ghiChu = null;
            if (inputBox.ShowDialog() == true)
            {
                ghiChu = inputBox.InputText;
            }

            if (string.IsNullOrWhiteSpace(ghiChu))
            {
                ghiChu = null;
            }

            var request = new AddItemRequest
            {
                IdHoaDon = _idHoaDon,
                IdSanPham = sanPham.IdSanPham,
                SoLuong = 1,
                GhiChu = ghiChu
            };

            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/app/nhanvien/goimon/add-item", request);
                if (response.IsSuccessStatusCode)
                {
                    var result = await response.Content.ReadFromJsonAsync<AddItemResponseDto>();
                    if (result?.updatedHoaDonInfo == null || result.newItem == null) return;

                    UpdateBillUI(result.updatedHoaDonInfo);

                    ChiTietDto newItem = result.newItem;

                    var existingItem = _chiTietItems.FirstOrDefault(c => c.IdChiTietHoaDon == newItem.IdChiTietHoaDon);
                    if (existingItem != null)
                    {
                        existingItem.SoLuong = newItem.SoLuong;
                        existingItem.ThanhTien = newItem.ThanhTien;
                    }
                    else
                    {
                        _chiTietItems.Add(newItem);
                    }
                    dgChiTietHoaDon.Items.Refresh();
                }
                else
                {
                    MessageBox.Show(await response.Content.ReadAsStringAsync(), "Lỗi thêm món");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi API");
            }
        }

        private async void BtnGiamSL_Click(object sender, RoutedEventArgs e)
        {
            // ... (Hàm này giữ nguyên) ...
            var item = (sender as Button)?.DataContext as ChiTietDto;
            if (item == null) return;
            await UpdateQuantityAsync(item, item.SoLuong - 1);
        }

        private async void BtnTangSL_Click(object sender, RoutedEventArgs e)
        {
            // ... (Hàm này giữ nguyên) ...
            var item = (sender as Button)?.DataContext as ChiTietDto;
            if (item == null) return;
            await UpdateQuantityAsync(item, item.SoLuong + 1);
        }

        private async void BtnXoaMon_Click(object sender, RoutedEventArgs e)
        {
            // ... (Hàm này giữ nguyên) ...
            var item = (sender as Button)?.DataContext as ChiTietDto;
            if (item == null) return;
            var result = MessageBox.Show($"Bạn có chắc muốn xóa [{item.TenSanPham}]?", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.Yes)
            {
                await UpdateQuantityAsync(item, 0);
            }
        }

        private async Task UpdateQuantityAsync(ChiTietDto item, int soLuongMoi)
        {
            // ... (Hàm này giữ nguyên) ...
            var request = new UpdateSoLuongRequest
            {
                IdChiTietHoaDon = item.IdChiTietHoaDon,
                SoLuongMoi = soLuongMoi
            };
            try
            {
                var response = await _httpClient.PutAsJsonAsync("api/app/nhanvien/goimon/update-quantity", request);
                if (response.IsSuccessStatusCode)
                {
                    var hoaDonInfo = await response.Content.ReadFromJsonAsync<HoaDonInfoDto>();
                    if (hoaDonInfo != null) UpdateBillUI(hoaDonInfo);
                    if (soLuongMoi <= 0)
                    {
                        _chiTietItems.Remove(item);
                    }
                    else
                    {
                        item.SoLuong = soLuongMoi;
                        item.ThanhTien = item.SoLuong * item.DonGia;
                    }
                    dgChiTietHoaDon.Items.Refresh();
                }
                else
                {
                    MessageBox.Show(await response.Content.ReadAsStringAsync(), "Lỗi cập nhật");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi API");
            }
        }

        // ... (Tất cả các hàm còn lại: BtnChonKhuyenMai_Click, BtnHuyKhuyenMai_Click, ApplyKhuyenMaiApiCallAsync, 
        // ...  BtnThanhToan_Click, BtnHuyDon_Click, BtnQuayLai_Click, BtnLuu_Click, BtnInPhieuGoiMon_Click 
        // ...  GIỮ NGUYÊN) ...

        private async void BtnChonKhuyenMai_Click(object sender, RoutedEventArgs e)
        {
            if (_isDataLoading) return;
            _isDataLoading = true;
            var dialog = new ChonKhuyenMaiWindow(_idHoaDon, _currentKhuyenMaiId);
            if (dialog.ShowDialog() == true)
            {
                int? selectedKmId = dialog.SelectedId;
                if (selectedKmId == 0) selectedKmId = null;
                await ApplyKhuyenMaiApiCallAsync(selectedKmId);
            }
            _isDataLoading = false;
        }
        private async void BtnHuyKhuyenMai_Click(object sender, RoutedEventArgs e)
        {
            if (_isDataLoading) return;
            _isDataLoading = true;
            await ApplyKhuyenMaiApiCallAsync(null);
            _isDataLoading = false;
        }
        private async Task ApplyKhuyenMaiApiCallAsync(int? idKhuyenMai)
        {
            var request = new ApplyPromotionRequest
            {
                IdHoaDon = _idHoaDon,
                IdKhuyenMai = idKhuyenMai
            };
            try
            {
                var response = await _httpClient.PutAsJsonAsync("api/app/nhanvien/goimon/apply-promotion", request);
                if (response.IsSuccessStatusCode)
                {
                    var hoaDonInfo = await response.Content.ReadFromJsonAsync<HoaDonInfoDto>();
                    if (hoaDonInfo != null)
                    {
                        await LoadDataAsync();
                        UpdateBillUI(hoaDonInfo);
                    }
                }
                else
                {
                    MessageBox.Show(await response.Content.ReadAsStringAsync(), "Lỗi áp dụng KM");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi API");
            }
        }
        private async void BtnThanhToan_Click(object sender, RoutedEventArgs e)
        {
            if (_isDataLoading) return;
            _isDataLoading = true;
            var request = new ApplyPromotionRequest
            {
                IdHoaDon = _idHoaDon,
                IdKhuyenMai = _currentKhuyenMaiId
            };
            try
            {
                var response = await _httpClient.PutAsJsonAsync("api/app/nhanvien/goimon/apply-promotion", request);
                if (!response.IsSuccessStatusCode)
                {
                    MessageBox.Show(await response.Content.ReadAsStringAsync(), "Lỗi áp dụng KM");
                    _isDataLoading = false;
                    return;
                }
                this.NavigationService?.Navigate(new ThanhToanView(_idHoaDon));
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi API");
            }
            _isDataLoading = false;
        }
        private async void BtnHuyDon_Click(object sender, RoutedEventArgs e)
        {
            var result = MessageBox.Show($"Bạn có chắc chắn muốn HỦY hóa đơn này không?\n(Các món đã thêm sẽ bị xóa)",
                            "Xác nhận Hủy", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result != MessageBoxResult.Yes) return;
            try
            {
                var response = await _httpClient.PutAsync($"api/app/nhanvien/goimon/cancel-order/{_idHoaDon}", null);
                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Đã hủy hóa đơn thành công!", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                    if (this.NavigationService.CanGoBack)
                    {
                        this.NavigationService.GoBack();
                    }
                }
                else
                {
                    MessageBox.Show(await response.Content.ReadAsStringAsync(), "Lỗi Hủy Đơn");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi API");
            }
        }
        private void BtnQuayLai_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService.CanGoBack)
            {
                this.NavigationService.GoBack();
            }
        }
        private async void BtnLuu_Click(object sender, RoutedEventArgs e)
        {
            if (AuthService.CurrentUser == null)
            {
                MessageBox.Show("Lỗi phiên đăng nhập.", "Lỗi");
                return;
            }
            try
            {
                var response = await _httpClient.PostAsync($"api/app/nhanvien/goimon/send-to-kitchen/{_idHoaDon}", null);
                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Đã lưu và gửi các món mới đến bếp/pha chế.", "Đã lưu");
                }
                else
                {
                    MessageBox.Show(await response.Content.ReadAsStringAsync(), "Lỗi Lưu Hóa Đơn");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi API");
            }
        }
        private async void BtnInPhieuGoiMon_Click(object sender, RoutedEventArgs e)
        {
            if (AuthService.CurrentUser == null)
            {
                MessageBox.Show("Lỗi phiên đăng nhập.", "Lỗi");
                return;
            }
            int idNhanVien = AuthService.CurrentUser.IdNhanVien;
            try
            {
                var response = await _httpClient.PostAsync($"api/app/nhanvien/goimon/print-and-notify-kitchen/{_idHoaDon}/{idNhanVien}", null);
                if (response.IsSuccessStatusCode)
                {
                    var printWindow = new PhieuGoiMonPreviewWindow(_idHoaDon);
                    printWindow.ShowDialog();
                    if (this.NavigationService.CanGoBack)
                    {
                        this.NavigationService.GoBack();
                    }
                }
                else
                {
                    MessageBox.Show(await response.Content.ReadAsStringAsync(), "Lỗi In Phiếu");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi API");
            }
        }
    }
}