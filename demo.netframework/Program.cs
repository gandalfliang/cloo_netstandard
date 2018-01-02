using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using demo.demos;

namespace demo.netframework
{
    class Program
    {
        static void Main(string[] args)
        {
            var src = @"C:\Users\ganda\Pictures\Screenshots\屏幕截图(2).png";
            var gau = new Gaussianblur(2.5f, 1);
            gau.Compute_cl(src);
            gau.Compute(src);
        }
    }
}
