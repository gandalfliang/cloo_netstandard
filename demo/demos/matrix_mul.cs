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
            Console.WriteLine($"��ʱ: {elapsed.TotalMilliseconds} ms\n");
        }

        public void OpenClMul()
        {
            //ѡȡ�豸
            var platform = ComputePlatform.Platforms.FirstOrDefault();
            var device = platform.Devices.FirstOrDefault();
            //�������������
            var properties = new ComputeContextPropertyList(platform);
            var context = new ComputeContext(new[] {device}, properties, null, IntPtr.Zero);
            //������У����ڿ���ִ�еĴ���
            ComputeCommandQueue commands = new ComputeCommandQueue(context, context.Devices[0],
                ComputeCommandQueueFlags.None);
            //��ȡopencl����
            var code = File.ReadAllText(@"demos\cls\matrix_mul.cl");
            //����
            var program = new ComputeProgram(context, code);
            try
            {
                program.Build(new []{device}, null, null, IntPtr.Zero);
            }
            catch (Exception ex)
            {
                throw;
            }
            //�������Ĵ��룬����cl��������kernel��ʶ������ǩ��ΪMatrixMul�ĺ���
            var kernel = program.CreateKernel("MatrixMul");
            //�����ģ
            int rank = Rank;
            //���������������
            var result = new ComputeBuffer<int>(context, ComputeMemoryFlags.WriteOnly, rank * rank);
            var matrix = CreateMatrix(context, rank);
            //�����ĺ��Ĵ��뺯�������ַ�ʽ������
            kernel.SetMemoryArgument(0, result);
            kernel.SetMemoryArgument(1, matrix);
            kernel.SetValueArgument(2, rank);
            Console.WriteLine($"����ƽ̨: {platform.Name}\n�����豸�� {device.Name}\n�����ģ: {rank}x{rank}");
            Stopwatch sw = Stopwatch.StartNew();
            //ִ�д���
            commands.Execute(kernel, null, new long[] {rank, rank}, null, null);
            int[] resultArray = new int[rank * rank];
            var arrHandle = GCHandle.Alloc(resultArray, GCHandleType.Pinned);
            //��ȡ����
            commands.Read(result, true, 0, rank * rank, arrHandle.AddrOfPinnedObject(), null);
            var elapsed = sw.Elapsed;
            Console.WriteLine($"��ʱ: {elapsed.TotalMilliseconds} ms\n");
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