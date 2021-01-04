// <copyright file="MainWindow.xaml.cs" company="Prime 23 Consultancy Limited">
// Copyright © 2016-2021 Prime 23 Consultancy Limited. All rights reserved.</copyright>

using System;
using System.ComponentModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Threading;
using System.Windows;
using System.Windows.Media;
using System.Windows.Media.Imaging;

using OpenCvSharp;
using OpenCvSharp.Extensions;

using PixelFormat = System.Drawing.Imaging.PixelFormat;
using Rect = OpenCvSharp.Rect;
using Size = OpenCvSharp.Size;

namespace Prime23.FaceDetector
{
    public partial class MainWindow
    {
        private readonly BackgroundWorker backgroundWorker;
        private readonly VideoCapture capture;
        private readonly CascadeClassifier cascadeClassifier;

        public MainWindow()
        {
            this.InitializeComponent();

            this.capture = new VideoCapture();
            this.cascadeClassifier = new CascadeClassifier(@"haarcascade_frontalface_default.xml");

            this.backgroundWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true, WorkerSupportsCancellation = true
            };
            this.backgroundWorker.DoWork += this.BackgroundWorker_DoWork;
            this.backgroundWorker.ProgressChanged += this.BackgroundWorker_ProgressChanged;
        }

        private static BitmapSource CreateBitmapSourceFromGdiBitmap(Bitmap bitmap)
        {
            if (bitmap == null)
            {
                throw new ArgumentNullException(nameof(bitmap));
            }

            Rectangle rect = new(0, 0, bitmap.Width, bitmap.Height);

            BitmapData bitmapData = bitmap.LockBits(
                rect,
                ImageLockMode.ReadWrite,
                PixelFormat.Format32bppArgb);

            try
            {
                int size = (rect.Width * rect.Height) * 4;

                return BitmapSource.Create(
                    bitmap.Width,
                    bitmap.Height,
                    bitmap.HorizontalResolution,
                    bitmap.VerticalResolution,
                    PixelFormats.Bgra32,
                    null,
                    bitmapData.Scan0,
                    size,
                    bitmapData.Stride);
            }
            finally
            {
                bitmap.UnlockBits(bitmapData);
            }
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bgWorker = (BackgroundWorker)sender;

            while (!bgWorker.CancellationPending)
            {
                using (Mat frameMat = this.capture.RetrieveMat())
                {
                    Rect[] rects = this.cascadeClassifier.DetectMultiScale(
                        frameMat,
                        1.1,
                        5,
                        HaarDetectionType.ScaleImage,
                        new Size(30, 30));

                    foreach (Rect rect in rects)
                    {
                        Cv2.Rectangle(frameMat, rect, Scalar.Red);
                    }

                    this.Dispatcher.Invoke(() => this.Info.Text = $"{rects.Length} faces detected");

                    Bitmap frameBitmap = frameMat.ToBitmap();
                    bgWorker.ReportProgress(0, frameBitmap);
                }

                Thread.Sleep(100);
            }
        }

        private void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            Bitmap frameBitmap = (Bitmap)e.UserState;
            this.Image.Source = CreateBitmapSourceFromGdiBitmap(frameBitmap);
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            this.backgroundWorker.CancelAsync();
            this.capture.Dispose();
            this.cascadeClassifier.Dispose();
        }

        private void MainWindow_OnLoaded(object sender, RoutedEventArgs e)
        {
            this.capture.Open(0, VideoCaptureAPIs.ANY);
            if (!this.capture.IsOpened())
            {
                this.Close();
                return;
            }

            this.backgroundWorker.RunWorkerAsync();
        }
    }
}