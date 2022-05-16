using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using System.Threading.Tasks;
using Windows.Graphics.Imaging;
using Windows.Storage.Streams;

namespace WinAuth.Utils
{
    [ComImport]
    [Guid("5B0D3235-4DBA-4D44-865E-8F1D0E4FD04D")]
    [InterfaceType(ComInterfaceType.InterfaceIsIUnknown)]
    unsafe interface IMemoryBufferByteAccess
    {
        void GetBuffer(out byte* buffer, out uint capacity);
    }

    internal static class QRDecodeHelper
    {
        public async static Task<string> GetQRInfoFromBitmap(RandomAccessStreamReference bitmap)
        {
            IRandomAccessStream stream = await bitmap.OpenReadAsync();
            BitmapDecoder decoder = await BitmapDecoder.CreateAsync(stream);
            SoftwareBitmap softwareBitmap = await decoder.GetSoftwareBitmapAsync();

            OpenCvSharp.Mat mat = SoftwareBitmap2Mat(softwareBitmap);

            OpenCvSharp.QRCodeDetector qRCodeDetector = new OpenCvSharp.QRCodeDetector();
            OpenCvSharp.Point2f[] points;
            string qrResult = qRCodeDetector.DetectAndDecode(mat, out points);

            return qrResult;
        }

        public static unsafe OpenCvSharp.Mat SoftwareBitmap2Mat(SoftwareBitmap softwareBitmap)
        {
            using (BitmapBuffer buffer = softwareBitmap.LockBuffer(BitmapBufferAccessMode.Write))
            {
                using (var reference = buffer.CreateReference())
                {
                    ((IMemoryBufferByteAccess)reference).GetBuffer(out var dataInBytes, out var capacity);

                    OpenCvSharp.Mat outputMat = new OpenCvSharp.Mat(softwareBitmap.PixelHeight, softwareBitmap.PixelWidth, OpenCvSharp.MatType.CV_8UC4, (IntPtr)dataInBytes);
                    return outputMat;
                }
            }
        }
    }
}
