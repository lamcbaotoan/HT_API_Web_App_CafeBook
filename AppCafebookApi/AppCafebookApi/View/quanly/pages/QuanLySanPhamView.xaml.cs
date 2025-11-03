using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;
using CafebookModel.Model.ModelApp;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Win32;
using System.IO;
using AppCafebookApi.Services;
using CafebookModel.Utils;
using AppCafebookApi.View.common;
using System.Globalization;
using OfficeOpenXml; // Cho Excel
using CafebookModel.Model.Entities; // Cho logic lưu mới

namespace AppCafebookApi.View.quanly.pages
{
    public partial class QuanLySanPhamView : Page
    {
        private static readonly HttpClient httpClient;
        private List<SanPhamDto> _allSanPhamList = new List<SanPhamDto>();
        private SanPhamUpdateRequestDto? _selectedSanPhamDetails = null;
        private string? _currentAnhBiaBase64 = null;

        // Cache
        private List<FilterLookupDto> _danhMucList = new List<FilterLookupDto>();
        private List<FilterLookupDto> _nguyenLieuList = new List<FilterLookupDto>();
        private List<DonViChuyenDoiDto> _donViTinhList = new List<DonViChuyenDoiDto>();

        static QuanLySanPhamView()
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5166")
            };
        }

        public QuanLySanPhamView()
        {
            InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadFiltersAsync();
            await LoadDataGridAsync();
            ResetForm();
            ResetDanhMucForm();
            ResetDinhLuongForm();
        }

        #region 1. Tải Dữ Liệu (Sản Phẩm, Lọc, Định Lượng)

        private async Task LoadFiltersAsync()
        {
            try
            {
                var filters = await httpClient.GetFromJsonAsync<SanPhamFiltersDto>("api/app/sanpham/filters");
                if (filters != null)
                {
                    _danhMucList = filters.DanhMucs;
                    _nguyenLieuList = filters.NguyenLieus;
                    _donViTinhList = filters.DonViTinhs;

                    // Tab 1: Lọc
                    var filterDanhMuc = new List<FilterLookupDto>(_danhMucList);
                    filterDanhMuc.Insert(0, new FilterLookupDto { Id = 0, Ten = "Tất cả Danh mục" });
                    cmbFilterDanhMuc.ItemsSource = filterDanhMuc;
                    if (cmbFilterDanhMuc.SelectedValue == null) cmbFilterDanhMuc.SelectedValue = 0;
                    if (cmbFilterTrangThai.SelectedValue == null) cmbFilterTrangThai.SelectedIndex = 0;

                    // Tab 2: Quản lý Danh mục
                    lbDanhMuc.ItemsSource = _danhMucList;

                    // Tab 3: Định lượng
                    var nlList = new List<FilterLookupDto>(_nguyenLieuList);
                    nlList.Insert(0, new FilterLookupDto { Id = 0, Ten = "-- Chọn Nguyên Liệu --" });
                    cmbNguyenLieu.ItemsSource = nlList;

                    // Form: ComboBox Danh Mục (Smart)
                    var formDanhMuc = new List<FilterLookupDto>(_danhMucList);
                    formDanhMuc.Insert(0, new FilterLookupDto { Id = 0, Ten = "-- Chọn Danh mục --" });
                    cmbDanhMuc.ItemsSource = formDanhMuc;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải bộ lọc: {ex.Message}", "Lỗi API");
            }
        }

        private async Task LoadDataGridAsync()
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            string searchText = txtSearchSanPham.Text;
            int danhMucId = (int)(cmbFilterDanhMuc.SelectedValue ?? 0);
            bool? trangThai = (cmbFilterTrangThai.SelectedItem as ComboBoxItem)?.Tag as bool?;
            try
            {
                var url = $"api/app/sanpham/search?searchText={Uri.EscapeDataString(searchText)}&danhMucId={danhMucId}";
                if (trangThai.HasValue)
                {
                    url += $"&trangThai={trangThai.Value}";
                }
                _allSanPhamList = (await httpClient.GetFromJsonAsync<List<SanPhamDto>>(url)) ?? new List<SanPhamDto>();
                dgSanPham.ItemsSource = _allSanPhamList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách sản phẩm: {ex.Message}", "Lỗi API");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async Task LoadDinhLuongGridAsync(int idSanPham)
        {
            if (idSanPham == 0)
            {
                panelDinhLuong.Visibility = Visibility.Collapsed;
                lblDinhLuongInfo.Visibility = Visibility.Visible;
                return;
            }
            try
            {
                var data = await httpClient.GetFromJsonAsync<List<DinhLuongDto>>($"api/app/sanpham/{idSanPham}/dinhluong");
                dgDinhLuong.ItemsSource = data;
                lblDinhLuongTitle.Text = $"Định lượng cho: {_selectedSanPhamDetails?.TenSanPham}";
                panelDinhLuong.Visibility = Visibility.Visible;
                lblDinhLuongInfo.Visibility = Visibility.Collapsed;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải định lượng: {ex.Message}", "Lỗi API");
            }
        }

        #endregion

        #region 2. Form Chi Tiết (Sản Phẩm)

        private void ResetForm()
        {
            _selectedSanPhamDetails = null;
            _currentAnhBiaBase64 = null;
            dgSanPham.SelectedItem = null;
            formChiTietSP.IsEnabled = true;
            panelActions.Visibility = Visibility.Visible;
            btnXoa.Visibility = Visibility.Collapsed;
            lblFormTitle.Text = "Thêm Sản Phẩm Mới";
            txtTenSanPham.Text = "";
            txtDonGia.Text = "0";
            txtMoTa.Text = "";
            cmbDanhMuc.SelectedValue = 0;
            cmbDanhMuc.Text = string.Empty;
            cmbNhomIn.SelectedIndex = 0; // "Bar"
            cmbTrangThai.SelectedIndex = 0; // "Đang bán"
            AnhSanPhamPreview.Source = HinhAnhHelper.LoadImageFromBase64(null, HinhAnhPaths.DefaultFoodIcon);
            panelDinhLuong.Visibility = Visibility.Collapsed;
            lblDinhLuongInfo.Visibility = Visibility.Visible;
            dgDinhLuong.ItemsSource = null;
        }

        private async void DgSanPham_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgSanPham.SelectedItem is not SanPhamDto selected)
            {
                ResetForm();
                return;
            }
            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                _selectedSanPhamDetails = await httpClient.GetFromJsonAsync<SanPhamUpdateRequestDto>($"api/app/sanpham/details/{selected.IdSanPham}");
                if (_selectedSanPhamDetails == null)
                {
                    ResetForm();
                    return;
                }
                _currentAnhBiaBase64 = _selectedSanPhamDetails.HinhAnhBase64;
                formChiTietSP.IsEnabled = true;
                panelActions.Visibility = Visibility.Visible;
                btnXoa.Visibility = Visibility.Visible;
                lblFormTitle.Text = $"Cập nhật: {_selectedSanPhamDetails.TenSanPham}";
                txtTenSanPham.Text = _selectedSanPhamDetails.TenSanPham;
                txtDonGia.Text = _selectedSanPhamDetails.GiaBan.ToString("F0");
                txtMoTa.Text = _selectedSanPhamDetails.MoTa;
                cmbDanhMuc.SelectedValue = _selectedSanPhamDetails.IdDanhMuc ?? 0;
                cmbNhomIn.Text = _selectedSanPhamDetails.NhomIn;
                cmbTrangThai.SelectedIndex = _selectedSanPhamDetails.TrangThaiKinhDoanh ? 0 : 1;
                AnhSanPhamPreview.Source = HinhAnhHelper.LoadImageFromBase64(_currentAnhBiaBase64, HinhAnhPaths.DefaultFoodIcon);
                await LoadDinhLuongGridAsync(_selectedSanPhamDetails.IdSanPham);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải chi tiết sản phẩm: {ex.Message}", "Lỗi API");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnChonAnh_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog { Filter = "Image files|*.png;*.jpg;*.jpeg", Title = "Chọn ảnh sản phẩm" };
            if (ofd.ShowDialog() == true)
            {
                try
                {
                    byte[] imageBytes = File.ReadAllBytes(ofd.FileName);
                    _currentAnhBiaBase64 = Convert.ToBase64String(imageBytes);
                    AnhSanPhamPreview.Source = HinhAnhHelper.LoadImageFromBase64(_currentAnhBiaBase64, HinhAnhPaths.DefaultFoodIcon);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi đọc file ảnh: {ex.Message}", "Lỗi File");
                    _currentAnhBiaBase64 = null;
                    AnhSanPhamPreview.Source = HinhAnhHelper.LoadImageFromBase64(null, HinhAnhPaths.DefaultFoodIcon);
                }
            }
        }

        private async void BtnLuu_Click(object sender, RoutedEventArgs e)
        {
            if (string.IsNullOrWhiteSpace(txtTenSanPham.Text))
            {
                MessageBox.Show("Tên sản phẩm không được để trống.", "Lỗi"); return;
            }
            if (!decimal.TryParse(txtDonGia.Text, out decimal donGia) || donGia < 0)
            {
                MessageBox.Show("Đơn giá không hợp lệ.", "Lỗi"); return;
            }
            if (string.IsNullOrWhiteSpace(cmbDanhMuc.Text) || ((int?)cmbDanhMuc.SelectedValue ?? 0) == 0 && !(_danhMucList.Any(d => d.Ten.Equals(cmbDanhMuc.Text, StringComparison.OrdinalIgnoreCase))))
            {
                MessageBox.Show("Vui lòng chọn hoặc nhập một danh mục hợp lệ.", "Lỗi"); return;
            }

            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                int? danhMucId = await GetOrCreateLookupIdAsync(cmbDanhMuc.Text, _danhMucList, "api/app/sanpham/danhmuc", true);
                if (danhMucId == null)
                {
                    LoadingOverlay.Visibility = Visibility.Collapsed;
                    return;
                }

                var dto = new SanPhamUpdateRequestDto
                {
                    TenSanPham = txtTenSanPham.Text,
                    IdDanhMuc = danhMucId,
                    GiaBan = donGia,
                    MoTa = txtMoTa.Text,
                    // SỬA LỖI NullReferenceException
                    TrangThaiKinhDoanh = (cmbTrangThai.SelectedIndex == 0), // 0 = Đang bán, 1 = Tạm ngưng
                    NhomIn = cmbNhomIn.Text,
                    HinhAnhBase64 = _currentAnhBiaBase64
                };

                HttpResponseMessage response;
                bool isCreating = (_selectedSanPhamDetails == null);
                if (isCreating)
                {
                    response = await httpClient.PostAsJsonAsync("api/app/sanpham", dto);
                }
                else
                {
                    dto.IdSanPham = _selectedSanPhamDetails.IdSanPham;
                    response = await httpClient.PutAsJsonAsync($"api/app/sanpham/{dto.IdSanPham}", dto);
                }

                if (response.IsSuccessStatusCode)
                {
                    await LoadDataGridAsync();
                    await LoadFiltersAsync();

                    if (isCreating) // THÊM MỚI (Yêu cầu 1)
                    {
                        var newSanPham = await response.Content.ReadFromJsonAsync<SanPham>();
                        MessageBox.Show($"Sản phẩm '{newSanPham.TenSanPham}' đã được tạo.\n\nVUI LÒNG THÊM ĐỊNH LƯỢNG NGUYÊN LIỆU.", "Thêm Định Lượng");

                        var newItemInGrid = _allSanPhamList.FirstOrDefault(s => s.IdSanPham == newSanPham.IdSanPham);
                        dgSanPham.SelectedItem = newItemInGrid; // Tự động chọn

                        // DgSanPham_SelectionChanged sẽ tự động được gọi, tải chi tiết và định lượng

                        MainTabControl.SelectedIndex = 2; // Chuyển sang Tab 3 (Định lượng)
                    }
                    else // CẬP NHẬT
                    {
                        MessageBox.Show("Lưu thành công!", "Thông báo");
                        ResetForm();
                    }
                }
                else
                {
                    MessageBox.Show($"Lỗi: {await response.Content.ReadAsStringAsync()}", "Lỗi API");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối: {ex.Message}", "Lỗi API");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async void BtnXoa_Click(object sender, RoutedEventArgs e)
        {
            // (Giữ nguyên)
            if (_selectedSanPhamDetails == null) return;
            var result = MessageBox.Show($"Bạn có chắc chắn muốn xóa '{_selectedSanPhamDetails.TenSanPham}'?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.No) return;

            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var response = await httpClient.DeleteAsync($"api/app/sanpham/{_selectedSanPhamDetails.IdSanPham}");
                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Xóa thành công!", "Thông báo");
                    await LoadDataGridAsync();
                    ResetForm();
                }
                else
                {
                    MessageBox.Show($"Lỗi: {await response.Content.ReadAsStringAsync()}", "Lỗi API");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối: {ex.Message}", "Lỗi API");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private void BtnLamMoiForm_Click(object sender, RoutedEventArgs e)
        {
            ResetForm();
        }

        #endregion

        #region 3. Lọc / Tìm kiếm / Excel

        private async void Filter_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbFilterDanhMuc.IsLoaded && cmbFilterTrangThai.IsLoaded)
            {
                await LoadDataGridAsync();
            }
        }

        private async void TxtSearchSanPham_TextChanged(object sender, TextChangedEventArgs e)
        {
            await LoadDataGridAsync();
        }

        private async void BtnClearFilter_Click(object sender, RoutedEventArgs e)
        {
            txtSearchSanPham.Text = "";
            cmbFilterDanhMuc.SelectedValue = 0;
            cmbFilterTrangThai.SelectedIndex = 0;
            await LoadDataGridAsync();
        }

        private void BtnExportToExcel_Click(object sender, RoutedEventArgs e)
        {
            // (Giữ nguyên)
            var sfd = new SaveFileDialog { Filter = "Excel Files (*.xlsx)|*.xlsx", FileName = $"DSSP_{DateTime.Now:yyyyMMdd_HHmm}.xlsx" };
            if (sfd.ShowDialog() == true)
            {
                try
                {
                    ExcelPackage.License.SetNonCommercialPersonal("CafeBook");
                    using (var package = new ExcelPackage(new FileInfo(sfd.FileName)))
                    {
                        var ws = package.Workbook.Worksheets.Add("DanhSachSanPham");
                        ws.Cells["A1"].Value = "Danh sách Sản phẩm";
                        ws.Cells["A1:D1"].Merge = true;
                        ws.Cells["A1"].Style.Font.Bold = true;
                        ws.Cells["A1"].Style.Font.Size = 16;
                        ws.Cells["A3"].LoadFromCollection(_allSanPhamList, true, OfficeOpenXml.Table.TableStyles.Medium9);
                        ws.Column(3).Style.Numberformat.Format = "#,##0 \"đ\"";
                        ws.Cells[ws.Dimension.Address].AutoFitColumns();
                        package.Save();
                    }
                    MessageBox.Show("Xuất Excel thành công!", "Thông báo");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi xuất Excel: {ex.Message}", "Lỗi");
                }
            }
        }

        #endregion

        #region 4. Tab Quản lý Danh mục (CRUD Danh Mục)

        private void ResetDanhMucForm()
        {
            txtTenDanhMuc.Text = "";
            lbDanhMuc.SelectedItem = null;
        }

        private void LbDanhMuc_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lbDanhMuc.SelectedItem is FilterLookupDto selected)
            {
                txtTenDanhMuc.Text = selected.Ten;
            }
        }

        private async void BtnThemDanhMuc_Click(object sender, RoutedEventArgs e)
        {
            await CrudLookupAsync("api/app/sanpham/danhmuc", null, txtTenDanhMuc.Text, false, true);
        }

        private async void BtnLuuDanhMuc_Click(object sender, RoutedEventArgs e)
        {
            if (lbDanhMuc.SelectedItem is FilterLookupDto selected)
            {
                await CrudLookupAsync($"api/app/sanpham/danhmuc/{selected.Id}", selected.Id, txtTenDanhMuc.Text, false, true);
            }
        }

        private async void BtnXoaDanhMuc_Click(object sender, RoutedEventArgs e)
        {
            if (lbDanhMuc.SelectedItem is FilterLookupDto selected)
            {
                await CrudLookupAsync($"api/app/sanpham/danhmuc/{selected.Id}", selected.Id, null, true, true);
            }
        }

        #endregion

        #region 5. Tab Định Lượng

        private void ResetDinhLuongForm()
        {
            cmbNguyenLieu.SelectedValue = 0;
            txtSoLuongNL.Text = "0";
            cmbDonViTinhNL.ItemsSource = null;
        }

        // SỰ KIỆN MỚI: Lọc ĐVT theo Nguyên liệu
        private void CmbNguyenLieu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int selectedNLId = 0;

            if (cmbNguyenLieu.SelectedItem is FilterLookupDto selectedNL)
            {
                selectedNLId = selectedNL.Id;
            }
            // Xử lý khi người dùng gõ
            else if (!string.IsNullOrEmpty(cmbNguyenLieu.Text))
            {
                var matchedNL = _nguyenLieuList.FirstOrDefault(nl => nl.Ten.Equals(cmbNguyenLieu.Text, StringComparison.OrdinalIgnoreCase));
                if (matchedNL != null)
                {
                    selectedNLId = matchedNL.Id;
                }
            }

            if (selectedNLId > 0)
            {
                // Lọc danh sách ĐVT cho nguyên liệu này
                var dvtChoNL = _donViTinhList.Where(d => d.IdNguyenLieu == selectedNLId).ToList();
                cmbDonViTinhNL.ItemsSource = dvtChoNL;
                if (dvtChoNL.Any())
                {
                    cmbDonViTinhNL.SelectedIndex = 0; // Chọn cái đầu tiên
                }
            }
            else
            {
                cmbDonViTinhNL.ItemsSource = null;
            }
        }

        private void DgDinhLuong_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgDinhLuong.SelectedItem is DinhLuongDto selected)
            {
                cmbNguyenLieu.SelectedValue = selected.IdNguyenLieu;
                // CmbNguyenLieu_SelectionChanged sẽ tự động kích hoạt và lọc cmbDonViTinhNL
                cmbDonViTinhNL.SelectedValue = selected.IdDonViSuDung;
                txtSoLuongNL.Text = selected.SoLuong.ToString(CultureInfo.InvariantCulture);
            }
        }

        private async void BtnLuuDinhLuong_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedSanPhamDetails == null) return;
            if (cmbNguyenLieu.SelectedValue == null || (int)cmbNguyenLieu.SelectedValue == 0)
            {
                MessageBox.Show("Vui lòng chọn một nguyên liệu.", "Lỗi"); return;
            }
            if (cmbDonViTinhNL.SelectedValue == null)
            {
                MessageBox.Show("Vui lòng chọn đơn vị tính.", "Lỗi"); return;
            }
            if (!decimal.TryParse(txtSoLuongNL.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out decimal soLuong) || soLuong <= 0)
            {
                MessageBox.Show("Số lượng phải là số dương.", "Lỗi"); return;
            }

            var dto = new DinhLuongUpdateRequestDto
            {
                IdSanPham = _selectedSanPhamDetails.IdSanPham,
                IdNguyenLieu = (int)cmbNguyenLieu.SelectedValue,
                IdDonViSuDung = (int)cmbDonViTinhNL.SelectedValue, // Lấy ĐVT
                SoLuong = soLuong
            };

            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var response = await httpClient.PostAsJsonAsync("api/app/sanpham/dinhluong", dto);
                if (response.IsSuccessStatusCode)
                {
                    await LoadDinhLuongGridAsync(dto.IdSanPham); // Tải lại lưới
                    ResetDinhLuongForm();
                }
                else
                {
                    MessageBox.Show($"Lỗi: {await response.Content.ReadAsStringAsync()}", "Lỗi API");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối: {ex.Message}", "Lỗi API");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async void BtnXoaDinhLuong_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedSanPhamDetails == null) return;
            if (dgDinhLuong.SelectedItem is not DinhLuongDto selected)
            {
                MessageBox.Show("Vui lòng chọn một nguyên liệu từ danh sách để xóa.", "Lỗi"); return;
            }

            var result = MessageBox.Show($"Xóa '{selected.TenNguyenLieu}' khỏi công thức?", "Xác nhận", MessageBoxButton.YesNo);
            if (result == MessageBoxResult.No) return;

            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var response = await httpClient.DeleteAsync($"api/app/sanpham/dinhluong/{_selectedSanPhamDetails.IdSanPham}/{selected.IdNguyenLieu}");
                if (response.IsSuccessStatusCode)
                {
                    await LoadDinhLuongGridAsync(_selectedSanPhamDetails.IdSanPham);
                    ResetDinhLuongForm();
                }
                else
                {
                    MessageBox.Show($"Lỗi: {await response.Content.ReadAsStringAsync()}", "Lỗi API");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối: {ex.Message}", "Lỗi API");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        #endregion

        #region 6. Helper Functions (Smart ComboBox, CRUD)

        private async Task<int?> GetOrCreateLookupIdAsync(string tenDaNhap, List<FilterLookupDto> list, string apiEndpoint, bool isDanhMuc = false)
        {
            if (string.IsNullOrWhiteSpace(tenDaNhap) || tenDaNhap.StartsWith("--"))
            {
                if (isDanhMuc) return null; // Danh mục là bắt buộc
                return null;
            }

            var item = list.FirstOrDefault(x => x.Ten.Equals(tenDaNhap, StringComparison.OrdinalIgnoreCase));
            if (item != null)
            {
                return item.Id;
            }

            try
            {
                var response = await httpClient.PostAsJsonAsync(apiEndpoint, new FilterLookupDto { Ten = tenDaNhap });
                if (response.IsSuccessStatusCode)
                {
                    var newItem = await response.Content.ReadFromJsonAsync<FilterLookupDto>();
                    if (newItem != null)
                    {
                        if (isDanhMuc) _danhMucList.Add(newItem);
                        return newItem.Id;
                    }
                    return null;
                }
                else
                {
                    MessageBox.Show($"Không thể tự động thêm '{tenDaNhap}': {await response.Content.ReadAsStringAsync()}", "Lỗi Tạo Mới");
                    return null;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi API khi tạo mới: {ex.Message}", "Lỗi");
                return null;
            }
        }

        private async Task CrudLookupAsync(string endpoint, int? id, string? ten, bool isDelete = false, bool isDanhMuc = false)
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                HttpResponseMessage response;
                if (isDelete)
                {
                    if (MessageBox.Show("Bạn có chắc chắn muốn xóa?", "Xác nhận", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
                    {
                        LoadingOverlay.Visibility = Visibility.Collapsed;
                        return;
                    }
                    response = await httpClient.DeleteAsync(endpoint);
                }
                else if (string.IsNullOrWhiteSpace(ten))
                {
                    MessageBox.Show("Tên không được để trống.", "Lỗi");
                    LoadingOverlay.Visibility = Visibility.Collapsed;
                    return;
                }
                else if (id.HasValue && id > 0) // Update (PUT)
                {
                    response = await httpClient.PutAsJsonAsync(endpoint, new FilterLookupDto { Id = id.Value, Ten = ten });
                }
                else // Create (POST)
                {
                    response = await httpClient.PostAsJsonAsync(endpoint, new FilterLookupDto { Ten = ten });
                }

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Thao tác thành công!", "Thông báo");
                    await LoadFiltersAsync(); // Tải lại TOÀN BỘ
                    if (isDanhMuc) ResetDanhMucForm();
                }
                else
                {
                    MessageBox.Show($"Lỗi: {await response.Content.ReadAsStringAsync()}", "Lỗi API");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối: {ex.Message}", "Lỗi API");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        #endregion
    }
}