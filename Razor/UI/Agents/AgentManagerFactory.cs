﻿using Assistant.Agents;

namespace Assistant.UI.Agents
{
    class AgentManagerFactory
    {
        public IAgentManager CreateAgentManager(Agent agent, AgentControls controls)
        {
            switch (agent)
            {
                case UseOnceAgent a: return new UseOnceAgentManager(a, controls);
                case SellAgent a: return new SellAgentManager(a, controls);
                case SearchExemptionAgent a: return new SearchExemptionAgentManager(a, controls);
                case ScavengerAgent a: return new ScavengerAgentManager(a, controls);
                case RestockAgent a: return new RestockAgentManager(a, controls);
                default: return null;
            }
        }
    }
}
