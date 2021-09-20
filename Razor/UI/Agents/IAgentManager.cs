namespace Assistant.UI.Agents
{
    interface IAgentManager
    {
        void OnSelected();
        void OnButtonPress(int num);

        void Detach();
    }
}
