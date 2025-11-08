// Tập tin: AppCafebookApi/View/quanly/pages/QuanLySachView.xaml.cs
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
using CafebookModel.Model.Entities;
using System.Net.Http.Headers;

namespace AppCafebookApi.View.quanly.pages
{
    public partial class QuanLySachView : Page
    {
        private static readonly HttpClient httpClient;
        private List<SachDto> _allSachList = new List<SachDto>();
        private SachDetailDto? _selectedSachDetails = null;
        private string? _currentAnhBiaFilePath = null;
        private bool _deleteImageRequest = false;

        private List<FilterLookupDto> _theLoaiList = new List<FilterLookupDto>();
        private List<FilterLookupDto> _tacGiaList = new List<FilterLookupDto>();
        private List<FilterLookupDto> _nhaXuatBanList = new List<FilterLookupDto>();

        static QuanLySachView()
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://127.0.0.1:5166")
            };
        }

        public QuanLySachView()
        {
            InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadFiltersAsync();
            await LoadDataGridAsync();
            await LoadRentalsAsync(null, null);
            ResetForm();
        }

        #region 1. Tải Dữ Liệu
        private async Task LoadFiltersAsync()
        {
            try
            {
                var filters = await httpClient.GetFromJsonAsync<SachFiltersDto>("api/app/sach/filters");
                if (filters != null)
                {
                    _theLoaiList = filters.TheLoais;
                    _tacGiaList = filters.TacGias;
                    _nhaXuatBanList = filters.NhaXuatBans;

                    // Bộ lọc (Grid)
                    var filterTheLoai = new List<FilterLookupDto>(_theLoaiList);
                    filterTheLoai.Insert(0, new FilterLookupDto { Id = 0, Ten = "Tất cả Thể loại" });
                    cmbFilterTheLoai.ItemsSource = filterTheLoai;
                    if (cmbFilterTheLoai.SelectedValue == null) cmbFilterTheLoai.SelectedValue = 0;

                    // SỬA: Form (Cột phải) - Gán nguồn cho ComboBox
                    cmbAddTheLoai.ItemsSource = _theLoaiList;
                    cmbAddTacGia.ItemsSource = _tacGiaList;
                    cmbAddNXB.ItemsSource = _nhaXuatBanList;

                    // Tab Danh mục
                    lbTacGia.ItemsSource = _tacGiaList;
                    lbTheLoai.ItemsSource = _theLoaiList;
                    lbNhaXuatBan.ItemsSource = _nhaXuatBanList;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải bộ lọc: {ex.Message}", "Lỗi API");
            }
        }

        // (LoadDataGridAsync và LoadRentalsAsync giữ nguyên)
        private async Task LoadDataGridAsync()
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            string searchText = txtSearchSach.Text;
            int theLoaiId = (int)(cmbFilterTheLoai.SelectedValue ?? 0);
            try
            {
                var url = $"api/app/sach/search?searchText={Uri.EscapeDataString(searchText)}&theLoaiId={theLoaiId}";
                _allSachList = (await httpClient.GetFromJsonAsync<List<SachDto>>(url)) ?? new List<SachDto>();
                dgSach.ItemsSource = _allSachList;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải danh sách sách: {ex.Message}", "Lỗi API");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }
        private async Task LoadRentalsAsync(DateTime? fromDate, DateTime? toDate)
        {
            try
            {
                string url = "api/app/sach/rentals";
                var queryParams = new List<string>();
                if (fromDate.HasValue)
                    queryParams.Add($"fromDate={fromDate.Value:yyyy-MM-dd}");
                if (toDate.HasValue)
                    queryParams.Add($"toDate={toDate.Value:yyyy-MM-dd}");

                if (queryParams.Count > 0)
                    url += "?" + string.Join("&", queryParams);

                var rentalData = await httpClient.GetFromJsonAsync<SachRentalsDto>(url);

                if (rentalData != null)
                {
                    dgSachQuaHan.ItemsSource = rentalData.SachQuaHan;
                    dgLichSuThue.ItemsSource = rentalData.LichSuThue;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải lịch sử thuê sách: {ex.Message}", "Lỗi API");
            }
        }
        #endregion

        #region 2. Form Chi Tiết Sách
        private void ResetForm()
        {
            _selectedSachDetails = null;
            _currentAnhBiaFilePath = null;
            _deleteImageRequest = false;

            dgSach.SelectedItem = null;
            formChiTiet.IsEnabled = true;
            panelActions.Visibility = Visibility.Visible;
            btnXoa.Visibility = Visibility.Collapsed;
            lblFormTitle.Text = "Thêm Sách Mới";
            txtTenSach.Text = "";
            txtNamXuatBan.Text = "";
            txtSoLuongTong.Text = "1";
            txtMoTa.Text = "";
            txtGiaBia.Text = "0";
            txtViTri.Text = "";

            // SỬA: Xóa văn bản của TextBox
            txtTacGiaList.Text = string.Empty;
            txtTheLoaiList.Text = string.Empty;
            txtNXBList.Text = string.Empty;

            // SỬA: Xóa lựa chọn ComboBox
            cmbAddTacGia.SelectedItem = null;
            cmbAddTheLoai.SelectedItem = null;
            cmbAddNXB.SelectedItem = null;

            AnhBiaPreview.Source = HinhAnhHelper.LoadImage(null, HinhAnhPaths.DefaultBookCover);
        }

        private async void DgSach_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgSach.SelectedItem is not SachDto selected)
            {
                ResetForm();
                return;
            }
            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                _selectedSachDetails = await httpClient.GetFromJsonAsync<SachDetailDto>($"api/app/sach/details/{selected.IdSach}");

                if (_selectedSachDetails == null)
                {
                    ResetForm();
                    return;
                }

                _currentAnhBiaFilePath = null;
                _deleteImageRequest = false;

                formChiTiet.IsEnabled = true;
                panelActions.Visibility = Visibility.Visible;
                btnXoa.Visibility = Visibility.Visible;
                lblFormTitle.Text = "Cập nhật Sách";

                txtTenSach.Text = _selectedSachDetails.TenSach;
                txtNamXuatBan.Text = _selectedSachDetails.NamXuatBan?.ToString() ?? "";
                txtSoLuongTong.Text = _selectedSachDetails.SoLuongTong.ToString();
                txtMoTa.Text = _selectedSachDetails.MoTa;
                txtGiaBia.Text = _selectedSachDetails.GiaBia?.ToString("F0") ?? "0";
                txtViTri.Text = _selectedSachDetails.ViTri;

                // SỬA: Đặt văn bản cho TextBox dựa trên List<int>
                var tacGiaNames = _tacGiaList
                    .Where(t => _selectedSachDetails.IdTacGias.Contains(t.Id))
                    .Select(t => t.Ten);
                txtTacGiaList.Text = string.Join(", ", tacGiaNames);

                var theLoaiNames = _theLoaiList
                    .Where(t => _selectedSachDetails.IdTheLoais.Contains(t.Id))
                    .Select(t => t.Ten);
                txtTheLoaiList.Text = string.Join(", ", theLoaiNames);

                var nxbNames = _nhaXuatBanList
                    .Where(t => _selectedSachDetails.IdNhaXuatBans.Contains(t.Id))
                    .Select(t => t.Ten);
                txtNXBList.Text = string.Join(", ", nxbNames);

                // Xóa lựa chọn ComboBox
                cmbAddTacGia.SelectedItem = null;
                cmbAddTheLoai.SelectedItem = null;
                cmbAddNXB.SelectedItem = null;

                AnhBiaPreview.Source = HinhAnhHelper.LoadImage(_selectedSachDetails.AnhBiaUrl, HinhAnhPaths.DefaultBookCover);
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải chi tiết sách: {ex.Message}", "Lỗi API");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        // (BtnChonAnh_Click và BtnXoaAnh_Click giữ nguyên)
        private void BtnChonAnh_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog { Filter = "Image files|*.png;*.jpg;*.jpeg", Title = "Chọn ảnh bìa sách" };
            if (ofd.ShowDialog() == true)
            {
                try
                {
                    _currentAnhBiaFilePath = ofd.FileName;
                    _deleteImageRequest = false;
                    AnhBiaPreview.Source = HinhAnhHelper.LoadImage(_currentAnhBiaFilePath, HinhAnhPaths.DefaultBookCover);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi đọc file ảnh: {ex.Message}", "Lỗi File");
                    _currentAnhBiaFilePath = null;
                    AnhBiaPreview.Source = HinhAnhHelper.LoadImage(null, HinhAnhPaths.DefaultBookCover);
                }
            }
        }
        private void BtnXoaAnh_Click(object sender, RoutedEventArgs e)
        {
            _currentAnhBiaFilePath = null;
            _deleteImageRequest = true;
            AnhBiaPreview.Source = HinhAnhHelper.LoadImage(null, HinhAnhPaths.DefaultBookCover);
        }
        #endregion

        #region 3. Lọc / Form
        // (Các hàm trong Region này giữ nguyên)
        private async void TxtSearchSach_TextChanged(object sender, TextChangedEventArgs e)
        {
            await LoadDataGridAsync();
        }
        private async void CmbFilterTheLoai_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (cmbFilterTheLoai.IsLoaded)
            {
                await LoadDataGridAsync();
            }
        }
        private async void BtnClearFilter_Click(object sender, RoutedEventArgs e)
        {
            txtSearchSach.Text = "";
            cmbFilterTheLoai.SelectedValue = 0;
            await LoadDataGridAsync();
        }
        private void BtnLamMoiForm_Click(object sender, RoutedEventArgs e)
        {
            ResetForm();
        }
        #endregion

        #region 4. Lưu/Xóa Sách

        // SỬA: BtnLuu_Click (dùng logic "Smart TextBox")
        private async void BtnLuu_Click(object sender, RoutedEventArgs e)
        {
            // (Validation giữ nguyên)
            if (string.IsNullOrWhiteSpace(txtTenSach.Text))
            {
                MessageBox.Show("Tên sách không được để trống.", "Lỗi"); return;
            }
            if (!int.TryParse(txtSoLuongTong.Text, out int soLuong) || soLuong <= 0)
            {
                MessageBox.Show("Số lượng tổng phải là số dương.", "Lỗi"); return;
            }
            if (!decimal.TryParse(txtGiaBia.Text, out _))
            {
                MessageBox.Show("Giá bìa phải là số (hoặc để 0).", "Lỗi"); return;
            }
            if (!string.IsNullOrEmpty(txtNamXuatBan.Text) && !int.TryParse(txtNamXuatBan.Text, out _))
            {
                MessageBox.Show("Năm xuất bản phải là số (hoặc để trống).", "Lỗi"); return;
            }

            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                // SỬA: Xử lý chuỗi tên để lấy List<int>
                // Tự động tạo mới nếu tên không tồn tại
                var tacGiaIds = await ProcessNameListAsync(txtTacGiaList.Text, "api/app/sach/tacgia", _tacGiaList);
                var theLoaiIds = await ProcessNameListAsync(txtTheLoaiList.Text, "api/app/sach/theloai", _theLoaiList);
                var nxbIds = await ProcessNameListAsync(txtNXBList.Text, "api/app/sach/nhaxuatban", _nhaXuatBanList);

                // Nếu ProcessNameListAsync trả về null (do lỗi), dừng lại
                if (tacGiaIds == null || theLoaiIds == null || nxbIds == null)
                {
                    LoadingOverlay.Visibility = Visibility.Collapsed;
                    return;
                }

                using var form = new MultipartFormDataContent();

                // 1. Thêm các trường dữ liệu (Giữ nguyên)
                form.Add(new StringContent(txtTenSach.Text), "TenSach");
                if (int.TryParse(txtNamXuatBan.Text, out int nam))
                    form.Add(new StringContent(nam.ToString()), "NamXuatBan");
                form.Add(new StringContent(txtMoTa.Text ?? ""), "MoTa");
                form.Add(new StringContent(soLuong.ToString()), "SoLuongTong");
                if (decimal.TryParse(txtGiaBia.Text, out decimal gia))
                    form.Add(new StringContent(gia.ToString()), "GiaBia");
                form.Add(new StringContent(txtViTri.Text ?? ""), "ViTri");

                // SỬA: Thêm các List<int> đã được xử lý
                foreach (var id in tacGiaIds)
                {
                    form.Add(new StringContent(id.ToString()), "IdTacGias");
                }
                foreach (var id in theLoaiIds)
                {
                    form.Add(new StringContent(id.ToString()), "IdTheLoais");
                }
                foreach (var id in nxbIds)
                {
                    form.Add(new StringContent(id.ToString()), "IdNhaXuatBans");
                }

                bool isCreating = (_selectedSachDetails == null);

                // 2. Thêm file (nếu có)
                if (!string.IsNullOrEmpty(_currentAnhBiaFilePath))
                {
                    var fileStream = File.OpenRead(_currentAnhBiaFilePath);
                    var streamContent = new StreamContent(fileStream);
                    streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg");
                    form.Add(streamContent, "AnhBiaUpload", Path.GetFileName(_currentAnhBiaFilePath));
                }

                // 3. Thêm cờ Xóa ảnh (nếu có)
                if (_deleteImageRequest)
                {
                    form.Add(new StringContent("true"), "XoaAnhBia");
                }

                if (!isCreating)
                {
                    form.Add(new StringContent(_selectedSachDetails!.IdSach.ToString()), "IdSach");
                }

                HttpResponseMessage response;

                if (isCreating)
                {
                    response = await httpClient.PostAsync("api/app/sach", form);
                }
                else
                {
                    response = await httpClient.PutAsync($"api/app/sach/{_selectedSachDetails!.IdSach}", form);
                }

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Lưu thành công!", "Thông báo");

                    // SỬA: Tải lại Filters để cập nhật các mục vừa tạo mới
                    await LoadFiltersAsync();
                    await LoadDataGridAsync();

                    if (isCreating)
                    {
                        var newSach = await response.Content.ReadFromJsonAsync<SachDetailDto>();
                        if (newSach != null)
                        {
                            var newItemInGrid = _allSachList.FirstOrDefault(s => s.IdSach == newSach.IdSach);
                            dgSach.SelectedItem = newItemInGrid;
                        }
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
                _currentAnhBiaFilePath = null;
                _deleteImageRequest = false;
            }
        }

        // THÊM: Hàm Helper để xử lý logic "Smart TextBox"
        /// <summary>
        /// Xử lý chuỗi tên (cách nhau bằng dấu phẩy),
        /// tự động tạo mới nếu tên chưa có trong cache.
        /// </summary>
        /// <returns>Danh sách ID hoặc null nếu có lỗi.</returns>
        private async Task<List<int>?> ProcessNameListAsync(string commaSeparatedText, string createEndpoint, List<FilterLookupDto> localCacheList)
        {
            var ids = new List<int>();
            var nameList = commaSeparatedText.Split(',')
                                            .Select(s => s.Trim())
                                            .Where(s => !string.IsNullOrEmpty(s))
                                            .Distinct(StringComparer.OrdinalIgnoreCase) // Loại bỏ trùng lặp
                                            .ToList();

            foreach (var name in nameList)
            {
                // 1. Kiểm tra cache (danh sách có sẵn)
                var existing = localCacheList.FirstOrDefault(t => t.Ten.Equals(name, StringComparison.OrdinalIgnoreCase));

                if (existing != null)
                {
                    ids.Add(existing.Id);
                }
                else
                {
                    // 2. Tự động tạo mới nếu không tìm thấy
                    try
                    {
                        var dto = new FilterLookupDto { Ten = name, MoTa = null };
                        var response = await httpClient.PostAsJsonAsync(createEndpoint, dto);

                        if (response.IsSuccessStatusCode)
                        {
                            var newDto = await response.Content.ReadFromJsonAsync<FilterLookupDto>();
                            if (newDto != null)
                            {
                                ids.Add(newDto.Id);
                                localCacheList.Add(newDto); // Cập nhật cache để lần sau không tạo nữa
                            }
                        }
                        else
                        {
                            MessageBox.Show($"Lỗi khi tự động tạo '{name}': {await response.Content.ReadAsStringAsync()}", "Lỗi API");
                            return null; // Dừng lại nếu có lỗi
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Lỗi kết nối khi tạo '{name}': {ex.Message}", "Lỗi API");
                        return null; // Dừng lại nếu có lỗi
                    }
                }
            }
            return ids;
        }


        // (BtnXoa_Click giữ nguyên)
        private async void BtnXoa_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedSachDetails == null) return;
            var result = MessageBox.Show($"Bạn có chắc chắn muốn xóa sách '{_selectedSachDetails.TenSach}'?",
                                          "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning);
            if (result == MessageBoxResult.No) return;
            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var response = await httpClient.DeleteAsync($"api/app/sach/{_selectedSachDetails.IdSach}");
                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Xóa thành công!", "Thông báo");
                    await LoadDataGridAsync();
                    ResetForm();
                }
                else if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    MessageBox.Show($"Lỗi: {await response.Content.ReadAsStringAsync()}", "Không thể xóa");
                }
                else
                {
                    MessageBox.Show($"Lỗi không xác định: {response.ReasonPhrase}", "Lỗi API");
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

        #region 5. CRUD Danh mục (Giữ nguyên)

        // (Tất cả các hàm trong region 5 giữ nguyên)
        private void BtnShowTacGia_Click(object sender, RoutedEventArgs e)
        {
            borderTacGia.Visibility = Visibility.Visible;
            borderTheLoai.Visibility = Visibility.Collapsed;
            borderNXB.Visibility = Visibility.Collapsed;
        }

        private void BtnShowTheLoai_Click(object sender, RoutedEventArgs e)
        {
            borderTacGia.Visibility = Visibility.Collapsed;
            borderTheLoai.Visibility = Visibility.Visible;
            borderNXB.Visibility = Visibility.Collapsed;
        }

        private void BtnShowNXB_Click(object sender, RoutedEventArgs e)
        {
            borderTacGia.Visibility = Visibility.Collapsed;
            borderTheLoai.Visibility = Visibility.Collapsed;
            borderNXB.Visibility = Visibility.Visible;
        }

        private void LbTacGia_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lbTacGia.SelectedItem is FilterLookupDto selected)
            {
                txtTenTacGia.Text = selected.Ten;
                txtMoTaTacGia.Text = selected.MoTa;
            }
        }
        private async void BtnThemTacGia_Click(object sender, RoutedEventArgs e)
        {
            await CrudLookupAsync("api/app/sach/tacgia", null, txtTenTacGia.Text, txtMoTaTacGia.Text);
        }
        private async void BtnLuuTacGia_Click(object sender, RoutedEventArgs e)
        {
            if (lbTacGia.SelectedItem is FilterLookupDto selected)
            {
                await CrudLookupAsync($"api/app/sach/tacgia/{selected.Id}", selected.Id, txtTenTacGia.Text, txtMoTaTacGia.Text);
            }
        }
        private async void BtnXoaTacGia_Click(object sender, RoutedEventArgs e)
        {
            if (lbTacGia.SelectedItem is FilterLookupDto selected)
            {
                await CrudLookupAsync($"api/app/sach/tacgia/{selected.Id}", selected.Id, null, null, isDelete: true);
            }
        }

        private void LbTheLoai_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lbTheLoai.SelectedItem is FilterLookupDto selected)
            {
                txtTenTheLoai.Text = selected.Ten;
                txtMoTaTheLoai.Text = selected.MoTa;
            }
        }
        private async void BtnThemTheLoai_Click(object sender, RoutedEventArgs e)
        {
            await CrudLookupAsync("api/app/sach/theloai", null, txtTenTheLoai.Text, txtMoTaTheLoai.Text);
        }
        private async void BtnLuuTheLoai_Click(object sender, RoutedEventArgs e)
        {
            if (lbTheLoai.SelectedItem is FilterLookupDto selected)
            {
                await CrudLookupAsync($"api/app/sach/theloai/{selected.Id}", selected.Id, txtTenTheLoai.Text, txtMoTaTheLoai.Text);
            }
        }
        private async void BtnXoaTheLoai_Click(object sender, RoutedEventArgs e)
        {
            if (lbTheLoai.SelectedItem is FilterLookupDto selected)
            {
                await CrudLookupAsync($"api/app/sach/theloai/{selected.Id}", selected.Id, null, null, isDelete: true);
            }
        }

        private void LbNhaXuatBan_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lbNhaXuatBan.SelectedItem is FilterLookupDto selected)
            {
                txtTenNhaXuatBan.Text = selected.Ten;
                txtMoTaNXB.Text = selected.MoTa;
            }
        }
        private async void BtnThemNXB_Click(object sender, RoutedEventArgs e)
        {
            await CrudLookupAsync("api/app/sach/nhaxuatban", null, txtTenNhaXuatBan.Text, txtMoTaNXB.Text);
        }
        private async void BtnLuuNXB_Click(object sender, RoutedEventArgs e)
        {
            if (lbNhaXuatBan.SelectedItem is FilterLookupDto selected)
            {
                await CrudLookupAsync($"api/app/sach/nhaxuatban/{selected.Id}", selected.Id, txtTenNhaXuatBan.Text, txtMoTaNXB.Text);
            }
        }
        private async void BtnXoaNXB_Click(object sender, RoutedEventArgs e)
        {
            if (lbNhaXuatBan.SelectedItem is FilterLookupDto selected)
            {
                await CrudLookupAsync($"api/app/sach/nhaxuatban/{selected.Id}", selected.Id, null, null, isDelete: true);
            }
        }

        private async Task CrudLookupAsync(string endpoint, int? id, string? ten, string? moTa, bool isDelete = false)
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
                else
                {
                    var dto = new FilterLookupDto { Ten = ten };
                    if (id.HasValue)
                        dto.Id = id.Value;
                    if (moTa != null)
                        dto.MoTa = moTa;

                    if (id.HasValue) // Update (PUT)
                    {
                        response = await httpClient.PutAsJsonAsync(endpoint, dto);
                    }
                    else // Create (POST)
                    {
                        response = await httpClient.PostAsJsonAsync(endpoint, dto);
                    }
                }

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Thao tác thành công!", "Thông báo");
                    await LoadFiltersAsync(); // Tải lại TOÀN BỘ (cho cả 3 listbox)
                    ResetLookupForms();
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

        private void ResetLookupForms()
        {
            txtTenTacGia.Text = string.Empty;
            txtMoTaTacGia.Text = string.Empty;
            lbTacGia.SelectedItem = null;

            txtTenTheLoai.Text = string.Empty;
            txtMoTaTheLoai.Text = string.Empty;
            lbTheLoai.SelectedItem = null;

            txtTenNhaXuatBan.Text = string.Empty;
            txtMoTaNXB.Text = string.Empty;
            lbNhaXuatBan.SelectedItem = null;
        }
        #endregion

        #region 6. Navigation Buttons
        // (Giữ nguyên)
        private void BtnXemBaoCaoSach_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService?.Navigate(new BapCaoTonKhoSachPreviewWindow());
        }
        private void BtnCaiDatPhiThue_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService?.Navigate(new CaiDatWindow());
        }
        #endregion

        #region 7. Lịch sử & Trễ hạn Actions
        // (Giữ nguyên)
        private async void BtnLocNgay_Click(object sender, RoutedEventArgs e)
        {
            await LoadRentalsAsync(dpTuNgay.SelectedDate, dpDenNgay.SelectedDate);
        }

        private async void BtnLamMoiLichSu_Click(object sender, RoutedEventArgs e)
        {
            dpTuNgay.SelectedDate = null;
            dpDenNgay.SelectedDate = null;
            await LoadRentalsAsync(null, null);
        }

        private void BtnLienHe_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is BaoCaoSachTreHanDto item)
            {
                MessageBox.Show($"Đang liên hệ khách: {item.HoTen}\nSĐT: {item.SoDienThoai}", "Liên hệ");
            }
        }

        private void BtnGiaHan_Click(object sender, RoutedEventArgs e)
        {
            if ((sender as Button)?.Tag is BaoCaoSachTreHanDto item)
            {
                MessageBox.Show($"Mở cửa sổ gia hạn cho sách: {item.TenSach}\nKhách: {item.HoTen}", "Gia hạn");
            }
        }
        #endregion

        // THÊM: Region 8 cho logic ComboBox Helper
        #region 8. ComboBox Helper Logic

        /// <summary>
        /// Hàm chung để thêm item được chọn từ ComboBox vào TextBox
        /// </summary>
        private void AddItemToTextBox(TextBox targetTextBox, ComboBox sourceComboBox)
        {
            if (sourceComboBox.SelectedItem is not FilterLookupDto selected)
            {
                return;
            }

            var currentText = targetTextBox.Text;
            var currentNames = currentText.Split(',')
                                          .Select(s => s.Trim())
                                          .Where(s => !string.IsNullOrEmpty(s))
                                          .ToList();

            // Chỉ thêm nếu tên chưa có trong danh sách
            if (!currentNames.Contains(selected.Ten, StringComparer.OrdinalIgnoreCase))
            {
                currentNames.Add(selected.Ten);
                targetTextBox.Text = string.Join(", ", currentNames);
            }

            // Reset ComboBox để chờ chọn tiếp
            sourceComboBox.SelectedItem = null;
            sourceComboBox.Text = string.Empty;
        }

        private void CmbAddTacGia_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AddItemToTextBox(txtTacGiaList, cmbAddTacGia);
        }

        private void CmbAddTheLoai_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AddItemToTextBox(txtTheLoaiList, cmbAddTheLoai);
        }

        private void CmbAddNXB_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            AddItemToTextBox(txtNXBList, cmbAddNXB);
        }

        #endregion
    }
}