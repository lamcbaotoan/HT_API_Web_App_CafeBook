using System;
using System.IO;
using System.Windows.Media; // Thêm
using System.Windows.Media.Imaging;

namespace AppCafebookApi.Services
{
    public static class HinhAnhHelper
    {
        public static BitmapImage LoadImageFromBase64(string? base64String, string defaultImagePath)
        {
            try
            {
                if (string.IsNullOrEmpty(base64String))
                {
                    // Nếu không có Base64, tải ảnh mặc định
                    return LoadImageFromUri(defaultImagePath);
                }

                // Thử giải mã Base64
                byte[] imageBytes = Convert.FromBase64String(base64String);
                using (var ms = new MemoryStream(imageBytes))
                {
                    var image = new BitmapImage();
                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = ms;
                    image.EndInit();
                    image.Freeze();
                    return image;
                }
            }
            catch
            {
                // Nếu Base64 bị lỗi, cũng tải ảnh mặc định
                return LoadImageFromUri(defaultImagePath);
            }
        }

        private static BitmapImage LoadImageFromUri(string uriPath)
        {
            try
            {
                // Thử tải ảnh từ URI
                var image = new BitmapImage();
                var uri = new Uri($"pack://application:,,,/AppCafebookApi;component{uriPath}", UriKind.Absolute);

                image.BeginInit();
                image.UriSource = uri;
                image.CacheOption = BitmapCacheOption.OnLoad;
                image.EndInit();
                image.Freeze();
                return image;
            }
            catch (Exception)
            {
                // ---------- SỬA LỖI TẠI ĐÂY ----------
                // Nếu tải file bị lỗi (VÍ DỤ SAI BUILD ACTION)
                // Tạo một ảnh 1x1 pixel rỗng (Transparent) để không bị crash

                var image = new BitmapImage();

                // Sửa "Transparent" thành "Pbgra32"
                var writeableBitmap = new WriteableBitmap(1, 1, 96, 96, PixelFormats.Pbgra32, null);

                using (var stream = new MemoryStream())
                {
                    var encoder = new PngBitmapEncoder();
                    encoder.Frames.Add(BitmapFrame.Create(writeableBitmap));
                    encoder.Save(stream);
                    stream.Position = 0; // Reset stream về đầu

                    image.BeginInit();
                    image.CacheOption = BitmapCacheOption.OnLoad;
                    image.StreamSource = stream; // Dùng StreamSource thay vì UriSource
                    image.EndInit();
                }

                image.Freeze();
                return image;
            }
        }
    }
}