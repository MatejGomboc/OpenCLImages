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
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Drawing;
using System.Drawing.Imaging;
using System.Text;
using Cloo;

namespace OpenCLImages
{
    internal class StarTracker
    {
        private ReadOnlyCollection<ComputeDevice> oclDevices = null;
        private ComputeContext oclContext = null;
        private ComputeCommandQueue oclCommandQueue = null;
        private ComputeKernel oclKernel = null;

        public ReadOnlyCollection<string> Devices
        {
            get
            {
                List<string> devices = new List<string>(oclDevices.Count);

                foreach(ComputeDevice device in oclDevices)
                {
                    devices.Add(device.Name.Trim());
                }

                return new ReadOnlyCollection<string>(devices);
            }
        }

        public StarTracker()
        {
            List<ComputeDevice> devices = new List<ComputeDevice>();

            foreach (ComputePlatform platform in ComputePlatform.Platforms)
            {
                devices.AddRange(platform.Devices);
            }

            oclDevices = new ReadOnlyCollection<ComputeDevice>(devices);
        }

        public void SetDevice(int deviceIndx)
        {
            if ((deviceIndx < 0) || (deviceIndx >= oclDevices.Count))
            {
                throw new IndexOutOfRangeException("Invalid OpenCL device index.");
            }

            if (oclContext != null)
            {
                oclContext.Dispose();
                oclContext = null;
            }

            if (oclCommandQueue != null)
            {
                oclCommandQueue.Dispose();
                oclCommandQueue = null;
            }

            if (oclKernel != null)
            {
                oclKernel.Dispose();
                oclKernel = null;
            }

            ComputeProgram oclProgram = null;

            try
            {
                oclContext = new ComputeContext(new ComputeDevice[] { oclDevices[deviceIndx] },
                    new ComputeContextPropertyList(oclDevices[deviceIndx].Platform), null, IntPtr.Zero);

                oclCommandQueue = new ComputeCommandQueue(oclContext, oclDevices[deviceIndx],
                    ComputeCommandQueueFlags.None);

                oclProgram = new ComputeProgram(oclContext,
                    Encoding.Default.GetString(Properties.Resources.Test));

                oclProgram.Build(new ComputeDevice[] { oclDevices[deviceIndx] }, "", null, IntPtr.Zero);

                oclKernel = oclProgram.CreateKernel("Test");
            }
            catch(BuildProgramFailureComputeException ex)
            {
                string buildLog = oclProgram.GetBuildLog(oclDevices[deviceIndx]);
                throw new Exception(buildLog, ex);
            }
            catch (Exception)
            {
                throw;
            }
            finally
            {
                if (oclProgram != null)
                {
                    oclProgram.Dispose();
                    oclProgram = null;
                }
            }
        }

        public Bitmap ProcessImage(Bitmap inImage)
        {
            ComputeImage2D oclInImage = null;
            ComputeImage2D oclOutImage = null;
            
            BitmapData inImageData = inImage.LockBits(new Rectangle(0, 0, inImage.Width, inImage.Height),
                    ImageLockMode.ReadOnly, PixelFormat.Format32bppArgb);

            Bitmap outImage = new Bitmap(inImage.Width, inImage.Height, PixelFormat.Format32bppArgb);

            BitmapData outImageData = outImage.LockBits(new Rectangle(0, 0, outImage.Width, outImage.Height),
                    ImageLockMode.WriteOnly, PixelFormat.Format32bppArgb);

            try
            {
                oclInImage = new ComputeImage2D(oclContext, ComputeMemoryFlags.ReadOnly |
                    ComputeMemoryFlags.CopyHostPointer, new ComputeImageFormat(ComputeImageChannelOrder.Bgra,
                    ComputeImageChannelType.UNormInt8), inImage.Width, inImage.Height, 0, inImageData.Scan0);

                oclOutImage = new ComputeImage2D(oclContext, ComputeMemoryFlags.WriteOnly |
                    ComputeMemoryFlags.CopyHostPointer, new ComputeImageFormat(ComputeImageChannelOrder.Bgra,
                    ComputeImageChannelType.UNormInt8), outImage.Width, outImage.Height, 0, outImageData.Scan0);

                oclKernel.SetMemoryArgument(0, oclInImage);
                oclKernel.SetMemoryArgument(1, oclOutImage);

                oclCommandQueue.Execute(oclKernel, new long[] { 0, 0 }, new long[] { oclInImage.Width,
                oclInImage.Height }, null, null);

                oclCommandQueue.Finish();

                oclCommandQueue.ReadFromImage(oclOutImage, outImageData.Scan0, true, null);
            }
            catch(Exception)
            {
                throw;
            }
            finally
            {
                inImage.UnlockBits(inImageData);
                outImage.UnlockBits(outImageData);
                
                if (oclInImage != null)
                {
                    oclInImage.Dispose();
                    oclInImage = null;
                }

                if (oclOutImage != null)
                {
                    oclOutImage.Dispose();
                    oclOutImage = null;
                }
            }

            return outImage;
        }
    }
}
