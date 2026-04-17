using Robot.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SurniakYurii.RobotChallange {
    public class DistanceHelper {
        public static int FindDistance(Position a, Position b)
        {
            return (int)(Math.Pow(a.X - b.X, 2) + Math.Pow(a.Y - b.Y, 2));
        }

        public static int ShebyshevDistance(Position a, Position b)
        {
            return (int)(Math.Max(Math.Abs(a.X - b.X), Math.Abs(a.Y - b.Y)));
        }
    }
}
