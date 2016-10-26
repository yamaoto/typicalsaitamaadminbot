namespace TsabConsole.Actions
{
    internal interface ITsabConsoleAction
    {
        string Syntax { get; }
        string Descriptioin { get; }
        string ActionName { get; }
        void Exec(string[] args);
    }
}
