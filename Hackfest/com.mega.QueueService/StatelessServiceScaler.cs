using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace com.mega.QueueService
{

    /// <summary>
    ///  THis class implement a basinc scaling feature.
    /// </summary>
    class StatelessServiceScaler
    {
        ScalingRule rule;
        string[] serviceNameToScale;

        Task scaleRunnerTask = null;
        CancellationTokenSource cancelSource = null;

        public StatelessServiceScaler(CancellationToken cancellationToken,ScalingRule rule,params string[] servicesNames)
        {
            this.rule = rule;
            this.serviceNameToScale = servicesNames;

            this.lastScaleDate = DateTime.MinValue;
            cancelSource = new CancellationTokenSource();
        }

        public void Start()
        {
            if (scaleRunnerTask != null)
                throw new InvalidOperationException("Already running");
            
            scaleRunnerTask = new Task(ScaleRunner,cancelSource.Token);
            scaleRunnerTask.Start(); 
        }


        public void Stop()
        {
            if (scaleRunnerTask==null)
                return;
            cancelSource.Cancel();
            scaleRunnerTask = null;
        }

        /// <summary>
        /// Last date when an scaling action occur.
        /// </summary>
        DateTime lastScaleDate;

        long lastMetricValue;

        /// <summary>
        /// Infor the scaler that a new metric value is available
        /// </summary>
        /// <param name="newMetric"></param>
        public void UpdateMetric(long newMetric)
        {
            // if last scale action is sooner that the delay -> do nothing
            if ((DateTime.Now - lastScaleDate).TotalSeconds < rule.DelayBetweenScaling)
                return;

            lastMetricValue = newMetric;
        }

        void ScaleRunner()
        {
            while(true)
            {
                var metric = lastMetricValue;

                // TODO : code rule logic implementation
                Task.Delay(1000);
            }
        }
        

        void RunScalingActionIncrease(string serviceName ,int nbInstance)
        {
            // TODO : implementation code for adding a new service instance
        }

        void RunScalingActionDecrease(string serviceName, int nbInstance)
        {
            // TODO : implementation code for removing a existing service instance
        }


    }
}
