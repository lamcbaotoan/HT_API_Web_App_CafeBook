using CafebookModel.Model.ModelApp.NhanVien;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Text.Json;
using CafebookModel.Model.Entities;
using AppCafebookApi.View.common;
using System.Windows.Media;
using AppCafebookApi.Services;

namespace AppCafebookApi.View.nhanvien.pages
{
    public partial class ThanhToanView : Page
    {
        private readonly int _idHoaDonGoc;
        private static readonly HttpClient _httpClient;
        private bool _isDataLoading = true;
        private bool _isSettingCustomer = false; // Cờ để tránh vòng lặp sự kiện

        // Dùng 4 Collection để quản lý
        private ObservableCollection<ChiTietDto> _itemsGoc = new ObservableCollection<ChiTietDto>();
        private ObservableCollection<ChiTietDto> _itemsTach = new ObservableCollection<ChiTietDto>();
        private ObservableCollection<PhuThuDto> _phuThuTach = new ObservableCollection<PhuThuDto>();

        private List<PhuThu> _phuThusKhaDung = new List<PhuThu>();
        private List<KhachHangTimKiemDto> _allKhachHangs = new List<KhachHangTimKiemDto>();

        // SỬA: Dùng DTO chính từ Model
        private List<KhuyenMaiHienThiDto> _availableKms = new List<KhuyenMaiHienThiDto>();

        // Trạng thái thanh toán
        private HoaDonInfoDto _hoaDonInfo = null!;
        private KhachHang? _currentKhachHang = null;
        private int? _currentKhuyenMaiId = null;
        private decimal _currentTienGocTach = 0;
        private decimal _currentTienPhuThuTach = 0;
        private decimal _currentGiamGiaKM = 0;
        private decimal _currentGiamGiaDiem = 0;
        private int _currentDiemSuDung = 0;
        private decimal _currentThanhTienTach = 0;
        private string _currentPhuongThuc = "Tiền mặt";

        // Quy định điểm
        private decimal _tiLeDoiDiem = 1000m;
        private decimal _tiLeNhanDiem = 10000m;

        // Thông tin quán
        private string _tenQuan = "", _diaChi = "", _sdt = "", _wifi = "";
        private Brush _defaultBorderBrush; // THÊM MỚI: Lưu màu nền mặc định

        static ThanhToanView()
        {
            _httpClient = new HttpClient { BaseAddress = new Uri("http://localhost:5166") };
        }

        public ThanhToanView(int idHoaDon)
        {
            InitializeComponent();
            _idHoaDonGoc = idHoaDon;

            dgGoc.ItemsSource = _itemsGoc;
            dgTach.ItemsSource = _itemsTach;
            dgPhuThuTach.ItemsSource = _phuThuTach;
            lbPhuThuKhaDung.ItemsSource = _phuThusKhaDung;
            _defaultBorderBrush = borderKhachHang.Background; // THÊM MỚI: Lưu màu nền
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
                var response = await _httpClient.GetFromJsonAsync<ThanhToanViewDto>($"api/app/nhanvien/thanhtoan/load/{_idHoaDonGoc}");
                if (response == null)
                {
                    MessageBox.Show("Không thể tải dữ liệu thanh toán.");
                    return;
                }

                _hoaDonInfo = response.HoaDonInfo;
                lblTieuDeThanhToan.Text = $"Thanh toán cho {response.HoaDonInfo.SoBan} - HĐ #{response.HoaDonInfo.IdHoaDon}";

                _itemsGoc.Clear();
                _itemsTach.Clear();
                response.ChiTietItems.ForEach(item => _itemsGoc.Add(item));

                _phuThuTach.Clear();
                response.PhuThusDaApDung.ForEach(pt => _phuThuTach.Add(pt));

                _phuThusKhaDung = response.PhuThusKhaDung;
                lbPhuThuKhaDung.ItemsSource = _phuThusKhaDung;

                _allKhachHangs = response.KhachHangsList;
                _currentKhachHang = response.KhachHang;

                _tiLeDoiDiem = response.DiemTichLuy_DoiVND;
                _tiLeNhanDiem = response.DiemTichLuy_NhanVND;

                _tenQuan = response.TenQuan;
                _diaChi = response.DiaChi;
                _sdt = response.SoDienThoai;
                _wifi = response.WifiMatKhau;

                // Cập nhật UI khách hàng theo logic mới
                SetKhachHangUI(_currentKhachHang);

                _currentKhuyenMaiId = response.IdKhuyenMaiDaApDung;
                await UpdateKhuyenMaiUI();

                txtDiemSuDung.Text = "0";

                UpdateTachTotals();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải dữ liệu: {ex.Message}", "Lỗi nghiêm trọng");
            }
            _isDataLoading = false;
        }

        #region Logic Tách/Gộp

        private void BtnChuyenQua_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = dgGoc.SelectedItems.Cast<ChiTietDto>().ToList();
            foreach (var selected in selectedItems)
            {
                _itemsGoc.Remove(selected);
                _itemsTach.Add(selected);
            }

            foreach (var item in lbPhuThuKhaDung.SelectedItems)
            {
                var phuThu = item as PhuThu;
                if (phuThu != null && !_phuThuTach.Any(p => p.IdPhuThu == phuThu.IdPhuThu))
                {
                    _phuThuTach.Add(new PhuThuDto
                    {
                        IdPhuThu = phuThu.IdPhuThu,
                        TenPhuThu = phuThu.TenPhuThu,
                        LoaiGiaTri = phuThu.LoaiGiaTri,
                        GiaTri = phuThu.GiaTri
                    });
                }
            }
            lbPhuThuKhaDung.UnselectAll();
            UpdateTachTotals();
        }

        private void BtnChuyenQuaTatCa_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _itemsGoc.ToList()) _itemsTach.Add(item);
            _itemsGoc.Clear();

            foreach (var item in lbPhuThuKhaDung.SelectedItems)
            {
                var phuThu = item as PhuThu;
                if (phuThu != null && !_phuThuTach.Any(p => p.IdPhuThu == phuThu.IdPhuThu))
                {
                    _phuThuTach.Add(new PhuThuDto
                    {
                        IdPhuThu = phuThu.IdPhuThu,
                        TenPhuThu = phuThu.TenPhuThu,
                        LoaiGiaTri = phuThu.LoaiGiaTri,
                        GiaTri = phuThu.GiaTri
                    });
                }
            }
            lbPhuThuKhaDung.UnselectAll();

            UpdateTachTotals();
        }

        private void BtnChuyenLai_Click(object sender, RoutedEventArgs e)
        {
            var selectedItems = dgTach.SelectedItems.Cast<ChiTietDto>().ToList();
            foreach (var selected in selectedItems)
            {
                _itemsTach.Remove(selected);
                _itemsGoc.Add(selected);
            }

            var selectedPhuThus = dgPhuThuTach.SelectedItems.Cast<PhuThuDto>().ToList();
            foreach (var selected in selectedPhuThus)
            {
                _phuThuTach.Remove(selected);
            }
            UpdateTachTotals();
        }

        private void BtnChuyenLaiTatCa_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in _itemsTach.ToList()) _itemsGoc.Add(item);
            _itemsTach.Clear();
            _phuThuTach.Clear();
            UpdateTachTotals();
        }

        #endregion

        #region Logic Tính Toán Panel Phải

        private void UpdateTachTotals()
        {
            if (_isDataLoading || lblTienCanThanhToan == null) return;

            _currentTienGocTach = _itemsTach.Sum(i => i.ThanhTien);

            _currentTienPhuThuTach = 0;
            foreach (var pt in _phuThuTach)
            {
                if (string.Equals(pt.LoaiGiaTri, "%", StringComparison.OrdinalIgnoreCase) || string.Equals(pt.LoaiGiaTri, "PhanTram", StringComparison.OrdinalIgnoreCase))
                {
                    pt.SoTien = _currentTienGocTach * (pt.GiaTri / 100);
                }
                else
                {
                    pt.SoTien = pt.GiaTri;
                }
                _currentTienPhuThuTach += pt.SoTien;
            }
            dgPhuThuTach.Items.Refresh();

            decimal tongTruocGiam = _currentTienGocTach + _currentTienPhuThuTach;
            lblTienCanThanhToan.Text = tongTruocGiam.ToString("N0") + " đ";

            _currentGiamGiaKM = 0;
            if (_currentKhuyenMaiId.HasValue && _currentKhuyenMaiId > 0)
            {
                try
                {
                    var km = _availableKms.FirstOrDefault(k => k.IdKhuyenMai == _currentKhuyenMaiId);
                    if (km != null)
                    {
                        if (string.Equals(km.LoaiGiamGia, "PhanTram", StringComparison.OrdinalIgnoreCase))
                        {
                            _currentGiamGiaKM = _currentTienGocTach * (km.GiaTriGiam / 100);
                        }
                        else
                        {
                            _currentGiamGiaKM = km.GiaTriGiam;
                        }
                    }
                }
                catch { }
            }
            lblTienGiamKM.Text = $"- {_currentGiamGiaKM:N0} đ";

            int.TryParse(txtDiemSuDung.Text, out _currentDiemSuDung);
            _currentGiamGiaDiem = _currentDiemSuDung * _tiLeDoiDiem;
            lblTienGiamDiem.Text = $"- {_currentGiamGiaDiem:N0} đ";

            _currentThanhTienTach = tongTruocGiam - _currentGiamGiaKM - _currentGiamGiaDiem;
            if (_currentThanhTienTach < 0) _currentThanhTienTach = 0;
            lblThanhTien.Text = _currentThanhTienTach.ToString("N0") + " đ";

            UpdateTienThua();
        }

        private void TxtKhachDua_TextChanged(object sender, TextChangedEventArgs e)
        {
            UpdateTienThua();
        }

        private void UpdateTienThua()
        {
            if (lblTienThua == null) return;

            if (decimal.TryParse(txtKhachDua.Text, out decimal khachDua))
            {
                decimal tienThua = khachDua - _currentThanhTienTach;
                lblTienThua.Text = tienThua.ToString("N0") + " đ";
            }
            else
            {
                lblTienThua.Text = (0 - _currentThanhTienTach).ToString("N0") + " đ";
            }
        }

        private void TxtDiemSuDung_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isDataLoading) return;

            if (int.TryParse(txtDiemSuDung.Text, out int diem) && _currentKhachHang != null)
            {
                if (diem > _currentKhachHang.DiemTichLuy)
                {
                    diem = _currentKhachHang.DiemTichLuy;
                    txtDiemSuDung.Text = diem.ToString();
                }

                decimal giamTuDiem = diem * _tiLeDoiDiem;
                decimal tongTruocDiem = _currentTienGocTach + _currentTienPhuThuTach - _currentGiamGiaKM;

                if (giamTuDiem > tongTruocDiem)
                {
                    int diemToiDa = (int)Math.Floor(tongTruocDiem / _tiLeDoiDiem);
                    txtDiemSuDung.Text = diemToiDa.ToString();
                }
            }
            UpdateTachTotals();
        }

        private void PaymentMethod_Changed(object sender, RoutedEventArgs e)
        {
            if (panelTienMat == null || rbTienMat == null || rbChuyenKhoan == null || rbThe == null || rbViDienTu == null)
            {
                return;
            }

            if (rbTienMat.IsChecked == true)
            {
                panelTienMat.Visibility = Visibility.Visible;
                _currentPhuongThuc = "Tiền mặt";
            }
            else
            {
                panelTienMat.Visibility = Visibility.Collapsed;
                if (rbChuyenKhoan.IsChecked == true) _currentPhuongThuc = "Chuyển khoản";
                else if (rbThe.IsChecked == true) _currentPhuongThuc = "Thẻ";
                else if (rbViDienTu.IsChecked == true) _currentPhuongThuc = "Ví điện tử";
            }
        }

        #endregion

        #region Logic Khách hàng & Điểm (Đã cải tiến)

        // HÀM MỚI: Xử lý khi gõ text (Search-as-you-type)
        private void TxtTimKhachHang_TextChanged(object sender, TextChangedEventArgs e)
        {
            if (_isSettingCustomer) return; // Nếu đang set text thì không tìm kiếm

            string query = txtTimKhachHang.Text.ToLower();

            if (string.IsNullOrEmpty(query))
            {
                lbKhachHangResults.Visibility = Visibility.Collapsed;
                return;
            }

            // Lọc danh sách DTO, nhưng cung cấp đối tượng KhachHang cho ListBox (để khớp DataTemplate XAML)
            var filteredList = _allKhachHangs
                .Where(khDto => khDto.DisplayText.ToLower().Contains(query))
                .Select(khDto => khDto.KhachHangData)
                .ToList();

            if (filteredList.Any())
            {
                lbKhachHangResults.ItemsSource = filteredList;
                lbKhachHangResults.Visibility = Visibility.Visible;
            }
            else
            {
                lbKhachHangResults.Visibility = Visibility.Collapsed;
            }
        }

        // HÀM MỚI: Xử lý khi chọn một khách hàng từ danh sách
        private void LbKhachHangResults_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lbKhachHangResults.SelectedItem is KhachHang selectedKh)
            {
                SetKhachHangUI(selectedKh); // Sử dụng helper
            }
        }

        // HÀM MỚI: Xử lý khi bấm phím Enter (chọn hoặc tạo mới)
        private async void TxtTimKhachHang_PreviewKeyDown(object sender, System.Windows.Input.KeyEventArgs e)
        {
            if (e.Key == System.Windows.Input.Key.Enter)
            {
                e.Handled = true; // Ngăn tiếng "bíp"
                await HandleFindOrCreateCustomer();
            }
        }

        // SỬA LẠI HÀM TxtTimKhachHang_LostFocus (dòng 316)
        private async void TxtTimKhachHang_LostFocus(object sender, RoutedEventArgs e)
        {
            // THÊM DÒNG NÀY: Nếu chuột đang trên nút Thanh Toán, không chạy
            // Vì nút thanh toán sẽ tự gọi logic này
            if (btnXacNhanThanhToan.IsMouseOver) return;

            await Task.Delay(150);
            if (lbKhachHangResults.IsKeyboardFocusWithin || lbKhachHangResults.IsFocused)
            {
                return;
            }
            await HandleFindOrCreateCustomer();
        }

        // THÊM HÀM MỚI NÀY (vào khu vực #region Logic Khách hàng & Điểm)
        // Xử lý Req 1: Bấm nút "X" để hủy chọn khách hàng
        private void BtnHuyKhachHang_Click(object sender, RoutedEventArgs e)
        {
            SetKhachHangUI(null);
            txtTimKhachHang.Text = ""; // Xóa luôn text tìm kiếm
        }

        // THAY THẾ HÀM CŨ HandleFindOrCreateCustomer bằng hàm này
        private async Task HandleFindOrCreateCustomer()
        {
            if (_isSettingCustomer) return;
            if (lbKhachHangResults.Visibility == Visibility.Visible)
            {
                lbKhachHangResults.Visibility = Visibility.Collapsed;
            }

            string query = txtTimKhachHang.Text;

            if (_currentKhachHang != null)
            {
                var khDto = _allKhachHangs.FirstOrDefault(kh => kh.IdKhachHang == _currentKhachHang.IdKhachHang);
                if (khDto != null && khDto.DisplayText.Equals(query, StringComparison.OrdinalIgnoreCase))
                {
                    return;
                }
            }

            if (string.IsNullOrWhiteSpace(query))
            {
                SetKhachHangUI(null);
                return;
            }

            btnXacNhanThanhToan.IsEnabled = false;
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/app/nhanvien/thanhtoan/find-or-create-customer", query);
                if (response.IsSuccessStatusCode)
                {
                    // SỬA: Đọc về DTO mới
                    var resultDto = await response.Content.ReadFromJsonAsync<KhachHangTimKiemDto>();

                    if (resultDto != null)
                    {
                        if (!_allKhachHangs.Any(kh => kh.IdKhachHang == resultDto.IdKhachHang))
                        {
                            _allKhachHangs.Add(resultDto);
                        }
                        SetKhachHangUI(resultDto.KhachHangData); // Cập nhật UI

                        // SỬA: Xử lý Req 2 (Hiển thị là tạo mới)
                        if (resultDto.IsNew)
                        {
                            lblTenKhachHang.Text = $"{resultDto.KhachHangData.HoTen} (Tài khoản mới)";
                            // Đổi màu nền để nhân viên chú ý
                            borderKhachHang.Background = new SolidColorBrush((Color)ColorConverter.ConvertFromString("#FF8F5E00"));
                        }
                    }
                    else
                    {
                        SetKhachHangUI(null); // Là khách vãng lai
                        _isSettingCustomer = true;
                        txtTimKhachHang.Text = query; // Giữ lại tên đã gõ
                        _isSettingCustomer = false;
                    }
                }
                else
                {
                    MessageBox.Show("Không thể tìm hoặc tạo khách hàng. Sử dụng khách vãng lai.", "Lỗi API");
                    SetKhachHangUI(null);
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi API khách hàng: {ex.Message}");
                SetKhachHangUI(null);
            }
            btnXacNhanThanhToan.IsEnabled = true;
        }

        // THAY THẾ HÀM CŨ SetKhachHangUI bằng hàm này
        private void SetKhachHangUI(KhachHang? khachHang)
        {
            _currentKhachHang = khachHang;
            _isSettingCustomer = true;
            borderKhachHang.Background = _defaultBorderBrush; // SỬA: Luôn reset màu nền

            if (_currentKhachHang != null)
            {
                var khDto = _allKhachHangs.FirstOrDefault(kh => kh.IdKhachHang == _currentKhachHang.IdKhachHang);

                borderKhachHang.Visibility = Visibility.Visible;
                btnHuyKhachHang.Visibility = Visibility.Visible; // SỬA: Hiển thị nút Hủy
                lblTenKhachHang.Text = _currentKhachHang.HoTen;
                lblDiemHienCo.Text = $"{_currentKhachHang.DiemTichLuy} điểm (Tương đương {_currentKhachHang.DiemTichLuy * _tiLeDoiDiem:N0} đ)";
                txtDiemSuDung.IsEnabled = true;
                btnApDungDiem.IsEnabled = true;

                txtTimKhachHang.Text = khDto?.DisplayText ?? _currentKhachHang.HoTen;
            }
            else // Khách vãng lai
            {
                borderKhachHang.Visibility = Visibility.Collapsed;
                btnHuyKhachHang.Visibility = Visibility.Collapsed; // SỬA: Ẩn nút Hủy
                lblTenKhachHang.Text = "Khách vãng lai";
                txtDiemSuDung.IsEnabled = false;
                btnApDungDiem.IsEnabled = false;
                txtDiemSuDung.Text = "0";

                if (!txtTimKhachHang.IsFocused)
                {
                    txtTimKhachHang.Text = "";
                }
            }

            lbKhachHangResults.Visibility = Visibility.Collapsed;
            _isSettingCustomer = false;
            UpdateTachTotals();
        }

        private void BtnApDungDiem_Click(object sender, RoutedEventArgs e)
        {
            TxtDiemSuDung_TextChanged(sender, null!);
        }

        #endregion

        #region Logic Khuyến Mãi (Cửa sổ mới)

        private async Task LoadAvailableKms()
        {
            if (_availableKms.Any()) return;

            try
            {
                var kms = await _httpClient.GetFromJsonAsync<List<KhuyenMaiHienThiDto>>($"api/app/nhanvien/khuyenmai/available/{_idHoaDonGoc}");
                _availableKms = kms ?? new List<KhuyenMaiHienThiDto>();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể tải danh sách KM: {ex.Message}");
                _availableKms = new List<KhuyenMaiHienThiDto>();
            }
        }

        private async Task UpdateKhuyenMaiUI()
        {
            await LoadAvailableKms();

            if (_currentKhuyenMaiId.HasValue && _currentKhuyenMaiId > 0)
            {
                var km = _availableKms.FirstOrDefault(k => k.IdKhuyenMai == _currentKhuyenMaiId);
                if (km == null)
                {
                    btnChonKhuyenMai.Content = "KM đã lưu (không hợp lệ)";
                }
                else
                {
                    btnChonKhuyenMai.Content = km.TenChuongTrinh;
                }
                btnHuyKhuyenMai.Visibility = Visibility.Visible;
            }
            else
            {
                btnChonKhuyenMai.Content = "-- Chọn Khuyến mãi --";
                btnHuyKhuyenMai.Visibility = Visibility.Collapsed;
            }
        }

        private async void BtnChonKhuyenMai_Click(object sender, RoutedEventArgs e)
        {
            await LoadAvailableKms();

            var dialog = new ChonKhuyenMaiWindow(_idHoaDonGoc, _currentKhuyenMaiId);
            if (dialog.ShowDialog() == true)
            {
                _currentKhuyenMaiId = dialog.SelectedId;
                if (_currentKhuyenMaiId == 0) _currentKhuyenMaiId = null;

                await UpdateKhuyenMaiUI();
                UpdateTachTotals();
            }
        }

        private void BtnHuyKhuyenMai_Click(object sender, RoutedEventArgs e)
        {
            _currentKhuyenMaiId = null;
            btnChonKhuyenMai.Content = "-- Chọn Khuyến mãi --";
            btnHuyKhuyenMai.Visibility = Visibility.Collapsed;
            UpdateTachTotals();
        }

        #endregion

        // THAY THẾ TOÀN BỘ HÀM BtnXacNhanThanhToan_Click (dòng 506)
        private async void BtnXacNhanThanhToan_Click(object sender, RoutedEventArgs e)
        {
            // *** BƯỚC 1: FIX RACE CONDITION ***
            // Buộc chạy và đợi logic tìm/tạo khách hàng hoàn tất
            // TRƯỚC KHI xây dựng request thanh toán.
            await HandleFindOrCreateCustomer();
            // *** KẾT THÚC FIX ***

            if (!_itemsTach.Any())
            {
                MessageBox.Show("Vui lòng chuyển ít nhất 1 món qua 'Hóa đơn thanh toán'.", "Chưa chọn món");
                return;
            }

            decimal.TryParse(txtKhachDua.Text, out decimal khachDua);
            if (_currentPhuongThuc == "Tiền mặt" && khachDua < _currentThanhTienTach)
            {
                MessageBox.Show("Tiền khách đưa không đủ.", "Thiếu tiền", MessageBoxButton.OK, MessageBoxImage.Warning);
                return;
            }

            bool isFullPaymentClient = !_itemsGoc.Any();
            if (isFullPaymentClient)
            {
                var confirmResult = MessageBox.Show(
                    "Xác nhận thanh toán cho toàn bộ hóa đơn này?",
                    "Xác nhận",
                    MessageBoxButton.YesNo,
                    MessageBoxImage.Question);

                if (confirmResult == MessageBoxResult.No)
                {
                    return;
                }
            }

            var request = new ThanhToanRequestDto
            {
                IdHoaDonGoc = _idHoaDonGoc,
                IdChiTietTach = _itemsTach.Select(i => i.IdChiTietHoaDon).ToList(),
                IdPhuThuTach = _phuThuTach.Select(p => p.IdPhuThu).ToList(),
                IdKhuyenMai = _currentKhuyenMaiId,
                PhuongThucThanhToan = _currentPhuongThuc,
                KhachDua = khachDua,
                DiemSuDung = _currentDiemSuDung,
                // _currentKhachHang LÚC NÀY ĐÃ ĐƯỢC CẬP NHẬT
                IdKhachHang = _currentKhachHang?.IdKhachHang
            };

            btnXacNhanThanhToan.IsEnabled = false;
            try
            {
                var response = await _httpClient.PostAsJsonAsync("api/app/nhanvien/thanhtoan/pay", request);
                if (response.IsSuccessStatusCode)
                {
                    var jsonDoc = JsonDocument.Parse(await response.Content.ReadAsStringAsync());
                    bool isFullPaymentServer = jsonDoc.RootElement.GetProperty("isFullPayment").GetBoolean();
                    int idHoaDonDaThanhToan = jsonDoc.RootElement.GetProperty("idHoaDonDaThanhToan").GetInt32();

                    var previewData = new HoaDonPreviewDto
                    {
                        IsProvisional = false,
                        TenQuan = _tenQuan,
                        DiaChi = _diaChi,
                        SoDienThoai = _sdt,
                        WifiMatKhau = _wifi,
                        IdHoaDon = idHoaDonDaThanhToan,
                        SoBan = _hoaDonInfo.SoBan,
                        ThoiGianTao = DateTime.Now,
                        TenNhanVien = AuthService.CurrentUser?.HoTen ?? "Nhân viên",
                        TenKhachHang = _currentKhachHang?.HoTen ?? "Khách vãng lai", // Đã đúng
                        Items = _itemsTach.ToList(),
                        Surcharges = _phuThuTach.ToList(),
                        TongTienGoc = _currentTienGocTach,
                        TongPhuThu = _currentTienPhuThuTach,
                        GiamGiaKM = _currentGiamGiaKM,
                        GiamGiaDiem = _currentGiamGiaDiem,
                        ThanhTien = _currentThanhTienTach,
                        PhuongThucThanhToan = _currentPhuongThuc,
                        KhachDua = decimal.TryParse(txtKhachDua.Text, out var kd) ? kd : 0,
                        TienThoi = decimal.TryParse(lblTienThua.Text.Replace(" đ", "").Replace(",", ""), out var tt) ? tt : 0
                    };

                    var previewWindow = new HoaDonPreviewWindow(previewData);
                    previewWindow.ShowDialog();

                    if (isFullPaymentServer)
                    {
                        var mainFrame = FindParentFrame();
                        if (mainFrame != null)
                        {
                            mainFrame.Navigate(new SoDoBanView());
                            while (mainFrame.CanGoBack)
                            {
                                mainFrame.RemoveBackEntry();
                            }
                        }
                    }
                    else
                    {
                        await LoadDataAsync();
                    }
                }
                else
                {
                    string errorMessage = await response.Content.ReadAsStringAsync();
                    MessageBox.Show(errorMessage, "Lỗi Thanh Toán");

                    if (errorMessage.Contains("Hóa đơn này đã được xử lý"))
                    {
                        var mainFrame = FindParentFrame();
                        if (mainFrame != null)
                        {
                            mainFrame.Navigate(new SoDoBanView());
                            while (mainFrame.CanGoBack)
                            {
                                mainFrame.RemoveBackEntry();
                            }
                        }
                    }
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(ex.Message, "Lỗi API");
            }
            btnXacNhanThanhToan.IsEnabled = true;
        }

        private Frame? FindParentFrame()
        {
            var currentWindow = Window.GetWindow(this);
            if (currentWindow == null) return null;
            var mainFrame = currentWindow.FindName("MainFrame") as Frame;
            return mainFrame;
        }

        private void BtnInTamTinh_Click(object sender, RoutedEventArgs e)
        {
            var previewData = new HoaDonPreviewDto
            {
                IsProvisional = true,
                TenQuan = _tenQuan,
                DiaChi = _diaChi,
                SoDienThoai = _sdt,
                WifiMatKhau = _wifi,
                IdHoaDon = _idHoaDonGoc,
                SoBan = _hoaDonInfo.SoBan,
                ThoiGianTao = DateTime.Now,
                TenNhanVien = AuthService.CurrentUser?.HoTen ?? "Nhân viên",
                TenKhachHang = _currentKhachHang?.HoTen ?? "Khách vãng lai",
                Items = _itemsTach.ToList(),
                Surcharges = _phuThuTach.ToList(),
                TongTienGoc = _currentTienGocTach,
                TongPhuThu = _currentTienPhuThuTach,
                GiamGiaKM = _currentGiamGiaKM,
                GiamGiaDiem = _currentGiamGiaDiem,
                ThanhTien = _currentThanhTienTach,
                PhuongThucThanhToan = _currentPhuongThuc,
                KhachDua = decimal.TryParse(txtKhachDua.Text, out var kd) ? kd : 0,
                TienThoi = decimal.TryParse(lblTienThua.Text.Replace(" đ", "").Replace(",", ""), out var tt) ? tt : 0
            };

            var previewWindow = new HoaDonPreviewWindow(previewData);
            previewWindow.ShowDialog();
        }

        private void BtnQuayLai_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService.CanGoBack)
            {
                this.NavigationService.GoBack();
            }
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
    }
}