namespace KaraokeMaster.App.Forms;

public sealed class TextInputForm : Form
{
    private readonly TextBox _textBox = new() { Dock = DockStyle.Top, Margin = new Padding(8) };

    public string InputText => _textBox.Text.Trim();

    public TextInputForm(string title, string prompt, string defaultValue = "")
    {
        Text = title;
        Width = 360;
        Height = 150;
        StartPosition = FormStartPosition.CenterParent;
        MinimizeBox = false;
        MaximizeBox = false;
        FormBorderStyle = FormBorderStyle.FixedDialog;

        var promptLabel = new Label { Text = prompt, Dock = DockStyle.Top, Height = 24, Padding = new Padding(8, 8, 8, 0) };
        _textBox.Text = defaultValue;
        _textBox.Width = 320;

        var okButton = new Button { Text = "OK", DialogResult = DialogResult.OK };
        var cancelButton = new Button { Text = "Cancel", DialogResult = DialogResult.Cancel };
        var buttonPanel = new FlowLayoutPanel
        {
            Dock = DockStyle.Bottom,
            FlowDirection = FlowDirection.RightToLeft,
            Height = 44,
            Padding = new Padding(8),
        };
        buttonPanel.Controls.Add(okButton);
        buttonPanel.Controls.Add(cancelButton);

        Controls.Add(_textBox);
        Controls.Add(buttonPanel);
        Controls.Add(promptLabel);

        AcceptButton = okButton;
        CancelButton = cancelButton;
    }
}
