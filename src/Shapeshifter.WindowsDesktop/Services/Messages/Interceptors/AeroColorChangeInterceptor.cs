namespace Shapeshifter.WindowsDesktop.Services.Messages.Interceptors
{
    using System;
    using System.Linq;
    using System.Windows;
    using System.Windows.Media;
    using System.Windows.Media.Animation;

    using Infrastructure.Events;

    using Interfaces;

    using Native.Interfaces;

    public class AeroColorChangeInterceptor : IAeroColorChangeInterceptor
    {
        public AeroColorChangeInterceptor()
        {
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

        void UpdateStoryboardColor(
            string storyboardKey,
            string targetPropertyPath,
            Color color)
        {
            var storyboardOriginal = (Storyboard)Application.Current.Resources[storyboardKey];
            var storyboard = storyboardOriginal.Clone();

            var animations = storyboard.Children.OfType<ColorAnimation>();
            foreach (var animation in animations)
            {
                var targetProperty = Storyboard.GetTargetProperty(animation);
                if (targetProperty.Path == targetPropertyPath + ".Color")
                {
                    animation.To = color;
                }
            }

            Application.Current.Resources[storyboardKey] = storyboard;
        }

        void UpdateAeroColor()
        {
            var aeroBrush = (SolidColorBrush)SystemParameters.WindowGlassBrush;
            var aeroColor = aeroBrush.Color;

            Application.Current.Resources["AccentColor"] = aeroColor;
            Application.Current.Resources["AccentDarkColor"] = aeroColor;
            Application.Current.Resources["AccentDarkerColor"] = aeroColor;

            UpdateStoryboards(aeroColor);
        }

        void UpdateStoryboards(Color aeroColor)
        {
            UpdateStoryboardColor(
                "BackgroundToAccentDarkTransitionStoryboard",
                "Background",
                aeroColor);

            UpdateStoryboardColor(
                "BackgroundToAccentTransitionStoryboard",
                "Background",
                aeroColor);

            UpdateStoryboardColor(
                "FillToAccentTransitionStoryboard",
                "Fill",
                aeroColor);

            UpdateStoryboardColor(
                "FillToAccentDarkTransitionStoryboard",
                "Fill",
                aeroColor);

            UpdateStoryboardColor(
                "BackgroundToAccentBorderToAccentDarkTransitionStoryboard",
                "Background",
                aeroColor);
            UpdateStoryboardColor(
                "BackgroundToAccentBorderToAccentDarkTransitionStoryboard",
                "BorderBrush",
                aeroColor);

            UpdateStoryboardColor(
                "BackgroundToAccentDarkBorderToAccentDarkerTransitionStoryboard",
                "Background",
                aeroColor);
            UpdateStoryboardColor(
                "BackgroundToAccentDarkBorderToAccentDarkerTransitionStoryboard",
                "BorderBrush",
                aeroColor);
        }
    }
}