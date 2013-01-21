using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace YutubVideoDownloader
{
    public partial class frmMain : Form
    {
        YutubFile yutub;
        DownloadManager dm;

        public frmMain()
        {
            InitializeComponent();
            yutub = new YutubFile();
            dm = new DownloadManager();
            System.Windows.Forms.Form.CheckForIllegalCrossThreadCalls = false;
        }

        private void frmMain_Load(object sender, EventArgs e)
        {
            txtDownloadPath.Text = System.IO.Path.GetTempPath();
        }

        private void btnPath_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog dlg = new FolderBrowserDialog())
            {
                if (dlg.ShowDialog() == System.Windows.Forms.DialogResult.OK)
                {
                    txtDownloadPath.Text = dlg.SelectedPath;
                }
                dlg.Dispose();
            }
        }

        private void btnVideo_Click(object sender, EventArgs e)
        {
            if (string.IsNullOrEmpty(txtVideoURL.Text)) return;
            yutub.VideoURL = txtVideoURL.Text;
            cbFormat.Items.Clear();
            for (int i = 0; i < yutub.FormatList.Count; i++)
            {
                cbFormat.Items.Add(yutub.FormatList[i]);
            }
            cbFormat.SelectedIndex = 0;
            lblTitle.Text = yutub.VideoTitle;
            picThumb.ImageLocation = yutub.ThumbUrl;
            lblLength.Text = yutub.Length;
        }

        private void btnDownload_Click(object sender, EventArgs e)
        {
            YutubFileFormat fmt = (YutubFileFormat)cbFormat.SelectedItem;
            dm.DownloadFileName = string.Format("{0}.{1}", yutub.VideoTitle, fmt.VideoExt);
            dm.DownloadPath = txtDownloadPath.Text;
            dm.DownloadURL = fmt.DownloadURL;
            dm.onDownloading += new dlgDownlading(dm_onDownloading);
            dm.Start();
        }

        void dm_onDownloading(int DownloadedBytes, int TotalBytes)
        {
            progressBar1.Value = (DownloadedBytes * 100) / TotalBytes;
        }
    }
}
