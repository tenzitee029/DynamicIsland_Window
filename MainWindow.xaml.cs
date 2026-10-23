using Dynamic_Island_Window.Core;
using Dynamic_Island_Window.Services;
using DynamicIslandWin.Core;
using System;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Animation;
using System.Windows.Threading;

namespace Dynamic_Island_Window
{
    public partial class MainWindow : Window
    {
        private IntPtr _windowHandle;
        private bool _isClickThrough = false;
        private MediaMonitor _mediaMonitor;
        private DispatcherTimer _autoHideTimer;
        private double _currentTargetWidth = 150;

        // Biến cho thao tác vuốt
        private bool _isDragging = false;
        private Point _dragStartPoint;
        public MainWindow()
        {
            InitializeComponent();
            // Cài đặt Timer 6 giây
            _autoHideTimer = new DispatcherTimer { Interval = TimeSpan.FromSeconds(6) };
            _autoHideTimer.Tick += (s, e) =>
            {
                _autoHideTimer.Stop();
                AnimateToPeekState(); // Hết 6s tự động trượt lên
            };

            // 1. Đẩy cửa sổ lên sát mép trên màn hình (để số 0 hoặc 10 tùy ý muốn cách mép bao nhiêu)
            this.Top = 0;
            // 2. Tự động canh giữa theo chiều ngang dựa vào độ phân giải màn hình
            this.Left = (SystemParameters.PrimaryScreenWidth - this.Width) / 2;

            // Khởi tạo Media Monitor
            _mediaMonitor = new MediaMonitor();

            // Lắng nghe sự kiện có nhạc mới
            _mediaMonitor.OnMediaChanged += (title, artist) =>
            {
                Dispatcher.Invoke(() =>
                {
                    if (string.IsNullOrEmpty(title))
                    {
                        HideCompletely(); // Không có nhạc -> Ẩn hoàn toàn
                    }
                    else
                    {
                        txtMedia.Text = $"{title} - {artist}";
                        txtMedia.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));

                        _currentTargetWidth = Math.Max(txtMedia.DesiredSize.Width + 60, 200);
                        _currentTargetWidth = Math.Min(_currentTargetWidth, 600);

                        PlayPopAndExpandAnimation(_currentTargetWidth);
                    }
                });
            };            // Sự kiện Loaded (khi cửa sổ đã hiển thị)
            this.Loaded += async (s, e) =>
            {
                MouseHook.Start();
                await _mediaMonitor.StartAsync(); // Khởi động việc theo dõi nhạc
            };

            this.Closed += (s, e) => MouseHook.Stop();
            MouseHook.OnMouseMove += MouseHook_OnMouseMove;
        }

        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);
            _windowHandle = new WindowInteropHelper(this).Handle;

            // Mặc định bật chế độ xuyên thấu khi app vừa khởi động
            EnableClickThrough(true);
        }

        private void MouseHook_OnMouseMove(int mouseX, int mouseY)
        {
            // Lấy tọa độ và kích thước hiện tại của Dynamic Island
            double islandLeft = this.Left;
            double islandTop = this.Top;
            double islandWidth = this.Width;
            double islandHeight = this.Height;

            // Kiểm tra xem con trỏ chuột có đang nằm lọt trong khu vực của viên thuốc không
            bool isMouseOverIsland = mouseX >= islandLeft && mouseX <= (islandLeft + islandWidth) &&
                                     mouseY >= islandTop && mouseY <= (islandTop + islandHeight);

            if (isMouseOverIsland && _isClickThrough)
            {
                // Chuột đã lướt vào -> Tắt chế độ xuyên thấu để có thể click được
                EnableClickThrough(false);
            }
            else if (!isMouseOverIsland && !_isClickThrough)
            {
                // Chuột lướt ra ngoài -> Bật lại chế độ xuyên thấu
                EnableClickThrough(true);
            }
        }

        public void EnableClickThrough(bool enable)
        {
            NativeMethods.SetWindowClickThrough(_windowHandle, enable);
            _isClickThrough = enable;
        }

        private void AnimateIslandWidth(double targetWidth)
        {
            // Tạo một hiệu ứng thay đổi giá trị kiểu Double (dành cho độ rộng)
            DoubleAnimation widthAnimation = new DoubleAnimation
            {
                To = targetWidth, // Độ rộng mục tiêu muốn đạt tới
                Duration = TimeSpan.FromMilliseconds(350), // Thời gian chạy (350ms là chuẩn mực độ mượt)

                // EasingFunction tạo độ "mượt" lúc bắt đầu và kết thúc (thay vì giật cục)
                EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut }
            };

            // Áp dụng hiệu ứng lên thuộc tính Width của viên thuốc (IslandBorder)
            IslandBorder.BeginAnimation(Border.WidthProperty, widthAnimation);
        }

        // 1. Tàng hình hoàn toàn
        private void HideCompletely()
        {
            _autoHideTimer.Stop();
            var shrinkAnim = new DoubleAnimation(0, TimeSpan.FromMilliseconds(300)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseIn } };
            IslandScale.BeginAnimation(ScaleTransform.ScaleXProperty, shrinkAnim);
            IslandScale.BeginAnimation(ScaleTransform.ScaleYProperty, shrinkAnim);
        }

        // 2. Nổ ra hình tròn và giãn dài
        private void PlayPopAndExpandAnimation(double targetWidth)
        {
            _autoHideTimer.Stop(); // Hủy đếm lùi

            // Đưa Island về lại vị trí trung tâm (nếu đang ở Peek State)
            IslandTranslate.BeginAnimation(TranslateTransform.YProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(300)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseOut } });
            PeekIndicator.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(200))); // Ẩn chấm xanh

            if (IslandScale.ScaleX == 0) // Nếu đang tàng hình
            {
                IslandBorder.Width = 50; // Bắt đầu bằng hình tròn
                txtMedia.Opacity = 0;

                // Hiệu ứng nảy (BackEase)
                var popAnim = new DoubleAnimation(1, TimeSpan.FromMilliseconds(350)) { EasingFunction = new BackEase { Amplitude = 0.5, EasingMode = EasingMode.EaseOut } };
                popAnim.Completed += (s, e) =>
                {
                    // Sau khi Zoom xong tròn thì giãn ra
                    AnimateIslandWidth(targetWidth);
                    txtMedia.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(300)));
                    _autoHideTimer.Start(); // Bắt đầu đếm 6s
                };

                IslandScale.BeginAnimation(ScaleTransform.ScaleXProperty, popAnim);
                IslandScale.BeginAnimation(ScaleTransform.ScaleYProperty, popAnim);
            }
            else // Nếu đang hiển thị sẵn, chỉ cần đổi kích thước
            {
                AnimateIslandWidth(targetWidth);
                txtMedia.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(300)));
                _autoHideTimer.Start(); // Reset bộ đếm 6s
            }
        }

        // 3. Trượt lên trên (Peek State)
        private void AnimateToPeekState()
        {
            // Trượt lên tọa độ Y = -35 (chỉ lòi một tí ở dưới)
            var slideUp = new DoubleAnimation(-35, TimeSpan.FromMilliseconds(400)) { EasingFunction = new QuadraticEase { EasingMode = EasingMode.EaseInOut } };
            IslandTranslate.BeginAnimation(TranslateTransform.YProperty, slideUp);

            // Thu ngắn lại thành viên thuốc nhỏ
            AnimateIslandWidth(100);

            // Ẩn chữ đi và hiện chấm xanh
            txtMedia.BeginAnimation(OpacityProperty, new DoubleAnimation(0, TimeSpan.FromMilliseconds(200)));
            PeekIndicator.BeginAnimation(OpacityProperty, new DoubleAnimation(1, TimeSpan.FromMilliseconds(400)));
        }

        private void IslandBorder_MouseDown(object sender, MouseButtonEventArgs e)
        {
            _isDragging = true;
            _dragStartPoint = e.GetPosition(this);
            IslandBorder.CaptureMouse(); // Giữ chặt chuột không cho lọt ra ngoài
            _autoHideTimer.Stop(); // Khi đang thao tác tay thì dừng đếm giờ
        }

        private void IslandBorder_MouseMove(object sender, MouseEventArgs e)
        {
            if (_isDragging)
            {
                var currentPos = e.GetPosition(this);
                var deltaY = currentPos.Y - _dragStartPoint.Y;

                // Nếu vuốt lên (deltaY âm)
                if (deltaY < -15)
                {
                    _isDragging = false;
                    IslandBorder.ReleaseMouseCapture();
                    AnimateToPeekState();
                }
                // Nếu vuốt xuống (deltaY dương) khi đang bị ẩn 1 nửa
                else if (deltaY > 15 && IslandTranslate.Y < 0)
                {
                    _isDragging = false;
                    IslandBorder.ReleaseMouseCapture();
                    PlayPopAndExpandAnimation(_currentTargetWidth); // Phục hồi lại
                }
            }
        }

        private void IslandBorder_MouseUp(object sender, MouseButtonEventArgs e)
        {
            _isDragging = false;
            IslandBorder.ReleaseMouseCapture();
            // Nếu nhả chuột ra mà vẫn đang mở full, đếm giờ 6s tiếp
            if (IslandTranslate.Y == 0) _autoHideTimer.Start();
        }
    }
}