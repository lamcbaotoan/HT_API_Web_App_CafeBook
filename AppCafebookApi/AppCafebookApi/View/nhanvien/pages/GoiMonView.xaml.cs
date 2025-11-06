using AppCafebookApi.Services;
using AppCafebookApi.View.common;
using CafebookModel.Model.ModelApp.NhanVien;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Text.Json; // Thêm
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
        private List<KhuyenMaiDto> _availableKms = new List<KhuyenMaiDto>(); // <-- LƯU TRỮ KM
        private int? _currentKhuyenMaiId = null; // <-- LƯU ID KM HIỆN TẠI
        private bool _isDataLoading = true;

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

                // Lưu danh sách KM để cập nhật UI
                _availableKms = response.KhuyenMais ?? new List<KhuyenMaiDto>();
                // Xóa ComboBox: cmbKhuyenMai.ItemsSource = _availableKms;

                UpdateBillUI(response.HoaDonInfo);

                if (lbLoaiSP.Items.Count > 0)
                {
                    lbLoaiSP.UpdateLayout();
                    var container = lbLoaiSP.ItemContainerGenerator.ContainerFromIndex(0) as FrameworkElement;
                    var rb = FindVisualChild<RadioButton>(container);
                    if (rb != null)
                    {
                        rb.IsChecked = true;
                    }
                    else
                    {
                        icSanPham.ItemsSource = _allSanPhams;
                    }
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
            if (info == null) return;

            lblTieuDeHoaDon.Text = $"Hóa đơn - {info.SoBan}";
            lblTongTien.Text = info.TongTienGoc.ToString("N0");
            lblTienGiam.Text = info.GiamGia.ToString("N0");
            lblThanhTien.Text = info.ThanhTien.ToString("N0") + " VND";

            _currentKhuyenMaiId = info.IdKhuyenMai; // Cập nhật ID hiện tại

            // Cập nhật Button thay vì ComboBox
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

        private void Category_Checked(object sender, RoutedEventArgs e)
        {
            var radioButton = sender as RadioButton;
            var danhMuc = radioButton?.DataContext as DanhMucDto;

            if (danhMuc == null)
            {
                if (radioButton != null && radioButton.Content.ToString() == "Tất cả")
                {
                    icSanPham.ItemsSource = _allSanPhams;
                }
                return;
            }

            if (danhMuc.IdDanhMuc == 0) // "Tất cả"
            {
                icSanPham.ItemsSource = _allSanPhams;
            }
            else
            {
                icSanPham.ItemsSource = _allSanPhams.Where(s => s.IdDanhMuc == danhMuc.IdDanhMuc).ToList();
            }
        }

        private async void ProductButton_Click(object sender, RoutedEventArgs e)
        {
            var sanPham = (sender as Button)?.DataContext as SanPhamDto;
            if (sanPham == null) return;

            var request = new AddItemRequest
            {
                IdHoaDon = _idHoaDon,
                IdSanPham = sanPham.IdSanPham,
                SoLuong = 1
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
            var item = (sender as Button)?.DataContext as ChiTietDto;
            if (item == null) return;
            await UpdateQuantityAsync(item, item.SoLuong - 1);
        }

        private async void BtnTangSL_Click(object sender, RoutedEventArgs e)
        {
            var item = (sender as Button)?.DataContext as ChiTietDto;
            if (item == null) return;
            await UpdateQuantityAsync(item, item.SoLuong + 1);
        }

        private async void BtnXoaMon_Click(object sender, RoutedEventArgs e)
        {
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

        // === LOGIC KHUYẾN MÃI MỚI (ĐÃ SỬA LỖI THREADING) ===

        private async void BtnChonKhuyenMai_Click(object sender, RoutedEventArgs e)
        {
            if (_isDataLoading) return; // Thêm bảo vệ
            _isDataLoading = true;

            // 1. Mở cửa sổ dialog
            var dialog = new ChonKhuyenMaiWindow(_idHoaDon, _currentKhuyenMaiId);

            // 2. Kiểm tra kết quả
            if (dialog.ShowDialog() == true)
            {
                // 3. Nếu người dùng nhấn "Áp dụng"
                int? selectedKmId = dialog.SelectedId;
                if (selectedKmId == 0) selectedKmId = null; // "Không áp dụng"

                // 4. Gọi API (Sửa: Bỏ Task.Run, gọi await trực tiếp)
                await ApplyKhuyenMaiApiCallAsync(selectedKmId);
            }

            _isDataLoading = false;
        }

        private async void BtnHuyKhuyenMai_Click(object sender, RoutedEventArgs e)
        {
            if (_isDataLoading) return; // Thêm bảo vệ
            _isDataLoading = true;

            // Gọi API với null để gỡ bỏ KM (Sửa: Bỏ Task.Run)
            await ApplyKhuyenMaiApiCallAsync(null);

            _isDataLoading = false;
        }

        // Hàm dùng chung để gọi API áp dụng/hủy KM
        private async Task ApplyKhuyenMaiApiCallAsync(int? idKhuyenMai)
        {
            // (Xóa 'if (_isDataLoading) return;' vì đã kiểm tra ở hàm gọi)

            var request = new ApplyPromotionRequest
            {
                IdHoaDon = _idHoaDon,
                IdKhuyenMai = idKhuyenMai
            };

            try
            {
                var response = await _httpClient.PutAsJsonAsync("api/app/nhanvien/goimon/apply-promotion", request);

                // Sửa: Vì hàm này đã chạy trên UI thread, chúng ta không cần Dispatcher
                if (response.IsSuccessStatusCode)
                {
                    var hoaDonInfo = await response.Content.ReadFromJsonAsync<HoaDonInfoDto>();
                    if (hoaDonInfo != null)
                    {
                        // Sửa: Phải AWAIT (chờ) LoadDataAsync xong
                        await LoadDataAsync();
                        // Sau khi LoadDataAsync có KM mới, UpdateBillUI mới chạy
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

        // SỬA LẠI HÀM NÀY
        private async void BtnThanhToan_Click(object sender, RoutedEventArgs e)
        {
            // YÊU CẦU: Lưu khuyến mãi hiện tại trước khi chuyển trang
            if (_isDataLoading) return;
            _isDataLoading = true;

            var request = new ApplyPromotionRequest
            {
                IdHoaDon = _idHoaDon,
                IdKhuyenMai = _currentKhuyenMaiId
            };

            try
            {
                // 1. Lưu KM vào CSDL (bảng HoaDon_KhuyenMai)
                var response = await _httpClient.PutAsJsonAsync("api/app/nhanvien/goimon/apply-promotion", request);
                if (!response.IsSuccessStatusCode)
                {
                    MessageBox.Show(await response.Content.ReadAsStringAsync(), "Lỗi áp dụng KM");
                    _isDataLoading = false;
                    return;
                }

                // 2. Điều hướng đến trang ThanhToanView (CHỈ TRUYỀN ID)
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
                // Gọi API (Đã sửa ở DonHangController)
                var response = await _httpClient.PutAsJsonAsync($"api/app/donhang/update-status/{_idHoaDon}", "Hủy");
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

        private void BtnLuu_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Đã lưu các thay đổi.", "Đã lưu");
        }

        private void BtnInTamTinh_Click(object sender, RoutedEventArgs e)
        {
            MessageBox.Show("Chức năng 'In Tạm Tính' đang được phát triển.", "Thông báo");
        }
    }
}