using System;
using System.ComponentModel;
using System.Drawing;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using System.Windows.Forms.VisualStyles;
using System.Windows.Interop;
using WamWooWam.Wpf.Interop;
using static WamWooWam.Wpf.Interop.WindowMessages;

namespace WamWooWam.Wpf
{
    /// <summary>
    /// A better <see cref="NotifyIcon"/>, adapted from https://www.codeproject.com/Tips/832441/NET-WinForms-Tray-Icon-Implemenation-with-Win-and
    /// </summary>
    /// <filterpriority>2</filterpriority>
    public class TrayIcon : Component
    {
        #region P/Invoke and structures

        [DllImport("user32.dll", EntryPoint = "RegisterWindowMessage", CharSet = CharSet.Auto)]
        private static extern int RegisterWindowMessage(string text);

        [DllImport("shell32.dll", CharSet = CharSet.Auto, EntryPoint = "Shell_NotifyIcon")]
        private static extern bool Shell_NotifyIcon(NotifyIconMessages message, NOTIFYICONDATA pnid);

        [DllImport("shell32.dll", CharSet = CharSet.Auto, EntryPoint = "Shell_NotifyIcon")]
        private static extern bool Shell_NotifyIcon(NotifyIconMessages message, NOTIFYICONDATA4 pnid);

        [DllImport("shell32.dll", CharSet = CharSet.Auto, EntryPoint = "Shell_NotifyIconGetRect")]
        private static extern int Shell_NotifyIconGetRect(ref NOTIFYICONIDENTIFIER identifier, ref Rectangle iconLocation);

        [DllImport("user32.dll", CharSet = CharSet.Auto, ExactSpelling = true, EntryPoint = "SetForegroundWindow")]
        public static extern bool SetForegroundWindow(IntPtr hWnd);        

        #endregion

        private string _hintText = string.Empty;
        private Icon _icon;
        private Form _ownerForm;
        private bool _enabled;
        private bool _iconAdded;
        private int _id;
        private bool _doubleClick;
        private bool _showDefaultTips = true;

        private static readonly object EVENT_MOUSEDOWN = new object();
        private static readonly object EVENT_MOUSEMOVE = new object();
        private static readonly object EVENT_MOUSEUP = new object();
        private static readonly object EVENT_MOUSECLICK = new object();
        private static readonly object EVENT_MOUSEDOUBLECLICK = new object();
        private static readonly object EVENT_BALLOONTIPSHOWN = new object();
        private static readonly object EVENT_BALLOONTIPCLICKED = new object();
        private static readonly object EVENT_BALLOONTIPCLOSED = new object();
        private static readonly object EVENT_TOOLTIPSHOWN = new object();
        private static readonly object EVENT_TOOLTIPCLOSED = new object();
        private static readonly object EVENT_TOOLTIPMEASURE = new object();
        private static readonly object EVENT_TOOLTIPPAINT = new object();
        private static readonly object EVENT_CONTEXTMENUSHOW = new object();
        private static int WM_TASKBARCREATED = RegisterWindowMessage("TaskbarCreated");

        private static int _trayIconId = 0;
        private const int CALLBACK_MESSAGE = 2048;

        public TrayIcon()
        {
            _id = ++_trayIconId;
        }

        #region Public properties

        [Description("Tray icon text. Should be less than 64 chars")]
        public string HintText
        {
            get { return _hintText; }
            set
            {
                if (_hintText.Length >= 128)
                {
                    if (TrimLongText)
                        value = value.Substring(0, 124) + "...";
                    else
                        throw new ArgumentOutOfRangeException("value", _hintText.Length, "Hint text should be less than 128 chars long");
                }
                _hintText = value;
                if (_enabled)
                    UpdateIcon();
            }
        }

        [Description("Hint text to be shown in hint when running on Vista+")]
        public string LongHintText { get; set; } = string.Empty;

        [Description("Tray icon")]
        public Icon Icon
        {
            get { return _icon; }
            set
            {
                _icon = value;
                if (_enabled)
                    UpdateIcon();
            }
        }

        [Description("Owner form")]
        public Form OwnerForm
        {
            get { return _ownerForm; }
            set
            {
                _ownerForm = value;
                if (_enabled)
                    UpdateIcon();
            }
        }

        [Description("If the icon is visible")]
        public bool Enabled
        {
            get { return _enabled; }
            set
            {
                _enabled = value;
                if (value)
                    UpdateIcon();
                else
                    RemoveIcon();
            }
        }

        [Description("GUID for this icon of this application")]
        public Guid Guid { get; set; }

        [Description("Context menu shown when user releases right mouse button on the tray icon")]
        public ContextMenuStrip ContextMenu { get; set; }

        [Description("Trim long text in tray icon params if is too long. Will throw exception otherwise")]
        [DefaultValue(false)]
        public bool TrimLongText { get; set; }

        [Description("Use large system icons is it is possible (WinVista or later)")]
        [DefaultValue(true)]
        public bool UseLargeIcons { get; set; } = true;

        [Description("Show default tips instead of user-defined UI when running on Vista or later")]
        [DefaultValue(true)]
        public bool ShowDefaultTips
        {
            get { return _showDefaultTips; }
            set
            {
                _showDefaultTips = value;
                if (_enabled)
                    UpdateIcon();
            }
        }

        #endregion

        #region Public events

        /// <summary>
        /// Occurs when the balloon tip is clicked.
        /// </summary>
        [Description("Occurs when the balloon tip is clicked.")]
        public event EventHandler BalloonTipClicked
        {
            add
            {
                Events.AddHandler(EVENT_BALLOONTIPCLICKED, value);
            }
            remove
            {
                Events.RemoveHandler(EVENT_BALLOONTIPCLICKED, value);
            }
        }

        /// <summary>
        /// Occurs when the context menu is required to show
        /// </summary>
        [Description("Occurs when the context menu is required to show")]
        public event EventHandler ContextMenuShowing
        {
            add
            {
                Events.AddHandler(EVENT_CONTEXTMENUSHOW, value);
            }
            remove
            {
                Events.RemoveHandler(EVENT_CONTEXTMENUSHOW, value);
            }
        }

        /// <summary>
        /// Occurs when the tooltip is being shown.
        /// Works only when running on Vista and <see cref="ShowDefaultTips"/> is false
        /// </summary>
        [Description("Occurs when the tooltip is being shown")]
        public event EventHandler<TooltipShowArgs> TooltipShown
        {
            add
            {
                Events.AddHandler(EVENT_TOOLTIPSHOWN, value);
            }
            remove
            {
                Events.RemoveHandler(EVENT_TOOLTIPSHOWN, value);
            }
        }

        /// <summary>
        /// Occurs when the tooltip is closed.
        /// Works only when running on Vista and <see cref="ShowDefaultTips"/> is false
        /// </summary>
        [Description("Occurs when the tooltip is closed.")]
        public event EventHandler TooltipClosed
        {
            add
            {
                Events.AddHandler(EVENT_TOOLTIPCLOSED, value);
            }
            remove
            {
                Events.RemoveHandler(EVENT_TOOLTIPCLOSED, value);
            }
        }

        /// <summary>
        /// Occurs when the balloon tip is closed by the user.
        /// </summary>
        [Description("Occurs when the balloon tip is closed by the user.")]
        public event EventHandler BalloonTipClosed
        {
            add
            {
                Events.AddHandler(EVENT_BALLOONTIPCLOSED, value);
            }
            remove
            {
                Events.RemoveHandler(EVENT_BALLOONTIPCLOSED, value);
            }
        }

        /// <summary>
        /// Occurs when the balloon tip is displayed on the screen.
        /// </summary>
        [Description("Occurs when the balloon tip is displayed on the screen.")]
        public event EventHandler BalloonTipShown
        {
            add
            {
                Events.AddHandler(EVENT_BALLOONTIPSHOWN, value);
            }
            remove
            {
                Events.RemoveHandler(EVENT_BALLOONTIPSHOWN, value);
            }
        }

        /// <summary>
        /// Occurs when the user clicks a <see cref="TrayIcon"/> with the mouse.
        /// </summary>
        [Description("Occurs when the user clicks a TrayIcon with the mouse.")]
        public event MouseEventHandler MouseClick
        {
            add
            {
                Events.AddHandler(EVENT_MOUSECLICK, value);
            }
            remove
            {
                Events.RemoveHandler(EVENT_MOUSECLICK, value);
            }
        }

        /// <summary>
        /// Occurs when the user double-clicks the <see cref="TrayIcon"/> with the mouse.
        /// </summary>
        [Description("Occurs when the user double-clicks the TrayIcon with the mouse.")]
        public event MouseEventHandler MouseDoubleClick
        {
            add
            {
                Events.AddHandler(EVENT_MOUSEDOUBLECLICK, value);
            }
            remove
            {
                Events.RemoveHandler(EVENT_MOUSEDOUBLECLICK, value);
            }
        }

        /// <summary>
        /// Occurs when the user presses the mouse button while the pointer is over the icon in the notification area of the taskbar.
        /// </summary>
        [Description("Occurs when the user presses the mouse button while the pointer is over the icon in the notification area of the taskbar.")]
        public event MouseEventHandler MouseDown
        {
            add
            {
                Events.AddHandler(EVENT_MOUSEDOWN, value);
            }
            remove
            {
                Events.RemoveHandler(EVENT_MOUSEDOWN, value);
            }
        }

        /// <summary>
        /// Occurs when the user moves the mouse while the pointer is over the icon in the notification area of the taskbar.
        /// </summary>
        [Description("Occurs when the user moves the mouse while the pointer is over the icon in the notification area of the taskbar.")]
        public event MouseEventHandler MouseMove
        {
            add
            {
                Events.AddHandler(EVENT_MOUSEMOVE, value);
            }
            remove
            {
                Events.RemoveHandler(EVENT_MOUSEMOVE, value);
            }
        }

        /// <summary>
        /// Occurs when the user releases the mouse button while the pointer is over the icon in the notification area of the taskbar.
        /// </summary>
        [Description("Occurs when the user releases the mouse button while the pointer is over the icon in the notification area of the taskbar.")]
        public event MouseEventHandler MouseUp
        {
            add
            {
                Events.AddHandler(EVENT_MOUSEUP, value);
            }
            remove
            {
                Events.RemoveHandler(EVENT_MOUSEUP, value);
            }
        }

        #endregion

        #region Input proxies

        private void OnBalloonTipClicked()
        {
            var eventHandler = (EventHandler)Events[EVENT_BALLOONTIPCLICKED];
            if (eventHandler == null)
                return;
            eventHandler(this, EventArgs.Empty);
        }

        private void OnToolTipShown(int x, int y)
        {
            return;
        }

        private void OnToolTipClosed()
        {
            if (_showDefaultTips)
                return;
        }

        private void OnBalloonTipClosed()
        {
            var eventHandler = (EventHandler)Events[EVENT_BALLOONTIPCLOSED];
            if (eventHandler == null)
                return;
            eventHandler(this, EventArgs.Empty);
        }

        private void OnBalloonTipShown()
        {
            var eventHandler = (EventHandler)Events[EVENT_BALLOONTIPSHOWN];
            if (eventHandler == null)
                return;
            eventHandler(this, EventArgs.Empty);
        }

        private void OnMouseClick(MouseEventArgs mea)
        {
            var mouseEventHandler = (MouseEventHandler)Events[EVENT_MOUSECLICK];
            if (mouseEventHandler == null)
                return;
            mouseEventHandler(this, mea);
        }

        private void OnMouseDoubleClick(MouseEventArgs mea)
        {
            var mouseEventHandler = (MouseEventHandler)Events[EVENT_MOUSEDOUBLECLICK];
            if (mouseEventHandler == null)
                return;
            mouseEventHandler(this, mea);
        }

        private void OnMouseDown(MouseEventArgs e)
        {
            var mouseEventHandler = (MouseEventHandler)Events[EVENT_MOUSEDOWN];
            if (mouseEventHandler == null)
                return;
            mouseEventHandler(this, e);
        }

        private void OnMouseMove(MouseEventArgs e)
        {
            var mouseEventHandler = (MouseEventHandler)Events[EVENT_MOUSEMOVE];
            if (mouseEventHandler == null)
                return;
            mouseEventHandler(this, e);
        }

        private void OnMouseUp(MouseEventArgs e)
        {
            var mouseEventHandler = (MouseEventHandler)Events[EVENT_MOUSEUP];
            if (mouseEventHandler == null)
                return;
            mouseEventHandler(this, e);
        }

        private void WmMouseDown(ref Message m, MouseButtons button, int clicks)
        {
            if (clicks == 2)
            {
                OnMouseDoubleClick(new MouseEventArgs(button, 2, 0, 0, 0));
                _doubleClick = true;
            }
            OnMouseDown(new MouseEventArgs(button, clicks, 0, 0, 0));
        }

        private void WmMouseMove(ref Message m)
        {
            OnMouseMove(new MouseEventArgs(Control.MouseButtons, 0, 0, 0, 0));
        }

        private void WmMouseUp(ref Message m, MouseButtons button)
        {
            OnMouseUp(new MouseEventArgs(button, 0, 0, 0, 0));
            if (!_doubleClick)
            {
                OnMouseClick(new MouseEventArgs(button, 0, 0, 0, 0));
            }
            _doubleClick = false;
        }

        private void WmTaskbarCreated()
        {
            _iconAdded = false;
            if (_enabled)
                UpdateIcon();
        }

        #endregion

        protected override void Dispose(bool disposing)
        {
            if (_ownerForm != null)
                RemoveIcon();
            _ownerForm = null;
            base.Dispose(disposing);
        }

        #region Public methods

        /// <summary>
        /// This method should be called from the owner form's WndProc message.
        /// The simplest way to do this is
        ///<code>protected override void WndProc(ref Message m)
        ///{
        ///  if (myTrayIcon.WndProc(ref m))
        ///     return;
        ///   base.WndProc(ref m);
        ///}</code>
        /// Yes, not the best solution. But it seems to be impossible to handle windows messages somehow different.
        /// </summary>
        /// <param name="msg">Message received</param>
        /// <returns>True if message was handled by TrayIcon (and no further processing required) and false otherwise.</returns>
        public bool WndProc(ref Message msg)
        {
            if (msg.Msg == (int)WM_DESTROY)
            {
                Dispose(true);
                return false;
            }

            if (msg.Msg != CALLBACK_MESSAGE)
                return false;

            if (msg.LParam == (IntPtr)WM_TASKBARCREATED)
            {
                WmTaskbarCreated();
                return true;
            }

            switch ((int)msg.LParam & 0xFFFF) // for WinVista and later lParam is restricted to 16 bits
            {
                case (int)WM_COMMAND: // 273, commands, ignored
                    return true;
                case (int)WM_INITMENUPOPUP: // 279, init popup menu, ignored
                    return true;
                case (int)WM_MOUSEMOVE:
                    WmMouseMove(ref msg);
                    return true;
                case (int)WM_LBUTTONDOWN:
                    WmMouseDown(ref msg, MouseButtons.Left, 1);
                    return true;
                case (int)WM_LBUTTONUP:
                    WmMouseUp(ref msg, MouseButtons.Left);
                    return true;
                case (int)WM_LBUTTONDBLCLK:
                    WmMouseDown(ref msg, MouseButtons.Left, 2);
                    return true;
                case (int)WM_RBUTTONDOWN:
                    WmMouseDown(ref msg, MouseButtons.Right, 1);
                    return true;
                case (int)WM_RBUTTONUP:
                    ShowContextMenu();
                    WmMouseUp(ref msg, MouseButtons.Right);
                    return true;
                case (int)WM_RBUTTONDBLCLK:
                    WmMouseDown(ref msg, MouseButtons.Right, 2);
                    return true;
                case (int)WM_MBUTTONDOWN:
                    WmMouseDown(ref msg, MouseButtons.Middle, 1);
                    return true;
                case (int)WM_MBUTTONUP:
                    WmMouseUp(ref msg, MouseButtons.Middle);
                    return true;
                case (int)WM_MBUTTONDBLCLK:
                    WmMouseDown(ref msg, MouseButtons.Middle, 2);
                    return true;
                case (int)NotifyIconParamMessages.NIN_BALLOONSHOW:
                    OnBalloonTipShown();
                    return true;
                case (int)NotifyIconParamMessages.NIN_BALLOONHIDE:
                    OnBalloonTipClosed();
                    return true;
                case (int)NotifyIconParamMessages.NIN_BALLOONTIMEOUT:
                    OnBalloonTipClosed();
                    return true;
                case (int)NotifyIconParamMessages.NIN_BALLOONUSERCLICK:
                    OnBalloonTipClicked();
                    return true;
                case (int)NotifyIconParamMessages.NIN_POPUPOPEN:
                    OnToolTipShown(((int)msg.WParam) & 0xFFFF, (((int)msg.WParam) >> 16) & 0xFFFF);
                    return true;
                case (int)NotifyIconParamMessages.NIN_POPUPCLOSE:
                    OnToolTipClosed();
                    return true;
            }

            return false;
        }

        /// <summary>
        /// Show balloon notification
        /// </summary>
        /// <param name="caption">Caption of the balloon. Limited to 64 chars</param>
        /// <param name="text">Text of the balloon. Limited to 256 chars</param>
        /// <param name="balloonIcon">The type of an icon to show</param>
        /// <param name="noSound">Set to true to suppress default system sound</param>
        /// <param name="realTime">Set to true if the notification is relevant only now. If Windows cannot show
        /// the notification now - it will be discarded</param>
        /// <exception cref="ArgumentOutOfRangeException">Shown if <see cref="TrimLongText"/> is false and <paramref name="caption"/> or <paramref name="text"/> overflows the limits</exception>
        public void ShowBalloonTip(string caption, string text, NotifyIconIcons balloonIcon, bool noSound = false, bool realTime = false)
        {
            if (_ownerForm == null)
                return;

            if (caption.Length >= 64)
            {
                if (TrimLongText)
                    caption = caption.Substring(0, 63);
                else
                    throw new ArgumentOutOfRangeException("caption", caption.Length, "Caption should be less than 64 chars");
            }
            if (text.Length >= 256)
            {
                if (TrimLongText)
                    text = text.Substring(0, 255);
                else
                    throw new ArgumentOutOfRangeException("text", text.Length, "Text should be less than 64 chars");
            }

            var data = new NOTIFYICONDATA4();
            InitNotifyIconData(ref data);
            data.szInfoTitle = caption;
            data.szInfo = text;
            data.uFlags = NotifyIconFlags.Info;
            if (realTime)
                data.uFlags |= NotifyIconFlags.RealTime;
            switch (balloonIcon)
            {
                case NotifyIconIcons.None:
                    data.dwInfoFlags = NotifyIconInfoFlags.IconNone;
                    break;
                case NotifyIconIcons.Error:
                    data.dwInfoFlags = NotifyIconInfoFlags.IconError;
                    break;
                case NotifyIconIcons.Info:
                    data.dwInfoFlags = NotifyIconInfoFlags.IconInfo;
                    break;
                case NotifyIconIcons.Warning:
                    data.dwInfoFlags = NotifyIconInfoFlags.IconWarning;
                    break;
                case NotifyIconIcons.User:
                    data.dwInfoFlags = NotifyIconInfoFlags.IconUser;
                    break;
            }

            data.dwInfoFlags |= NotifyIconInfoFlags.RespectQuietTime;
            if (UseLargeIcons)
                data.dwInfoFlags |= NotifyIconInfoFlags.LargeIcon;
            if (noSound)
                data.dwInfoFlags |= NotifyIconInfoFlags.NoSound;

            if (!Shell_NotifyIcon(NotifyIconMessages.Modify, data))
                throw new Win32Exception("Shell_NotifyIcon failed");
        }

        /// <summary>
        /// Show balloon notification
        /// </summary>
        /// <param name="caption">Caption of the balloon. Limited to 64 chars</param>
        /// <param name="text">Text of the balloon. Limited to 256 chars</param>
        /// <param name="balloonIcon">Icon to show in balloon. Recommended to be at least 32x32</param>
        /// <param name="backupIcon">Icon type when running on WindowsXP which is unable to use custom icons here</param>
        /// <param name="noSound">Set to true to suppress default system sound</param>
        /// <param name="realTime">Set to true if the notification is relevant only now. If Windows cannot show
        /// the notification now - it will be discarded</param>
        /// <exception cref="ArgumentOutOfRangeException">Shown if <see cref="TrimLongText"/> is false and <paramref name="caption"/> or <paramref name="text"/> overflows the limits</exception>
        public void ShowBalloonTip(string caption, string text, Icon balloonIcon, NotifyIconIcons backupIcon, bool noSound = false, bool realTime = false)
        {
            if (_ownerForm == null)
                return;

            if (caption.Length >= 64)
            {
                if (TrimLongText)
                    caption = caption.Substring(0, 61) + "...";
                else
                    throw new ArgumentOutOfRangeException("caption", caption.Length, "Caption should be less than 64 chars");
            }
            if (text.Length >= 256)
            {
                if (TrimLongText)
                    text = text.Substring(0, 253) + "...";
                else
                    throw new ArgumentOutOfRangeException("text", text.Length, "Text should be less than 64 chars");
            }

            var data = new NOTIFYICONDATA4();
            InitNotifyIconData(ref data);
            data.szInfoTitle = caption;
            data.szInfo = text;
            data.uFlags = NotifyIconFlags.Info;
            if (realTime)
                data.uFlags |= NotifyIconFlags.RealTime;
            data.dwInfoFlags = NotifyIconInfoFlags.IconUser;
            data.hBalloonIcon = balloonIcon.Handle;

            data.dwInfoFlags |= NotifyIconInfoFlags.RespectQuietTime;
            if (UseLargeIcons)
                data.dwInfoFlags |= NotifyIconInfoFlags.LargeIcon;
            if (noSound)
                data.dwInfoFlags |= NotifyIconInfoFlags.NoSound;

            if (!Shell_NotifyIcon(NotifyIconMessages.Modify, data))
                throw new Win32Exception("Shell_NotifyIcon failed");
        }

        /// <summary>
        /// Gets position of the tray icon on the screen
        /// </summary>
        /// <returns>Position of the tray icon on the screen</returns>
        /// <remarks>Works only on Win7 and later. Will return <see cref="Cursor.Position"/> when running on earlier OS</remarks>
        public Point GetIconPosition()
        {
            var ident = new NOTIFYICONIDENTIFIER
            {
                cbSize = (uint)Marshal.SizeOf(typeof(NOTIFYICONIDENTIFIER)),
                hWnd = _ownerForm.Handle,
                uID = (uint)_id
            };
            //ident.guidItem = guid; // don't know why, but setting GUID produces "The parameter is incorrect" HRESULT

            var rect = new Rectangle();
            var result = Shell_NotifyIconGetRect(ref ident, ref rect);
            if (result != 0)
                throw new Win32Exception(result);

            return new Point(rect.Left, rect.Top);
        }

        /// <summary>
        /// Sets the focus to taskbar
        /// Recommended to call when context menu canceled
        /// </summary>
        public void SetFocus()
        {
            var data = new NOTIFYICONDATA();
            InitNotifyIconData(ref data);
            Shell_NotifyIcon(NotifyIconMessages.SetFocus, data);
        }

        #endregion

        private void ShowBalloonTipLegacy(string caption, string text, NotifyIconIcons balloonIcon, bool noSound = false, bool realTime = false)
        {
            var data = new NOTIFYICONDATA();
            InitNotifyIconData(ref data);
            data.szInfoTitle = caption;
            data.szInfo = text;
            data.uFlags = NotifyIconFlags.Info;
            if (realTime)
                data.uFlags |= NotifyIconFlags.RealTime;
            switch (balloonIcon)
            {
                case NotifyIconIcons.None:
                    data.dwInfoFlags = NotifyIconInfoFlags.IconNone;
                    break;
                case NotifyIconIcons.Error:
                    data.dwInfoFlags = NotifyIconInfoFlags.IconError;
                    break;
                case NotifyIconIcons.Info:
                    data.dwInfoFlags = NotifyIconInfoFlags.IconInfo;
                    break;
                case NotifyIconIcons.Warning:
                    data.dwInfoFlags = NotifyIconInfoFlags.IconWarning;
                    break;
                case NotifyIconIcons.User:
                    data.dwInfoFlags = NotifyIconInfoFlags.IconUser;
                    break;
            }

            data.dwInfoFlags |= NotifyIconInfoFlags.RespectQuietTime;
            if (UseLargeIcons)
                data.dwInfoFlags |= NotifyIconInfoFlags.LargeIcon;
            if (noSound)
                data.dwInfoFlags |= NotifyIconInfoFlags.NoSound;

            if (!Shell_NotifyIcon(NotifyIconMessages.Modify, data))
                throw new Win32Exception("Shell_NotifyIcon failed");
        }

        private void InitNotifyIconData(ref NOTIFYICONDATA data)
        {
            data.hWnd = _ownerForm.Handle;
            data.uID = _id;
            data.guidItem = Guid;
        }

        private void InitNotifyIconData(ref NOTIFYICONDATA4 data)
        {
            data.hWnd = _ownerForm.Handle;
            data.uID = _id;
            data.guidItem = Guid;
        }

        private void UpdateIcon()
        {
            if (_ownerForm == null)
                return;
            if (DesignMode)
                return;

            var data = new NOTIFYICONDATA4();
            InitNotifyIconData(ref data);

            if (!_iconAdded)
            {
                data.uTimeoutOrVersion = 4;

                if (!Shell_NotifyIcon(NotifyIconMessages.Add, data))
                    throw new Win32Exception("Shell_NotifyIcon failed");

                if (!Shell_NotifyIcon(NotifyIconMessages.SetVersion, data))
                    throw new Win32Exception("Shell_NotifyIcon failed");
            }

            data.hIcon = _icon == null ? IntPtr.Zero : _icon.Handle;
            data.szTip = _hintText;
            data.uCallbackMessage = CALLBACK_MESSAGE;
            data.uFlags = NotifyIconFlags.Icon | NotifyIconFlags.Message | NotifyIconFlags.Tip;
            if (_showDefaultTips)
                data.uFlags |= NotifyIconFlags.ShowTip;

            if (!Shell_NotifyIcon(NotifyIconMessages.Modify, data))
                throw new Win32Exception("Shell_NotifyIcon failed");

            _iconAdded = true;
        }

        private void UpdateIconLegacy()
        {
            var data = new NOTIFYICONDATA();
            InitNotifyIconData(ref data);

            if (!_iconAdded)
            {
                if (!Shell_NotifyIcon(NotifyIconMessages.Add, data))
                    throw new Win32Exception("Shell_NotifyIcon failed");
            }

            data.hIcon = _icon == null ? IntPtr.Zero : _icon.Handle;
            data.szTip = _hintText;
            data.uCallbackMessage = CALLBACK_MESSAGE;
            data.uFlags = NotifyIconFlags.Icon | NotifyIconFlags.Message | NotifyIconFlags.Tip | NotifyIconFlags.Guid;

            Shell_NotifyIcon(NotifyIconMessages.Modify, data);

            _iconAdded = true;
        }

        private void RemoveIcon()
        {
            if (_ownerForm == null)
                return;
            if (!_iconAdded)
                return;

            var data = new NOTIFYICONDATA4();
            InitNotifyIconData(ref data);

            if (!Shell_NotifyIcon(NotifyIconMessages.Delete, data))
                throw new Win32Exception("Shell_NotifyIcon failed");
            _iconAdded = false;
        }

        private void RemoveIconLegacy()
        {
            if (_ownerForm == null)
                return;
            if (!_iconAdded)
                return;
            var data = new NOTIFYICONDATA();
            InitNotifyIconData(ref data);

            if (!Shell_NotifyIcon(NotifyIconMessages.Delete, data))
                throw new Win32Exception("Shell_NotifyIcon failed");
            _iconAdded = false;
        }

        private void ShowContextMenu()
        {
            if (ContextMenu != null)
            {
                SetForegroundWindow(ContextMenu.Handle);
                ContextMenu.Show(_ownerForm, _ownerForm.PointToClient(GetIconPosition()), ToolStripDropDownDirection.AboveRight);
                return;
            }

            var eventHandler = (EventHandler)Events[EVENT_CONTEXTMENUSHOW];
            if (eventHandler == null)
                return;
            eventHandler(this, EventArgs.Empty);
        }
    }

    public class TooltipShowArgs : EventArgs
    {
        public TooltipShowArgs(Point position)
        {
            Position = position;
        }

        public Point Position { get; }
    }

    public enum NotifyIconIcons
    {
        None,
        Info,
        Warning,
        Error,
        User
    }
}