using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace Shapeshifter.WindowsDesktop.Controls
{
	/// <summary>
	/// Interaction logic for NumberTextBox.xaml
	/// </summary>
	public partial class NumberTextBox : TextBox
	{
		public static readonly DependencyProperty MinimumProperty = DependencyProperty.Register(nameof(Minimum), typeof(int), typeof(NumberTextBox), new PropertyMetadata(int.MinValue));
		public static readonly DependencyProperty MaximumProperty = DependencyProperty.Register(nameof(Maximum), typeof(int), typeof(NumberTextBox), new PropertyMetadata(int.MaxValue));

		readonly Regex numberRegex;

		public int Minimum
		{
			get { return (int)GetValue(MinimumProperty); }
			set { SetValue(MinimumProperty, value); }
		}

		public int Maximum
		{
			get { return (int)GetValue(MaximumProperty); }
			set { SetValue(MaximumProperty, value); }
		}

		public NumberTextBox()
		{
			InitializeComponent();

			numberRegex = new Regex("[0-9\\-]+");

			PreviewTextInput += NumberTextBox_PreviewTextInput;
			DataObject.AddPastingHandler(this, NumberTextBox_Pasting);
		}

		void NumberTextBox_Pasting(object sender, DataObjectPastingEventArgs e)
		{
			if (e.DataObject.GetDataPresent(typeof(String)))
			{
				var text = (string)e.DataObject.GetData(typeof(String));
				if (IsTextAllowed(InsertStringAtLocation(Text, CaretIndex, text)))
					return;
			}

			e.CancelCommand();
		}

		void NumberTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
		{
			if(!IsTextAllowed(e.Text)) { 
				e.Handled = true;
				return;
			}

			var number = int.Parse(InsertStringAtLocation(Text, CaretIndex, e.Text));
			e.Handled = !(number >= Minimum && number <= Maximum);
		}

		string InsertStringAtLocation(string origin, int index, string textToInsert) {
			var text = origin.Substring(0, index) + textToInsert;
			if(index + textToInsert.Length < origin.Length)
				text += origin.Substring(index + textToInsert.Length);

			return text;
		}

		bool IsTextAllowed(string text)
		{
			return numberRegex.IsMatch(text);
		}
	}
}
