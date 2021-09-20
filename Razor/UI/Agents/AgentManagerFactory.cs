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
                case SellAgent a: return new SellAgentManager(a, controls);
                case SearchExemptionAgent a: return new SearchExemptionAgentManager(a, controls);
                case ScavengerAgent a: return new ScavengerAgentManager(a, controls);
                case RestockAgent a: return new RestockAgentManager(a, controls);
                case OrganizerAgent a: return new OrganizerAgentManager(a, controls);
                case IgnoreAgent a: return new IgnoreAgentManager(a, controls);
                case BuyAgent a: return new BuyAgentManager(a, controls);
                default: return null;
            }
        }
    }
}
