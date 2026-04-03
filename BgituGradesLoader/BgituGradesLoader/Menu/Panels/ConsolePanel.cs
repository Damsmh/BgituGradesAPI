namespace BgituGradesLoader.Menu.Panels
{
    public abstract class ConsolePanel
    {
        public abstract string Title { get; }
        public abstract Task Run();
    }
}
