using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace _3P71_2
{
    class Program
    {
        //Data
        double[,] ulyssess22TSP =
        {
            {1,38.24,20.42},{2,39.57,26.15},{3,40.56,25.32},
            {4,36.26,23.12},{5,33.48,10.54},{6,37.56,12.19},
            {7,38.42,13.11},{8,37.52,20.44},{9,41.23,9.10},
            {10,41.17,13.05},{11,36.08,-5.21},{12,38.47,15.13},
            {13,38.15,15.35},{14,37.51,15.17},{15,35.49,14.32},
            {16,39.36,19.56},{17,38.09,24.36},{18,36.09,23.00},
            {19,40.44,13.57},{20,40.33,14.15},{21,40.37,14.23},
            {22,37.57,22.56}
        };

        double[,] eil51TSP =
        {
            {1,37,52},
            {2,49,49},
            {3,52,64},
            {4,20,26},
            {5,40,30},
            {6,21,47},
            {7,17,63},
            {8,31,62},
            {9,52,33},
            {10,51,21},
            {11,42,41},
            {12,31,32},
            {13,5,25},
            {14,12,42},
            {15,36,16},
            {16,52,41},
            {17,27,23},
            {18,17,33},
            {19,13,13},
            {20,57,58},
            {21,62,42},
            {22,42,57},
            {23,16,57},
            {24,8,52},
            {25,7,38},
            {26,27,68},
            {27,30,48},
            {28,43,67},
            {29,58,48},
            {30,58,27},
            {31,37,69},
            {32,38,46},
            {33,46,10},
            {34,61,33},
            {35,62,63},
            {36,63,69},
            {37,32,22},
            {38,45,35},
            {39,59,15},
            {40,5,6},
            {41,10,17},
            {42,21,10},
            {43,5,64},
            {44,30,15},
            {45,39,10},
            {46,32,39},
            {47,25,32},
            {48,25,55},
            {49,48,28},
            {50,56,37},
            {51,30,40}
        };

        public enum CrossoverType
        {
            UOX,
            PMX,
            UOXPMX
        }
        public enum ElitismMode
        {
            top10P,
            top1
        }

        public readonly string[] randomSeeds = new string[] {
            "This is a real seed",
            "Still a seed",
            "*Slaps roof of car* You can fit so many seeds in this baby",
            "I wonder if I'll loose marks over this",
            "Yeet"
        };

        public Program()
        {
            //Setup
            if (File.Exists("output.csv"))
            {
                File.Delete("output.csv");
            }
            List<double[,]> datasets = new List<double[,]> { ulyssess22TSP, eil51TSP };
            string s = "";
            do
            {
                Console.Clear();
                Console.WriteLine("1. Output all permutations. 2. Output custom run");
                s = Console.ReadLine();

            }
            while (s != "1" && s != "2");
            switch (s)
            {
                case "1":
                    double[,] set = eil51TSP;
                    int mPopSize = 1000;
                    int mGenerationSpan = 1000;
                    int tSize = 5;
                    int round = 5;
                    int startCity = 1;
                    bool useConvergence = true;
                    ElitismMode elitMode = ElitismMode.top10P;
                    CrossoverType crossoverType = CrossoverType.PMX;
                    double crossoverRate = 1.0;
                    double mutateRate = 0.1;
                    string seed = "SampleRandomSeed";
                    City[] cit = new City[set.GetLength(0)];
                    for (int i = 0; i < set.GetLength(0); i++)
                    {
                        cit[i] = new City(set[i, 1], set[i, 2]);
                    }
                    GeneticTS g1 = new GeneticTS(
                        elitMode,
                        crossoverType,
                        crossoverRate,
                        mutateRate,
                        mPopSize,
                        mGenerationSpan,
                        seed.GetHashCode(),
                        tSize,
                        startCity,
                        cit,
                        useConvergence,
                        round,
                        CalculateSafeZone(elitMode, mPopSize)
                    );
                    Output1(g1, g1.bestFitnesses, g1.avgFitnesses, "CustomRun");
                    break;
                case "2":
                    List<double> crossovers = new List<double> { 1, .9 };
                    List<double> mutations = new List<double> { 0, .1, 1 };

                    int maxPopSize = 1000;
                    int maxGenerationSpan = 1000;
                    int tournamentSize = 5;
                    int roundDigits = 5;
                    int startCityIndex = 1;
                    bool allowConvergence = true;
                    foreach (double[,] dataset in datasets)
                    {
                        foreach (CrossoverType crossType in Enum.GetValues(typeof(CrossoverType)))
                        {
                            foreach (double crossRate in crossovers)
                            {
                                foreach (double mutRate in mutations)
                                {
                                    foreach (ElitismMode eMode in Enum.GetValues(typeof(ElitismMode)))
                                    {
                                        GeneticTS geneticTS = null;
                                        List<List<double>> setBest = new List<List<double>>();
                                        List<List<double>> setAvg = new List<List<double>>();
                                        string experimentName = string.Format("SET: {0} Elietism: {1} Crossover Type: {2} Crossover Rate: {3} Mutation Rate: {4}", 
                                            dataset.GetLength(0), eMode, crossType, crossRate, mutRate);

                                        Console.WriteLine("Starting " + experimentName);
                                        for (int b = 0; b < randomSeeds.Length; b++)
                                        {
                                            City[] cities = new City[dataset.GetLength(0)];
                                            for (int i = 0; i < dataset.GetLength(0); i++)
                                            {
                                                cities[i] = new City(dataset[i, 1], dataset[i, 2]);
                                            }
                                            //Call GeneticTS
                                            geneticTS = new GeneticTS(
                                                eMode,
                                                crossType,
                                                crossRate,
                                                mutRate,
                                                maxPopSize,
                                                maxGenerationSpan,
                                                randomSeeds[b].GetHashCode(),
                                                tournamentSize,
                                                startCityIndex,
                                                cities,
                                                allowConvergence,
                                                roundDigits,
                                                CalculateSafeZone(eMode, maxPopSize)
                                            );

                                            setBest.Add(geneticTS.bestFitnesses);
                                            setAvg.Add(geneticTS.avgFitnesses);
                                        }
                                        OutputAvg(geneticTS, setBest, setAvg, experimentName);
                                    }

                                }
                            }
                        }

                    }
                    break;
            }
        }
        private int CalculateSafeZone(ElitismMode eMode, int maxPopSize)
        {
            switch (eMode)
            {
                case ElitismMode.top10P:
                    return (int)Math.Ceiling((double) maxPopSize * 0.1);
                case Program.ElitismMode.top1:
                    return 1;
                default:
                    return 0;
            }
        }
        private void OutputAvg(GeneticTS geneticTS, List<List<double>> bestMaster, List<List<double>> avgMaster, string experimentName)
        {
            if (!File.Exists("output.csv"))
            {
                File.Create("output.csv").Close();
            }
            StreamWriter data = new StreamWriter("output.csv", true);

            string[] expInfo = geneticTS.GetExperimentInfo();
            foreach (var line in expInfo)
            {
                data.WriteLine(line);
            }
            data.WriteLine("");
            data.WriteLine(experimentName);
            data.WriteLine("Best Fitness " + "," + "Average Fitness");
            //Depth
            for (int i = 0; i < bestMaster.Max(x => x.Count) - 1; i++)
            {
                //Breadth
                double avgBest = 0;
                double avgAvg = 0;
                for (int k = 0; k < bestMaster.Count; k++)
                { 
                    avgBest += (i >= bestMaster[k].Count) ? bestMaster[k].Last() : bestMaster[k][i];
                    avgAvg += (i >= avgMaster[k].Count) ? avgMaster[k].Last() : avgMaster[k][i];
                }
                data.WriteLine(avgBest / bestMaster.Count + "," + avgAvg / avgMaster.Count);
            }
            data.WriteLine("");
            data.Close();
            data.Dispose();
        }
        private void Output1(GeneticTS geneticTS, List<double> bestMaster, List<double> avgMaster, string experimentName)
        {
            if (!File.Exists("output.csv"))
            {
                File.Create("output.csv").Close();
            }
            StreamWriter data = new StreamWriter("output.csv", true);

            string[] expInfo = geneticTS.GetExperimentInfo();
            foreach (var line in expInfo)
            {
                data.WriteLine(line);
            }
            data.WriteLine("");
            data.WriteLine(experimentName);
            data.WriteLine("Best Fitness " + "," + "Average Fitness");
            for (int i = 0; i < bestMaster.Count; i++)
            {
                data.WriteLine(bestMaster[i]+ ", " +  avgMaster[i]);
            }
            data.WriteLine("");
            data.Close();
            data.Dispose();
        }
        static void Main(string[] args) { Program P = new Program(); }
    }
    
}
