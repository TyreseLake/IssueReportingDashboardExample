using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using API.Entities;

namespace API.Helpers.DBSCANHelpers
{
    public class Point
    {
        public const int NOISE = -1;
        public const int UNCLASSIFIED = 0;
        public int ClusterId;
        public float X, Y;
        public IssueReport IssueReport { get; set; }
        public Point(IssueReport issueReport, float x, float y)
        {
            this.IssueReport = issueReport;
            this.X = x;
            this.Y = y;
        }
        public override string ToString()
        {
            return String.Format("{0}, {1}-({2}, {3})", IssueReport.MobileIssueId, IssueReport.Subject, X, Y);
        }
        public static float DistanceSquared(Point p1, Point p2)
        {
            float diffX = p2.X - p1.X;
            float diffY = p2.Y - p1.Y;
            var result = diffX * diffX + diffY * diffY;
            // var result = ((float)Math.Sqrt((Math.Pow(p1.X - p2.X, 2) + Math.Pow(p1.Y - p2.Y, 2))));
            return result;
        }
    }
}