/*
 * gaussian function:
 * https://zh.wikipedia.org/wiki/%E9%AB%98%E6%96%AF%E6%A8%A1%E7%B3%8A
 */
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text;
using Cloo;

namespace demo.demos
{
    public class Gaussianblur
    {
        private double[] _matrix;
        public float Variance { get; }
        public int Radius { get; }

        public Gaussianblur(float variance,int radius)
        {
            if(variance<0||radius==0)
                throw new ArgumentException();

            Variance = variance;
            Radius = radius;
            var width = radius * 2 + 1;
            _matrix=new Double[width*width];
            ComputeWeightMatrix();
        }

        public void Compute(string imageFile)
        {
        }

        public void Compute_cl(string imageFile)
        {
            //选取设备
            var platform = ComputePlatform.Platforms.FirstOrDefault();
            var device = platform.Devices.FirstOrDefault();
            //设置相关上下文
            var properties = new ComputeContextPropertyList(platform);
            var context = new ComputeContext(new[] { device }, properties, null, IntPtr.Zero);
            //命令队列，用于控制执行的代码
            ComputeCommandQueue commands = new ComputeCommandQueue(context, context.Devices[0],
                ComputeCommandQueueFlags.None);
            //读取opencl代码
            var code = File.ReadAllText(@"demos\cls\gaussianblur.cl");
            //编译
            var program = new ComputeProgram(context, code);
            try
            {
                program.Build(new[] { device }, null, null, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                throw;
            }
            //创建核心代码，就是cl代码中以kernel标识，函数签名为MatrixMul的函数
//            var kernel = program.CreateKernel("MatrixMul");
//            //矩阵规模
//            int rank = Rank;
//            //储存计算结果的数组
//            var result = new ComputeBuffer<int>(context, ComputeMemoryFlags.WriteOnly, rank * rank);
//            var matrix = CreateMatrix(context, rank);
//            //创建的核心代码函数以这种方式来传参
//            kernel.SetMemoryArgument(0, result);
//            kernel.SetMemoryArgument(1, matrix);
//            kernel.SetValueArgument(2, rank);
//            Console.WriteLine($"运行平台: {platform.Name}\n运行设备： {device.Name}\n矩阵规模: {rank}x{rank}");
//            Stopwatch sw = Stopwatch.StartNew();
//            //执行代码
//            commands.Execute(kernel, null, new long[] { rank, rank }, null, null);
//            int[] resultArray = new int[rank * rank];
//            var arrHandle = GCHandle.Alloc(resultArray, GCHandleType.Pinned);
//            //读取数据
//            commands.Read(result, true, 0, rank * rank, arrHandle.AddrOfPinnedObject(), null);
//            var elapsed = sw.Elapsed;
//            Console.WriteLine($"耗时: {elapsed.TotalMilliseconds} ms\n");
//            arrHandle.Free();
//            kernel.Dispose();
        }

        private void ComputeWeightMatrix()
        {
            var center = Radius;
            var conBase = 2 * Math.Pow(Variance, 2);
            var conRoot = 1 / (Math.PI * conBase);

            double sum = 0d;
            for (int x = -Radius; x <= Radius; x++)
            {
                for (int y = Radius; y >= -Radius; y--)
                {
                    var weight = conRoot * Math.Pow(Math.E, -(x * x + y * y) / conBase);
                    _matrix[GridPosToArrayIndex(x, y, center, Radius)] = weight;
                    sum += weight;
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
    }
}
