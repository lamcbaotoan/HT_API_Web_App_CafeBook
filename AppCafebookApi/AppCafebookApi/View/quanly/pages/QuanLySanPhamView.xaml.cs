// Tập tin: AppCafebookApi/View/quanly/pages/QuanLySanPhamView.xaml.cs
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
using System.IO; // <-- Thêm
using AppCafebookApi.Services;
using CafebookModel.Utils;
using AppCafebookApi.View.common;
using System.Globalization;
using OfficeOpenXml;
using CafebookModel.Model.Entities;
using System.Net.Http.Headers; // <-- Thêm

namespace AppCafebookApi.View.quanly.pages
{
    public partial class QuanLySanPhamView : Page
    {
        private static readonly HttpClient httpClient;
        private List<SanPhamDto> _allSanPhamList = new List<SanPhamDto>();

        private SanPhamDetailDto? _selectedSanPhamDetails = null;

        // SỬA: Đã xóa Base64, dùng 2 biến này để quản lý file upload
        private string? _currentAnhBiaFilePath = null; // Lưu đường dẫn file cục bộ
        private bool _deleteImageRequest = false;     // Đánh dấu yêu cầu xóa ảnh

        // Cache (Giữ nguyên)
        private List<FilterLookupDto> _danhMucList = new List<FilterLookupDto>();
        private List<FilterLookupDto> _nguyenLieuList = new List<FilterLookupDto>();
        private List<DonViChuyenDoiDto> _donViTinhList = new List<DonViChuyenDoiDto>();

        static QuanLySanPhamView()
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://127.0.0.1:5166")
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

        // (Hàm LoadFiltersAsync giữ nguyên)
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
                    var filterDanhMuc = new List<FilterLookupDto>(_danhMucList);
                    filterDanhMuc.Insert(0, new FilterLookupDto { Id = 0, Ten = "Tất cả Danh mục" });
                    cmbFilterDanhMuc.ItemsSource = filterDanhMuc;
                    if (cmbFilterDanhMuc.SelectedValue == null) cmbFilterDanhMuc.SelectedValue = 0;
                    if (cmbFilterTrangThai.SelectedValue == null) cmbFilterTrangThai.SelectedIndex = 0;
                    lbDanhMuc.ItemsSource = _danhMucList;
                    var nlList = new List<FilterLookupDto>(_nguyenLieuList);
                    nlList.Insert(0, new FilterLookupDto { Id = 0, Ten = "-- Chọn Nguyên Liệu --" });
                    cmbNguyenLieu.ItemsSource = nlList;
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

        // (Hàm LoadDataGridAsync giữ nguyên)
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

        // (Hàm LoadDinhLuongGridAsync giữ nguyên)
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

            // SỬA: Đặt lại 2 biến quản lý file
            _currentAnhBiaFilePath = null;
            _deleteImageRequest = false;

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

            // SỬA: Dùng HinhAnhHelper.LoadImage (với nguồn null)
            AnhSanPhamPreview.Source = HinhAnhHelper.LoadImage(null, HinhAnhPaths.DefaultFoodIcon);

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
                _selectedSanPhamDetails = await httpClient.GetFromJsonAsync<SanPhamDetailDto>($"api/app/sanpham/details/{selected.IdSanPham}");

                if (_selectedSanPhamDetails == null)
                {
                    ResetForm();
                    return;
                }

                // SỬA: Đặt lại 2 biến quản lý file
                _currentAnhBiaFilePath = null;
                _deleteImageRequest = false;

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

                // SỬA: Dùng HinhAnhHelper.LoadImage với URL
                AnhSanPhamPreview.Source = HinhAnhHelper.LoadImage(_selectedSanPhamDetails.HinhAnhUrl, HinhAnhPaths.DefaultFoodIcon);
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
                    // SỬA: Chỉ lưu đường dẫn file và đánh dấu
                    _currentAnhBiaFilePath = ofd.FileName;
                    _deleteImageRequest = false; // Đã chọn file mới, không xóa

                    // SỬA: Dùng HinhAnhHelper.LoadImage (nó tự nhận diện đường dẫn file)
                    AnhSanPhamPreview.Source = HinhAnhHelper.LoadImage(_currentAnhBiaFilePath, HinhAnhPaths.DefaultFoodIcon);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi đọc file ảnh: {ex.Message}", "Lỗi File");
                    _currentAnhBiaFilePath = null;
                    AnhSanPhamPreview.Source = HinhAnhHelper.LoadImage(null, HinhAnhPaths.DefaultFoodIcon);
                }
            }
        }

        // THÊM: Nút Xóa Ảnh
        private void BtnXoaAnh_Click(object sender, RoutedEventArgs e)
        {
            _currentAnhBiaFilePath = null; // Xóa đường dẫn file
            _deleteImageRequest = true;   // Đánh dấu yêu cầu xóa
            AnhSanPhamPreview.Source = HinhAnhHelper.LoadImage(null, HinhAnhPaths.DefaultFoodIcon); // Hiển thị ảnh mặc định
        }

        // SỬA: VIẾT LẠI HOÀN TOÀN HÀM LƯU
        private async void BtnLuu_Click(object sender, RoutedEventArgs e)
        {
            // (Phần validation giữ nguyên)
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

                // SỬA: Dùng MultipartFormDataContent thay vì DTO
                using var form = new MultipartFormDataContent();

                // 1. Thêm các trường dữ liệu (giống tên thuộc tính DTO)
                form.Add(new StringContent(txtTenSanPham.Text), "TenSanPham");
                if (danhMucId.HasValue)
                    form.Add(new StringContent(danhMucId.Value.ToString()), "IdDanhMuc");
                form.Add(new StringContent(donGia.ToString()), "GiaBan");
                form.Add(new StringContent(txtMoTa.Text ?? ""), "MoTa");
                form.Add(new StringContent((cmbTrangThai.SelectedIndex == 0).ToString()), "TrangThaiKinhDoanh");
                form.Add(new StringContent(cmbNhomIn.Text ?? ""), "NhomIn");

                bool isCreating = (_selectedSanPhamDetails == null);

                // 2. Thêm file (nếu có)
                if (!string.IsNullOrEmpty(_currentAnhBiaFilePath))
                {
                    var fileStream = File.OpenRead(_currentAnhBiaFilePath);
                    var streamContent = new StreamContent(fileStream);
                    streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg"); // Hoặc "image/png"
                    form.Add(streamContent, "HinhAnhUpload", Path.GetFileName(_currentAnhBiaFilePath));
                }

                // 3. Thêm cờ Xóa ảnh (nếu có)
                if (_deleteImageRequest)
                {
                    form.Add(new StringContent("true"), "XoaHinhAnh");
                }

                // Gán Id nếu là Update
                if (!isCreating)
                {
                    // === SỬA LỖI CS8602 (Dòng 316) ===
                    // Thêm '!' vì logic !isCreating đảm bảo _selectedSanPhamDetails không null
                    form.Add(new StringContent(_selectedSanPhamDetails!.IdSanPham.ToString()), "IdSanPham");
                }


                HttpResponseMessage response;

                if (isCreating)
                {
                    response = await httpClient.PostAsync("api/app/sanpham", form);
                }
                else
                {
                    // === SỬA LỖI CS8602 (Dòng 328) ===
                    // Thêm '!' (lý do tương tự)
                    response = await httpClient.PutAsync($"api/app/sanpham/{_selectedSanPhamDetails!.IdSanPham}", form);
                }

                if (response.IsSuccessStatusCode)
                {
                    await LoadDataGridAsync();
                    await LoadFiltersAsync();

                    if (isCreating)
                    {
                        var newSanPham = await response.Content.ReadFromJsonAsync<SanPhamDetailDto>();

                        // === SỬA LỖI CS8602 (Dòng 339) ===
                        // Thêm kiểm tra null sau khi deserialize
                        if (newSanPham == null)
                        {
                            MessageBox.Show("Lỗi: API đã tạo thành công nhưng không trả về dữ liệu.", "Lỗi Phản Hồi API");
                            return; // Thoát sớm
                        }

                        MessageBox.Show($"Sản phẩm '{newSanPham.TenSanPham}' đã được tạo.\n\nVUI LÒNG THÊM ĐỊNH LƯỢNG NGUYÊN LIỆU.", "Thêm Định Lượng");

                        var newItemInGrid = _allSanPhamList.FirstOrDefault(s => s.IdSanPham == newSanPham.IdSanPham);
                        dgSanPham.SelectedItem = newItemInGrid;
                        MainTabControl.SelectedIndex = 2;
                    }
                    else // CẬP NHẬT
                    {
                        MessageBox.Show("Lưu thành công!", "Thông báo");
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
                // SỬA: Đặt lại 2 biến quản lý file
                _currentAnhBiaFilePath = null;
                _deleteImageRequest = false;
            }
        }

        private async void BtnXoa_Click(object sender, RoutedEventArgs e)
        {
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
        // ... (BtnXoa_Click, BtnLamMoiForm giữ nguyên) ...
        #endregion

        // ... (Toàn bộ các Region 3, 4, 5, 6 giữ nguyên) ...
        // (Các hàm Tab Danh Mục và Tab Định Lượng giữ nguyên)
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
        private void CmbNguyenLieu_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            int selectedNLId = 0;
            if (cmbNguyenLieu.SelectedItem is FilterLookupDto selectedNL)
            {
                selectedNLId = selectedNL.Id;
            }
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
                var dvtChoNL = _donViTinhList.Where(d => d.IdNguyenLieu == selectedNLId).ToList();
                cmbDonViTinhNL.ItemsSource = dvtChoNL;
                if (dvtChoNL.Any())
                {
                    cmbDonViTinhNL.SelectedIndex = 0;
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
                IdDonViSuDung = (int)cmbDonViTinhNL.SelectedValue,
                SoLuong = soLuong
            };
            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var response = await httpClient.PostAsJsonAsync("api/app/sanpham/dinhluong", dto);
                if (response.IsSuccessStatusCode)
                {
                    await LoadDinhLuongGridAsync(dto.IdSanPham);
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
                if (isDanhMuc) return null;
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