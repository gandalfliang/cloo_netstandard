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
            var gau = new Gaussianblur(10.5f, 10);
            gau.Compute_cl(@"C:\Users\ganda\Pictures\Screenshots\屏幕截图(1).png");
        }
    }
}
