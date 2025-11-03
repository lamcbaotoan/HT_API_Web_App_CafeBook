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

namespace AppCafebookApi.View.quanly.pages
{
    public partial class QuanLySachView : Page
    {
        private static readonly HttpClient httpClient;
        private List<SachDto> _allSachList = new List<SachDto>();
        private SachUpdateRequestDto? _selectedSachDetails = null;
        private string? _currentAnhBiaBase64 = null;

        // --- THÊM MỚI: Cache cho các ComboBox và ListBox ---
        private List<FilterLookupDto> _theLoaiList = new List<FilterLookupDto>();
        private List<FilterLookupDto> _tacGiaList = new List<FilterLookupDto>();
        private List<FilterLookupDto> _nhaXuatBanList = new List<FilterLookupDto>();
        // ---

        static QuanLySachView()
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5166")
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

        /// <summary>
        /// Tải dữ liệu cho các ComboBox Lọc VÀ Form VÀ Tab Quản lý
        /// </summary>
        private async Task LoadFiltersAsync()
        {
            try
            {
                var filters = await httpClient.GetFromJsonAsync<SachFiltersDto>("api/app/sach/filters");
                if (filters != null)
                {
                    // 1. Cache dữ liệu
                    _theLoaiList = filters.TheLoais;
                    _tacGiaList = filters.TacGias;
                    _nhaXuatBanList = filters.NhaXuatBans;

                    // 2. Filter Thể loại (cho DataGrid)
                    var filterTheLoai = new List<FilterLookupDto>(_theLoaiList);
                    filterTheLoai.Insert(0, new FilterLookupDto { Id = 0, Ten = "Tất cả Thể loại" });
                    cmbFilterTheLoai.ItemsSource = filterTheLoai;
                    if (cmbFilterTheLoai.SelectedValue == null) cmbFilterTheLoai.SelectedValue = 0;

                    // 3. ComboBox Thể loại (cho Form) - Tạo bản copy
                    var formTheLoai = new List<FilterLookupDto>(_theLoaiList);
                    formTheLoai.Insert(0, new FilterLookupDto { Id = 0, Ten = "-- Chọn Thể loại --" });
                    cmbTheLoai.ItemsSource = formTheLoai;

                    // 4. ComboBox Tác giả (cho Form) - Tạo bản copy
                    var formTacGia = new List<FilterLookupDto>(_tacGiaList);
                    formTacGia.Insert(0, new FilterLookupDto { Id = 0, Ten = "-- Chọn Tác giả --" });
                    cmbTacGia.ItemsSource = formTacGia;

                    // 5. ComboBox NXB (cho Form) - Tạo bản copy
                    var formNXB = new List<FilterLookupDto>(_nhaXuatBanList);
                    formNXB.Insert(0, new FilterLookupDto { Id = 0, Ten = "-- Chọn NXB --" });
                    cmbNhaXuatBan.ItemsSource = formNXB;

                    // 6. ListBoxes (cho Tab Quản lý Danh mục) - Dùng list gốc
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

        /// <summary>
        /// Tải DataGrid (có áp dụng lọc)
        /// </summary>
        private async Task LoadDataGridAsync()
        {
            // (Giữ nguyên hàm này)
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

        /// <summary>
        /// Tải dữ liệu cho Tab 2 (Lịch sử)
        /// </summary>
        private async Task LoadRentalsAsync()
        {
            // (Sửa lỗi 'HoTen')
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

        /// <summary>
        /// Đưa Form về trạng thái thêm mới
        /// </summary>
        private void ResetForm()
        {
            // (Cập nhật để thêm txtGiaBia và txtViTri)
            _selectedSachDetails = null;
            _currentAnhBiaBase64 = null;

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

            AnhBiaPreview.Source = HinhAnhHelper.LoadImageFromBase64(null, HinhAnhPaths.DefaultBookCover);
        }

        /// <summary>
        /// Fill Form khi chọn một dòng
        /// </summary>
        private async void DgSach_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            // (Cập nhật để thêm txtGiaBia và txtViTri)
            if (dgSach.SelectedItem is not SachDto selected)
            {
                ResetForm();
                return;
            }
            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                _selectedSachDetails = await httpClient.GetFromJsonAsync<SachUpdateRequestDto>($"api/app/sach/details/{selected.IdSach}");
                if (_selectedSachDetails == null)
                {
                    ResetForm();
                    return;
                }

                _currentAnhBiaBase64 = _selectedSachDetails.AnhBiaBase64;

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

                AnhBiaPreview.Source = HinhAnhHelper.LoadImageFromBase64(_currentAnhBiaBase64, HinhAnhPaths.DefaultBookCover);
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

        /// <summary>
        /// Xử lý nút "Chọn ảnh"
        /// </summary>
        private void BtnChonAnh_Click(object sender, RoutedEventArgs e)
        {
            // (Giữ nguyên)
            var ofd = new OpenFileDialog
            {
                Filter = "Image files|*.png;*.jpg;*.jpeg",
                Title = "Chọn ảnh bìa sách"
            };

            if (ofd.ShowDialog() == true)
            {
                try
                {
                    byte[] imageBytes = File.ReadAllBytes(ofd.FileName);
                    _currentAnhBiaBase64 = Convert.ToBase64String(imageBytes);
                    AnhBiaPreview.Source = HinhAnhHelper.LoadImageFromBase64(_currentAnhBiaBase64, HinhAnhPaths.DefaultBookCover);
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi đọc file ảnh: {ex.Message}", "Lỗi File");
                    _currentAnhBiaBase64 = null;
                    AnhBiaPreview.Source = HinhAnhHelper.LoadImageFromBase64(null, HinhAnhPaths.DefaultBookCover);
                }
            }
        }

        // --- CÁC HÀM XỬ LÝ LỌC VÀ FORM ---
        #region Xử lý Lọc và Form
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

        // --- HÀM LOGIC CHÍNH CHO VIỆC LƯU/XÓA SÁCH ---
        #region Lưu/Xóa Sách (Logic Smart ComboBox)

        /// <summary>
        /// Xử lý nút "Lưu" (Thêm mới hoặc Cập nhật)
        /// </summary>
        private async void BtnLuu_Click(object sender, RoutedEventArgs e)
        {
            // (Cập nhật để thêm validation cho txtGiaBia)
            if (string.IsNullOrWhiteSpace(txtTenSach.Text))
            {
                MessageBox.Show("Tên sách không được để trống.", "Lỗi");
                return;
            }
            if (!int.TryParse(txtSoLuongTong.Text, out int soLuong) || soLuong <= 0)
            {
                MessageBox.Show("Số lượng tổng phải là số dương.", "Lỗi");
                return;
            }
            if (!decimal.TryParse(txtGiaBia.Text, out _))
            {
                MessageBox.Show("Giá bìa phải là số (hoặc để 0).", "Lỗi");
                return;
            }
            if (!string.IsNullOrEmpty(txtNamXuatBan.Text) && !int.TryParse(txtNamXuatBan.Text, out _))
            {
                MessageBox.Show("Năm xuất bản phải là số (hoặc để trống).", "Lỗi");
                return;
            }

            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                // --- LOGIC SMART COMBOBOX ---
                int? tacGiaId = await GetOrCreateLookupIdAsync(cmbTacGia.Text, _tacGiaList, "api/app/sach/tacgia");
                int? theLoaiId = await GetOrCreateLookupIdAsync(cmbTheLoai.Text, _theLoaiList, "api/app/sach/theloai");
                int? nhaXuatBanId = await GetOrCreateLookupIdAsync(cmbNhaXuatBan.Text, _nhaXuatBanList, "api/app/sach/nhaxuatban");
                // ---

                var dto = new SachUpdateRequestDto
                {
                    TenSach = txtTenSach.Text,
                    IdTheLoai = theLoaiId,
                    IdTacGia = tacGiaId,
                    IdNhaXuatBan = nhaXuatBanId,
                    NamXuatBan = int.TryParse(txtNamXuatBan.Text, out int nam) ? nam : null,
                    MoTa = txtMoTa.Text,
                    SoLuongTong = soLuong,
                    AnhBiaBase64 = _currentAnhBiaBase64,
                    GiaBia = decimal.TryParse(txtGiaBia.Text, out decimal gia) ? gia : 0,
                    ViTri = txtViTri.Text
                };

                HttpResponseMessage response;
                if (_selectedSachDetails == null) // THÊM MỚI
                {
                    response = await httpClient.PostAsJsonAsync("api/app/sach", dto);
                }
                else // CẬP NHẬT
                {
                    dto.IdSach = _selectedSachDetails.IdSach;
                    response = await httpClient.PutAsJsonAsync($"api/app/sach/{dto.IdSach}", dto);
                }

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Lưu thành công!", "Thông báo");
                    await LoadFiltersAsync(); // Tải lại bộ lọc (quan trọng, vì có thể có Tác giả/Thể loại mới)
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

        /// <summary>
        /// Hàm helper cho "Smart ComboBox"
        /// </summary>
        private async Task<int?> GetOrCreateLookupIdAsync(string tenDaNhap, List<FilterLookupDto> list, string apiEndpoint)
        {
            if (string.IsNullOrWhiteSpace(tenDaNhap))
            {
                return null;
            }

            // 1. Thử tìm chính xác (không phân biệt hoa thường)
            var item = list.FirstOrDefault(x => x.Ten.Equals(tenDaNhap, StringComparison.OrdinalIgnoreCase));
            if (item != null)
            {
                return item.Id; // Tìm thấy
            }

            // 2. Nếu không tìm thấy -> Tạo mới
            try
            {
                var response = await httpClient.PostAsJsonAsync(apiEndpoint, new FilterLookupDto { Ten = tenDaNhap });
                if (response.IsSuccessStatusCode)
                {
                    var newItem = await response.Content.ReadFromJsonAsync<FilterLookupDto>();
                    return newItem?.Id; // Trả về ID mới
                }
                else
                {
                    // Có thể tên đã tồn tại (do API check)
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


        /// <summary>
        /// Xử lý nút "Xóa"
        /// </summary>
        private async void BtnXoa_Click(object sender, RoutedEventArgs e)
        {
            // (Giữ nguyên)
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

        // --- LOGIC CHO TAB MỚI: QUẢN LÝ DANH MỤC ---
        #region CRUD Tác Giả / Thể Loại / NXB

        // -- Tác Giả --
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

        // -- Thể Loại --
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

        // -- Nhà Xuất Bản --
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

        /// <summary>
        /// Hàm CRUD dùng chung cho 3 danh mục
        /// </summary>
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

        // --- CÁC NÚT ĐIỀU HƯỚNG ---
        #region Navigation Buttons

        /// <summary>
        /// Điều hướng đến trang Báo cáo Tồn kho Sách
        /// </summary>
        private void BtnXemBaoCaoSach_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService?.Navigate(new BapCaoTonKhoSachPreviewWindow());
        }

        /// <summary>
        /// (REQ 1) Điều hướng đến trang Cài đặt
        /// </summary>
        private void BtnCaiDatPhiThue_Click(object sender, RoutedEventArgs e)
        {
            this.NavigationService?.Navigate(new CaiDatWindow());
        }

        #endregion
    }
}