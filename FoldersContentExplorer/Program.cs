namespace FoldersContentExplorer
{
    class Program
    {
        static void Main(string[] args)
        {
            var explorer = new Explorer(args);

            explorer.Explore();
        }
    }
}
