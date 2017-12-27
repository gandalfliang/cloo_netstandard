using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using Cloo;

namespace demo
{
    public class MatrixMultiple
    {
        private readonly int Rank;

        private readonly int[,] matrixA;
        int[,] sum;

        public MatrixMultiple(int rank)
        {
            Rank = rank;
            matrixA = new int[rank, rank];
            sum = new int[rank, rank];
            InitArrays();
        }

        private void InitArrays()
        {
            var rnd = new Random();
            for (int i = 0; i < Rank; i++)
            for (int j = 0; j < Rank; j++)
                matrixA[i, j] = rnd.Next(1, 100);
        }

        public void RegularMul()
        {
            Stopwatch sw = Stopwatch.StartNew();

            for (int i = 0; i < Rank; i++)
            for (int j = 0; j < Rank; j++)
            for (int k = 0; k < Rank; k++)
                sum[i, j] += matrixA[i, k] * matrixA[k, j];

            var elapsed = sw.Elapsed;
            Console.WriteLine($"耗时: {elapsed.TotalMilliseconds} ms\n");
        }

        public void OpenClMul()
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
            var code = File.ReadAllText(@"demos\cls\matrix_mul.cl");
            //编译
            var program = new ComputeProgram(context, code);
            try
            {
                program.Build(new []{device}, null, null, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                throw;
            }
            //创建核心代码，就是cl代码中以kernel标识，函数签名为MatrixMul的函数
            var kernel = program.CreateKernel("MatrixMul");
            //矩阵规模
            int rank = Rank;
            //储存计算结果的数组
            var result = new ComputeBuffer<int>(context, ComputeMemoryFlags.WriteOnly, rank * rank);
            var matrix = CreateMatrix(context, rank);
            //创建的核心代码函数以这种方式来传参
            kernel.SetMemoryArgument(0, result);
            kernel.SetMemoryArgument(1, matrix);
            kernel.SetValueArgument(2, rank);
            Console.WriteLine($"运行平台: {platform.Name}\n运行设备： {device.Name}\n矩阵规模: {rank}x{rank}");
            Stopwatch sw = Stopwatch.StartNew();
            //执行代码
            commands.Execute(kernel, null, new long[] {rank, rank}, null, null);
            int[] resultArray = new int[rank * rank];
            var arrHandle = GCHandle.Alloc(resultArray, GCHandleType.Pinned);
            //读取数据
            commands.Read(result, true, 0, rank * rank, arrHandle.AddrOfPinnedObject(), null);
            var elapsed = sw.Elapsed;
            Console.WriteLine($"耗时: {elapsed.TotalMilliseconds} ms\n");
            arrHandle.Free();
            kernel.Dispose();
        }

        private ComputeBuffer<int> CreateMatrix(ComputeContext context, int rank)
        {
            int size = rank * rank;
            int[] datas = new int[size];
            for (int i = 0; i < size; i++)
            {
                datas[i] = i % 10;
            }
            var matrix = new ComputeBuffer<int>(context, ComputeMemoryFlags.CopyHostPointer, datas);
            return matrix;
        }
    }
}