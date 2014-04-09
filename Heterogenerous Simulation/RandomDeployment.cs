﻿using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Network_Simulation;

namespace Heterogenerous_Simulation
{
    class RandomDeployment : Deployment
    {
        public RandomDeployment(double percentageOfTunnelingTracer, double percentageOfMarkingTracer, double percentageOfFilteringTracer)
            : base(percentageOfTunnelingTracer, percentageOfMarkingTracer, percentageOfFilteringTracer)
        { }

        protected override void doDeploy(NetworkTopology networkTopology)
        {
            MarkingTracerID = new List<int>();
            FilteringTracerID = new List<int>();

            // Create random array.
            int[] randomArray = DataUtility.RandomArray(networkTopology.Nodes.Count);
            int randomArrayIndex = 0;

            // Randomly select tunneling tracer.
            for (; randomArrayIndex < numberOfTTracer; randomArrayIndex++)
            {
                networkTopology.Nodes[randomArray[randomArrayIndex]].Tracer = NetworkTopology.TracerType.Tunneling;
            }

            // Randomly select marking tracer.
            for (; randomArrayIndex < numberOfTTracer + numberOfMTracer; randomArrayIndex++)
            {
                networkTopology.Nodes[randomArray[randomArrayIndex]].Tracer = NetworkTopology.TracerType.Marking;
                MarkingTracerID.Add(randomArray[randomArrayIndex]);
            }

            // Randomly select filtering tracer.
            for (; randomArrayIndex < numberOfTTracer + numberOfMTracer + numberOfFTracer; randomArrayIndex++)
            {
                networkTopology.Nodes[randomArray[randomArrayIndex]].Tracer = NetworkTopology.TracerType.Filtering;
                FilteringTracerID.Add(randomArray[randomArrayIndex]);
            }
        }

        protected override void write2SQLite(NetworkTopology networkTopology)
        {
            //throw new NotImplementedException();
        }
    }
}
