using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

class Program
{
    static void Main()
    {
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

        var vehiclePositions = ReadVehiclePositions(@"C:\Users\archi\Downloads\VehiclePositions_DataFile\VehiclePositions.dat");

        foreach (var coordinate in coordinates)
        {
            var nearestVehicle = FindNearestVehicle(vehiclePositions, coordinate);
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

    static VehiclePosition FindNearestVehicle(List<VehiclePosition> vehiclePositions, Coordinate coordinate)
    {
        VehiclePosition nearestVehicle = null;
        double minDistance = double.MaxValue;

        foreach (var vehiclePosition in vehiclePositions)
        {
            double distance = CalculateDistance(coordinate.Latitude, coordinate.Longitude, vehiclePosition.Latitude, vehiclePosition.Longitude);
            if (distance < minDistance)
            {
                minDistance = distance;
                nearestVehicle = vehiclePosition;
            }
        }

        return nearestVehicle;
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
