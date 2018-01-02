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
            var src = @"C:\Users\ganda\Pictures\temp.png";
            var gau = new Gaussianblur(2.5f, 5);
            gau.Compute_cl(src);
            gau.Compute(src);
        }
    }
}
