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
using OfficeOpenXml;
using OfficeOpenXml.Table;
using System.Globalization;

namespace AppCafebookApi.View.quanly.pages
{
    public partial class QuanLyKhuyenMaiView : Page
    {
        private static readonly HttpClient httpClient;
        private List<KhuyenMaiDto> _allKhuyenMaiList = new List<KhuyenMaiDto>();
        private KhuyenMaiUpdateRequestDto? _selectedKhuyenMai = null;
        private List<FilterLookupDto> _sanPhamList = new List<FilterLookupDto>();

        static QuanLyKhuyenMaiView()
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5166")
            };
        }

        public QuanLyKhuyenMaiView()
        {
            InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            cmbFilterTrangThai.SelectedIndex = 0; // "Tất cả"

            await LoadKhuyenMaiFiltersAsync();
            await LoadKhuyenMaiDataAsync();
        }

        /// <summary>
        /// Tải các bộ lọc (Danh sách sản phẩm)
        /// </summary>
        private async Task LoadKhuyenMaiFiltersAsync()
        {
            try
            {
                var response = await httpClient.GetAsync("/api/app/khuyenmai/filters");
                if (response.IsSuccessStatusCode)
                {
                    _sanPhamList = (await response.Content.ReadFromJsonAsync<List<FilterLookupDto>>()) ?? new List<FilterLookupDto>();
                    _sanPhamList.Insert(0, new FilterLookupDto { Id = 0, Ten = "Không áp dụng (cho toàn hóa đơn)" });

                    cmbSanPhamApDung.ItemsSource = _sanPhamList;
                    cmbSanPhamApDung.SelectedValue = 0;
                }
                else
                {
                    MessageBox.Show("Không thể tải danh sách sản phẩm.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi khi tải bộ lọc: {ex.Message}");
            }
        }

        /// <summary>
        /// Tải dữ liệu chính cho DataGrid và làm mới form
        /// </summary>
        private async Task LoadKhuyenMaiDataAsync()
        {
            SetLoading(true);
            try
            {
                string maKM = txtSearchMaKM.Text;
                string trangThai = (cmbFilterTrangThai.SelectedItem as ComboBoxItem)?.Content?.ToString() ?? "Tất cả";

                var response = await httpClient.GetAsync($"/api/app/khuyenmai/search?maKhuyenMai={maKM}&trangThai={trangThai}");
                if (response.IsSuccessStatusCode)
                {
                    _allKhuyenMaiList = (await response.Content.ReadFromJsonAsync<List<KhuyenMaiDto>>()) ?? new List<KhuyenMaiDto>();
                    dgKhuyenMai.ItemsSource = _allKhuyenMaiList;
                    LamMoiUI(); // Làm mới form chi tiết
                }
                else
                {
                    MessageBox.Show("Không thể tải danh sách khuyến mãi.");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối: {ex.Message}");
            }
            finally
            {
                SetLoading(false);
            }
        }

        private async void Filter_Changed(object sender, RoutedEventArgs e)
        {
            if (IsLoaded)
            {
                await LoadKhuyenMaiDataAsync();
            }
        }

        private async void DgKhuyenMai_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            if (dgKhuyenMai.SelectedItem is KhuyenMaiDto selectedDto)
            {
                await LoadChiTietKhuyenMai(selectedDto.IdKhuyenMai);
            }
        }

        /// <summary>
        /// SỬA: Cập nhật logic tải chi tiết (Trạng thái SỬA)
        /// </summary>
        private async Task LoadChiTietKhuyenMai(int id)
        {
            SetLoading(true);
            try
            {
                var response = await httpClient.GetAsync($"/api/app/khuyenmai/{id}");
                if (response.IsSuccessStatusCode)
                {
                    _selectedKhuyenMai = await response.Content.ReadFromJsonAsync<KhuyenMaiUpdateRequestDto>();
                    if (_selectedKhuyenMai != null)
                    {
                        // Hiển thị dữ liệu lên form
                        txtMaKhuyenMai.Text = _selectedKhuyenMai.MaKhuyenMai;
                        txtTenKM.Text = _selectedKhuyenMai.TenChuongTrinh;
                        txtMoTa.Text = _selectedKhuyenMai.MoTa;

                        cmbLoaiGiamGia.SelectedValue = _selectedKhuyenMai.LoaiGiamGia == "PhanTram"
                            ? (cmbLoaiGiamGia.Items[0] as ComboBoxItem)
                            : (cmbLoaiGiamGia.Items[1] as ComboBoxItem);

                        txtGiaTriGiam.Text = _selectedKhuyenMai.GiaTriGiam.ToString(CultureInfo.InvariantCulture);
                        txtGiamToiDa.Text = _selectedKhuyenMai.GiamToiDa?.ToString(CultureInfo.InvariantCulture) ?? "0";
                        txtDieuKienApDung.Text = _selectedKhuyenMai.DieuKienApDung;
                        txtDonHangToiThieu.Text = _selectedKhuyenMai.HoaDonToiThieu?.ToString(CultureInfo.InvariantCulture) ?? "0";
                        txtSoLuongConLai.Text = _selectedKhuyenMai.SoLuongConLai?.ToString() ?? "0";
                        cmbSanPhamApDung.SelectedValue = _selectedKhuyenMai.IdSanPhamApDung ?? 0;
                        dpNgayBatDau.SelectedDate = _selectedKhuyenMai.NgayBatDau;
                        dpNgayKetThuc.SelectedDate = _selectedKhuyenMai.NgayKetThuc;
                        txtDayOfWeek.Text = _selectedKhuyenMai.NgayTrongTuan;
                        txtTimeStart.Text = _selectedKhuyenMai.GioBatDau;
                        txtTimeEnd.Text = _selectedKhuyenMai.GioKetThuc;

                        // SỬA: Ẩn/Hiện các nút cho trạng thái "Chỉnh Sửa"
                        btnThem.Visibility = Visibility.Collapsed;
                        btnLuu.Visibility = Visibility.Visible;
                        btnXoa.Visibility = Visibility.Visible;
                        btnTamDung.Visibility = Visibility.Visible;

                        // Bật các nút (an toàn)
                        btnLuu.IsEnabled = true;
                        btnXoa.IsEnabled = true;
                        btnTamDung.IsEnabled = true;
                    }
                }
                else
                {
                    MessageBox.Show("Không tìm thấy chi tiết khuyến mãi.");
                    LamMoiUI(); // <-- Quan trọng: Nếu lỗi, reset về trạng thái "Thêm Mới"
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi tải chi tiết: {ex.Message}");
                LamMoiUI(); // <-- Quan trọng: Nếu lỗi, reset về trạng thái "Thêm Mới"
            }
            finally
            {
                SetLoading(false);
            }
        }

        private async void BtnThem_Click(object sender, RoutedEventArgs e)
        {
            if (!ValidateInput()) return;

            var dto = MapDtoFromUi();
            dto.TrangThai = "Hoạt động"; // Mặc định khi thêm mới

            SetLoading(true);
            try
            {
                var response = await httpClient.PostAsJsonAsync("/api/app/khuyenmai", dto);
                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Thêm khuyến mãi thành công!");
                    await LoadKhuyenMaiDataAsync(); // Tải lại lưới
                }
                else
                {
                    string error = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Lỗi khi thêm: {error}", "Lỗi");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối: {ex.Message}");
            }
            finally
            {
                SetLoading(false);
            }
        }

        private async void BtnLuu_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedKhuyenMai == null) return;
            if (!ValidateInput()) return;

            var dto = MapDtoFromUi();
            dto.IdKhuyenMai = _selectedKhuyenMai.IdKhuyenMai;
            dto.TrangThai = _selectedKhuyenMai.TrangThai; // Giữ trạng thái cũ khi lưu

            SetLoading(true);
            try
            {
                var response = await httpClient.PutAsJsonAsync($"/api/app/khuyenmai/{dto.IdKhuyenMai}", dto);
                if (response.IsSuccessStatusCode)
                {
                    MessageBox.Show("Cập nhật thành công!");
                    await LoadKhuyenMaiDataAsync(); // Tải lại lưới
                }
                else
                {
                    string error = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Lỗi khi cập nhật: {error}", "Lỗi");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối: {ex.Message}");
            }
            finally
            {
                SetLoading(false);
            }
        }

        private async void BtnXoa_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedKhuyenMai == null) return;

            if (MessageBox.Show($"Bạn có chắc muốn xóa mã: {_selectedKhuyenMai.MaKhuyenMai}?", "Xác nhận xóa", MessageBoxButton.YesNo, MessageBoxImage.Warning) == MessageBoxResult.Yes)
            {
                SetLoading(true);
                try
                {
                    var response = await httpClient.DeleteAsync($"/api/app/khuyenmai/{_selectedKhuyenMai.IdKhuyenMai}");
                    if (response.IsSuccessStatusCode)
                    {
                        MessageBox.Show("Xóa thành công!");
                        await LoadKhuyenMaiDataAsync();
                    }
                    else
                    {
                        string error = await response.Content.ReadAsStringAsync();
                        MessageBox.Show($"Lỗi khi xóa: {error}", "Lỗi");
                    }
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi kết nối: {ex.Message}");
                }
                finally
                {
                    SetLoading(false);
                }
            }
        }

        private async void BtnTamDung_Click(object sender, RoutedEventArgs e)
        {
            if (_selectedKhuyenMai == null) return;

            SetLoading(true);
            try
            {
                var response = await httpClient.PatchAsync($"/api/app/khuyenmai/togglestatus/{_selectedKhuyenMai.IdKhuyenMai}", null);
                if (response.IsSuccessStatusCode)
                {
                    string newStatus = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Đã cập nhật trạng thái thành: {newStatus}");
                    await LoadKhuyenMaiDataAsync(); // Tải lại để cập nhật chi tiết
                }
                else
                {
                    string error = await response.Content.ReadAsStringAsync();
                    MessageBox.Show($"Lỗi: {error}", "Lỗi");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Lỗi kết nối: {ex.Message}");
            }
            finally
            {
                SetLoading(false);
            }
        }

        private async void BtnLamMoi_Click(object sender, RoutedEventArgs e)
        {
            LamMoiUI(); // Xóa form
            if (_allKhuyenMaiList.Count == 0) // Chỉ tải lại nếu lưới rỗng
            {
                await LoadKhuyenMaiDataAsync();
            }
        }

        private KhuyenMaiUpdateRequestDto MapDtoFromUi()
        {
            return new KhuyenMaiUpdateRequestDto
            {
                MaKhuyenMai = txtMaKhuyenMai.Text,
                TenChuongTrinh = txtTenKM.Text,
                MoTa = txtMoTa.Text,
                LoaiGiamGia = (cmbLoaiGiamGia.SelectedItem as ComboBoxItem)?.Tag.ToString() ?? "SoTien",
                GiaTriGiam = decimal.TryParse(txtGiaTriGiam.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var gt) ? gt : 0,

                GiamToiDa = decimal.TryParse(txtGiamToiDa.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var max) ? max : 0,
                HoaDonToiThieu = decimal.TryParse(txtDonHangToiThieu.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var min) ? min : 0,
                SoLuongConLai = int.TryParse(txtSoLuongConLai.Text, out var sl) ? sl : 0,
                DieuKienApDung = txtDieuKienApDung.Text,
                IdSanPhamApDung = (int)cmbSanPhamApDung.SelectedValue,
                NgayBatDau = dpNgayBatDau.SelectedDate ?? DateTime.Today,
                NgayKetThuc = dpNgayKetThuc.SelectedDate ?? DateTime.Today.AddDays(1),
                NgayTrongTuan = txtDayOfWeek.Text,
                GioBatDau = txtTimeStart.Text,
                GioKetThuc = txtTimeEnd.Text
            };
        }

        /// <summary>
        /// SỬA: Xóa (clear) tất cả các trường UI (Trạng thái THÊM MỚI)
        /// </summary>
        private void LamMoiUI()
        {
            _selectedKhuyenMai = null;
            dgKhuyenMai.SelectedItem = null; // Bỏ chọn trên lưới

            txtMaKhuyenMai.Clear();
            txtTenKM.Clear();
            txtMoTa.Clear();
            cmbLoaiGiamGia.SelectedIndex = -1;
            txtGiaTriGiam.Clear();

            txtGiamToiDa.Text = "0";
            txtDieuKienApDung.Clear();
            txtDonHangToiThieu.Text = "0";
            txtSoLuongConLai.Text = "0";
            if (cmbSanPhamApDung.Items.Count > 0)
                cmbSanPhamApDung.SelectedValue = 0;
            dpNgayBatDau.SelectedDate = null;
            dpNgayKetThuc.SelectedDate = null;
            txtDayOfWeek.Clear();
            txtTimeStart.Clear();
            txtTimeEnd.Clear();

            // SỬA: Ẩn/Hiện các nút cho trạng thái "Thêm Mới"
            btnThem.Visibility = Visibility.Visible;
            btnLuu.Visibility = Visibility.Collapsed;
            btnXoa.Visibility = Visibility.Collapsed;
            btnTamDung.Visibility = Visibility.Collapsed;

            // Vẫn giữ logic IsEnabled (an toàn)
            btnLuu.IsEnabled = false;
            btnXoa.IsEnabled = false;
            btnTamDung.IsEnabled = false;
        }

        private bool ValidateInput()
        {
            if (string.IsNullOrWhiteSpace(txtMaKhuyenMai.Text))
            {
                MessageBox.Show("Mã khuyến mãi không được để trống.");
                return false;
            }
            if (string.IsNullOrWhiteSpace(txtTenKM.Text))
            {
                MessageBox.Show("Tên chương trình không được để trống.");
                return false;
            }
            if (cmbLoaiGiamGia.SelectedItem == null)
            {
                MessageBox.Show("Vui lòng chọn loại giảm giá.");
                return false;
            }
            if (!decimal.TryParse(txtGiaTriGiam.Text, NumberStyles.Any, CultureInfo.InvariantCulture, out var gtri) || gtri <= 0)
            {
                MessageBox.Show("Giá trị giảm phải là một số lớn hơn 0.");
                return false;
            }
            if (dpNgayBatDau.SelectedDate == null || dpNgayKetThuc.SelectedDate == null)
            {
                MessageBox.Show("Ngày bắt đầu và kết thúc không được để trống.");
                return false;
            }
            if (dpNgayKetThuc.SelectedDate < dpNgayBatDau.SelectedDate)
            {
                MessageBox.Show("Ngày kết thúc không thể nhỏ hơn ngày bắt đầu.");
                return false;
            }

            // Kiểm tra giờ nếu được nhập
            if (!string.IsNullOrWhiteSpace(txtTimeStart.Text) && !TimeSpan.TryParse(txtTimeStart.Text, out _))
            {
                MessageBox.Show("Giờ bắt đầu không hợp lệ. Vui lòng nhập dạng HH:mm (ví dụ: 08:00).");
                return false;
            }
            if (!string.IsNullOrWhiteSpace(txtTimeEnd.Text) && !TimeSpan.TryParse(txtTimeEnd.Text, out _))
            {
                MessageBox.Show("Giờ kết thúc không hợp lệ. Vui lòng nhập dạng HH:mm (ví dụ: 22:00).");
                return false;
            }

            return true;
        }

        private void BtnQuayLai_Click(object sender, RoutedEventArgs e)
        {
            if (NavigationService.CanGoBack)
            {
                NavigationService.GoBack();
            }
        }

        private void SetLoading(bool isLoading)
        {
            LoadingOverlay.Visibility = isLoading ? Visibility.Visible : Visibility.Collapsed;
            MainPanel.IsEnabled = !isLoading;
        }

        // (Hàm Export Excel giữ nguyên)
        private async void BtnExportToExcel_Click(object sender, RoutedEventArgs e)
        {
            if (_allKhuyenMaiList.Count == 0)
            {
                MessageBox.Show("Không có dữ liệu để xuất.", "Thông báo");
                return;
            }

            SaveFileDialog sfd = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"DS_KhuyenMai_{DateTime.Now:ddMMyyyy_HHmmss}.xlsx"
            };

            if (sfd.ShowDialog() == true)
            {
                SetLoading(true);
                try
                {
                    ExcelPackage.License.SetNonCommercialPersonal("CafeBook");

                    await Task.Run(() =>
                    {
                        using (var package = new ExcelPackage(new FileInfo(sfd.FileName)))
                        {
                            var ws = package.Workbook.Worksheets.Add("DanhSachKhuyenMai");
                            ws.Cells["A1"].Value = "Danh sách Khuyến mãi";
                            ws.Cells["A1:D1"].Merge = true;
                            ws.Cells["A1"].Style.Font.Bold = true;
                            ws.Cells["A1"].Style.Font.Size = 16;

                            // Sửa: Đảm bảo dữ liệu mới nhất được xuất
                            var dataToExport = _allKhuyenMaiList.Select(km => new {
                                km.MaKhuyenMai,
                                km.TenKhuyenMai,
                                km.GiaTriGiam,
                                km.GiamToiDa,
                                km.SoLuongConLai,
                                km.DieuKienApDung,
                                km.NgayBatDau,
                                km.NgayKetThuc,
                                km.TrangThai
                            });

                            ws.Cells["A3"].LoadFromCollection(dataToExport, true, TableStyles.Medium9);

                            // Định dạng cột
                            ws.Cells[ws.Dimension.Address].AutoFitColumns();
                            var cols = ws.Cells["3:3"];
                            int ngayBatDauCol = cols.First(c => c.Text == "NgayBatDau").Start.Column;
                            int ngayKetThucCol = cols.First(c => c.Text == "NgayKetThuc").Start.Column;

                            ws.Column(ngayBatDauCol).Style.Numberformat.Format = "dd/MM/yyyy";
                            ws.Column(ngayKetThucCol).Style.Numberformat.Format = "dd/MM/yyyy";

                            package.Save();
                        }
                    });
                    MessageBox.Show("Xuất Excel thành công!", "Thông báo");
                }
                catch (Exception ex)
                {
                    MessageBox.Show($"Lỗi khi xuất Excel: {ex.Message}\n\nĐảm bảo file không đang được mở.", "Lỗi");
                }
                finally
                {
                    SetLoading(false);
                }
            }
        }
    }
}