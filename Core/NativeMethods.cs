using System;
using System.Runtime.InteropServices;

namespace DynamicIslandWin.Core
{
    public static class NativeMethods
    {
        // Các hằng số hệ thống của Windows
        private const int GWL_EXSTYLE = -20;
        private const int WS_EX_TRANSPARENT = 0x00000020; // Cờ làm cửa sổ xuyên chuột

        // Import API cho bản 32-bit
        [DllImport("user32.dll", EntryPoint = "GetWindowLong")]
        private static extern int GetWindowLong32(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLong")]
        private static extern int SetWindowLong32(IntPtr hWnd, int nIndex, int dwNewLong);

        // Import API cho bản 64-bit
        [DllImport("user32.dll", EntryPoint = "GetWindowLongPtr")]
        private static extern IntPtr GetWindowLongPtr64(IntPtr hWnd, int nIndex);

        [DllImport("user32.dll", EntryPoint = "SetWindowLongPtr")]
        private static extern IntPtr SetWindowLongPtr64(IntPtr hWnd, int nIndex, IntPtr dwNewLong);

        // Hàm Wrapper để tự động xử lý 32-bit hay 64-bit
        public static IntPtr GetWindowLongPtr(IntPtr hWnd, int nIndex)
        {
            if (IntPtr.Size == 8) return GetWindowLongPtr64(hWnd, nIndex);
            return new IntPtr(GetWindowLong32(hWnd, nIndex));
        }

        public static IntPtr SetWindowLongPtr(IntPtr hWnd, int nIndex, IntPtr dwNewLong)
        {
            if (IntPtr.Size == 8) return SetWindowLongPtr64(hWnd, nIndex, dwNewLong);
            return new IntPtr(SetWindowLong32(hWnd, nIndex, dwNewLong.ToInt32()));
        }

        /// <summary>
        /// Bật/Tắt chế độ click xuyên qua cho cửa sổ
        /// </summary>
        /// <param name="hwnd">Handle của cửa sổ</param>
        /// <param name="isClickThrough">true = Xuyên chuột, false = Nhận chuột bình thường</param>
        public static void SetWindowClickThrough(IntPtr hwnd, bool isClickThrough)
        {
            IntPtr exStyle = GetWindowLongPtr(hwnd, GWL_EXSTYLE);

            if (isClickThrough)
            {
                // Thêm cờ Transparent (Xuyên chuột) bằng toán tử Bitwise OR
                exStyle = new IntPtr(exStyle.ToInt64() | WS_EX_TRANSPARENT);
            }
            else
            {
                // Bỏ cờ Transparent (Nhận chuột) bằng toán tử Bitwise AND NOT
                exStyle = new IntPtr(exStyle.ToInt64() & ~WS_EX_TRANSPARENT);
            }

            SetWindowLongPtr(hwnd, GWL_EXSTYLE, exStyle);
        }
    }
}