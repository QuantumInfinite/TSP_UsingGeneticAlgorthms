using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace _3P71_2
{
    [Serializable]
    class Tour
    {
        private int[] path;

        public int[] Path { get => path;}

        private double cost;

        public double Cost { get => cost; }

        public Tour(int[] path, double cost)
        {
            this.path = path;
            this.cost = cost;
        }
    }
}
