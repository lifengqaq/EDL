using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;

namespace lfEDL.Avalonia.Dialogs
{
    public partial class XiaomiAuthDialog : Window
    {
        private TextBox _blobBox;
        private TextBox _signatureBox;

        public XiaomiAuthDialog()
        {
            InitializeComponent();
        }

        public XiaomiAuthDialog(string blob) : this()
        {
            if (_blobBox != null)
                _blobBox.Text = blob;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            _blobBox = this.FindControl<TextBox>("BlobBox");
            _signatureBox = this.FindControl<TextBox>("SignatureBox");

            var copyBtn = this.FindControl<Button>("CopyBlobBtn");
            if (copyBtn != null) copyBtn.Click += CopyBlob_Click;

            var pasteBtn = this.FindControl<Button>("PasteSignatureBtn");
            if (pasteBtn != null) pasteBtn.Click += PasteSignature_Click;

            var doneBtn = this.FindControl<Button>("DoneBtn");
            if (doneBtn != null) doneBtn.Click += DoneBtn_Click;

            var cancelBtn = this.FindControl<Button>("CancelBtn");
            if (cancelBtn != null) cancelBtn.Click += CancelBtn_Click;
        }

        private async void CopyBlob_Click(object sender, RoutedEventArgs e)
        {
            if (_blobBox != null && !string.IsNullOrEmpty(_blobBox.Text))
            {
                var clipboard = GetTopLevel(this)?.Clipboard;
                if (clipboard != null)
                    await clipboard.SetTextAsync(_blobBox.Text);
            }
        }

        private async void PasteSignature_Click(object sender, RoutedEventArgs e)
        {
            if (_signatureBox != null)
            {
                var clipboard = GetTopLevel(this)?.Clipboard;
                if (clipboard != null)
                {
                    string text = await clipboard.GetTextAsync();
                    if (!string.IsNullOrEmpty(text))
                        _signatureBox.Text = text.Trim();
                }
            }
        }

        private void DoneBtn_Click(object sender, RoutedEventArgs e)
        {
            string signature = _signatureBox?.Text?.Trim();
            Close(signature);
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            Close(null);
        }
    }
}
