using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace gifsharp
{
    class Program
    {
        static void Main(string[] args)
        {
            var loader = new GifLoader();

            loader.Start();
        }
    }
}
