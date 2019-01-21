using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SharedLibrary
{
    public class SharedVariables
    {
        public static log4net.ILog logs = log4net.LogManager.GetLogger(System.Reflection.MethodBase.GetCurrentMethod().DeclaringType);

        static Aggregators.AggregatorMCI v_aggregatorMCI;
        public static Aggregators.AggregatorMCI prp_aggregatorMCI
        {
            get
            {
                if (v_aggregatorMCI == null)
                    v_aggregatorMCI = new Aggregators.AggregatorMCI();
                return v_aggregatorMCI;
            }
        }

        static Aggregators.AggregatorMobinOne v_aggregatorMobinOne;
        public static Aggregators.AggregatorMobinOne prp_aggregatorMobinOne
        {
            get
            {
                if (v_aggregatorMobinOne == null)
                    v_aggregatorMobinOne = new Aggregators.AggregatorMobinOne();
                return v_aggregatorMobinOne;
            }
        }

        static Aggregators.AggregatorMobinOneMapfa v_aggregatorMobinOneMapfa;
        public static Aggregators.AggregatorMobinOneMapfa prp_aggregatorMobinOneMapfa
        {
            get
            {
                if (v_aggregatorMobinOneMapfa == null)
                    v_aggregatorMobinOneMapfa = new Aggregators.AggregatorMobinOneMapfa();
                return v_aggregatorMobinOneMapfa;
            }
        }

        static Aggregators.AggregatorMTN v_aggregatorMTN;
        public static Aggregators.AggregatorMTN prp_aggregatorMTN
        {
            get
            {
                if (v_aggregatorMTN == null)
                    v_aggregatorMTN = new Aggregators.AggregatorMTN();
                return v_aggregatorMTN;
            }
        }

        static Aggregators.AggregatorTelepromo v_aggregatorTelepromo;
        public static Aggregators.AggregatorTelepromo prp_aggregatorTelepromo
        {
            get
            {
                if (v_aggregatorTelepromo == null)
                    v_aggregatorTelepromo = new Aggregators.AggregatorTelepromo();
                return v_aggregatorTelepromo;
            }
        }

        static Aggregators.AggregatorTelepromoMapfa v_aggregatorTelepromoMapfa;
        public static Aggregators.AggregatorTelepromoMapfa prp_aggregatorTelepromoMapfa
        {
            get
            {
                if (v_aggregatorTelepromoMapfa == null)
                    v_aggregatorTelepromoMapfa = new Aggregators.AggregatorTelepromoMapfa();
                return v_aggregatorTelepromoMapfa;
            }
        }

        public static Aggregators.Aggregator fnc_getAggregator(string aggregatorName)
        {
            Aggregators.Aggregator agg;
            string aggregatorNameLower = aggregatorName.ToLower();
            if (aggregatorNameLower == "MciDirect".ToLower())
            {
                agg = SharedVariables.prp_aggregatorMCI;
            }
            else if (aggregatorNameLower == "MobinOne".ToLower())
            {
                agg = SharedVariables.prp_aggregatorMobinOne;
            }
            else if (aggregatorNameLower == "MobinOneMapfa".ToLower())
            {
                agg = SharedVariables.prp_aggregatorMobinOneMapfa;
            }
            else if (aggregatorNameLower == "MTN".ToLower())
            {
                agg = SharedVariables.prp_aggregatorMTN;
            }
            else if (aggregatorNameLower == "Telepromo".ToLower())
            {
                agg = SharedVariables.prp_aggregatorTelepromo;
            }
            else if (aggregatorNameLower == "TelepromoMapfa".ToLower())
            {
                agg = SharedVariables.prp_aggregatorTelepromoMapfa;
            }
            else
            {
                //jhoobin,PardisImi,PardisPlatform,SamssonTci
                throw new Exception("No implementation for " + aggregatorName);
            }
            return agg;
        }
    }
}
