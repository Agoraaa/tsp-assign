using System.Net.Mail;
using System.Runtime.InteropServices;
using static Utils;

public class Solver
{
    public int popSize = 100;
    public int eliteCnt = 5;

    public int mutantCnt = 10;
    public readonly Node[] cities;
    public Node[] bestSol;
    public float bestZ = float.MaxValue;
    float _resetThreshold = float.MaxValue;

    float[][] penalties;

    int[][] closestNbs;

    public float unscaledLambda = 0.4f;

    public int nbRadius = 20;

    // public int nbCount = 100;

    public float lambda;

    Random rng = Random.Shared;

    public DateTime startTime;

    public DateTime lastImprovement;

    List<int[]> neighborhoods;

    List<bool[]> nbMatrices;

    public Solver(IEnumerable<Node> _cities)
    {
        startTime = DateTime.Now;
        this.cities = _cities.ToArray();
        bestSol = GenerateRandomTour();
        bestZ = EvalSolution(bestSol);
        lastImprovement = DateTime.Now;
        penalties = Enumerable.Repeat(0, cities.Length).Select(x => new float[cities.Length]).ToArray();

        float avgDist = 0.0f;
        foreach (var city in _cities)
        {
            foreach (var city2 in _cities)
            {
                avgDist += city.DistTo(city2);
            }
        }
        avgDist /= (float)(this.cities.Length * this.cities.Length);
        lambda = unscaledLambda * avgDist / 30.0f;

        nbMatrices = new();
        neighborhoods =
        [
            Enumerable.Range(0, cities.Length / 2).Select(x => (2 * x) + 0).ToArray(),
            // Enumerable.Range(0, cities.Length / 2).Select(x => (2 * x) + 1).ToArray(),
            // Enumerable.Range(0, cities.Length / 3).Select(x => (3 * x) + 0).ToArray(),
            // Enumerable.Range(0, cities.Length / 3).Select(x => (3 * x) + 1).ToArray(),
            // Enumerable.Range(0, cities.Length / 3).Select(x => (3 * x) + 2).ToArray(),
        ];
        int additionalNbCnt = 20;
        for (int i = 0; i < additionalNbCnt; i++)
        {
            int curInd = rng.Next(2);
            var newNb = new List<int>();
            while (curInd < cities.Length - 1)
            {
                newNb.Add(curInd);
                curInd += rng.Next(2, 4);
            }
            if ((newNb[0] != 0) && (newNb[newNb.Count - 1] < cities.Length - 2)) newNb.Add(cities.Length - 1);

            neighborhoods.Add(newNb.ToArray());
        }
        foreach (var nb in neighborhoods)
        {
            nbMatrices.Add(
                Enumerable.Range(0, cities.Length)
                .Select(x => nb.Contains(x))
                .ToArray()
                );
        }

        closestNbs = new int[cities.Length][];
        for (int i = 0; i < cities.Length; i++)
        {
            closestNbs[i] = new int[cities.Length];
        }
        for (int i = 0; i < cities.Length; i++)
        {
            List<(int nbInd, float dist)> neighbors = [];
            var curCity = cities[i];

            for (int j = 0; j < cities.Length; j++)
            {
                var toCity = cities[j];
                neighbors.Add((toCity.id, curCity.DistTo(toCity)));
            }
            neighbors.Sort((x, y) => y.dist.CompareTo(x.dist));
            closestNbs[curCity.id] = neighbors.Select(x => x.nbInd).Reverse().ToArray();
        }

    }

    Node[] GenerateRandomTour()
    {
        var res = new Node[cities.Length];
        cities.CopyTo(res, 0);
        Shuffle(res);

        // improve by local search
        int halfN = cities.Length / 2;
        var evens = Enumerable.Range(0, halfN).Select(x => 2 * x).ToArray();
        var odds = Enumerable.Range(0, halfN).Select(x => (2 * x) + 1).ToArray();
        Neighborhoods.FillHoles(res, evens);
        Neighborhoods.FillHoles(res, odds);
        Neighborhoods.FillHoles(res, evens);
        Neighborhoods.FillHoles(res, odds);
        Neighborhoods.FillHoles(res, evens);
        Neighborhoods.FillHoles(res, odds);
        return res;
    }

    // OX crossover by Oliver et al.
    Node[] Crossover(Node[] parent1, Node[] parent2)
    {
        int toInd(int ind)
        {
            return ind % cities.Length;
        }
        var isUsed = new bool[cities.Length];

        int startIndex = rng.Next(cities.Length);
        int p1InheritCnt = rng.Next(cities.Length);
        Node[] res = new Node[cities.Length];
        for (int i = startIndex; i < startIndex + p1InheritCnt; i++)
        {
            int cityInd = toInd(i);
            res[cityInd] = parent1[cityInd];
            isUsed[parent1[cityInd].id] = true;
        }
        int lastP2ind = startIndex + p1InheritCnt;
        for (int i = startIndex + p1InheritCnt; i < startIndex + cities.Length; i++)
        {
            int cityInd = toInd(i);
            while (isUsed[parent2[toInd(lastP2ind)].id])
            {
                lastP2ind++;
            }
            res[cityInd] = parent2[toInd(lastP2ind)];
            isUsed[parent2[toInd(lastP2ind)].id] = true;
            lastP2ind++;
        }
        return res;
    }

    // simple swap
    void Mutate(Node[] chromosome)
    {
        var ind1 = rng.Next(chromosome.Length);
        var ind2 = rng.Next(chromosome.Length);
        var tmp = chromosome[ind1];
        chromosome[ind1] = chromosome[ind2];
        chromosome[ind2] = tmp;
    }

    // move the tour start 1 forward so that neighborhoods are different
    void Advance(Node[] tour)
    {
        var lastElem = tour[cities.Length - 1];
        Array.Copy(tour, 0, tour, 1, tour.Length - 1);
        tour[0] = lastElem;
    }

    float[][] getCostMat(Node[] tour, int[] holes)
    {
        var res = Enumerable.Repeat(0, holes.Length).Select(x => new float[holes.Length]).ToArray();
        for (int i = 0; i < holes.Length; i++)
        {
            var holeInd = holes[i];
            var precedingCity = tour[(holeInd - 1 + tour.Length) % tour.Length];
            var succeedingCity = tour[(holeInd + 1 + tour.Length) % tour.Length];

            for (int j = 0; j < holes.Length; j++)
            {
                var cityToMove = tour[holes[j]];
                // why work smarter when you can work harder
                // its not like adding if branches would be faster
                res[j][i] = precedingCity.DistTo(cityToMove) +
                            succeedingCity.DistTo(cityToMove) +
                            penalties[precedingCity.id][cityToMove.id] +
                            penalties[cityToMove.id][precedingCity.id] +
                            penalties[succeedingCity.id][cityToMove.id] +
                            penalties[cityToMove.id][succeedingCity.id];
            }
        }
        return res;
    }

    void Educate(Node[] chromosome)
    {
        for (int iterCnt = 0; iterCnt < neighborhoods.Count; iterCnt++)
        {
            int[] holes;
            if (false)
            {
                // take N nearest neighbors in the neighborhood
                var bigNb = nbMatrices[iterCnt];
                var targetCity = rng.Next(cities.Length);
                var nb = closestNbs[targetCity].Where(x => bigNb[x]).Take(nbRadius).ToHashSet();
                holes = Enumerable.Range(0, cities.Length).Where(x => nb.Contains(chromosome[x].id)).ToArray();
            }
            else
            {
                // just take N random, this one seems to produce better results
                var bigNb = neighborhoods[iterCnt];
                Shuffle(bigNb);
                holes = bigNb.Take(nbRadius).ToArray();
            }
            var assignments = Neighborhoods.SolveAssignment(getCostMat(chromosome, holes));

            var holeCities = holes.Select(x => chromosome[x]).ToArray();
            for (int i = 0; i < holes.Length; i++)
            {
                chromosome[holes[assignments[i]]] = holeCities[i];
            }

        }
    }

    // get TSP features for Guided Search
    List<(int city1, int city2)> GetFeatures(Node[] tour)
    {
        List<(int city1, int city2)> res = new();
        var city1Ind = tour[tour.Length - 1].id;
        var city2Ind = tour[0].id;
        if (city2Ind < city1Ind)
        {
            // swap two indexes
            city1Ind ^= city2Ind;
            city2Ind ^= city1Ind;
            city1Ind ^= city2Ind;
        }
        res.Add((city1Ind, city2Ind));

        for (int i = 0; i < tour.Length - 1; i++)
        {
            city1Ind = tour[i].id;
            city2Ind = tour[i + 1].id;
            if (city2Ind < city1Ind)
            {
                // swap two indexes
                city1Ind ^= city2Ind;
                city2Ind ^= city1Ind;
                city1Ind ^= city2Ind;
            }
            res.Add((city1Ind, city2Ind));
        }
        return res;
    }

    float EvalWithFeatures(Node[] tour)
    {
        float tourZ = EvalSolution(tour);
        if (tourZ < bestZ)
        {
            if (tourZ < _resetThreshold)
            {
                penalties = Enumerable.Repeat(0, cities.Length).Select(x => new float[cities.Length]).ToArray();
                _resetThreshold = 0.99f * tourZ;
                // System.Console.WriteLine($"New best found with Z={tourZ:F4} at T={(DateTime.Now - startTime).TotalSeconds:F2} seconds. Weights reset");
            }
            else
            {
                // System.Console.WriteLine($"New best found with Z={tourZ:F4} at T={(DateTime.Now - startTime).TotalSeconds:F2} seconds");
            }
            bestZ = tourZ;
            tour.CopyTo(bestSol, 0);
            lastImprovement = DateTime.Now;
        }
        float featureZ = 0.0f;
        foreach (var feature in GetFeatures(tour))
        {
            featureZ += penalties[feature.city1][feature.city2];
        }
        return tourZ + (lambda * featureZ);
    }

    float[][] CalculateUtils(IEnumerable<Node[]> solutions)
    {
        var res = Enumerable.Repeat(0, cities.Length).Select(x => new float[cities.Length]).ToArray();
        var solCnt = 0;
        foreach (var sol in solutions)
        {
            solCnt++;
            foreach (var feature in GetFeatures(sol))
            {
                var i = feature.city1;
                var j = feature.city2;
                // why not just initialize penalties at 1??
                res[i][j] += cities[i].DistTo(cities[j]) / (1 + penalties[i][j]);
            }
        }
        for (int i = 0; i < cities.Length - 1; i++)
        {
            for (int j = i + 1; j < cities.Length; j++)
            {
                res[i][j] /= solCnt;
            }
        }
        return res;
    }

    float[][] CalculateUtil(Node[] solution)
    {
        var res = Enumerable.Repeat(0, cities.Length).Select(x => new float[cities.Length]).ToArray();
        foreach (var feature in GetFeatures(solution))
        {
            var i = feature.city1;
            var j = feature.city2;
            // why not just initialize penalties at 1??
            res[i][j] += cities[i].DistTo(cities[j]) / (1 + penalties[i][j]);
        }
        return res;
    }

    public void SolveGGA()
    {
        var allPops = new List<List<(Node[] tour, float Z)>>();
        int macroPopCnt = 1;
        var population = new List<(Node[] tour, float Z)>();
        _resetThreshold = -1.0f;
        for (int i = 0; i < macroPopCnt; i++)
        {

            population = Enumerable.Repeat(0, popSize)
            .Select(x => GenerateRandomTour())
            .Select(x => (x, EvalWithFeatures(x)))
            .ToList();

            var startTs = DateTime.Now;
            lastImprovement = DateTime.Now;
            while ((DateTime.Now - lastImprovement).TotalSeconds < 0.5f)
            {
                population.Sort((x, y) => x.Z.CompareTo(y.Z));
                var newPop = new List<(Node[] tour, float Z)>(popSize);
                newPop.AddRange(population.Take(eliteCnt));

                while (newPop.Count < popSize)
                {
                    var par1 = population[rng.Next(popSize)].tour;
                    var par2 = population[rng.Next(popSize)].tour;
                    var child = Crossover(par1, par2);
                    if (rng.NextSingle() < 0.3f)
                    {
                        Mutate(child);
                    }
                    Educate(child);
                    newPop.Add((child, EvalWithFeatures(child)));
                }
                population = newPop;
            }
            System.Console.WriteLine($"Pop {i + 1} done");
            allPops.Add(population);
        }
        foreach (var subPop in allPops)
        {
            population.AddRange(subPop.Take(popSize / macroPopCnt));
        }
        System.Console.WriteLine("Switching to Guided Genetic Algorithm...");
        for (int i = 0; i < popSize; i++)
        {
            for (int j = 0; j < cities.Length / 30; j++)
            {
                //Mutate(population[i].tour);
            }
            population[i] = (population[i].tour, EvalSolution(population[i].tour));
        }
        population.Sort((x, y) => x.Z.CompareTo(y.Z));

        bool swap = false;
        long genNum = 1;
        bestZ = float.MaxValue;
        var lastZ = bestZ;
        _resetThreshold = float.MaxValue;
        while (true)
        {
            population.Sort((x, y) => x.Z.CompareTo(y.Z));
            if (swap ^ (DateTime.Now.Second % 2 == 0))
            {
                swap ^= true;
                var outputStr = $"Z={EvalSolution(population[0].tour):F4} GuidedZ={EvalWithFeatures(population[0].tour):F4} BestZ={bestZ:F4} Generation: {genNum} Time: {(DateTime.Now - startTime).TotalSeconds:F0} Seconds ";
                if (lastZ != bestZ)
                {
                    lastZ = bestZ;
                    outputStr += $"New best Z={bestZ:F4}";
                }
                System.Console.WriteLine(outputStr);
            }
            var newPop = new List<(Node[] tour, float Z)>(popSize);
            newPop.AddRange(population.Take(eliteCnt).Select(x => (x.tour, EvalWithFeatures(x.tour))));

            for (int i = 0; i < mutantCnt; i++)
            {
                var mutant = GenerateRandomTour();
                population.Add((mutant, EvalWithFeatures(mutant)));
            }
            while (newPop.Count < popSize)
            {
                var par1 = population[rng.Next(popSize)].tour;
                var par2 = population[rng.Next(popSize)].tour;
                var child = Crossover(par1, par2);
                // how ofen does education undo the mutation?
                // Mutate(child);
                Educate(child);
                newPop.Add((child, EvalWithFeatures(child)));
            }
            population = newPop;

            var utils = CalculateUtils(population.Take(eliteCnt).Select(x => x.tour));
            var maxUtil = 0.0f;
            var penaltyToUpdate = (0, 0);
            for (int i = 0; i < cities.Length - 1; i++)
            {
                for (int j = i + 1; j < cities.Length; j++)
                {
                    if (utils[i][j] > maxUtil)
                    {
                        maxUtil = utils[i][j];
                        penaltyToUpdate = (i, j);
                    }
                }
            }
            penalties[penaltyToUpdate.Item1][penaltyToUpdate.Item2]++;
            genNum++;
        }
    }

    public void SolveGLS()
    {
        var curSol = GenerateRandomTour();
        var swap = false;
        var lastZ = float.MaxValue;
        long iterNum = 1;
        while (true)
        {
            if (swap ^ (DateTime.Now.Second % 2 == 0))
            {
                swap ^= true;
                var outputStr = $"Z={EvalSolution(curSol):F4} GuidedZ={EvalWithFeatures(curSol):F4} BestZ={bestZ:F4} Iteration: {iterNum} Time: {(DateTime.Now - startTime).TotalSeconds:F0} Seconds ";
                if (lastZ != bestZ)
                {
                    lastZ = bestZ;
                    outputStr += $"New best Z={bestZ:F4}";
                }
                System.Console.WriteLine(outputStr);
            }
            Advance(curSol);
            Educate(curSol);
            var utils = CalculateUtil(curSol);
            var maxUtil = 0.0f;
            var penaltyToUpdate = (0, 0);
            for (int i = 0; i < cities.Length - 1; i++)
            {
                for (int j = i + 1; j < cities.Length; j++)
                {
                    if (utils[i][j] > maxUtil)
                    {
                        maxUtil = utils[i][j];
                        penaltyToUpdate = (i, j);
                    }
                }
            }
            penalties[penaltyToUpdate.Item1][penaltyToUpdate.Item2]++;
            iterNum++;
        }
    }
}