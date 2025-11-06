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
using System.IO; // <-- THÊM
using AppCafebookApi.Services;
using CafebookModel.Utils;
using AppCafebookApi.View.common;
using CafebookModel.Model.Entities;
using System.Net.Http.Headers; // <-- THÊM

namespace AppCafebookApi.View.quanly.pages
{
    public partial class QuanLySachView : Page
    {
        private static readonly HttpClient httpClient;
        private List<SachDto> _allSachList = new List<SachDto>();

        private SachDetailDto? _selectedSachDetails = null;

        // SỬA: Đã xóa Base64, dùng 2 biến này để quản lý file upload
        private string? _currentAnhBiaFilePath = null; // Lưu đường dẫn file cục bộ
        private bool _deleteImageRequest = false;     // Đánh dấu yêu cầu xóa ảnh

        // Cache
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
            await LoadRentalsAsync();
            ResetForm();
        }

        // ... (Hàm LoadFiltersAsync, LoadDataGridAsync, LoadRentalsAsync giữ nguyên) ...
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
                    var filterTheLoai = new List<FilterLookupDto>(_theLoaiList);
                    filterTheLoai.Insert(0, new FilterLookupDto { Id = 0, Ten = "Tất cả Thể loại" });
                    cmbFilterTheLoai.ItemsSource = filterTheLoai;
                    if (cmbFilterTheLoai.SelectedValue == null) cmbFilterTheLoai.SelectedValue = 0;
                    var formTheLoai = new List<FilterLookupDto>(_theLoaiList);
                    formTheLoai.Insert(0, new FilterLookupDto { Id = 0, Ten = "-- Chọn Thể loại --" });
                    cmbTheLoai.ItemsSource = formTheLoai;
                    var formTacGia = new List<FilterLookupDto>(_tacGiaList);
                    formTacGia.Insert(0, new FilterLookupDto { Id = 0, Ten = "-- Chọn Tác giả --" });
                    cmbTacGia.ItemsSource = formTacGia;
                    var formNXB = new List<FilterLookupDto>(_nhaXuatBanList);
                    formNXB.Insert(0, new FilterLookupDto { Id = 0, Ten = "-- Chọn NXB --" });
                    cmbNhaXuatBan.ItemsSource = formNXB;
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

        private async Task LoadRentalsAsync()
        {
            try
            {
                var rentalData = await httpClient.GetFromJsonAsync<SachRentalsDto>("api/app/sach/rentals");
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

            // SỬA: Đặt lại 2 biến quản lý file
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
            cmbTacGia.SelectedValue = 0;
            cmbTacGia.Text = string.Empty;
            cmbTheLoai.SelectedValue = 0;
            cmbTheLoai.Text = string.Empty;
            cmbNhaXuatBan.SelectedValue = 0;
            cmbNhaXuatBan.Text = string.Empty;
            txtGiaBia.Text = "0";
            txtViTri.Text = "";

            // SỬA: Dùng HinhAnhHelper.LoadImage (với nguồn null)
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
                //MessageBox.Show($"URL nhận được từ API:\n{_selectedSachDetails.AnhBiaUrl}", "DEBUG URL");
                // SỬA: Đặt lại 2 biến quản lý file
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
                cmbTacGia.SelectedValue = _selectedSachDetails.IdTacGia ?? 0;
                cmbTheLoai.SelectedValue = _selectedSachDetails.IdTheLoai ?? 0;
                cmbNhaXuatBan.SelectedValue = _selectedSachDetails.IdNhaXuatBan ?? 0;
                txtGiaBia.Text = _selectedSachDetails.GiaBia?.ToString("F0") ?? "0";
                txtViTri.Text = _selectedSachDetails.ViTri;

                // SỬA: Dùng HinhAnhHelper.LoadImage với URL
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

        private void BtnChonAnh_Click(object sender, RoutedEventArgs e)
        {
            var ofd = new OpenFileDialog { Filter = "Image files|*.png;*.jpg;*.jpeg", Title = "Chọn ảnh bìa sách" };
            if (ofd.ShowDialog() == true)
            {
                try
                {
                    // SỬA: Chỉ lưu đường dẫn file
                    _currentAnhBiaFilePath = ofd.FileName;
                    _deleteImageRequest = false; // Đã chọn file mới

                    // SỬA: Dùng HinhAnhHelper.LoadImage (tự nhận diện đường dẫn file)
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

        // THÊM: Nút Xóa Ảnh
        private void BtnXoaAnh_Click(object sender, RoutedEventArgs e)
        {
            _currentAnhBiaFilePath = null; // Xóa đường dẫn file
            _deleteImageRequest = true;   // Đánh dấu yêu cầu xóa
            AnhBiaPreview.Source = HinhAnhHelper.LoadImage(null, HinhAnhPaths.DefaultBookCover); // Hiển thị ảnh mặc định
        }
        #endregion

        #region 3. Lọc / Form
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

        // SỬA: VIẾT LẠI HOÀN TOÀN HÀM LƯU
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
                // (Helper GetOrCreateLookupIdAsync giữ nguyên)
                int? tacGiaId = await GetOrCreateLookupIdAsync(cmbTacGia.Text, _tacGiaList, "api/app/sach/tacgia");
                int? theLoaiId = await GetOrCreateLookupIdAsync(cmbTheLoai.Text, _theLoaiList, "api/app/sach/theloai");
                int? nhaXuatBanId = await GetOrCreateLookupIdAsync(cmbNhaXuatBan.Text, _nhaXuatBanList, "api/app/sach/nhaxuatban");

                // SỬA: Dùng MultipartFormDataContent thay vì DTO
                using var form = new MultipartFormDataContent();

                // 1. Thêm các trường dữ liệu (tên phải khớp với DTO)
                form.Add(new StringContent(txtTenSach.Text), "TenSach");
                if (theLoaiId.HasValue)
                    form.Add(new StringContent(theLoaiId.Value.ToString()), "IdTheLoai");
                if (tacGiaId.HasValue)
                    form.Add(new StringContent(tacGiaId.Value.ToString()), "IdTacGia");
                if (nhaXuatBanId.HasValue)
                    form.Add(new StringContent(nhaXuatBanId.Value.ToString()), "IdNhaXuatBan");

                if (int.TryParse(txtNamXuatBan.Text, out int nam))
                    form.Add(new StringContent(nam.ToString()), "NamXuatBan");

                form.Add(new StringContent(txtMoTa.Text ?? ""), "MoTa");
                form.Add(new StringContent(soLuong.ToString()), "SoLuongTong");

                if (decimal.TryParse(txtGiaBia.Text, out decimal gia))
                    form.Add(new StringContent(gia.ToString()), "GiaBia");

                form.Add(new StringContent(txtViTri.Text ?? ""), "ViTri");

                bool isCreating = (_selectedSachDetails == null);

                // 2. Thêm file (nếu có)
                if (!string.IsNullOrEmpty(_currentAnhBiaFilePath))
                {
                    var fileStream = File.OpenRead(_currentAnhBiaFilePath);
                    var streamContent = new StreamContent(fileStream);
                    streamContent.Headers.ContentType = new MediaTypeHeaderValue("image/jpeg"); // Hoặc image/png
                    // Tên "AnhBiaUpload" phải khớp với tham số ở Controller
                    form.Add(streamContent, "AnhBiaUpload", Path.GetFileName(_currentAnhBiaFilePath));
                }

                // 3. Thêm cờ Xóa ảnh (nếu có)
                if (_deleteImageRequest)
                {
                    // Tên "XoaAnhBia" phải khớp với tham số ở Controller
                    form.Add(new StringContent("true"), "XoaAnhBia");
                }

                // Gán Id nếu là Update
                if (!isCreating)
                {
                    // === SỬA LỖI CS8602 (Dòng 351) ===
                    // Thêm '!' vì logic !isCreating đảm bảo _selectedSachDetails không null
                    form.Add(new StringContent(_selectedSachDetails!.IdSach.ToString()), "IdSach");
                }

                HttpResponseMessage response;

                if (isCreating)
                {
                    response = await httpClient.PostAsync("api/app/sach", form);
                }
                else
                {
                    // === SỬA LỖI CS8602 (Dòng 362) ===
                    // Thêm '!' (lý do tương tự)
                    response = await httpClient.PutAsync($"api/app/sach/{_selectedSachDetails!.IdSach}", form);
                }

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Lưu thành công!", "Thông báo");
                    await LoadFiltersAsync();
                    await LoadDataGridAsync();

                    if (isCreating)
                    {
                        var newSach = await response.Content.ReadFromJsonAsync<SachDetailDto>();

                        // === SỬA LỖI CS8602 (Dòng 374) ===
                        // Thêm kiểm tra null sau khi deserialize
                        if (newSach == null)
                        {
                            MessageBox.Show("Lỗi: API đã tạo thành công nhưng không trả về dữ liệu.", "Lỗi Phản Hồi API");
                            return; // Thoát sớm
                        }

                        var newItemInGrid = _allSachList.FirstOrDefault(s => s.IdSach == newSach.IdSach);
                        dgSach.SelectedItem = newItemInGrid; // Tự động chọn
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
                // SỬA: Đặt lại cache file
                _currentAnhBiaFilePath = null;
                _deleteImageRequest = false;
            }
        }

        // (Hàm GetOrCreateLookupIdAsync giữ nguyên)
        private async Task<int?> GetOrCreateLookupIdAsync(string tenDaNhap, List<FilterLookupDto> list, string apiEndpoint)
        {
            if (string.IsNullOrWhiteSpace(tenDaNhap) || tenDaNhap.StartsWith("--"))
            {
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
                    return newItem?.Id;
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

        // (Hàm BtnXoa_Click giữ nguyên)
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

        #region 5. CRUD Danh mục (Tác giả, Thể loại, NXB)
        // (Toàn bộ các hàm trong region này giữ nguyên)
        private void LbTacGia_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lbTacGia.SelectedItem is FilterLookupDto selected)
            {
                txtTenTacGia.Text = selected.Ten;
            }
        }
        private async void BtnThemTacGia_Click(object sender, RoutedEventArgs e)
        {
            await CrudLookupAsync("api/app/sach/tacgia", null, txtTenTacGia.Text);
        }
        private async void BtnLuuTacGia_Click(object sender, RoutedEventArgs e)
        {
            if (lbTacGia.SelectedItem is FilterLookupDto selected)
            {
                await CrudLookupAsync($"api/app/sach/tacgia/{selected.Id}", selected.Id, txtTenTacGia.Text);
            }
        }
        private async void BtnXoaTacGia_Click(object sender, RoutedEventArgs e)
        {
            if (lbTacGia.SelectedItem is FilterLookupDto selected)
            {
                await CrudLookupAsync($"api/app/sach/tacgia/{selected.Id}", selected.Id, null, isDelete: true);
            }
        }
        private void LbTheLoai_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lbTheLoai.SelectedItem is FilterLookupDto selected)
            {
                txtTenTheLoai.Text = selected.Ten;
            }
        }
        private async void BtnThemTheLoai_Click(object sender, RoutedEventArgs e)
        {
            await CrudLookupAsync("api/app/sach/theloai", null, txtTenTheLoai.Text);
        }
        private async void BtnLuuTheLoai_Click(object sender, RoutedEventArgs e)
        {
            if (lbTheLoai.SelectedItem is FilterLookupDto selected)
            {
                await CrudLookupAsync($"api/app/sach/theloai/{selected.Id}", selected.Id, txtTenTheLoai.Text);
            }
        }
        private async void BtnXoaTheLoai_Click(object sender, RoutedEventArgs e)
        {
            if (lbTheLoai.SelectedItem is FilterLookupDto selected)
            {
                await CrudLookupAsync($"api/app/sach/theloai/{selected.Id}", selected.Id, null, isDelete: true);
            }
        }
        private void LbNhaXuatBan_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (lbNhaXuatBan.SelectedItem is FilterLookupDto selected)
            {
                txtTenNhaXuatBan.Text = selected.Ten;
            }
        }
        private async void BtnThemNXB_Click(object sender, RoutedEventArgs e)
        {
            await CrudLookupAsync("api/app/sach/nhaxuatban", null, txtTenNhaXuatBan.Text);
        }
        private async void BtnLuuNXB_Click(object sender, RoutedEventArgs e)
        {
            if (lbNhaXuatBan.SelectedItem is FilterLookupDto selected)
            {
                await CrudLookupAsync($"api/app/sach/nhaxuatban/{selected.Id}", selected.Id, txtTenNhaXuatBan.Text);
            }
        }
        private async void BtnXoaNXB_Click(object sender, RoutedEventArgs e)
        {
            if (lbNhaXuatBan.SelectedItem is FilterLookupDto selected)
            {
                await CrudLookupAsync($"api/app/sach/nhaxuatban/{selected.Id}", selected.Id, null, isDelete: true);
            }
        }
        private async Task CrudLookupAsync(string endpoint, int? id, string? ten, bool isDelete = false)
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
                else if (id.HasValue) // Update (PUT)
                {
                    response = await httpClient.PutAsJsonAsync(endpoint, new FilterLookupDto { Id = id.Value, Ten = ten ?? "" });
                }
                else // Create (POST)
                {
                    response = await httpClient.PostAsJsonAsync(endpoint, new FilterLookupDto { Ten = ten ?? "" });
                }

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Thao tác thành công!", "Thông báo");
                    await LoadFiltersAsync(); // Tải lại TOÀN BỘ
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
            lbTacGia.SelectedItem = null;
            txtTenTheLoai.Text = string.Empty;
            lbTheLoai.SelectedItem = null;
            txtTenNhaXuatBan.Text = string.Empty;
            lbNhaXuatBan.SelectedItem = null;
        }
        #endregion

        #region 6. Navigation Buttons
        // (Toàn bộ các hàm trong region này giữ nguyên)
        private void BtnXemBaoCaoSach_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService?.Navigate(new BapCaoTonKhoSachPreviewWindow());
        }
        private void BtnCaiDatPhiThue_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService?.Navigate(new CaiDatWindow());
        }
        #endregion
    }
}