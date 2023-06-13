using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

class Program
{
    static void Main()
    {
        // Define the coordinates to find the nearest vehicle positions
        var coordinates = new List<Coordinate>
        {
            new Coordinate(1, 34.544909, -102.100843),
            new Coordinate(2, 32.345544, -99.123124),
            new Coordinate(3, 33.234235, -100.214124),
            new Coordinate(4, 35.195739, -95.348899),
            new Coordinate(5, 31.895839, -97.789573),
            new Coordinate(6, 32.895839, -101.789573),
            new Coordinate(7, 34.115839, -100.225732),
            new Coordinate(8, 32.335839, -99.992232),
            new Coordinate(9, 33.535339, -94.792232),
            new Coordinate(10, 32.234235, -100.222222)
        };

        // Read the binary data file
        var vehiclePositions = ReadVehiclePositions(@"C:\Users\archi\Downloads\VehiclePositions_DataFile\VehiclePositions.dat");

        // Build the k-d tree with vehicle positions
        var kdTree = BuildKDTree(vehiclePositions);

        // Find the nearest vehicle position for each coordinate
        foreach (var coordinate in coordinates)
        {
            var nearestVehicle = FindNearestVehicle(kdTree, coordinate);
            if (nearestVehicle != null)
            {
                double distance = CalculateDistance(coordinate.Latitude, coordinate.Longitude, nearestVehicle.Latitude, nearestVehicle.Longitude);
                Console.WriteLine($"Nearest Vehicle to Position #{coordinate.Position}: Vehicle ID: {nearestVehicle.VehicleId}, Distance: {distance} km");
            }
        }
        Console.ReadLine();
    }

    static List<VehiclePosition> ReadVehiclePositions(string filePath)
    {
        var vehiclePositions = new List<VehiclePosition>();

        using (var reader = new BinaryReader(File.Open(filePath, FileMode.Open)))
        {
            while (reader.BaseStream.Position < reader.BaseStream.Length)
            {
                int vehicleId = reader.ReadInt32();
                string registration = ReadNullTerminatedString(reader);
                float latitude = reader.ReadSingle();
                float longitude = reader.ReadSingle();
                ulong recordedTimeUTC = reader.ReadUInt64();

                var vehiclePosition = new VehiclePosition(vehicleId, registration, latitude, longitude, recordedTimeUTC);
                vehiclePositions.Add(vehiclePosition);
            }
        }

        return vehiclePositions;
    }

    static string ReadNullTerminatedString(BinaryReader reader)
    {
        var stringBuilder = new StringBuilder();
        char currentChar;

        while ((currentChar = reader.ReadChar()) != '\0')
        {
            stringBuilder.Append(currentChar);
        }

        return stringBuilder.ToString();
    }

    static KDTree<VehiclePosition> BuildKDTree(List<VehiclePosition> vehiclePositions)
    {
        var kdTree = new KDTree<VehiclePosition>(2); // Create a 2-dimensional k-d tree

        // Build the k-d tree with vehicle positions
        foreach (var vehiclePosition in vehiclePositions)
        {
            double[] point = { vehiclePosition.Latitude, vehiclePosition.Longitude };
            kdTree.AddPoint(point, vehiclePosition);
        }

        return kdTree;
    }

    static VehiclePosition FindNearestVehicle(KDTree<VehiclePosition> kdTree, Coordinate coordinate)
    {
        double[] targetPoint = { coordinate.Latitude, coordinate.Longitude };
        kdTree.FindNearest(targetPoint, out double[] nearestPoint, out _, out VehiclePosition result);

        return result;
    }

    static double CalculateDistance(double lat1, double lon1, double lat2, double lon2)
    {
        const double earthRadius = 6371; // in kilometers

        double dLat = ConvertToRadians(lat2 - lat1);
        double dLon = ConvertToRadians(lon2 - lon1);

        double lat1Rad = ConvertToRadians(lat1);
        double lat2Rad = ConvertToRadians(lat2);

        double a = Math.Sin(dLat / 2) * Math.Sin(dLat / 2) +
                   Math.Cos(lat1Rad) * Math.Cos(lat2Rad) *
                   Math.Sin(dLon / 2) * Math.Sin(dLon / 2);

        double c = 2 * Math.Atan2(Math.Sqrt(a), Math.Sqrt(1 - a));

        double distance = earthRadius * c;

        return distance;
    }

    static double ConvertToRadians(double degrees)
    {
        return degrees * (Math.PI / 180);
    }
}

class Coordinate
{
    public int Position { get; }
    public double Latitude { get; }
    public double Longitude { get; }

    public Coordinate(int position, double latitude, double longitude)
    {
        Position = position;
        Latitude = latitude;
        Longitude = longitude;
    }
}

class VehiclePosition
{
    public int VehicleId { get; }
    public string Registration { get; }
    public float Latitude { get; }
    public float Longitude { get; }
    public ulong RecordedTimeUTC { get; }

    public VehiclePosition(int vehicleId, string registration, float latitude, float longitude, ulong recordedTimeUTC)
    {
        VehicleId = vehicleId;
        Registration = registration;
        Latitude = latitude;
        Longitude = longitude;
        RecordedTimeUTC = recordedTimeUTC;
    }
}

class KDTree<T>
{
    private KDNode root;
    private int dimensions;

    public KDTree(int dimensions)
    {
        root = null;
        this.dimensions = dimensions;
    }

    public void AddPoint(double[] point, T value)
    {
        root = AddPoint(root, point, value, 0);
    }

    private KDNode AddPoint(KDNode node, double[] point, T value, int depth)
    {
        if (node == null)
        {
            return new KDNode(point, value);
        }

        int currentDimension = depth % dimensions;

        if (point[currentDimension] < node.Point[currentDimension])
        {
            node.Left = AddPoint(node.Left, point, value, depth + 1);
        }
        else
        {
            node.Right = AddPoint(node.Right, point, value, depth + 1);
        }

        return node;
    }

    public void FindNearest(double[] targetPoint, out double[] nearestPoint, out double nearestDistance, out T nearestValue)
    {
        nearestPoint = null;
        nearestDistance = double.MaxValue;
        nearestValue = default(T);

        FindNearest(root, targetPoint, ref nearestPoint, ref nearestDistance, ref nearestValue, 0);
    }

    private void FindNearest(KDNode node, double[] targetPoint, ref double[] nearestPoint, ref double nearestDistance, ref T nearestValue, int depth)
    {
        if (node == null)
        {
            return;
        }

        double distance = EuclideanDistance(targetPoint, node.Point);

        if (distance < nearestDistance)
        {
            nearestPoint = node.Point;
            nearestDistance = distance;
            nearestValue = node.Value;
        }

        int currentDimension = depth % dimensions;

        if (targetPoint[currentDimension] < node.Point[currentDimension])
        {
            FindNearest(node.Left, targetPoint, ref nearestPoint, ref nearestDistance, ref nearestValue, depth + 1);

            if (Math.Abs(node.Point[currentDimension] - targetPoint[currentDimension]) < nearestDistance)
            {
                FindNearest(node.Right, targetPoint, ref nearestPoint, ref nearestDistance, ref nearestValue, depth + 1);
            }
        }
        else
        {
            FindNearest(node.Right, targetPoint, ref nearestPoint, ref nearestDistance, ref nearestValue, depth + 1);

            if (Math.Abs(node.Point[currentDimension] - targetPoint[currentDimension]) < nearestDistance)
            {
                FindNearest(node.Left, targetPoint, ref nearestPoint, ref nearestDistance, ref nearestValue, depth + 1);
            }
        }
    }

    private double EuclideanDistance(double[] point1, double[] point2)
    {
        double sum = 0;

        for (int i = 0; i < dimensions; i++)
        {
            double diff = point1[i] - point2[i];
            sum += diff * diff;
        }

        return Math.Sqrt(sum);
    }

    class KDNode
    {
        public double[] Point { get; }
        public T Value { get; }
        public KDNode Left { get; set; }
        public KDNode Right { get; set; }

        public KDNode(double[] point, T value)
        {
            Point = point;
            Value = value;
            Left = null;
            Right = null;
        }
    }
}
