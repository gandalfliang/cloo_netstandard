using System;
using Cloo;
using demo.demos;

namespace demo
{
    class Program
    {
        static void Main(string[] args)
        {
            System.Console.WriteLine("hello from opencl");
            //获取所有平台
            var platforms = ComputePlatform.Platforms;
            foreach(var platform in platforms)
            {
                Console.WriteLine($"{platform.Name},{platform.Version}");
                //获取该平台下的计算设备
                var devices = platform.QueryDevices();
                foreach(var device in devices)
                {
                    Console.WriteLine($"    Device:{device.Name}");
                }
            }

//            var matrixmul=new MatrixMultiple(500);
//            matrixmul.RegularMul();
//            matrixmul.OpenClMul();
//            System.Console.WriteLine("done");

            var gau = new Gaussianblur(1.5f, 1);
            gau.Compute_cl(@"");
        }
    }
}
