using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SemC
{
    public class Record
    {
        public string Name { get; set; }
        public int Population { get; set; }
        public double Area { get; set; }

        public override string ToString() => $"{Name} (Obyvatelé: {Population}, Rozloha: {Area} km²)";
    }
}
