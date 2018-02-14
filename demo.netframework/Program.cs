using System.IO;
using demo.demos;

namespace demo.netframework
{
    class Program
    {
        static void Main(string[] args)
        {
            var gau = new Gaussianblur(2.5f, 20);

            DirectoryInfo dir =new DirectoryInfo(@"C:\Users\ganda\Pictures\testimages\");
            foreach (var fileInfo in dir.EnumerateFiles())
            {
                if (fileInfo.FullName.EndsWith(".png"))
                {
                    var src = fileInfo.FullName;
                    gau.Compute_cl(src,src+"._opencl.bmp");
                    gau.Compute(src,src+"._normal.bmp");
                }
            }
            
            
        }
    }
}
