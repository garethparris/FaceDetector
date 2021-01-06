// <copyright file="MainWindow.xaml.cs" company="Prime 23 Consultancy Limited">
// Copyright © 2016-2021 Prime 23 Consultancy Limited. All rights reserved.</copyright>

using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Drawing;
using System.Linq;
using System.Threading;
using System.Windows;

using OpenCvSharp;
using OpenCvSharp.Extensions;
using OpenCvSharp.WpfExtensions;

using Point = OpenCvSharp.Point;
using Rect = OpenCvSharp.Rect;
using Size = OpenCvSharp.Size;

namespace Prime23.FaceDetector
{
    public partial class MainWindow
    {
        private static readonly Scalar EyeColor = new(255, 0, 0);
        private static readonly Scalar FaceColor = new(255, 0, 255);

        private readonly BackgroundWorker backgroundWorker;
        private readonly VideoCapture capture;
        private readonly CascadeClassifier eyesCascadeClassifier;
        private readonly CascadeClassifier faceCascadeClassifier;

        public MainWindow()
        {
            this.InitializeComponent();

            this.capture = new VideoCapture();
            this.faceCascadeClassifier = new CascadeClassifier(@"haarcascade_frontalface_alt.xml");
            this.eyesCascadeClassifier = new CascadeClassifier(@"haarcascade_eye_tree_eyeglasses.xml");

            this.backgroundWorker = new BackgroundWorker
            {
                WorkerReportsProgress = true, WorkerSupportsCancellation = true
            };
            this.backgroundWorker.DoWork += this.BackgroundWorker_DoWork;
            this.backgroundWorker.ProgressChanged += this.BackgroundWorker_ProgressChanged;
        }

        private void BackgroundWorker_DoWork(object sender, DoWorkEventArgs e)
        {
            BackgroundWorker bgWorker = (BackgroundWorker)sender;

            while (!bgWorker.CancellationPending)
            {
                using (Mat frameMat = this.capture.RetrieveMat())
                {
                    Rect[] faceRects = this.faceCascadeClassifier.DetectMultiScale(frameMat);

                    foreach (Rect faceRect in faceRects)
                    {
                        Point point = new(faceRect.X + faceRect.Width / 2, faceRect.Y + faceRect.Height / 2);
                        Cv2.Ellipse(frameMat, point, new Size(faceRect.Width / 2, faceRect.Height / 2), 0, 0, 360, FaceColor);

                        Mat faceRoi = frameMat.SubMat(faceRect);

                        Rect[] eyeRects = this.eyesCascadeClassifier.DetectMultiScale(faceRoi);

                        foreach (Rect eyeRect in eyeRects)
                        {
                            Point eyeCenter = new(faceRect.X + eyeRect.X + eyeRect.Width / 2, faceRect.Y + eyeRect.Y + eyeRect.Height / 2);
                            int radius = (int)Math.Round((eyeRect.Width + eyeRect.Height) * .25);
                            Cv2.Circle(frameMat, eyeCenter, radius, EyeColor, 4);
                        }
                    }

                    this.Dispatcher.Invoke(() => this.Info.Text = $"{faceRects.Length} faces detected");

                    Bitmap frameBitmap = frameMat.ToBitmap();
                    bgWorker.ReportProgress(0, frameBitmap);
                }

                Thread.Sleep(100);
            }
        }

        private void BackgroundWorker_ProgressChanged(object sender, ProgressChangedEventArgs e)
        {
            Bitmap frameBitmap = (Bitmap)e.UserState;
            this.Image.Source = frameBitmap.ToBitmapSource();
        }

        private void MainWindow_OnClosing(object sender, CancelEventArgs e)
        {
            this.backgroundWorker.CancelAsync();
            this.capture.Dispose();
            this.faceCascadeClassifier.Dispose();
            this.eyesCascadeClassifier.Dispose();
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