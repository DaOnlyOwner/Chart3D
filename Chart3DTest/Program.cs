using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Chart3D;


namespace Chart3DTest
{
    class Program
    {
        static void Main(string[] args)
        {
            Chart3D.Chart3D test = new Chart3D.Chart3D( (a,b) => a*a, 0.1f,0.1f,3.14f,3.14f,10,10,4 );
            test.Run();

        }
    }
}
