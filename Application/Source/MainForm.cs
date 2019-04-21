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
using System.Drawing.Imaging;
using System.Text;
using System.Windows.Forms;
using Cloo;

namespace OpenCLImages
{
    public partial class MainForm : Form
    {
        public MainForm()
        {
            InitializeComponent();

            foreach (ComputePlatform platform in ComputePlatform.Platforms)
            {
                foreach (ComputeDevice device in platform.Devices)
                {
                    this.comboBoxDevices.Items.Add(device.Name.Trim());
                }
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
                this.splitContainer.Panel2.BackgroundImage = new Bitmap(openFileDialog.FileName);
            }
        }

        private void buttonGenerate_Click(object sender, EventArgs e)
        {
            if (this.splitContainer.Panel2.BackgroundImage == null)
            {
                MessageBox.Show("No image loaded.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            ComputePlatform oclPlatform = null;
            ComputeDevice oclDevice = null;

            foreach (ComputePlatform platform in ComputePlatform.Platforms)
            {
                foreach (ComputeDevice device in platform.Devices)
                {
                    if (device.Name.Trim() == this.comboBoxDevices.Text)
                    {
                        oclPlatform = platform;
                        oclDevice = device;
                    }
                }
            }

            if (oclDevice == null)
            {
                MessageBox.Show("Invalid OpenCL device.", "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);
                return;
            }

            ComputeContext oclContext = new ComputeContext(new ComputeDevice[] { oclDevice },
                new ComputeContextPropertyList(oclPlatform), null, IntPtr.Zero);

            string oclSource = Encoding.Default.GetString(Properties.Resources.Test);
            ComputeProgram oclProgram = new ComputeProgram(oclContext, oclSource);

            try
            {
                oclProgram.Build(new ComputeDevice[] { oclDevice }, "", null, IntPtr.Zero);
            }
            catch(ComputeException exception)
            {
                MessageBox.Show(exception.Message + "\n\n" + oclProgram.GetBuildLog(oclDevice),
                    "Error", MessageBoxButtons.OK, MessageBoxIcon.Error);

                oclProgram.Dispose();
                oclContext.Dispose();
                return;
            }

#if DEBUG
            MessageBox.Show(oclProgram.GetBuildLog(oclDevice),
                    "OpenCL program build completed", MessageBoxButtons.OK, MessageBoxIcon.Information);
#endif

            ComputeKernel oclKernel = oclProgram.CreateKernel("Test");

            Bitmap inBitmap = (Bitmap)this.splitContainer.Panel2.BackgroundImage;

            BitmapData inBmpData = inBitmap.LockBits(new Rectangle(0, 0, inBitmap.Width, inBitmap.Height),
                ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            ComputeImage2D oclInImage = new ComputeImage2D(oclContext, ComputeMemoryFlags.ReadOnly |
                ComputeMemoryFlags.CopyHostPointer,
                new ComputeImageFormat(ComputeImageChannelOrder.Bgra, ComputeImageChannelType.UNormInt8),
                inBitmap.Width, inBitmap.Height, 0, inBmpData.Scan0);

            Bitmap outBitmap = new Bitmap(inBitmap.Width, inBitmap.Height, PixelFormat.Format32bppArgb);

            BitmapData outBmpData = outBitmap.LockBits(new Rectangle(0, 0, outBitmap.Width, outBitmap.Height),
                ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            ComputeImage2D oclOutImage = new ComputeImage2D(oclContext, ComputeMemoryFlags.WriteOnly |
                ComputeMemoryFlags.CopyHostPointer,
                new ComputeImageFormat(ComputeImageChannelOrder.Bgra, ComputeImageChannelType.UNormInt8),
                outBitmap.Width, outBitmap.Height, 0, outBmpData.Scan0);

            oclKernel.SetMemoryArgument(0, oclInImage);
            oclKernel.SetMemoryArgument(1, oclOutImage);

            ComputeCommandQueue oclCommandQueue = new ComputeCommandQueue(oclContext, oclDevice,
                ComputeCommandQueueFlags.None);

            oclCommandQueue.Execute(oclKernel, new long[] { 0, 0 },
                new long[] { outBitmap.Width, outBitmap.Height },
                null, null);
            oclCommandQueue.Finish();

            oclCommandQueue.ReadFromImage(oclOutImage, outBmpData.Scan0, true, null);

            outBitmap.UnlockBits(outBmpData);

            this.splitContainer.Panel2.BackgroundImage = outBitmap;

            oclInImage.Dispose();
            oclOutImage.Dispose();
            oclCommandQueue.Dispose();
            oclKernel.Dispose();
            oclProgram.Dispose();
            oclContext.Dispose();
        }
    }
}
