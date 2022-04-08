using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Interop;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace WpfApp5
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        const int WM_THREAD_FINISHED = User32.WM_USER + 1;

        private Thread FirstThread = null;
        private Thread SecondThread = null;

        private ConcurrentQueue<QueueMessage> MessagesForFirstThread;
        private ConcurrentQueue<QueueMessage> MessagesForSecondThread;
        private WMReceiver Receiver;
        public MainWindow()
        {
            InitializeComponent();
            Receiver = new WMReceiver(FirstThreadMessagesHandler);
            MessagesForFirstThread = new ConcurrentQueue<QueueMessage>();
            MessagesForSecondThread = new ConcurrentQueue<QueueMessage>();
        }
        protected override void OnSourceInitialized(EventArgs e)
        {
            base.OnSourceInitialized(e);

            HwndSource HwndSource = PresentationSource.FromVisual(this) as HwndSource;
            HwndSource.AddHook(WndProc);
        }

        private IntPtr WndProc(IntPtr Hwnd, int Msg, IntPtr WParam, IntPtr LParam, ref bool Handled)
        {
            switch (Msg)
            {
                case WM_THREAD_FINISHED:
                    StartSecondThreadButton.Content = "Запустить";
                    SecondThread = null;
                    Handled = true;
                    break;
            }
            return IntPtr.Zero;
        }
        private void FirstThreadMessagesHandler(System.Windows.Forms.Message Msg)
        {
            switch (Msg.Msg)
            {
                case WM_THREAD_FINISHED:
                    StartFirstThreadButton.Content = "Запустить";
                    FirstThread = null;
                    break;
            }
        }

        private void StartFirstThreadButton_Click(object sender, RoutedEventArgs e)
        {
            if (FirstThread == null)
            {
                FirstThread = new Thread(new ParameterizedThreadStart(ExecuteFirstThread));
                ThreadParams Params = new ThreadParams();
                Params.X = 180;
                Params.Size = 25;
                Params.WindowHieght = (int)(this.Content as FrameworkElement).ActualHeight;
                Params.WindowHandle = new WindowInteropHelper(this).Handle;
                Params.ReceiverHandle = Receiver.Handle;
                FirstThread.Start(Params);
                StartFirstThreadButton.Content = "Завершить";
            }
            else
            {
                QueueMessage Msg = new QueueMessage();
                Msg.Type = 2;
                MessagesForFirstThread.Enqueue(Msg);
            }
        }
        private void ExecuteFirstThread(object Params)
        {
            ThreadParams StartParams = Params as ThreadParams;
            int X = StartParams.X;
            int Y = 0;
            int Size = StartParams.Size;
            int WindowHeight = StartParams.WindowHieght;

            IntPtr DC = User32.GetDC(StartParams.WindowHandle);
            Gdi32.SelectObject(DC, Gdi32.GetStockObject(Gdi32.StockObjects.DC_BRUSH));
            Gdi32.SelectObject(DC, Gdi32.GetStockObject(Gdi32.StockObjects.DC_PEN));
            bool MovingDown = true;
            bool StopFlag = false;
            while (!StopFlag)
            {
                Gdi32.SetDCBrushColor(DC, 0x000000FF);
                Gdi32.SetDCPenColor(DC, 0x000000FF);
                Gdi32.Ellipse(DC, X, Y, X + Size, Y + Size);
                Thread.Sleep(5);
                Gdi32.SetDCBrushColor(DC, 0x00FFFFFF);
                Gdi32.SetDCPenColor(DC, 0x00FFFFFF);
                Gdi32.Ellipse(DC, X, Y, X + Size, Y + Size);
                //логика движения
                if (MovingDown == true && (Y + Size) < WindowHeight)
                {
                    Y++;
                }
                else
                {
                    MovingDown = false;
                }
                if (MovingDown == false && Y > 0)
                {
                    Y--;
                }
                else
                {
                    MovingDown = true;
                }
                //
                QueueMessage Msg;
                if (MessagesForFirstThread.TryDequeue(out Msg))
                {
                    switch (Msg.Type)
                    {
                        case 1:
                            WindowHeight = Msg.Value;
                            break;
                        case 2:
                            StopFlag = true;
                            break;
                    }
                }
            }
            User32.ReleaseDC(StartParams.WindowHandle, DC);
            User32.PostMessage(StartParams.ReceiverHandle, WM_THREAD_FINISHED, 0, 0);
        }

        private void StartSecondThreadButton_Click(object sender, RoutedEventArgs e)
        {
            if (SecondThread == null)
            {
                SecondThread = new Thread(new ParameterizedThreadStart(ExecuteSecondThread));
                ThreadParams Params = new ThreadParams();
                Params.X = 250;
                Params.Size = 25;
                Params.WindowHieght = (int)(this.Content as FrameworkElement).ActualHeight;
                Params.WindowHandle = new WindowInteropHelper(this).Handle;
                Params.ReceiverHandle = Receiver.Handle;
                SecondThread.Start(Params);
                StartSecondThreadButton.Content = "Завершить";
            }
            else
            {
                QueueMessage Msg = new QueueMessage();
                Msg.Type = 2;
                MessagesForSecondThread.Enqueue(Msg);
            }
        }

        private void Window_SizeChanged(object sender, SizeChangedEventArgs e)
        {
            QueueMessage Msg = new QueueMessage();
            Msg.Type = 1;
            Msg.Value = (int)(this.Content as FrameworkElement).ActualHeight;
            MessagesForFirstThread.Enqueue(Msg);
            MessagesForSecondThread.Enqueue(Msg);
        }

        private void ExecuteSecondThread(object Params)
        {
            ThreadParams StartParams = Params as ThreadParams;
            int X = StartParams.X;
            int Y = 0;
            int Size = StartParams.Size;
            int WindowHeight = StartParams.WindowHieght;
            Graphics G = Graphics.FromHwnd(StartParams.WindowHandle);

            bool MovingDown = true;
            bool StopFlag = false;

            System.Drawing.Brush DrawBrush = new SolidBrush(System.Drawing.Color.Blue);
            System.Drawing.Brush ClearBrush = new SolidBrush(System.Drawing.Color.FromArgb(255, 255, 255));
            while (!StopFlag)
            {
                G.FillEllipse(DrawBrush, X, Y, Size, Size);
                Thread.Sleep(3);
                G.FillEllipse(ClearBrush, X, Y, Size, Size);
                // логика движения
                if(MovingDown == true && (Y + Size) < WindowHeight)
                {
                    Y++;
                }
                else
                {
                    MovingDown = false;
                }
                if (MovingDown == false && Y > 0)
                {
                    Y--;
                }
                else
                {
                    MovingDown = true;
                }
                //
                QueueMessage Msg;
                if (MessagesForSecondThread.TryDequeue(out Msg))
                {
                    switch (Msg.Type)
                    {
                        case 1:
                            WindowHeight = Msg.Value;
                            G.Dispose();
                            G = Graphics.FromHwnd(StartParams.WindowHandle);
                            break;
                        case 2:
                            StopFlag = true;
                            break;
                    }
                }
            }
            G.Dispose();
            User32.PostMessage(StartParams.WindowHandle, WM_THREAD_FINISHED, 0, 0);
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            e.Cancel = FirstThread != null || SecondThread != null;
        }
    }
}
