using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml;
using System;
using System.Threading.Tasks;

namespace lfEDL.Avalonia.Dialogs
{
    public partial class XiaomiAuthDialog : Window
    {
        private TextBox _tokenBox;
        private TextBox _signatureBox;
        private Button _copyTokenBtn;
        private Button _pasteSignatureBtn;
        private Button _authBtn;
        private Button _cancelBtn;

        public XiaomiAuthDialog()
        {
            InitializeComponent();
        }

        public XiaomiAuthDialog(string token) : this()
        {
            if (_tokenBox != null)
                _tokenBox.Text = token;
        }

        private void InitializeComponent()
        {
            AvaloniaXamlLoader.Load(this);

            _tokenBox = this.FindControl<TextBox>("TokenBox");
            _signatureBox = this.FindControl<TextBox>("SignatureBox");

            _copyTokenBtn = this.FindControl<Button>("CopyTokenBtn");
            if (_copyTokenBtn != null) _copyTokenBtn.Click += CopyTokenBtn_Click;

            _pasteSignatureBtn = this.FindControl<Button>("PasteSignatureBtn");
            if (_pasteSignatureBtn != null) _pasteSignatureBtn.Click += PasteSignatureBtn_Click;

            _authBtn = this.FindControl<Button>("AuthBtn");
            if (_authBtn != null) _authBtn.Click += AuthBtn_Click;

            _cancelBtn = this.FindControl<Button>("CancelBtn");
            if (_cancelBtn != null) _cancelBtn.Click += CancelBtn_Click;
        }

        private async void CopyTokenBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_tokenBox != null && !string.IsNullOrEmpty(_tokenBox.Text))
            {
                var clipboard = GetTopLevel(this)?.Clipboard;
                if (clipboard != null)
                {
                    await clipboard.SetTextAsync(_tokenBox.Text);
                }
            }
        }

        private async void PasteSignatureBtn_Click(object sender, RoutedEventArgs e)
        {
            if (_signatureBox != null)
            {
                var clipboard = GetTopLevel(this)?.Clipboard;
                if (clipboard != null)
                {
                    string text = await clipboard.GetTextAsync();
                    if (!string.IsNullOrEmpty(text))
                    {
                        _signatureBox.Text = text.Trim();
                    }
                }
            }
        }

        private void AuthBtn_Click(object sender, RoutedEventArgs e)
        {
            string signature = _signatureBox?.Text?.Trim();
            if (string.IsNullOrEmpty(signature))
            {
                // Simple validation prompt? Or just close with empty (which means cancel)
                // For now, let's assume empty is invalid but allow closing.
                // Or maybe show a red border?
                // Let's just return what we have.
            }
            Close(signature);
        }

        private void CancelBtn_Click(object sender, RoutedEventArgs e)
        {
            Close(null);
        }
    }
}

