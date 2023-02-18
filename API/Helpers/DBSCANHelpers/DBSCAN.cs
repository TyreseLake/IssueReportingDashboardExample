using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace API.Helpers.DBSCANHelpers
{
    public static class DBSCAN
    {
        public static List<List<Point>> GetClusters(List<Point> points, float eps, int minPts)
        {
            if (points == null) return null;
            List<List<Point>> clusters = new List<List<Point>>();
            eps *= eps; // square eps
            int clusterId = 1;
            for (int i = 0; i < points.Count; i++)
            {
                Point p = points[i];
                if (p.ClusterId == Point.UNCLASSIFIED)
                {
                    if (ExpandCluster(points, p, clusterId, eps, minPts)) clusterId++;
                }
            }
            // sort out points into their clusters, if any
            int maxClusterId = points.OrderBy(p => p.ClusterId).Last().ClusterId;
            if (maxClusterId < 1) return clusters; // no clusters, so list is empty
            for (int i = 0; i < maxClusterId; i++) clusters.Add(new List<Point>());
            foreach (Point p in points)
            {
                if (p.ClusterId > 0) clusters[p.ClusterId - 1].Add(p);
            }
            return clusters;
        }

        public static bool AddToCluster(List<List<Point>> clusters, List<Point> points, Point newPoint, float eps, int minPts){
            if (points == null) return false;
            eps *= eps; // square eps

            int clusterId = points.OrderBy(p => p.ClusterId).Last().ClusterId;
            int initialClusterId = clusterId;
            points.Add(newPoint);
            if (newPoint.ClusterId == Point.UNCLASSIFIED)
            {
                if (LazyExpandCluster(points, newPoint, clusterId+1, eps, minPts)) clusterId++;
            }

            int maxClusterId = points.OrderBy(p => p.ClusterId).Last().ClusterId;

            if (initialClusterId < maxClusterId) clusters.Add(new List<Point>());

            //if (newPoint.ClusterId > 0) clusters[newPoint.ClusterId - 1].Add(newPoint);
            clusters[newPoint.ClusterId - 1].Add(newPoint);

            return true;
        }

        public static List<List<Point>> AddRangeToCluster(List<List<Point>> clusters, List<Point> points, 
            List<Point> newPoints, float eps, int minPts)
        {
            if (points == null) return null;
            eps *= eps; // square eps

            int clusterId = points.OrderBy(p => p.ClusterId).Last().ClusterId;
            int initialClusterId = clusterId;
            points.AddRange(newPoints);
            foreach(var newPoint in newPoints)
            {
                if (newPoint.ClusterId == Point.UNCLASSIFIED)
                {
                    if (LazyExpandCluster(points, newPoint, clusterId+1, eps, minPts)) clusterId++;
                }
            }

            // sort out points into their clusters, if any
            int maxClusterId = points.OrderBy(p => p.ClusterId).Last().ClusterId;

            // create a new cluster to store the result
            var newCluster = new List<List<Point>>();
            for (int i = 0; i < maxClusterId; i++) newCluster.Add(new List<Point>());

            for (int i = initialClusterId; i < maxClusterId; i++) clusters.Add(new List<Point>());
            foreach (Point p in newPoints)
            {
                //if (p.ClusterId > 0) clusters[p.ClusterId - 1].Add(p);
                clusters[p.ClusterId - 1].Add(p);
                newCluster[p.ClusterId -1].Add(p);
            }

            return newCluster;
        }

        public static List<Point> GetRegion(List<Point> points, Point p, float eps)
        {
            List<Point> region = new List<Point>();
            for (int i = 0; i < points.Count; i++)
            {
                float distSquared = Point.DistanceSquared(p, points[i]);
                if (distSquared <= eps) region.Add(points[i]);
            }
            return region;
        }
        
        public static List<Point> GetRegionSorted(List<Point> points, Point p, float eps)
        {
            List<float> distances = new List<float>();
            List<Point> region = new List<Point>();
            for (int i = 0; i < points.Count; i++)
            {
                float distSquared = Point.DistanceSquared(p, points[i]);
                if (distSquared <= eps) 
                {
                    if(region.Count() > 0)
                    {
                        var index = distances.BinarySearch(distSquared);
                        if (index < 0) index = ~index;
                        distances.Insert(index, distSquared);
                        region.Insert(index, points[i]);
                    }
                    else
                    {
                        distances.Add(distSquared);
                        region.Add(points[i]);
                    }
                }
            }
            return region;
        }
        
        private static bool ExpandCluster(List<Point> points, Point p, int clusterId, float eps, int minPts)
        {
            List<Point> seeds = GetRegion(points, p, eps);
            if (seeds.Count < minPts) // no core point
            {
                p.ClusterId = Point.NOISE;
                return false;
            }
            else // all points in seeds are density reachable from point 'p'
            {
                for (int i = 0; i < seeds.Count; i++) seeds[i].ClusterId = clusterId;
                seeds.Remove(p);
                while (seeds.Count > 0)
                {
                    Point currentP = seeds[0];
                    List<Point> result = GetRegion(points, currentP, eps);
                    if (result.Count >= minPts)
                    {
                        for (int i = 0; i < result.Count; i++)
                        {
                            Point resultP = result[i];
                            if (resultP.ClusterId == Point.UNCLASSIFIED || resultP.ClusterId == Point.NOISE)
                            {
                                if (resultP.ClusterId == Point.UNCLASSIFIED) seeds.Add(resultP);
                                resultP.ClusterId = clusterId;
                            }
                        }
                    }
                    seeds.Remove(currentP);
                }
                return true;
            }
        }

        private static bool LazyExpandCluster(List<Point> points, Point p, int nextClusterId, float eps, int minPts)
        {
            List<Point> seeds = GetRegionSorted(points, p, eps);
            if (seeds.Count < minPts) // no core point
            {
                p.ClusterId = Point.NOISE;
                return false;
            }
            else // all points in seeds are density reachable from point 'p'
            {
                var closestClusterId = seeds.Where(s => s.ClusterId > 0)
                    .Select(s => s.ClusterId)
                    .DefaultIfEmpty(nextClusterId)
                    .First();

                foreach(var point in seeds.Where(s => s.ClusterId == Point.UNCLASSIFIED || s.ClusterId == Point.NOISE))
                {
                    point.ClusterId = closestClusterId;
                }

                return true;
            }
        }
    }
}