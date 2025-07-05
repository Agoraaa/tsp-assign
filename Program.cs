using System.Numerics;
using static Utils;
using static Neighborhoods;
using System.Globalization;

internal partial class Program
{
    private static void Main(string[] args)
    {
        Thread.CurrentThread.CurrentCulture = CultureInfo.InvariantCulture;
        string filePath = $"tsplib/{args[0]}.tsp";
        string fileText = "";
        try
        {
            using StreamReader reader = new(filePath);
            fileText = reader.ReadToEnd();
        }
        catch (IOException e)
        {
            Console.WriteLine("The file could not be read:");
            Console.WriteLine(e.Message);
            return;
        }
        Console.WriteLine("File reading successful. Parsing the file...");

        List<Node> cities = [];
        var lines = fileText.Trim().Split('\n');
        bool foundCoordSection = false;
        foreach (var line in lines)
        {
            if (!foundCoordSection)
            {
                if (line.Trim() == "NODE_COORD_SECTION")
                {
                    foundCoordSection = true;
                }
                continue;
            }
            if (line.Trim() == "EOF") break;
            var splitted = line.Trim().Split().Where(x => !string.IsNullOrWhiteSpace(x)).ToArray();
            var id = int.Parse(splitted[0])-1;
            var x = float.Parse(splitted[1]);
            var y = float.Parse(splitted[2]);
            var city = new Node(new Vector2(x, y), id);
            cities.Add(city);
        }
        Console.WriteLine($"Parsing successful. Solving a problem with {cities.Count} cities.");
        var solver = new Solver(cities);
        // solver.SolveGGA();
        solver.SolveGLS();
    }
}