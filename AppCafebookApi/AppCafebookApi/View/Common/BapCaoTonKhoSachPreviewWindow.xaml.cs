using CafebookModel.Model.ModelApp;
using Microsoft.Win32;
// using Newtonsoft.Json.Linq; // Không cần using này
using OfficeOpenXml;
using OfficeOpenXml.Style;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Net.Http.Json;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Navigation;

// --- SỬA LỖI CS0246: THÊM 2 USING NÀY ---
using System.Text.Json;
using System.Text.Json.Serialization;
// --- KẾT THÚC SỬA LỖI ---

namespace AppCafebookApi.View.common
{
    public partial class BapCaoTonKhoSachPreviewWindow : Page
    {
        private static readonly HttpClient httpClient;
        private BaoCaoSachTongHopDto? currentReportData;

        static BapCaoTonKhoSachPreviewWindow()
        {
            httpClient = new HttpClient
            {
                BaseAddress = new Uri("http://localhost:5166")
            };
        }

        public BapCaoTonKhoSachPreviewWindow()
        {
            InitializeComponent();
        }

        private async void Page_Loaded(object sender, RoutedEventArgs e)
        {
            await LoadFiltersAsync();
            await GenerateReportAsync();
        }

        private async Task LoadFiltersAsync()
        {
            try
            {
                var response = await httpClient.GetFromJsonAsync<JsonElement?>("api/app/baocaosach/filters");

                // SỬA LỖI CS0019: Dùng .HasValue để kiểm tra null
                if (!response.HasValue)
                {
                    MessageBox.Show("Không nhận được dữ liệu lọc.", "Lỗi API");
                    return;
                }

                // Chuyển đổi an toàn
                var theLoais = response.Value.GetProperty("theLoais").Deserialize<List<FilterLookupDto>>() ?? new List<FilterLookupDto>();
                theLoais.Insert(0, new FilterLookupDto { Id = 0, Ten = "Tất cả Thể loại" });
                cmbTheLoai.ItemsSource = theLoais;
                cmbTheLoai.SelectedValue = 0;

                var tacGias = response.Value.GetProperty("tacGias").Deserialize<List<FilterLookupDto>>() ?? new List<FilterLookupDto>();
                tacGias.Insert(0, new FilterLookupDto { Id = 0, Ten = "Tất cả Tác giả" });
                cmbTacGia.ItemsSource = tacGias;
                cmbTacGia.SelectedValue = 0;
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể tải bộ lọc: {ex.Message}", "Lỗi API");
            }
        }

        private async void BtnGenerate_Click(object sender, RoutedEventArgs e)
        {
            await GenerateReportAsync();
        }

        private async Task GenerateReportAsync()
        {
            LoadingOverlay.Visibility = Visibility.Visible;
            btnGenerate.IsEnabled = false;

            var request = new BaoCaoSachRequestDto
            {
                SearchText = string.IsNullOrEmpty(txtSearch.Text) ? null : txtSearch.Text,
                TheLoaiId = (int)cmbTheLoai.SelectedValue == 0 ? (int?)null : (int)cmbTheLoai.SelectedValue,
                TacGiaId = (int)cmbTacGia.SelectedValue == 0 ? (int?)null : (int)cmbTacGia.SelectedValue
            };

            try
            {
                var response = await httpClient.PostAsJsonAsync("api/app/baocaosach/report", request);
                if (response.IsSuccessStatusCode)
                {
                    currentReportData = await response.Content.ReadFromJsonAsync<BaoCaoSachTongHopDto>();
                    if (currentReportData != null)
                    {
                        PopulateUi(currentReportData);
                    }
                }
                else
                {
                    MessageBox.Show($"Lỗi API: {response.ReasonPhrase}", "Lỗi");
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Không thể kết nối API: {ex.Message}", "Lỗi");
            }
            finally
            {
                LoadingOverlay.Visibility = Visibility.Collapsed;
                btnGenerate.IsEnabled = true;
            }
        }

        private void PopulateUi(BaoCaoSachTongHopDto data)
        {
            lblTongDauSach.Text = data.Kpi.TongDauSach.ToString();
            lblTongSoLuong.Text = data.Kpi.TongSoLuong.ToString();
            lblDangChoThue.Text = data.Kpi.DangChoThue.ToString();
            lblSanSang.Text = data.Kpi.SanSang.ToString();

            dgInventoryDetails.ItemsSource = data.ChiTietTonKho;
            dgRentedOverdue.ItemsSource = data.SachTreHan;
            dgTopRented.ItemsSource = data.TopSachThue;
        }

        private async void BtnExportToExcel_Click(object sender, RoutedEventArgs e)
        {
            if (currentReportData == null)
            {
                MessageBox.Show("Chưa có dữ liệu báo cáo để xuất. Vui lòng nhấn 'Lọc Báo Cáo' trước.", "Chưa có dữ liệu");
                return;
            }

            var sfd = new SaveFileDialog
            {
                Filter = "Excel Files (*.xlsx)|*.xlsx",
                FileName = $"BaoCaoTonKhoSach_{DateTime.Now:yyyyMMdd_HHmm}.xlsx"
            };

            if (sfd.ShowDialog() == true)
            {
                try
                {
                    LoadingOverlay.Visibility = Visibility.Visible;
                    await CreateExcelReportAsync(sfd.FileName, currentReportData);
                    LoadingOverlay.Visibility = Visibility.Collapsed;

                    MessageBox.Show($"Đã xuất báo cáo thành công!\n\nĐường dẫn: {sfd.FileName}", "Thành công", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    LoadingOverlay.Visibility = Visibility.Collapsed;
                    MessageBox.Show($"Có lỗi khi xuất Excel: {ex.Message}\n\nĐảm bảo bạn đã đóng file Excel (nếu nó đang mở).", "Lỗi Xuất File", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        // Dán đè lên hàm CreateExcelReportAsync CŨ của bạn
        private async Task CreateExcelReportAsync(string filePath, BaoCaoSachTongHopDto data)
        {
            // Giấy phép EPPlus
            ExcelPackage.License.SetNonCommercialPersonal("CafeBook");
            var fileInfo = new FileInfo(filePath);

            // Xóa file cũ nếu tồn tại để tránh lỗi
            if (fileInfo.Exists)
            {
                fileInfo.Delete();
            }

            using (var package = new ExcelPackage(fileInfo))
            {
                // --- TÙY CHỈNH STYLE ---
                // Định nghĩa một style chung cho tiêu đề
                Action<ExcelRange> styleTitle = (range) =>
                {
                    range.Style.Font.Bold = true;
                    range.Style.Font.Size = 14;
                    range.Style.HorizontalAlignment = ExcelHorizontalAlignment.Center;
                    range.Style.VerticalAlignment = ExcelVerticalAlignment.Center;
                    range.Style.Fill.PatternType = ExcelFillStyle.Solid;
                    range.Style.Fill.BackgroundColor.SetColor(System.Drawing.ColorTranslator.FromHtml("#4F81BD")); // Màu xanh đậm
                    range.Style.Font.Color.SetColor(System.Drawing.Color.White);
                };

                // --- Sheet 1: TỔNG QUAN (Sheet mới) ---
                var wsTongQuan = package.Workbook.Worksheets.Add("Tổng Quan");
                wsTongQuan.Cells["A1:D1"].Merge = true;
                wsTongQuan.Cells["A1"].Value = "BÁO CÁO TỔNG QUAN - CAFEBOOK";
                styleTitle(wsTongQuan.Cells["A1:D1"]);

                wsTongQuan.Cells["A3"].Value = "Báo cáo được tạo lúc:";
                wsTongQuan.Cells["B3"].Value = DateTime.Now;
                wsTongQuan.Cells["B3"].Style.Numberformat.Format = "dd/MM/yyyy HH:mm:ss";
                wsTongQuan.Cells["A3:B3"].Style.Font.Bold = true;

                // Thêm dữ liệu KPI
                wsTongQuan.Cells["A5"].Value = "Chỉ số";
                wsTongQuan.Cells["B5"].Value = "Số lượng";
                wsTongQuan.Cells["A5:B5"].Style.Font.Bold = true;
                wsTongQuan.Cells["A5:B5"].Style.Fill.PatternType = ExcelFillStyle.Solid;
                wsTongQuan.Cells["A5:B5"].Style.Fill.BackgroundColor.SetColor(System.Drawing.Color.LightGray);

                wsTongQuan.Cells["A6"].Value = "Tổng đầu sách";
                wsTongQuan.Cells["B6"].Value = data.Kpi.TongDauSach;

                wsTongQuan.Cells["A7"].Value = "Tổng số lượng (cuốn)";
                wsTongQuan.Cells["B7"].Value = data.Kpi.TongSoLuong;

                wsTongQuan.Cells["A8"].Value = "Đang cho thuê";
                wsTongQuan.Cells["B8"].Value = data.Kpi.DangChoThue;

                wsTongQuan.Cells["A9"].Value = "Sẵn sàng cho thuê";
                wsTongQuan.Cells["B9"].Value = data.Kpi.SanSang;

                // Kẻ viền cho bảng KPI
                var kpiRange = wsTongQuan.Cells["A5:B9"];
                kpiRange.Style.Border.Top.Style = ExcelBorderStyle.Thin;
                kpiRange.Style.Border.Bottom.Style = ExcelBorderStyle.Thin;
                kpiRange.Style.Border.Left.Style = ExcelBorderStyle.Thin;
                kpiRange.Style.Border.Right.Style = ExcelBorderStyle.Thin;

                wsTongQuan.Cells[wsTongQuan.Dimension.Address].AutoFitColumns();


                // --- Sheet 2: Tồn Kho Chi Tiết ---
                var ws1 = package.Workbook.Worksheets.Add("Chi Tiết Tồn Kho");
                ws1.Cells["A1:F1"].Merge = true;
                ws1.Cells["A1"].Value = "Báo cáo Tồn kho Chi tiết";
                styleTitle(ws1.Cells["A1:F1"]);

                // Load dữ liệu từ Collection và áp dụng Style Bảng chuyên nghiệp
                // "true" = in ra tên cột (headers)
                ws1.Cells["A3"].LoadFromCollection(data.ChiTietTonKho, true, OfficeOpenXml.Table.TableStyles.Medium9);
                ws1.Cells[ws1.Dimension.Address].AutoFitColumns();


                // --- Sheet 3: Sách Đang Thuê & Trễ Hạn ---
                var ws2 = package.Workbook.Worksheets.Add("Sách Trễ Hạn");
                ws2.Cells["A1:F1"].Merge = true;
                ws2.Cells["A1"].Value = "Báo cáo Sách Đang Thuê & Trễ Hạn";
                styleTitle(ws2.Cells["A1:F1"]);

                ws2.Cells["A3"].LoadFromCollection(data.SachTreHan, true, OfficeOpenXml.Table.TableStyles.Medium10);

                // Cần định dạng lại cột ngày tháng sau khi load
                // +3 là vì bắt đầu từ A3 (1 title, 1 header, 1 data row)
                int rowCountSheet2 = data.SachTreHan.Count + 3;
                ws2.Cells[$"D4:E{rowCountSheet2}"].Style.Numberformat.Format = "dd/MM/yyyy HH:mm";
                ws2.Cells[ws2.Dimension.Address].AutoFitColumns();


                // --- Sheet 4: Top Sách Thuê ---
                var ws3 = package.Workbook.Worksheets.Add("Top Sách Thuê");
                ws3.Cells["A1:C1"].Merge = true;
                ws3.Cells["A1"].Value = "Top Sách Được Thuê Nhiều Nhất";
                styleTitle(ws3.Cells["A1:C1"]);

                ws3.Cells["A3"].LoadFromCollection(data.TopSachThue, true, OfficeOpenXml.Table.TableStyles.Medium11);
                ws3.Cells[ws3.Dimension.Address].AutoFitColumns();

                // Lưu file
                await package.SaveAsync();
            }
        }

        private void BtnBack_Click(object sender, RoutedEventArgs e)
        {
            if (this.NavigationService != null && this.NavigationService.CanGoBack)
            {
                this.NavigationService.GoBack();
            }
        }
    }
}