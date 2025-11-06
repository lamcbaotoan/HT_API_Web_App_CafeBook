using AppCafebookApi.Services; // <--- THÊM DÒNG NÀY
using CafebookModel.Model.Data;
using CafebookModel.Utils; // <--- SỬ DỤNG HELPER MỚI
using System;
using System.Windows;
using System.Windows.Media; // Thêm
using System.Windows.Media.Imaging; // Thêm
using System.Windows.Threading;
using System.Windows.Controls; // <-- Thêm dòng này để nhận diện StackPanel
using System.Linq;             // <-- Thêm dòng này để nhận diện .OfType và .FirstOrDefault

namespace AppCafebookApi.View.Common
{
    public partial class WelcomeWindow : Window
    {
        private readonly NhanVienDto? _user;
        private DispatcherTimer? _timer;

        public WelcomeWindow()
        {
            InitializeComponent();
        }

        public WelcomeWindow(NhanVienDto user)
        {
            InitializeComponent();
            _user = user;
        }

        private void Window_Loaded(object sender, RoutedEventArgs e)
        {
            if (_user != null)
            {
                txtUserGreeting.Text = $"Xin chào, {_user.HoTen}";

                // 1. Tải ảnh bằng HinhAnhHelper
                BitmapImage avatar = HinhAnhHelper.LoadImage(
                    _user.AnhDaiDien ?? string.Empty,
                    HinhAnhPaths.DefaultAvatar
                );

                // 2. GÁN ẢNH VÀO UI (ĐÃ SỬA)
                if (avatar != null)
                {
                    // Trong WelcomeWindow.xaml, không có Image, 
                    // chúng ta gán vào <Ellipse.Fill> của <Ellipse>

                    // (Giả sử Ellipse nằm trong StackPanel)
                    var stackPanel = txtWelcome.Parent as StackPanel;
                    if (stackPanel != null)
                    {
                        // Tìm Ellipse (là control đầu tiên trong StackPanel)
                        var ellipse = stackPanel.Children.OfType<System.Windows.Shapes.Ellipse>().FirstOrDefault();
                        if (ellipse != null)
                        {
                            ellipse.Fill = new ImageBrush(avatar)
                            {
                                Stretch = Stretch.UniformToFill
                            };
                        }
                    }
                }
            }

            // Cấu hình timer
            _timer = new DispatcherTimer
            {
                Interval = TimeSpan.FromMilliseconds(2500)
            };
            _timer.Tick += Timer_Tick;
            _timer.Start();
        }

        private void Timer_Tick(object? sender, EventArgs e)
        {
            _timer?.Stop();
            this.Close();
        }

        // XÓA 2 HÀM LoadImageFromBase64 VÀ LoadImageFromPath CŨ Ở ĐÂY
    }
}