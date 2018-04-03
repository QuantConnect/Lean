using System.Drawing;
using Microsoft.VisualStudio.PlatformUI;
using System.Windows.Forms;

namespace QuantConnect.VisualStudioPlugin
{
    /// <summary>
    /// Interaction logic for ErrorDialog.xaml
    /// </summary>
    internal partial class ErrorDialog : DialogWindow
    {
        public ErrorDialog(string title, string errorMessage)
        {
            InitializeComponent();
            var size = TextRenderer.MeasureText(errorMessage, new Font(textBox.FontFamily.ToString(), (float)textBox.FontSize));
            Width = size.Width > 800 ? 800 : size.Width;
            Title = title;
            textBox.Text = errorMessage;
        }
    }
}
