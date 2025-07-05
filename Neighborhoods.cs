public static class Neighborhoods
{
    public static int[] SolveAssignment(float[][] costMat)
    {
        int n = costMat.Length;
        int m = costMat[0].Length;
        float[] u = new float[n + 1];
        float[] v = new float[m + 1];
        int[] p = new int[m + 1];
        int[] way = new int[m + 1];
        float[][] A = new float[n + 1][];
        A[0] = new float[m + 1];
        for (int i = 1; i < n + 1; i++)
        {
            A[i] = new float[m + 1];
            for (int j = 1; j < m + 1; j++)
            {
                A[i][j] = costMat[i - 1][j - 1];
            }
        }

        for (int i = 1; i <= n; ++i)
        {
            p[0] = i;
            int j0 = 0;
            float[] minv = Enumerable.Repeat(float.MaxValue, m + 1).ToArray();
            bool[] used = Enumerable.Repeat(false, m + 1).ToArray();
            do
            {
                used[j0] = true;
                int i0 = p[j0];
                float delta = float.MaxValue;
                int j1 = 0;
                for (int j = 1; j <= m; ++j)
                    if (!used[j])
                    {
                        float cur = A[i0][j] - u[i0] - v[j];
                        if (cur < minv[j])
                        {
                            minv[j] = cur;
                            way[j] = j0;
                        }
                        if (minv[j] < delta)
                        {

                            delta = minv[j];
                            j1 = j;
                        }
                    }
                for (int j = 0; j <= m; ++j)
                    if (used[j])
                    {

                        u[p[j]] += delta;
                        v[j] -= delta;
                    }
                    else
                    {
                        minv[j] -= delta;
                    }
                j0 = j1;
            } while (p[j0] != 0);
            do
            {
                int j1 = way[j0];
                p[j0] = p[j1];
                j0 = j1;
            } while (j0 > 0);
        }
        var res = new int[n];
        for (int i = 0; i < n; i++)
        {
            res[p[i + 1] - 1] = i;
        }
        return res;
    }

    public static void FillHoles(Node[] tour, int[] holes)
    {
        var n = holes.Length;
        var assignmentMat = new float[n][];
        for (int i = 0; i < n; i++)
        {
            var currCity = tour[holes[i]];
            assignmentMat[i] = new float[n];
            for (int j = 0; j < n; j++)
            {
                Node precedingCity = tour[(holes[j] - 1 + tour.Length) % tour.Length];
                Node succeedingCity = tour[(holes[j] + 1 + tour.Length) % tour.Length];
                assignmentMat[i][j] = currCity.DistTo(precedingCity) + currCity.DistTo(succeedingCity);
            }
        }
        var assignments = SolveAssignment(assignmentMat);
        var holeCities = holes.Select(x => tour[x]).ToArray();
        for (int i = 0; i < n; i++)
        {
            tour[holes[assignments[i]]] = holeCities[i];
        }
    }
    
    public static void Swap(ref Node[] tour, Random rng = null){
        if (rng is null){
            rng = Random.Shared;
        }
        var ll = new LinkedList<Node>(tour);
        bool[] isDone = new bool[tour.Length];
        var checkedCount = 0;
        while (checkedCount < tour.Length)
        {
            var currCity = ll.First;
            var candidates = new List<(LinkedListNode<Node> previous, float gain)>();
            while (isDone[currCity.Value.id-1])
            {
                currCity = currCity.Next ?? ll.First;
            }
            var prev = currCity.Previous ?? ll.Last;
            var gainedDist = prev.Value.DistTo(currCity.Value) + currCity.Value.DistTo((currCity.Next ?? ll.First).Value);
            var cityToMove = currCity.Next ?? ll.First;
            while (cityToMove != prev)
            {
                var lostDist = cityToMove.Value.DistTo(currCity.Value) + currCity.Value.DistTo((cityToMove.Next?? ll.First).Value);
                candidates.Add((cityToMove, gainedDist - lostDist));
                cityToMove = cityToMove.Next ?? ll.First;
            }
            candidates.Add((cityToMove.Previous ?? ll.Last, 0.0f));
            candidates.Sort((x, y) => -1*x.gain.CompareTo(y.gain));
            if(rng.NextSingle() < 0.9f) cityToMove = candidates[0].previous;
            else if(rng.NextSingle() < 0.5f) cityToMove = candidates[1].previous;
            else if(rng.NextSingle() < 0.5f) cityToMove = candidates[2].previous;
            else cityToMove = candidates[rng.Next(candidates.Count)].previous;
        
            ll.Remove(currCity);
            ll.AddAfter(cityToMove, currCity);
            
            isDone[currCity.Value.id-1] = true;
            checkedCount += 1;
        }
        tour = ll.ToArray();
}

}