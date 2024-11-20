using System;
using System.IO;
using System.Net.Sockets;
using System.Text;
using System.Windows.Forms;

namespace FileRequestApp
{
    public partial class MainForm : Form
    {
        private const int BufferSize = 1024;

        public MainForm()
        {
            InitializeComponent();
        }

        private void btnRequestFile_Click(object sender, EventArgs e)
        {
            string ipAddress = txtIPAddress.Text;
            if (!int.TryParse(txtPort.Text, out int port))
            {
                MessageBox.Show("Please enter a valid port number.");
                return;
            }

            string fileName = txtFileName.Text;
            string savePath = txtSavePath.Text;

            if (string.IsNullOrEmpty(ipAddress) || string.IsNullOrEmpty(fileName) || string.IsNullOrEmpty(savePath))
            {
                MessageBox.Show("Please fill in all fields.");
                return;
            }

            RequestFile(ipAddress, port, fileName, savePath);
        }

        private void btnBrowse_Click(object sender, EventArgs e)
        {
            using (FolderBrowserDialog folderBrowserDialog = new FolderBrowserDialog())
            {
                if (folderBrowserDialog.ShowDialog() == DialogResult.OK)
                {
                    txtSavePath.Text = folderBrowserDialog.SelectedPath;
                }
            }
        }

        private void RequestFile(string ipAddress, int port, string fileName, string savePath)
        {
            try
            {
                TcpClient client = new TcpClient(ipAddress, port);
                NetworkStream stream = client.GetStream();

                byte[] requestData = Encoding.UTF8.GetBytes(fileName);
                stream.Write(requestData, 0, requestData.Length);

                byte[] buffer = new byte[BufferSize];
                MemoryStream ms = new MemoryStream();
                int bytesRead;

                while ((bytesRead = stream.Read(buffer, 0, BufferSize)) > 0)
                {
                    ms.Write(buffer, 0, bytesRead);
                }

                string fullSavePath = Path.Combine(savePath, fileName);
                File.WriteAllBytes(fullSavePath, ms.ToArray());
                MessageBox.Show($"File {fileName} saved to {fullSavePath}");

                client.Close();
            }
            catch (Exception ex)
            {
                MessageBox.Show($"Error requesting file: {ex.Message}");
            }
        }
    }
}
