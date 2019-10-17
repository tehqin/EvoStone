using System;

namespace StrategySearch.Mapping.Sizers
{
   // Linearly resize the map based on a range of sizes
   class LinearMapSizer : MapSizer
   {
      private int _minSize;
      private int _range;

      public LinearMapSizer(int minSize, int maxSize)
      {
         _minSize = minSize;
         _range = maxSize-minSize;
      }

      public int GetSize(double portionDone)
      {
         int size = (int)((portionDone+1e-9) * _range) + _minSize;
         return Math.Min(size, _minSize+_range);
      }
   }
}
