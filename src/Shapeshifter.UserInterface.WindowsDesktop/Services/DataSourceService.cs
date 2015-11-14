﻿using System;
using System.Diagnostics.CodeAnalysis;
using System.Windows;
using System.Windows.Interop;
using System.Windows.Media.Imaging;
using Shapeshifter.UserInterface.WindowsDesktop.Api;
using Shapeshifter.UserInterface.WindowsDesktop.Data;
using Shapeshifter.UserInterface.WindowsDesktop.Data.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Infrastructure.Dependencies.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Images.Interfaces;
using Shapeshifter.UserInterface.WindowsDesktop.Services.Interfaces;

namespace Shapeshifter.UserInterface.WindowsDesktop.Services
{
    [ExcludeFromCodeCoverage]
    internal class DataSourceService : IDataSourceService, ISingleInstance
    {
        private readonly IImagePersistenceService imagePersistenceService;

        public DataSourceService(IImagePersistenceService imagePersistenceService)
        {
            this.imagePersistenceService = imagePersistenceService;
        }

        private static BitmapSource GetWindowIcon(IntPtr windowHandle)
        {
            var hIcon = WindowApi.SendMessage(windowHandle, (int) Message.WM_GETICON, WindowApi.ICON_BIG, IntPtr.Zero);
            if (hIcon == IntPtr.Zero)
            {
                hIcon = WindowApi.GetClassLongPtr(windowHandle, WindowApi.GCL_HICON);
            }

            if (hIcon == IntPtr.Zero)
            {
                //TODO: define this constant in the api. it's messy in here.
                hIcon = WindowApi.LoadIcon(IntPtr.Zero, (IntPtr) 0x7F00 /*IDI_APPLICATION*/);
            }

            if (hIcon != IntPtr.Zero)
            {
                return Imaging.CreateBitmapSourceFromHIcon(hIcon, Int32Rect.Empty, BitmapSizeOptions.FromEmptyOptions());
            }
            throw new InvalidOperationException("Could not load window icon.");
        }

        public IDataSource GetDataSource()
        {
            var activeWindowHandle = WindowApi.GetForegroundWindow();

            var windowTitle = WindowApi.GetWindowTitle(activeWindowHandle);
            var windowIcon = GetWindowIcon(activeWindowHandle);

            var iconBytes = imagePersistenceService.ConvertBitmapSourceToByteArray(windowIcon);
            return new DataSource(iconBytes, windowTitle);
        }
    }
}