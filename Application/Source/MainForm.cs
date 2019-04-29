/*
	Copyright (C) 2019 Matej Gomboc

    This program is free software: you can redistribute it and/or modify
    it under the terms of the GNU Affero General Public License as published
    by the Free Software Foundation, either version 3 of the License, or
    (at your option) any later version.

    This program is distributed in the hope that it will be useful,
    but WITHOUT ANY WARRANTY; without even the implied warranty of
    MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
    GNU Affero General Public License for more details.

    You should have received a copy of the GNU Affero General Public License
    along with this program.  If not, see <https://www.gnu.org/licenses/>.
*/

using System;
using System.Drawing;
using System.Windows.Forms;

namespace OpenCLImages
{
    internal partial class MainForm : Form
    {
        private StarTracker starTracker = null;

        public MainForm()
        {
            InitializeComponent();

            starTracker = new StarTracker();

            foreach (string device in starTracker.Devices)
            {
                this.comboBoxDevices.Items.Add(device);
            }
        }

        private void buttonLoad_Click(object sender, EventArgs e)
        {
            OpenFileDialog openFileDialog = new OpenFileDialog();
            openFileDialog.AddExtension = true;
            openFileDialog.CheckFileExists = true;
            openFileDialog.CheckPathExists = true;
            openFileDialog.Multiselect = false;
            openFileDialog.ValidateNames = true;
            openFileDialog.Title = "Select input image";
            openFileDialog.Filter = "Image Files|*.jpg;*.jpeg;*.png;*.bmp;*tif;*tiff;*gif;";

            if (openFileDialog.ShowDialog() == DialogResult.OK)
            {
                if (this.splitContainer.Panel2.BackgroundImage != null)
                {
                    this.splitContainer.Panel2.BackgroundImage.Dispose();
                    this.splitContainer.Panel2.BackgroundImage = null;
                }
                this.splitContainer.Panel2.BackgroundImage = new Bitmap(openFileDialog.FileName);
            }
        }

        private void comboBoxDevices_SelectedIndexChanged(object sender, EventArgs e)
        {
            starTracker.SetDevice(comboBoxDevices.SelectedIndex);
        }

        private void buttonGenerate_Click(object sender, EventArgs e)
        {
            if (this.splitContainer.Panel2.BackgroundImage == null)
            {
                MessageBox.Show("No image loaded.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            if (this.comboBoxDevices.SelectedIndex == -1)
            {
                MessageBox.Show("No OpenCL device selected.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            Bitmap newImage = starTracker.ProcessImage((Bitmap)this.splitContainer.Panel2.BackgroundImage);

            this.splitContainer.Panel2.BackgroundImage.Dispose();
            this.splitContainer.Panel2.BackgroundImage = null;

            this.splitContainer.Panel2.BackgroundImage = newImage;
        }
    }
}
