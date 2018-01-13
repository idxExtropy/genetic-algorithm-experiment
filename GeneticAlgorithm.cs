using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace intellegere
{
    class GeneticAlgorithm
    {
        enum ChromosomeType
        {
            NUMBER,
            OPERATOR,
            INVALID
        }

        enum Operators
        {
            SUM,
            SUBTRACT,
            MULTIPLY,
            DIVIDE
        }

        public static byte[,] population;
        public static float[] fitness;
        public static string[] realization;
        public static decimal targetValue;
        public static double totalFitness;
        public static int[] rouletteWheel;
        public static float diversity;

        public static decimal mutationRate, startingMutationRate, crossoverRate;

        private static int populationSize, chromosomeLength;

        //===================================================================
        // Initializes the starting population and creates the random
        // starting members.
        //===================================================================
        public static void InitializePopulation(int popSize, int chromLength, decimal target, decimal mutRate, decimal crossRate)
        {
            Random randomChrom = new Random();

            // Initialize starting population characteristics.
            fitness = new float[popSize];
            rouletteWheel = new int[popSize];
            realization = new string[popSize];
            population = new byte [popSize, (int)(chromLength/2)];
            
            populationSize = popSize;
            chromosomeLength = (int)(chromLength/2);
            targetValue = target;
            mutationRate = mutRate;
            startingMutationRate = mutRate;
            crossoverRate = crossRate;

            for (int i = 0; i < populationSize; i++)
            {
                for (int j = 0; j < chromosomeLength; j++)
                {
                    population[i, j] = (byte)randomChrom.Next(0, 13);
                    population[i, j] += (byte)(randomChrom.Next(0, 13) << 4);
                }

                realization[i] = string.Empty;
                fitness[i] = 0;
            }
        }

        //===================================================================
        // Reorganize the population by genetic crossover.
        //===================================================================
        public static void GeneticCrossover()
        {
            GenerateRouletteWheel();

            Random crossoverChance = new Random();
            Random crossoverBit = new Random();

            byte [,] newPopulation = new byte[populationSize, (int)(chromosomeLength)];

            // Create a new population from the previous generation.
            for (int i = 0; i < populationSize; i += 2)
            {
                int a, b;
                GetParents(out a, out b);

                for (int j = 0; j < chromosomeLength; j++)
                {
                    newPopulation[i, j] = population[a,j];
                    newPopulation[i+1, j] = population[b, j];
                }

                // Perform genetic crossover.
                if (crossoverChance.Next(1, 10000) <= crossoverRate * 100)
                {
                    int bit = crossoverBit.Next(0,chromosomeLength*8);
                    for (int j = chromosomeLength - 1; j >= 0; j--)
                    {
                        if (bit / 8 <= j)
                        {
                            for (int k = 7; k >= bit % 8; k--)
                            {
                                if (newPopulation[i, j] >> k != population[b, j] >> k)
                                {
                                    newPopulation[i, j] ^= (byte)(0x01 << k);
                                    newPopulation[i + 1, j] ^= (byte)(0x01 << k);
                                }
                            }
                        }
                    }
                }
            }

            population = newPopulation;
        }

        //===================================================================
        // Get a parent for the current child based on fitness.
        //===================================================================
        private static void GetParents(out int a, out int b)
        {
            Random parent = new Random((int)DateTime.Now.Ticks);
            a = rouletteWheel[parent.Next(0, populationSize)];
            b = rouletteWheel[parent.Next(0, populationSize)];
        }

        //===================================================================
        // Generate the roulette wheel for selection.
        //===================================================================
        private static void GenerateRouletteWheel()
        {
            Random survivor = new Random();
            float[] tempFitness = new float[populationSize];
            Array.Copy(fitness, tempFitness, tempFitness.Length);

            // Loop through every member of the population.
            int iMember = 0;
            for (int i = 0; i < populationSize; i++)
            {
                while ((float)(tempFitness[iMember] / totalFitness) >= (float)1 / (float)populationSize)
                {
                    rouletteWheel[i] = iMember;
                    tempFitness[iMember] -= (float)(tempFitness[iMember] / totalFitness);
                    i++;

                    if (iMember == populationSize || i == populationSize)
                    {
                        for (int j = i; j < populationSize; j++)
                        {
                            rouletteWheel[j] = survivor.Next(0, populationSize);
                        }
                        break;
                    }
                }

                iMember++;
                i--;

                if (iMember == populationSize)
                {
                    for (int j = i; j < populationSize; j++)
                    {
                        rouletteWheel[j] = survivor.Next(0, populationSize);
                    }
                    break;
                }
            }
        }

        //===================================================================
        // Perform random mutations among the current population.
        //===================================================================
        public static void MutatePopulation()
        {
            Random randomMutation = new Random();

            // Loop through every member of the population.
            for (int i = 0; i < populationSize; i++)
            {
                // Loop through every chromosome;
                for (int j = 0; j < chromosomeLength; j++)
                {
                    for (int k = 0; k < 8; k++)
                    {
                        if (randomMutation.Next(1, 10000) <= mutationRate * 100)
                        {
                            population[i, j] ^= (byte)(0x01 << k);
                        }
                    }
                }
            }
        }

        //===================================================================
        // Assigns a fitness score to every member of the current population
        // and returns the index of the member most fit to survive.
        //  Parameters: 
        //   iBestMember - index of the best member found (output).
        //  Returns: fitness value of the most fit member.
        //===================================================================
        public static void AssignFitness(out int iBestMember)
        {
            totalFitness = 0;
            float mostFit = 0.0f;
            iBestMember = 0;

            // Loop through every member of the population.
            for (int i = 0; i < populationSize; i++)
            {
                fitness[i] = AnalyzeMember(i);
                if (fitness[i] != float.MaxValue)
                {
                    totalFitness += fitness[i];
                }

                if (fitness[i] >= mostFit)
                {
                    // Most fit yet.
                    iBestMember = i;
                    mostFit = fitness[i];
                }
            }
        }

        //===================================================================
        // Assigns the fitness of a member of the current population by
        // comparing the total result to that of the target.
        //  Parameters:
        //   member - index of the member to be analyzed.
        //  Returns: Fitness value.
        //===================================================================
        public static float AnalyzeMember(int member)
        {
            int lastType = -1;

            int operatorType = -1;
            decimal organismValue = 0;

            realization[member] = String.Empty;

            for (int i = 0; i < chromosomeLength; i++)
            {
                for (int j = 0; j < 2; j++)
                {
                    byte currentValue = 0x00;
                    currentValue = (byte)((population[member, i] >> (4 * j)) & 0x0f);

                    int currentType = GetCurrentType(currentValue);

                    // Determine if this chromosome makes sense.
                    if (currentType == (int)ChromosomeType.INVALID)
                    {
                        continue;
                    }
                    else if (currentType == lastType)
                    {
                        continue;
                    }

                    // Apply the chromosome to the organism value.
                    if (currentType == (int)ChromosomeType.NUMBER &&
                        lastType == -1)
                    {
                        realization[member] += currentValue + " ";
                        organismValue = currentValue;
                        lastType = currentType;
                        continue;
                    }
                    
                    if (currentType == (int)ChromosomeType.OPERATOR &&
                        lastType == (int)ChromosomeType.NUMBER)
                    {
                        operatorType = currentValue - 10;
                        lastType = currentType;
                        continue;
                    }
                    
                    if (currentType == (int)ChromosomeType.NUMBER &&
                        lastType == (int)ChromosomeType.OPERATOR)
                    {
                        switch (operatorType)
                        {
                            case (int)Operators.SUM:
                            {
                                realization[member] += "+ " + currentValue + " ";
                                organismValue += currentValue;
                                break;
                            }
                            case (int)Operators.SUBTRACT:
                            {
                                realization[member] += "- " + currentValue + " ";
                                organismValue -= currentValue;
                                break;
                            }
                            case (int)Operators.MULTIPLY:
                            {
                                realization[member] += "* " + currentValue + " ";
                                organismValue *= currentValue;
                                break;
                            }
                            case (int)Operators.DIVIDE:
                            {
                                if (currentValue != 0)
                                {
                                    realization[member] += "/ " + currentValue + " ";
                                    organismValue /= currentValue;
                                    lastType = (int)ChromosomeType.OPERATOR;
                                    continue;
                                }
                                break;
                            }
                        }

                        lastType = currentType;
                        continue;
                    }
                }
            }

            realization[member] += "= " + organismValue.ToString("N1");

            if ((float)Math.Round(organismValue,1) != (float)Math.Round(targetValue,1))
            {
                return ((float)(1 / (Math.Abs(targetValue - organismValue))));
            }
            
            return (float.MaxValue);
        }

        //===================================================================
        //  Updates the mutation rate based on the amount of variability.
        //===================================================================
        public static void UpdateMutationRate()
        {
            int uniqueMembers = 0;
            string[] uniqueStrings = new string[populationSize];

            // Calculate just how diverse this population is.
            for (int i = 0; i < populationSize; i++)
            {
                string currentString = string.Empty;
                for (int j = 0; j < chromosomeLength; j++)
                {
                    currentString += Convert.ToString(population[i, j],2);
                }

                if (!((IList<string>)uniqueStrings).Contains(currentString))
                {
                    uniqueStrings[uniqueMembers] = currentString;
                    uniqueMembers++;
                }
            }

            // Increase or decrease the mutation rate depending on the diversity.
            diversity = (float)uniqueMembers / (float)populationSize * 100;

            // Use a 4-Tier approach.
            if (diversity > 70)
            {
                mutationRate = startingMutationRate;
            }
            else if (diversity <= 70 && diversity > 50)
            {
                mutationRate *= (decimal)1.1;
            }
            else if (diversity <= 50 && diversity > 25)
            {
                mutationRate *= (decimal)2;
            }
            else if (diversity <= 25)
            {
                mutationRate *= (decimal)5;
            }
        }

        //===================================================================
        //  Gets and returns the current chromosome type.
        //===================================================================
        public static int GetCurrentType(byte value)
        {
            if (value < 10 && value >= 0)
            {
                return ((int)ChromosomeType.NUMBER);
            }
            else if (value >= 10 && value < 14)
            {
                return ((int)ChromosomeType.OPERATOR);
            }

            return ((int)ChromosomeType.INVALID);
        }
    }
}
