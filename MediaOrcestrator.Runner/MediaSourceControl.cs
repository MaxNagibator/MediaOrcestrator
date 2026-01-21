using MediaOrcestrator.Core.Services;
using MediaOrcestrator.Domain;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows.Forms;

namespace MediaOrcestrator.Runner
{
    public partial class MediaSourceControl : UserControl
    {
        private Orcestrator _orcestrator;
        private IMediaSource _source;

        public MediaSourceControl()
        {
            InitializeComponent();
        }

        public void SetMediaSource(IMediaSource source)
        {
            _source = source;
            label1.Text = source.Name;
        }

        internal void SetZalup(Orcestrator orcestrator)
        {
            _orcestrator = orcestrator;
        }

        private async void button1_Click(object sender, EventArgs e)
        {
            await _orcestrator.Sync();
        }
    }
}
