using System.Text;

public static class Utils
{
    public static void Shuffle<T>(T[] list)
    {
        var rng = Random.Shared;
        int n = list.Length;
        while (n > 1)
        {
            n--;
            int k = rng.Next(n + 1);
            T temp = list[k];
            list[k] = list[n];
            list[n] = temp;
        }
    }

    public static float EvalSolution(IEnumerable<Node> cities)
    {
        var currCity = cities.First();
        var firstCity = currCity;
        float res = 0.0f;
        foreach (var city in cities.Skip(1))
        {
            res += currCity.DistTo(city);
            currCity = city;
        }
        res += currCity.DistTo(firstCity);
        return res;
    }
    public static void PrintSolution(IEnumerable<Node> cities){
        var res = new StringBuilder($"{cities.First().id}");
        foreach (var city in cities.Skip(1))
        {
            res.Append($" -> {city.id}");
        }
        System.Console.WriteLine(res.ToString());
    }
}
