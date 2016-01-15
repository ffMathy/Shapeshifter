namespace Shapeshifter.WindowsDesktop.Controls.Clipboard
{
    using System.Windows;
    using System.Windows.Controls;

    /// <summary>
    ///     Interaction logic for DataSourceControl.xaml
    /// </summary>
    public partial class DataSourceControl: StackPanel
    {
        public static readonly DependencyProperty TextVisibilityProperty =
            DependencyProperty.Register(
                nameof(TextVisibility),
                typeof (Visibility),
                typeof (DataSourceControl),
                new PropertyMetadata(Visibility.Visible));

        public Visibility TextVisibility
        {
            get
            {
                return (Visibility) GetValue(TextVisibilityProperty);
            }
            set
            {
                SetValue(TextVisibilityProperty, value);
            }
        }

        public DataSourceControl()
        {
            InitializeComponent();
        }
    }
}