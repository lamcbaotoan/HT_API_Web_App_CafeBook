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
using System.Windows.Controls.Primitives; // <-- Đã thêm

namespace AppCafebookApi.View.quanly.pages
{
    public partial class QuanLyBanView : Page
    {
        private static readonly HttpClient httpClient;
        private List<KhuVucDto> _khuVucList = new List<KhuVucDto>();
        private object? _selectedItem;
        private bool _isAddingNew = false;
        private int? _navigateToBanId = null;

        // Class nội bộ
        private class BanGridItem : BanDto
        {
            public string? TenKhuVuc { get; set; }
        }

        private List<BanGridItem> _allBansList = new List<BanGridItem>();
        private List<KhuVucDto> _filterKhuVucList = new List<KhuVucDto>();

        private List<ThongBaoDto> _allThongBaoList = new List<ThongBaoDto>();
        private bool _showSuCoHistory = false;


        static QuanLyBanView()
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5166")
            };
        }

        public QuanLyBanView()
        {
            InitializeComponent();
        }

        public QuanLyBanView(int banIdToNavigate) : this()
        {
            _navigateToBanId = banIdToNavigate;
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadDataAsync();
            await LoadThongBaoSuCoAsync();

            if (_navigateToBanId.HasValue)
            {
                SelectAndShowBan(_navigateToBanId.Value);
                _navigateToBanId = null;
            }
        }

        private async Task LoadDataAsync()
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                _khuVucList = (await httpClient.GetFromJsonAsync<List<KhuVucDto>>("api/app/banquanly/tree"))
                                 ?? new List<KhuVucDto>();

                // 1. Nạp dữ liệu cho ComboBox (trong Form chi tiết)
                cmbKhuVuc.ItemsSource = _khuVucList;

                // 2. Nạp dữ liệu cho DataGrid Khu Vực (Tab 1)
                dgKhuVuc.ItemsSource = _khuVucList;
                ApplyKhuVucFilter(); // Áp dụng filter (nếu có)

                // 3. Tạo danh sách phẳng (flat list) cho DataGrid Bàn (Tab 2)
                _allBansList = _khuVucList.SelectMany(kv => kv.Bans.Select(b => new BanGridItem
                {
                    IdBan = b.IdBan,
                    SoBan = b.SoBan,
                    SoGhe = b.SoGhe,
                    TrangThai = b.TrangThai,
                    GhiChu = b.GhiChu,
                    IdKhuVuc = b.IdKhuVuc,
                    TenKhuVuc = kv.TenKhuVuc
                })).OrderBy(b => b.IdBan).ToList();

                // 4. Nạp dữ liệu cho DataGrid Bàn
                dgAllBans.ItemsSource = _allBansList;

                // 5. Nạp dữ liệu cho ComboBox Lọc Bàn
                _filterKhuVucList = new List<KhuVucDto>(_khuVucList);
                _filterKhuVucList.Insert(0, new KhuVucDto { IdKhuVuc = 0, TenKhuVuc = "--- Tất cả Khu vực ---" });
                cmbFilterKhuVuc.ItemsSource = _filterKhuVucList;
                cmbFilterKhuVuc.SelectedIndex = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải dữ liệu: {ex.Message}", "Lỗi API");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
                if (_navigateToBanId == null)
                    ResetForm();
            }
        }

        // --- (LoadThongBaoSuCoAsync, ApplySuCoFilter, BtnToggleSuCoHistory_Click, BtnDanhDauDaDoc_Click giữ nguyên) ---
        #region Sự Cố Bàn (Tab 3)
        private async Task LoadThongBaoSuCoAsync()
        {
            try
            {
                _allThongBaoList = (await httpClient.GetFromJsonAsync<List<ThongBaoDto>>("api/app/thongbao/all"))
                                       ?? new List<ThongBaoDto>();

                _allThongBaoList = _allThongBaoList.Where(t => t.LoaiThongBao == "SuCoBan").ToList();

                ApplySuCoFilter();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải thông báo sự cố: {ex.Message}");
            }
        }

        private void ApplySuCoFilter()
        {
            if (_showSuCoHistory)
            {
                lblSuCoTitle.Text = "Lịch sử Sự cố (Đã xử lý)";
                lbThongBaoBan.ItemsSource = _allThongBaoList
                    .Where(t => t.DaXem == true)
                    .OrderByDescending(t => t.ThoiGianTao)
                    .ToList();
            }
            else
            {
                lblSuCoTitle.Text = "Sự cố Bàn cần xử lý";
                lbThongBaoBan.ItemsSource = _allThongBaoList
                    .Where(t => t.DaXem == false)
                    .OrderByDescending(t => t.ThoiGianTao)
                    .ToList();
            }
        }

        private void BtnToggleSuCoHistory_Click(object sender, RoutedEventArgs e)
        {
            _showSuCoHistory = (sender as ToggleButton).IsChecked == true;
            ApplySuCoFilter();
        }

        private async void BtnDanhDauDaDoc_Click(object sender, RoutedEventArgs e)
        {
            var button = sender as Button;
            var thongBao = button?.DataContext as ThongBaoDto;

            if (thongBao != null)
            {
                if (thongBao.DaXem == false)
                {
                    try
                    {
                        var idThongBao = thongBao.IdThongBao;
                        var response = await httpClient.PostAsync($"api/app/thongbao/mark-as-read/{idThongBao}", null);

                        if (response.IsSuccessStatusCode)
                        {
                            var itemInCache = _allThongBaoList.FirstOrDefault(t => t.IdThongBao == idThongBao);
                            if (itemInCache != null)
                            {
                                itemInCache.DaXem = true;
                            }
                            ApplySuCoFilter();
                        }
                        else
                        {
                            MessageBox.Show($"Lỗi cập nhật: {await response.Content.ReadAsStringAsync()}");
                        }
                    }
                    catch (Exception ex)
                    {
                        MessageBox.Show($"Lỗi cập nhật: {ex.Message}");
                    }
                }
                else
                {
                    MessageBox.Show("Sự cố này đã được xử lý trước đó.");
                }
            }
        }
        #endregion

        private void SelectAndShowBan(int banId)
        {
            // (Giữ nguyên)
            var ban = _khuVucList.SelectMany(kv => kv.Bans).FirstOrDefault(b => b.IdBan == banId);
            if (ban != null)
            {
                _selectedItem = ban;
                PopulateForm();
                var gridItem = _allBansList.FirstOrDefault(b => b.IdBan == banId);
                if (gridItem != null)
                {
                    dgAllBans.SelectedItem = gridItem;
                    dgAllBans.ScrollIntoView(gridItem);
                }
            }
        }

        // --- (Các hàm Lọc/Tìm kiếm Bàn giữ nguyên) ---
        #region Lọc/Tìm kiếm Bàn (Tab 2)
        private void CmbFilterKhuVuc_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void TxtSearch_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyFilter();
        }

        private void BtnClearFilter_Click(object sender, RoutedEventArgs e)
        {
            // CẬP NHẬT: Xóa luôn filter của Tab 1
            txtSearchKhuVuc.Text = "";
            ApplyKhuVucFilter();

            if (cmbFilterKhuVuc.SelectedIndex != 0)
                cmbFilterKhuVuc.SelectedIndex = 0;

            if (!string.IsNullOrEmpty(txtSearch.Text))
                txtSearch.Text = "";

            ApplyFilter();
        }

        private void ApplyFilter()
        {
            if (_allBansList == null) return;
            IEnumerable<BanGridItem> filteredView = _allBansList;
            if (cmbFilterKhuVuc.SelectedItem is KhuVucDto selectedKhuVuc && selectedKhuVuc.IdKhuVuc != 0)
            {
                filteredView = filteredView.Where(b => b.IdKhuVuc == selectedKhuVuc.IdKhuVuc);
            }
            string searchText = txtSearch.Text.Trim();
            if (!string.IsNullOrEmpty(searchText))
            {
                filteredView = filteredView.Where(b =>
                    b.SoBan.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    (b.GhiChu != null && b.GhiChu.Contains(searchText, StringComparison.OrdinalIgnoreCase)) ||
                    b.TrangThai.Contains(searchText, StringComparison.OrdinalIgnoreCase)
                );
            }
            dgAllBans.ItemsSource = filteredView.ToList();
        }
        #endregion

        // --- THÊM MỚI: Các hàm Lọc/Tìm kiếm Khu Vực ---
        #region Lọc/Tìm kiếm Khu Vực (Tab 1)

        private void TxtSearchKhuVuc_TextChanged(object sender, TextChangedEventArgs e)
        {
            ApplyKhuVucFilter();
        }

        private void ApplyKhuVucFilter()
        {
            if (_khuVucList == null) return;

            IEnumerable<KhuVucDto> filteredView = _khuVucList;
            string searchText = txtSearchKhuVuc.Text.Trim();

            if (!string.IsNullOrEmpty(searchText))
            {
                filteredView = filteredView.Where(kv =>
                    kv.TenKhuVuc.Contains(searchText, StringComparison.OrdinalIgnoreCase) ||
                    (kv.MoTa != null && kv.MoTa.Contains(searchText, StringComparison.OrdinalIgnoreCase))
                );
            }
            dgKhuVuc.ItemsSource = filteredView.ToList();
        }

        #endregion


        // --- CẬP NHẬT: Các hàm chọn Grid ---
        #region Grid Selection & Form Logic

        private void DgKhuVuc_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgKhuVuc.SelectedItem is KhuVucDto selectedKhuVuc)
            {
                _selectedItem = selectedKhuVuc;
                _isAddingNew = false;

                // Bỏ chọn Grid Bàn
                dgAllBans.SelectedItem = null;

                PopulateForm();
            }
        }

        private void DgAllBans_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgAllBans.SelectedItem == null)
            {
                return;
            }

            if (dgAllBans.SelectedItem is BanGridItem selectedBan)
            {
                _selectedItem = _khuVucList
                    .SelectMany(kv => kv.Bans)
                    .FirstOrDefault(b => b.IdBan == selectedBan.IdBan);

                _isAddingNew = false;

                // Bỏ chọn Grid Khu Vực
                dgKhuVuc.SelectedItem = null;

                PopulateForm();
            }
        }

        private void PopulateForm()
        {
            // (Giữ nguyên)
            panelKhuVuc.Visibility = Visibility.Collapsed;
            panelBan.Visibility = Visibility.Collapsed;
            formChiTiet.IsEnabled = false;
            btnXemLichSu.Visibility = Visibility.Collapsed;
            formChiTiet.DataContext = null;

            if (_selectedItem == null)
            {
                lblFormTitle.Text = "Chọn một mục để xem chi tiết";
                return;
            }

            formChiTiet.IsEnabled = true;

            if (_selectedItem is KhuVucDto kv)
            {
                lblFormTitle.Text = "Chi tiết Khu Vực";
                panelKhuVuc.Visibility = Visibility.Visible;
                panelBan.Visibility = Visibility.Collapsed; // Ẩn panel Bàn
                formChiTiet.DataContext = kv;
            }
            else if (_selectedItem is BanDto ban)
            {
                lblFormTitle.Text = "Chi tiết Bàn";
                panelBan.Visibility = Visibility.Visible;
                panelKhuVuc.Visibility = Visibility.Collapsed; // Ẩn panel Khu vực
                formChiTiet.DataContext = ban;

                cmbKhuVuc.SelectedValue = ban.IdKhuVuc;
                cmbTrangThai.Text = ban.TrangThai;
                btnXemLichSu.Visibility = Visibility.Visible;
            }
        }

        private void BtnThemKhuVuc_Click(object sender, RoutedEventArgs e)
        {
            _selectedItem = new KhuVucDto();
            _isAddingNew = true;

            // CẬP NHẬT: Bỏ chọn cả 2 Grid
            dgKhuVuc.SelectedItem = null;
            dgAllBans.SelectedItem = null;

            PopulateForm();
            lblFormTitle.Text = "Thêm Khu Vực Mới";
            formChiTiet.DataContext = _selectedItem;
        }

        private void BtnThemBan_Click(object sender, RoutedEventArgs e)
        {
            int defaultKhuVucId = _khuVucList.FirstOrDefault()?.IdKhuVuc ?? 0;
            _selectedItem = new BanDto { IdKhuVuc = defaultKhuVucId, TrangThai = "Trống" };
            _isAddingNew = true;

            // CẬP NHẬT: Bỏ chọn cả 2 Grid
            dgKhuVuc.SelectedItem = null;
            dgAllBans.SelectedItem = null;

            PopulateForm();
            lblFormTitle.Text = "Thêm Bàn Mới";
            formChiTiet.DataContext = _selectedItem;
        }

        private void ResetForm()
        {
            _selectedItem = null;
            _isAddingNew = false;
            dgAllBans.SelectedItem = null;
            dgKhuVuc.SelectedItem = null; // Thêm
            PopulateForm();
        }

        #endregion

        // --- (Các hàm Lưu, Xóa, Xem Lịch sử giữ nguyên, chúng đã hỗ trợ KhuVuc) ---
        #region API Calls (Lưu, Xóa, Lịch sử)
        private async void BtnLuu_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedItem == null) return;
            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                HttpResponseMessage response;
                if (_selectedItem is KhuVucDto kv)
                {
                    var dto = new KhuVucUpdateRequestDto { TenKhuVuc = txtTenKhuVuc.Text, MoTa = txtMoTaKhuVuc.Text };
                    if (_isAddingNew)
                        response = await httpClient.PostAsJsonAsync("api/app/banquanly/khuvuc", dto);
                    else
                        response = await httpClient.PutAsJsonAsync($"api/app/banquanly/khuvuc/{kv.IdKhuVuc}", dto);
                }
                else if (_selectedItem is BanDto ban)
                {
                    var dto = new BanUpdateRequestDto
                    {
                        SoBan = txtSoBan.Text,
                        SoGhe = int.TryParse(txtSoGhe.Text, out int ghe) ? ghe : 0,
                        IdKhuVuc = (int)cmbKhuVuc.SelectedValue,
                        TrangThai = cmbTrangThai.Text,
                        GhiChu = txtGhiChu.Text
                    };

                    if (_isAddingNew)
                        response = await httpClient.PostAsJsonAsync("api/app/banquanly/ban", dto);
                    else
                        response = await httpClient.PutAsJsonAsync($"api/app/banquanly/ban/{ban.IdBan}", dto);
                }
                else
                {
                    throw new InvalidOperationException("Loại đối tượng không xác định.");
                }

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Lưu thành công!", "Thông báo");
                    await LoadDataAsync();
                }
                else
                {
                    MessageBox.Show($"Lỗi: {await response.Content.ReadAsStringAsync()}", "Lỗi API");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async void BtnXoa_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedItem == null) return;
            string endpoint;
            string tenMuc;

            if (_selectedItem is KhuVucDto kv)
            {
                endpoint = $"api/app/banquanly/khuvuc/{kv.IdKhuVuc}";
                tenMuc = kv.TenKhuVuc;
            }
            else if (_selectedItem is BanDto ban)
            {
                endpoint = $"api/app/banquanly/ban/{ban.IdBan}";
                tenMuc = ban.SoBan;
            }
            else
            {
                return;
            }

            if (MessageBox.Show($"Bạn có chắc chắn muốn xóa '{tenMuc}' không?", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.No)
            {
                return;
            }

            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var response = await httpClient.DeleteAsync(endpoint);

                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Xóa thành công!", "Thông báo");
                    await LoadDataAsync();
                }
                else if (response.StatusCode == HttpStatusCode.Conflict)
                {
                    MessageBox.Show($"Lỗi: {await response.Content.ReadAsStringAsync()}", "Không thể xóa");
                }
                else
                {
                    MessageBox.Show($"Lỗi: {response.ReasonPhrase}", "Lỗi API");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi: {ex.Message}", "Lỗi");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }

        private async void BtnXemLichSu_Click(object sender, RoutedEventArgs e)
        {
            if (!(_selectedItem is BanDto ban)) return;

            LoadingOverlay.Visibility = Visibility.Visible;
            try
            {
                var history = await httpClient.GetFromJsonAsync<BanHistoryDto>($"api/app/banquanly/ban/{ban.IdBan}/history");
                if (history != null)
                {
                    MessageBox.Show(
                        $"Lịch sử Bàn: {ban.SoBan}\n\n" +
                        $"- Tổng số lượt phục vụ (HĐ đã thanh toán): {history.SoLuotPhucVu}\n" +
                        $"- Tổng doanh thu mang lại: {history.TongDoanhThu:N0} VND\n" +
                        $"- Tổng số lượt đặt trước: {history.SoLuotDatTruoc}",
                        "Lịch sử Bàn"
                    );
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải lịch sử: {ex.Message}", "Lỗi API");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
            }
        }
        #endregion
    }
}