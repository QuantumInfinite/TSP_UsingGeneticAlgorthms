using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text;
using System.Threading.Tasks;

namespace _3P71_2
{
    class GeneticTS
    {
        //Params
        public readonly Random random;
        public readonly City[] cities;
        public readonly Program.ElitismMode elietismMode;
        public readonly Program.CrossoverType crossType;
        public readonly double crossoverRate;
        public readonly double mutationRate;
        public readonly int maxPopSize;
        public readonly int maxGenerationSpan;
        public readonly int randomSeed;
        public readonly int tournamentSize;
        public readonly int startCityIndex;
        public readonly bool allowConvergence;
        public readonly int roundDigits;
        public readonly string experimentName;
        List<Tour> tours;

        //Convergence Counters
        public HashSet<double> foundPaths = new HashSet<double>();
        public double[,] connectionCosts;

        //Needed variables
        public readonly int safeZone = 0;
        Crossover crossover;
        //Stats
        double currentBestCost;
        public List<double> bestFitnesses;
        public List<double> avgFitnesses;

        public GeneticTS(Program.ElitismMode elietismMode, Program.CrossoverType crossType, double crossoverRate, 
            double mutationRate, int maxPopSize, int maxGenerationSpan, int randomSeed, int tournamentSize, 
            int startCityIndex, City[] cities, bool allowConvergence, int roundDigits, int safeZone)
        {
            //Parameters
            this.elietismMode = elietismMode;
            this.crossType = crossType;
            this.crossoverRate = crossoverRate;
            this.mutationRate = mutationRate;
            this.maxPopSize = maxPopSize;
            this.maxGenerationSpan = maxGenerationSpan;
            this.randomSeed = randomSeed;
            this.tournamentSize = tournamentSize;
            this.startCityIndex = startCityIndex;
            this.cities = cities;
            this.allowConvergence = allowConvergence;
            this.roundDigits = roundDigits;
            this.safeZone = safeZone;
            experimentName = string.Format("{0}-{1}-{2}-{3}-{4}({5})", cities.Length, elietismMode, crossoverRate, mutationRate, allowConvergence, randomSeed);

           
            //Setup
            connectionCosts = new double[cities.GetLength(0) + 1, cities.GetLength(0) + 1];
            random = new Random(randomSeed);
            crossover = new Crossover(this);
            avgFitnesses = new List<double>();
            bestFitnesses = new List<double>();
            Console.WriteLine("     Starting " + experimentName);
            MainLoop();
        }

        /// <summary>
        /// Does all the work
        /// </summary>
        private void MainLoop()
        {
            currentBestCost = int.MaxValue;
            avgFitnesses = new List<double>();
            bestFitnesses = new List<double>();
            foundPaths.Clear();
            tours = GenerateTours();
            currentBestCost = tours[0].Cost;
            Stopwatch stopwatch = Stopwatch.StartNew();
            int convergenceCounter = 0;
            for (int i = 0; i < maxGenerationSpan; i++)
            {
                avgFitnesses.Add(tours.Sum(x => x.Cost) / tours.Count);
                bestFitnesses.Add(tours[0].Cost);

                switch (crossType)
                {
                    case Program.CrossoverType.UOX:
                        tours = crossover.UOXCrossover(tours);
                        break;
                    case Program.CrossoverType.PMX:
                        tours = crossover.PMXCrossover(tours);
                        break;
                    case Program.CrossoverType UOXPMX:
                        tours = crossover.UOXCrossover(tours);
                        tours = crossover.PMXCrossover(tours);
                        break;
                }
                
                tours = Mutate(tours);

                //Convergence check. Progress after this point is basically random, so might as well stop
                if (tours[0].Cost == bestFitnesses.Last() || Math.Abs(avgFitnesses.Last() - bestFitnesses.Last()) < 1)
                {
                    convergenceCounter++;
                    if (convergenceCounter > maxGenerationSpan / 5)
                    {
                        Console.WriteLine("         breaking after " + i);
                        break;
                    }
                }
                else
                {
                    convergenceCounter = 0;
                }
            }
            bestFitnesses = TrimTail(bestFitnesses);

        }
        /// <summary>
        /// Generates a string array contaaining all the important information about this GeneticTS run
        /// </summary>
        /// <returns>Information on this experiment</returns>
        public string[] GetExperimentInfo()
        {
            return new string[]
            {
                (",,Data set: " + cities.Length),
                (",,startCityIndex: " + startCityIndex),
                (",,crossoverRate: " + crossoverRate),
                (",,mutationRate: " + mutationRate),
                (",,maxGenerationSpan: " + maxGenerationSpan),
                (",,maxPopSize: " + maxPopSize),
                (",,Tournement Size: " + tournamentSize),
                (",,Seed: \"" + randomSeed + "\""),
                (",,Final Path Cost: " + tours[0].Cost),
                (",,Tour: " + PathToString(tours[0].Path)),
            };
        }
        /// <summary>
        /// Trims the tail of a list of doubles so that the final value, if it repeats, repeats no more than list.Count/20 times
        /// </summary>
        /// <param name="list"> list to trim</param>
        /// <returns>trimd list</returns>
        private List<double> TrimTail(List<double> list)
        {
            int firstIndexOf = list.IndexOf(list.Last()) + 1 + (list.Count / 20);
            if (firstIndexOf < list.Count - 1)
            {
                list.RemoveRange(firstIndexOf, list.Count - firstIndexOf);
            }
            return list;
        }
        /// <summary>
        /// Mutates the a number of random tours in the given list.
        /// Mutation is done through random swapping.
        /// mutateCount is calculated to dramatically redice the number of Math.Random calls that would be needed.
        /// On average, it will do the same number of mutations as just doing Math.Random.Next(2) < mutatuinRate
        /// </summary>
        /// <param name="tourList"></param>
        /// <returns></returns>
        private List<Tour> Mutate(List<Tour> tourList)
        {
            int mutateCount = (int)(mutationRate * tourList.Count) / 2;
            for (int i = 0; i < mutateCount; i++)
            {
                int parentIndex = TournementSelect(tourList);
                int[] child = (int[])tourList[parentIndex].Path.Clone();

                //mutate ( swap )
                int swapIndexA = random.Next(1, child.Length - 1);
                int swapIndexB = random.Next(1, child.Length - 1);
                int tempVal = child[swapIndexA];
                child[swapIndexA] = child[swapIndexB];
                child[swapIndexB] = tempVal;

                tourList = AddChild(tourList, child, parentIndex);
            }
            return Prioritize(tourList);
        }
        /// <summary>
        /// Generates tour paths randomly
        /// </summary>
        /// <returns>List of randomly generated tours</returns>
        private List<Tour> GenerateTours()
        {
            tours = new List<Tour>();
            int[] curPath = new int[cities.Length];
            double cost;
            for (int i = 0; i < curPath.Length; i++)
            {
                curPath[i] = i + 1;
            }

            for (int i = 0; i < maxPopSize; i++)
            {
                curPath = Shuffle(curPath);
                cost = CalcTourCost(curPath);
                if (foundPaths.Add(cost))
                {
                    tours.Add(new Tour(curPath, cost));
                }
            }

            return Prioritize(tours);
        }
        /// <summary>
        /// Sorts list of tours from min -> max based on tour cost.
        /// Also prune's list to ensure there are no more than maxPopSize tours
        /// </summary>
        /// <param name="tourList">tourList to sort</param>
        /// <returns>Sorted and potentially shrunk list</returns>
        public List<Tour> Prioritize(List<Tour> tourList)
        {
            List<Tour> tours = new List<Tour>(tourList.OrderBy(tour => tour.Cost));
            if (tourList.Count > maxPopSize)
            {
                tours.RemoveRange(maxPopSize, (tours.Count - maxPopSize));
            }
            return tours;
        }
        /// <summary>
        /// Converts an array of integers into a single string with arrows
        /// </summary>
        /// <param name="path">integer array representing path</param>
        /// <returns>Path in string form</returns>
        private string PathToString(int[] path)
        {
            string output = "";
            for (int i = 0; i < path.Length - 1; i++)
            {
                output += path[i] + " -> ";
                CostBetween(path[i], path[i + 1]);
            }
            output += path[path.Length - 1];
            return output;
        }

        /// <summary>
        /// Iterates through given path and calculates its overall cost
        /// </summary>
        /// <param name="path">Path to iterate</param>
        /// <returns>cost of entire tour</returns>
        public double CalcTourCost(int[] path)
        {
            double cost = 0;
            for (int i = 0; i < path.Length - 1; i++)
            {
                cost += CostBetween(path[i], path[i + 1]);
            }
            return cost;
        }
        /// <summary>
        /// Calculates the cost between two cities, given their indexes
        /// </summary>
        /// <param name="from">first city</param>
        /// <param name="to"> second city</param>
        /// <returns>cost between cities</returns>
        double CostBetween(int from, int to)
        {
            double val = connectionCosts[from, to];
            if (val == 0)
            {
                val = Math.Sqrt(Math.Pow(cities[from - 1].x - cities[to - 1].x, 2) + Math.Pow(cities[from - 1].y - cities[to - 1].y, 2));
                connectionCosts[from, to] = val;
                connectionCosts[to, from] = val;
            }
            return val;
        }
        /// <summary>
        /// Knuff shuffle of a given array of integers
        /// </summary>
        /// <param name="cities">array of integers, in order from 1 to cities.length - 1</param>
        /// <returns> shuffled array</returns>
        private int[] Shuffle(int[] cities)
        {
            List<int> output = new List<int>();
            for (int i = 0; i < cities.Length; i++)
            {
                if (cities[i] != startCityIndex)
                {
                    output.Add(cities[i]);
                }
            }

            for (int i = output.Count; i > 1; i--)
            {
                int k = random.Next(i);
                int value = output[k];
                output[k] = output[i - 1];
                output[i - 1] = value;
            }

            output.Insert(0, startCityIndex);
            output.Add(startCityIndex);

            return output.ToArray();
        }
        /// <summary>
        /// Selects a tour through tournement selection. Size of tournement is set in initialization
        /// </summary>
        /// <param name="tourList">List to choose from</param>
        /// <returns>index of winner of tournement</returns>
        public int TournementSelect(List<Tour> tourList)
        {
            int bestIndex = random.Next(0, tourList.Count - 1);
            for (int i = 0; i < tournamentSize - 1; i++)
            {
                int ranIndex = random.Next(0, tourList.Count - 1);
                if (tourList[bestIndex].Cost > tourList[ranIndex].Cost)
                {
                    bestIndex = ranIndex;
                }
            }
            return bestIndex;
        }
        /// <summary>
        /// Adds given child to given List, and if it's parent is not protected by current elitism mode, deletes given parent
        /// </summary>
        /// <param name="tourList">list to append</param>
        /// <param name="ch">child</param>
        /// <param name="parentIndex">index of parent in tourList</param>
        /// <returns>new tourList</returns>
        public List<Tour> AddChild(List<Tour> tourList, int[] ch, int parentIndex)
        {
            double cost = Math.Round(CalcTourCost(ch), roundDigits); ;
            if (allowConvergence || foundPaths.Add(cost))
            {
                if (parentIndex > safeZone)
                {
                    tourList.RemoveAt(parentIndex);
                }
                tourList.Add(new Tour(ch, cost));
            }
            return tourList;
        }

    }
}