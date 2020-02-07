using DeckSearch.Search;
using System;

namespace DeckSearch
{
    class Program
    {
        static void Main(string[] args)
        {
            var search = new DistributedSearch(args[0]);
            search.Run();
        }
    }
}
