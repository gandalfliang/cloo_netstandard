/*
 * gaussian function:
 * https://zh.wikipedia.org/wiki/%E9%AB%98%E6%96%AF%E6%A8%A1%E7%B3%8A
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Drawing;
using System.Drawing.Imaging;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Cloo;
using Cloo.Bindings;

namespace demo.demos
{
    public class Gaussianblur
    {
        private char[] dstBytes;
        private float[] _matrix;
        public float Variance { get; }
        public int Radius { get; }

        public Gaussianblur(float variance,int radius)
        {
            if(variance<0||radius==0)
                throw new ArgumentException();

            Variance = variance;
            Radius = radius;
            var width = radius * 2 + 1;
            _matrix=new float[width*width];
            ComputeWeightMatrix();
        }

        public void Compute(string imageFile,string dst)
        {
            using (var bitmap = new Bitmap(imageFile))
            {
                Console.WriteLine($"处理图片尺寸：{bitmap.Width}*{bitmap.Height}");
                var datas = bitmap.LockBits(new Rectangle(new Point(), new Size(bitmap.Width, bitmap.Height)),ImageLockMode.ReadOnly,bitmap.PixelFormat);
                var dataSize = datas.Stride * datas.Height;
                var argbs = new byte[dataSize];
                var dsts = new byte[dataSize];
                int matrixWidth = Radius * 2 + 1;
                Marshal.Copy(datas.Scan0, argbs, 0, dataSize);

                Stopwatch sw=Stopwatch.StartNew();
                for (int y = 0; y < bitmap.Height; y++)
                {
                    for (int x = 0; x < bitmap.Width; x++)
                    {
                        float sumA = 0;
                        float sumR = 0;
                        float sumG = 0;
                        float sumB=0;
                        for (int i = 0; i < _matrix.Length; i++)
                        {
                            var pos = transform_pos(x, y, matrixWidth, bitmap.Width, bitmap.Height, Radius, i);
                            var position = pos.Y * datas.Stride + pos.X*4;
                            sumR += argbs[position] * _matrix[i];
                            sumG += argbs[position + 1] * _matrix[i];
                            sumB += argbs[position + 2] * _matrix[i];
                            sumA += argbs[position + 3] * _matrix[i];
                        }
                        var dstPos = y * datas.Stride + x * 4;
                        dsts[dstPos] = (byte)sumR;
                        dsts[dstPos+1] = (byte)sumG;
                        dsts[dstPos+2] = (byte)sumB;
                        dsts[dstPos+3] = (byte)sumA;
                    }
                }
                bitmap.UnlockBits(datas);

                var elapse = sw.Elapsed;
                Console.WriteLine($"常规方法耗时: {elapse.TotalMilliseconds} ms");
                Debug.WriteLine($"Costing: {elapse}");

                var handle = GCHandle.Alloc(dsts, GCHandleType.Pinned);
                using (var dstBmp = new Bitmap(datas.Width, datas.Height, datas.Stride, bitmap.PixelFormat,
                    handle.AddrOfPinnedObject()))
                {
                    dstBmp.Save(dst);
                }

                handle.Free();
            }
        }

        public void Compute_cl(string imageFile,string dst)
        {
            //选取设备
            var platform = ComputePlatform.Platforms.FirstOrDefault();
            var device = platform.Devices.FirstOrDefault();
            //设置相关上下文
            var properties = new ComputeContextPropertyList(platform);
            var context = new ComputeContext(new[] {device}, properties, null, IntPtr.Zero);
            //命令队列，用于控制执行的代码
            ComputeCommandQueue commands = new ComputeCommandQueue(context, context.Devices[0],
                ComputeCommandQueueFlags.None);
            //读取opencl代码
            var code = File.ReadAllText(@"gaussianblur.cl");
            //编译
            var program = new ComputeProgram(context, code);
            try
            {
                program.Build(new[] {device}, null, null, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                throw;
            }

            var images = CreateImageFromBitmap(imageFile, context,
                ComputeMemoryFlags.ReadWrite | ComputeMemoryFlags.CopyHostPointer);

            //创建核心代码，就是cl代码中以kernel标识，函数签名为MatrixMul的函数
            var kernel = program.CreateKernel("gaussian_blur");
            //矩阵规模
            //储存计算结果的数组
           
            //创建的核心代码函数以这种方式来传参
            var resultBuffer=new ComputeBuffer<char>(context,ComputeMemoryFlags.WriteOnly, dstBytes.Length);
            kernel.SetMemoryArgument(0, images);
            kernel.SetMemoryArgument(1, resultBuffer);
            kernel.SetMemoryArgument(2, new ComputeBuffer<float>(context,ComputeMemoryFlags.ReadOnly|ComputeMemoryFlags.CopyHostPointer,_matrix));
            kernel.SetValueArgument(3, Radius);
            kernel.SetValueArgument(4, (int)images.Width);
            Console.WriteLine($"\n运行平台: {platform.Name}\n运行设备： {device.Name}");
            Stopwatch sw = Stopwatch.StartNew();
            var climg = images;
            Console.WriteLine($"处理图片尺寸：{climg.Width}*{climg.Height}");

            //执行代码
            commands.Execute(kernel, null, new long[] {climg.Width, climg.Height}, null, null);
           
            //read data
            char[] resultArray = new char[dstBytes.Length];
            var arrHandle = GCHandle.Alloc(resultArray, GCHandleType.Pinned);
            commands.Read(resultBuffer, true, 0, dstBytes.Length, arrHandle.AddrOfPinnedObject(), null);
            //commands.ReadFromImage(images.Item2, processeddata.Scan0, true, null);

            var resultHandle = GCHandle.Alloc(resultArray, GCHandleType.Pinned);
            var bmp=new Bitmap(climg.Width,climg.Height, climg.Width*4, PixelFormat.Format32bppArgb, resultHandle.AddrOfPinnedObject());
            var elapsed = sw.Elapsed;
            Console.WriteLine($"OpenCL处理耗时: {elapsed.TotalMilliseconds} ms\n");
            kernel.Dispose();

            bmp.Save(dst);
            arrHandle.Free();
        }

        private Point transform_pos(int centerX, int centerY, int matrixWidth, int bmpWidth,int bmpHeight,int radius, int index)
        {
            int x = index % matrixWidth;
            int offsetX = x - (radius + 1);
            int y = index / matrixWidth;
            int offsetY = radius - y;
            int newX = centerX + offsetX;
            int newY = centerY - offsetY;
            newX = newX < 0 ? 0 : newX > bmpWidth-1 ? bmpWidth-1 : newX;
            newY = newY < 0 ? 0 : newY > bmpHeight-1 ? bmpHeight-1 : newY;
            return new Point(newX,newY);
        }

        private void ComputeWeightMatrix()
        {
            var center = Radius;
            var conBase = 2 * Math.Pow(Variance, 2);
            var conRoot = 1 / (Math.PI * conBase);

            float sum = 0f;
            for (int x = -Radius; x <= Radius; x++)
            {
                for (int y = Radius; y >= -Radius; y--)
                {
                    var weight = conRoot * Math.Pow(Math.E, -(x * x + y * y) / conBase);
                    _matrix[GridPosToArrayIndex(x, y, center, Radius)] = (float)weight;
                    sum += (float)weight;
                }
            }
            for (int i = 0; i < _matrix.Length; i++)
            {
                _matrix[i] /= sum;
            }
        }

        private int GridPosToArrayIndex(int x, int y,int center,int radius)
        {
            var width = radius * 2 + 1;
            return Math.Abs(y - center) * width + (x + center);
        }

        private ComputeImage2D CreateImageFromBitmap(string file,ComputeContext ctx,ComputeMemoryFlags flags)
        {
            if (!File.Exists(file))
                throw new FileNotFoundException();

            unsafe
            {
                var bitmap = new Bitmap(file);
                if (bitmap.PixelFormat != PixelFormat.Format32bppArgb)
                    throw new ArgumentException("Pixel format not supported.");

                //ComputeImageFormat format = Tools.ConvertImageFormat(bitmap.PixelFormat);
                ComputeImageFormat format = new ComputeImageFormat(ComputeImageChannelOrder.Rgba, ComputeImageChannelType.UnsignedInt8);
                BitmapData bitmapData = bitmap.LockBits(new Rectangle(new Point(), bitmap.Size), ImageLockMode.ReadWrite, bitmap.PixelFormat);
                ComputeImage2D image;
                try
                {
                    image = new ComputeImage2D(ctx, flags, format, bitmapData.Width, bitmapData.Height,
                        bitmapData.Stride, bitmapData.Scan0);
                    dstBytes = new char[bitmapData.Width*bitmapData.Height*4];
                }
                finally
                {
                    bitmap.UnlockBits(bitmapData);
                }
                return image;
            }
        }
    }
}
