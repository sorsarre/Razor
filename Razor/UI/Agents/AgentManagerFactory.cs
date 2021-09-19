using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Assistant.Agents;

namespace Assistant.UI.Agents
{
    class AgentManagerFactory
    {
        public IAgentManager CreateAgentManager(Agent agent, AgentControls controls)
        {
            switch (agent)
            {
                case UseOnceAgent a: return new UseOnceAgentManager(a, controls);
                default: return null;
            }
        }
    }
}
