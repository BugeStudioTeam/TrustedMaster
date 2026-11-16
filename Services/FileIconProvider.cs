using System;
using System.Threading.Tasks;
using Microsoft.UI.Xaml.Media;
using Microsoft.UI.Xaml.Media.Imaging;
using Windows.Storage;
using Windows.Storage.FileProperties;

namespace TrustedMaster.Services
{
    public static class FileIconProvider
    {
        public static async Task<ImageSource?> GetIconAsync(string path, uint size)
        {
            try
            {
                StorageFile file = await StorageFile.GetFileFromPathAsync(path);
                using StorageItemThumbnail thumbnail = await file.GetThumbnailAsync(ThumbnailMode.SingleItem, size, ThumbnailOptions.UseCurrentScale);

                if (thumbnail is null)
                {
                    return null;
                }

                BitmapImage image = new BitmapImage();
                await image.SetSourceAsync(thumbnail);
                return image;
            }
            catch
            {
                return null;
            }
        }
    }
}
