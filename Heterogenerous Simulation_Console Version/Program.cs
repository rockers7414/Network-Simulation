﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.IO;
using Network_Simulation;
using Deployment_Simulation;

namespace Heterogenerous_Simulation_Console_Version
{
    class Program
    {
        static void Main(string[] args)
        {
            string filename = args[0];
            int TunnelingTracer = Convert.ToInt32(args[1]);
            int MarkingTracer = Convert.ToInt32(args[2]);
            int FilteringTracer = Convert.ToInt32(args[3]);
            double AttackNodes = Convert.ToDouble(args[4]);
            int VictimNodes = Convert.ToInt32(args[5]);
            int TotalPacket = Convert.ToInt32(args[6]);
            int PercentageOfAttackPacket = Convert.ToInt32(args[7]);
            int AttackPacketPerSecond = Convert.ToInt32(args[8]);
            int NormalPacketPerSecond = Convert.ToInt32(args[9]);
            double ProbabilityOfPacketTunneling = Convert.ToDouble(args[10]);
            double ProbabilityOfPackeMarking = Convert.ToDouble(args[11]);
            double StartFiltering = Convert.ToDouble(args[12]);
            int InitTimeOfAttackPacket = Convert.ToInt32(args[13]);
            bool DynamicProbability = Convert.ToBoolean(args[14]);
            bool ConsiderDistance = Convert.ToBoolean(args[15]);
            double PercentageOfTracer = Convert.ToDouble(args[16]);
            string methodName = args[17];

            string dbName = string.Format("{0}_T{1}M{2}F{3}_A{4}V{5}_Pkt{6}_{7}", Path.GetFileNameWithoutExtension(filename),
                                                                               TunnelingTracer,
                                                                               MarkingTracer,
                                                                               FilteringTracer,
                                                                               AttackNodes,
                                                                               VictimNodes,
                                                                               TotalPacket,
                                                                               PercentageOfAttackPacket);


            filename = Path.Combine(Environment.CurrentDirectory, "maps", filename);

            SQLiteUtility sql = new SQLiteUtility(ref dbName);
            NetworkTopology networkTopology = new NetworkTopology(AttackNodes, VictimNodes);
            networkTopology.ReadBriteFile(filename);

            NoneDeployment noneDeploy = new NoneDeployment(0, 0, 0);
            if (sql.CreateTable(noneDeploy.ToString()))
            {
                noneDeploy.Deploy(networkTopology);
                Simulator noneSimulator = new Simulator(noneDeploy, networkTopology, sql, "None");
                noneSimulator.Run(AttackPacketPerSecond, NormalPacketPerSecond, TotalPacket, PercentageOfAttackPacket, ProbabilityOfPacketTunneling, ProbabilityOfPackeMarking, StartFiltering, InitTimeOfAttackPacket, DynamicProbability, ConsiderDistance);
            }
            else 
            {
                // Load Attackers and Victim.
                sql.LoadAttackersAndVictim(ref networkTopology);
            }

            switch (methodName) 
            {
                case "RandomDeployment":
                    RandomDeployment randomDeploy = new RandomDeployment(TunnelingTracer * PercentageOfTracer / 100, MarkingTracer * PercentageOfTracer / 100, FilteringTracer * PercentageOfTracer / 100);
                    if (sql.CreateTable(randomDeploy.ToString()))
                    {
                        randomDeploy.Deploy(networkTopology);
                        Simulator randomSimulator = new Simulator(randomDeploy, networkTopology, sql, "Random");
                        randomSimulator.Run(AttackPacketPerSecond, NormalPacketPerSecond, TotalPacket, PercentageOfAttackPacket, ProbabilityOfPacketTunneling, ProbabilityOfPackeMarking, StartFiltering, InitTimeOfAttackPacket, DynamicProbability, ConsiderDistance);
                    }
                    break;

                case "KCutDeployment":
                    KCutDeployment kcutDeploy = new KCutDeployment(TunnelingTracer * PercentageOfTracer / 100, MarkingTracer * PercentageOfTracer / 100, FilteringTracer * PercentageOfTracer / 100, typeof(KCutStartWithSideNodeConsiderCoefficient));
                    if (sql.CreateTable("KCutDeployV1")) 
                    {
                        kcutDeploy.Deploy(networkTopology);
                        Simulator kcutSimulator = new Simulator(kcutDeploy.Deployment, networkTopology, sql, "V1");
                        kcutSimulator.Run(AttackPacketPerSecond, NormalPacketPerSecond, TotalPacket, PercentageOfAttackPacket, ProbabilityOfPacketTunneling, ProbabilityOfPackeMarking, StartFiltering, InitTimeOfAttackPacket, DynamicProbability, ConsiderDistance);
                    }
                    break;

                case "KCutDeployment2":
                    KCutDeploymentV2 kcut2Deploy = new KCutDeploymentV2(TunnelingTracer * PercentageOfTracer / 100, MarkingTracer * PercentageOfTracer / 100, FilteringTracer * PercentageOfTracer / 100, typeof(KCutStartWithSideNodeConsiderCoefficient));
                    if (sql.CreateTable("KCutDeployV2"))
                    {
                        kcut2Deploy.Deploy(networkTopology);
                        Simulator kcut2Simulator = new Simulator(kcut2Deploy.Deployment, networkTopology, sql, "V2");
                        kcut2Simulator.Run(AttackPacketPerSecond, NormalPacketPerSecond, TotalPacket, PercentageOfAttackPacket, ProbabilityOfPacketTunneling, ProbabilityOfPackeMarking, StartFiltering, InitTimeOfAttackPacket, DynamicProbability, ConsiderDistance);
                    }
                    break;

                case "OPTRandomDeployment":
                    // TODO: Optimal Random
                    break;
                case "OPTKCutDeployment":
                    // TODO: Optimal KCutV1
                    break;
                case "OPTKCutDeploymentV2":
                    // TODO: Optimal KCutV2
                    break;
            }
        }
    }
}
