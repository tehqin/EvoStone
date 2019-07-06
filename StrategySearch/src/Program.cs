using StrategySearch.Search;

namespace StrategySearch
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
