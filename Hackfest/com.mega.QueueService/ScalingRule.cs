using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace com.mega.QueueService
{
    class ScalingRule
    {
        // TODO : used metric based on average message count per instance ? 

        /// <summary>
        /// The scaler will never decrease instance count lower than this value
        /// </summary>
        public int MinimalInstanceCount { get; set; }

        /// <summary>
        /// The scaler will never increase instance count higher than this value
        /// </summary>
        public int MaximalInstanceCount { get; set; }

        /// <summary>
        /// if nb of message are lower thant this value, a stateless service will be removed.
        /// </summary>
        public long DecreaseThreshold { get; set; }

        /// <summary>
        /// if nb of message are higher thant this value, a stateless service will be addedd .
        /// </summary>
        public long IncreaseThreshold { get; set; }

        /// <summary>
        /// delay (in second) between 2 scaling action (inc or dec)
        /// </summary>
        public int DelayBetweenScaling { get; set; }
    }
}
