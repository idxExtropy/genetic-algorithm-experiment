using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Data;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Windows.Forms;

namespace intellegere
{
    public partial class FormMain : Form
    {
        // Static class variables.
        public static bool isRunning = false;
        public static double bestFitness = 0.0;
        public static long generation = 0;

        public FormMain()
        {
            InitializeComponent();

            tabControl.SelectedIndex = 0;

            richTextBoxDescription.Text = Environment.NewLine + Environment.NewLine +
                "Imagine an isolated environment home to a population of organisms whose chromosomes consist of simple 4-bit genes.  These genes manifest in mathematical concepts such as numbers (0-9) and operations (+,-,*,/)." +
                Environment.NewLine + Environment.NewLine + "Now imagine that the environment is best suited to organisms who reach a specific target value.  Would evolution naturally find a way to produce an organism perfectly suited for the environment?  How many generations would it take?  How large would the population have to be?  How long must the chromosomes be and how often must they mutate?" +
                Environment.NewLine + Environment.NewLine + "This software utilizes genetic algorithms designed to answer these question and more.  Once a target value and environment is setup, simply click 'evolve a solution' and watch nature unfold in the output tab." +
                Environment.NewLine + Environment.NewLine + Environment.NewLine + "\tThe Author," +
                Environment.NewLine + "\tThomas Calloway";
        }
        
        //==========================================================================
        // Begin the process of evolving a solution for the requested value.
        //==========================================================================
        private void buttonEvolve_Click(object sender, EventArgs e)
        {
            DateTime evolutionStartTime = DateTime.Now;

            generation = 1;
            int iCurrentBest = 0;
            float bestYet = 0.0f;

            isRunning = true;
            
            richTextBoxOutput.Clear();
            buttonEvolve.Enabled = false;
            progressBarEvolving.Style = ProgressBarStyle.Marquee;
            progressBarEvolving.MarqueeAnimationSpeed = 100;
            toolStripStatusLabelStatus.Text = "Evolving a solution... (Gen 0)";

            // Create starting population of requested size.
            GeneticAlgorithm.InitializePopulation((int)numericUpDownPopulationSize.Value, 
                (int)numericUpDownChromosomeLength.Value, numericUpDownTarget.Value, numericUpDownMutationRate.Value,
                numericUpDownCrossoverRate.Value);

            while (isRunning)
            {
                // Assign fitness to population.
                GeneticAlgorithm.AssignFitness(out iCurrentBest);

                // Update the mutation rate based on lack of diversity.
                GeneticAlgorithm.UpdateMutationRate();

                if (GeneticAlgorithm.fitness[iCurrentBest] > bestYet)
                {
                    bestYet = GeneticAlgorithm.fitness[iCurrentBest];
                    WriteBestFit(iCurrentBest);
                }

                if (bestYet == float.MaxValue || !isRunning)
                {
                    // Stop evolving.
                    break;
                }

                // Perform genetic crossover.
                GeneticAlgorithm.GeneticCrossover();

                // Perform some additional 'Mutation'. 
                GeneticAlgorithm.MutatePopulation();
                
                toolStripStatusLabelStatus.Text = "Evolving a solution... (Gen " + generation + ")";

                // Service the UI for a brief while.
                spinUI(3);

                generation++;
            }

            WriteSummary(evolutionStartTime);

            buttonEvolve.Enabled = true;
            progressBarEvolving.Style = ProgressBarStyle.Continuous;
            progressBarEvolving.Value = 0;
            toolStripStatusLabelStatus.Text = String.Empty;
            spinUI(100);
        }

        //==========================================================================
        // Write the current populations best fit to the UI.
        //==========================================================================
        private void WriteBestFit(int iBestMember)
        {
            richTextBoxOutput.AppendText(Environment.NewLine);

            // Show the generation of the current best suited.
            richTextBoxOutput.AppendText("Generation: " + generation + Environment.NewLine);
            
            // Show the individual ID.
            richTextBoxOutput.AppendText("Individual: " + iBestMember + Environment.NewLine);

            // Show the current population diversity.
            richTextBoxOutput.AppendText("Population Diversity: " + GeneticAlgorithm.diversity.ToString("N2") + "%" + Environment.NewLine);

            // Show the encode chromosomes.
            richTextBoxOutput.AppendText("Encoded Chromosomes: " + Environment.NewLine + "[ ");
            for (int i = 0; i < numericUpDownChromosomeLength.Value/2; i++)
            {
                richTextBoxOutput.AppendText( Convert.ToString(GeneticAlgorithm.population[iBestMember,i],2));
            }
            richTextBoxOutput.AppendText(" ]" + Environment.NewLine);
            
            // Show the decoded chromosomes and solution.
            richTextBoxOutput.AppendText("Decoded Chromosomes: " + Environment.NewLine + "[ ");
            richTextBoxOutput.AppendText("  " + GeneticAlgorithm.realization[iBestMember]);
            richTextBoxOutput.AppendText(" ]" + Environment.NewLine);

            richTextBoxOutput.Select(richTextBoxOutput.Text.Length, 0);
            richTextBoxOutput.ScrollToCaret();
        }

        //==========================================================================
        // Service the user interface for the requested number of milliseconds.
        //==========================================================================
        private void spinUI(int millisecondSpin)
        {
            DateTime startTime = DateTime.Now;
            TimeSpan duration = DateTime.Now - startTime;

            while (duration.TotalMilliseconds < millisecondSpin)
            {
                Application.DoEvents();
                duration = DateTime.Now - startTime;
            }
        }

        //==========================================================================
        // Stop the evolution process.
        //==========================================================================
        private void buttonHalt_Click(object sender, EventArgs e)
        {
            isRunning = false;
        }

        //==========================================================================
        // Write the evolution summary.
        //==========================================================================
        private void WriteSummary(DateTime evolutionStartTime)
        {
            TimeSpan evolutionTime = DateTime.Now - evolutionStartTime;
            
            richTextBoxOutput.AppendText(Environment.NewLine);
            richTextBoxOutput.AppendText(":: EVOLUTION COMPLETE ::" + Environment.NewLine + Environment.NewLine);
            richTextBoxOutput.AppendText("Total Duration: ");
            richTextBoxOutput.AppendText(evolutionTime.Days + " days, ");
            richTextBoxOutput.AppendText(evolutionTime.Hours + " hours, ");
            richTextBoxOutput.AppendText(evolutionTime.Minutes + " minutes, ");
            richTextBoxOutput.AppendText(evolutionTime.Seconds + " seconds, ");
            richTextBoxOutput.AppendText(evolutionTime.Milliseconds + " milliseconds.");
            richTextBoxOutput.Select(richTextBoxOutput.Text.Length, 0);
            richTextBoxOutput.ScrollToCaret();
        }
    }
}
