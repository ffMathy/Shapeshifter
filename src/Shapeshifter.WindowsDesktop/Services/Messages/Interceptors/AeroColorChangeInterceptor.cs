namespace Shapeshifter.WindowsDesktop.Services.Messages.Interceptors
{
    using System;
    using System.Linq;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Animation;

    using Infrastructure.Events;
    using Infrastructure.Logging.Interfaces;

    using Interfaces;

    using Native.Interfaces;

    using Services.Interfaces;

    public class AeroColorChangeInterceptor : IAeroColorChangeInterceptor
    {
        readonly ILogger logger;
        readonly IColorBrightnessAdjustmentService colorBrightnessAdjustmentService;

        public AeroColorChangeInterceptor(
            ILogger logger,
            IColorBrightnessAdjustmentService colorBrightnessAdjustmentService)
        {
            this.logger = logger;
            this.colorBrightnessAdjustmentService = colorBrightnessAdjustmentService;
        }

        public void Install(IntPtr windowHandle)
        {
            UpdateAeroColor();
        }

        public void Uninstall()
        {
        }

        public void ReceiveMessageEvent(WindowMessageReceivedArgument e)
        {
            if (e.Message != Message.WM_DWMCOLORIZATIONCOLORCHANGED) return;

            UpdateAeroColor();
        }

        void UpdateAeroColor()
        {
            var aeroBrush = (SolidColorBrush)SystemParameters.WindowGlassBrush;
            var aeroColor = aeroBrush.Color;

            SetColor("Accent", aeroColor);
            SetColor("AccentDark", colorBrightnessAdjustmentService.AdjustBrightness(aeroColor, -40));
            SetColor("AccentDarker", colorBrightnessAdjustmentService.AdjustBrightness(aeroColor, -80));

            logger.Information("Aero color was changed to " + aeroColor + ".");
        }

        static void SetColor(string key, Color color)
        {
            var resources = Application.Current.Resources;
            resources[key + "Brush"] = new SolidColorBrush(color);
        }
    }
}