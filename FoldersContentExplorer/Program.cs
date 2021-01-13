namespace FoldersContentExplorer
{
    class Program
    {
        static void Main(string[] args)
        {
            var explorer = new Explorer(new string[] { @"E:\" });

            explorer.Explore();
        }
    }
}
