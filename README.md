# TSP-Assign
This repository is created for practicing some of the heuristic algorithms in Symmetric TSP problem. Used 3 methods are:

## ASSIGN Neighborhood
In a given TSP solution, if we "poke holes" in every even indexed space and then redistribute the cities removed, the redistribution can be done really efficiently because it turns into the assignment problem, which has a time complexity of $O(n^3)$. Though, for large instances this is still a large number, so we also restrict the solution space by limiting the number of holes poked. Also we don't need to restrict ourselves to the even indexes, as long as none of the 2 indexes are consecutive we can generate lots of different neighorhoods.

In fact, at some point I mistakenly made the solver poke 2 consecutive points which made it so that assignment algorithm was unreliable. However, this somehow made it escape local minima and produce even better results. Perhaps there is a pure faulty ASSIGN based solver but I decided not to explore this territory.

## Guided Local Search (GLS)
GLS is very similar to Lagrangian relaxation. After a local optimum is reached, we assign some penalties to the features of the current solution. For TSP and its variations the features are simply whether the current solution contains an edge between node $i$ and node $j$. ASSIGN neighborhood works very well with this feature set because we only need to update assignment costs in the assignment algorithm. 

## Guided Genetic Algorithm (GGA)
Pretty much work the same way as GLS, but a population is managed instead. For updating the penalties, how many of the top $K$ solutions have the features are calculated and utility cost is multiplied by this ratio. For the recombination, OX crossover is used. And for the mutation a simple swap between 2 cities is done.

GGA didn't give good results but it was very fun to implement. I suspect it was due to poor population diversity management. 

# Usage
The code currently uses GLS solver, you can switch to GGA solver in the `Program.cs` file. The code also uses TSPLIB instances.

After downloading the git repository, write in the terminal
```bash
> dotnet run <instance-name>
```
to solve the given instance (e.g `a280`, `rat99`).

# Acknowledgements
I'd like to thank Andrey Lopatin for writing a very concise assignment problem solver. I also would like to thank `cp-algorithms.com` for writing the best (and the only) good explanation of the $O(n^3)$ hungarian algorithm.