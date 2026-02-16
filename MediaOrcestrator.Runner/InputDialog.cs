using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MediaOrcestrator.Runner;
public partial class InputDialog : Form
{
    public InputDialog()
    {
        InitializeComponent();
    }

    public string InputText { get; private set; }

    public InputDialog(string prompt, string title = "Ввод данных", string defaultValue = "")
    {
        InitializeComponent();
        this.Text = title;
        lblPrompt.Text = prompt;
        txtInput.Text = defaultValue;
        txtInput.Select();
    }

    private void btnOk_Click(object sender, EventArgs e)
    {
        InputText = txtInput.Text;
        DialogResult = DialogResult.OK;
        Close();
    }

    private void btnCancel_Click(object sender, EventArgs e)
    {
        DialogResult = DialogResult.Cancel;
        Close();
    }
}
