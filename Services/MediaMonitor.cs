using System;
using System.Threading.Tasks;
using Windows.Media.Control;

namespace Dynamic_Island_Window.Services
{
    public class MediaMonitor
    {
        // Sự kiện phát ra khi bài hát thay đổi (trả về Tên bài và Ca sĩ)
        public event Action<string, string> OnMediaChanged;

        private GlobalSystemMediaTransportControlsSessionManager _sessionManager;

        public async Task StartAsync()
        {
            // Yêu cầu quyền truy cập vào trình quản lý Media của Windows
            _sessionManager = await GlobalSystemMediaTransportControlsSessionManager.RequestAsync();

            if (_sessionManager != null)
            {
                // Lắng nghe khi người dùng đổi ứng dụng phát nhạc (vd: tắt Spotify, mở YouTube)
                _sessionManager.CurrentSessionChanged += SessionManager_CurrentSessionChanged;
                UpdateCurrentMedia();
            }
        }

        private void SessionManager_CurrentSessionChanged(GlobalSystemMediaTransportControlsSessionManager sender, CurrentSessionChangedEventArgs args)
        {
            UpdateCurrentMedia();
        }

        private async void UpdateCurrentMedia()
        {
            var session = _sessionManager.GetCurrentSession();
            if (session != null)
            {
                // Lắng nghe khi bài hát thay đổi trên cùng một ứng dụng (vd: next bài trên Spotify)
                session.MediaPropertiesChanged -= Session_MediaPropertiesChanged;
                session.MediaPropertiesChanged += Session_MediaPropertiesChanged;

                await FetchMediaProperties(session);
            }
            else
            {
                // Không có gì đang phát
                OnMediaChanged?.Invoke("", "");
            }
        }

        private async void Session_MediaPropertiesChanged(GlobalSystemMediaTransportControlsSession sender, MediaPropertiesChangedEventArgs args)
        {
            await FetchMediaProperties(sender);
        }

        private async Task FetchMediaProperties(GlobalSystemMediaTransportControlsSession session)
        {
            try
            {
                var properties = await session.TryGetMediaPropertiesAsync();
                if (properties != null)
                {
                    OnMediaChanged?.Invoke(properties.Title, properties.Artist);
                }
            }
            catch
            {
                // Bỏ qua lỗi nếu ứng dụng media đột ngột bị đóng (crash)
            }
        }
    }
}