using System.Collections.Generic;

namespace GAME.Utilities
{
    public class WeightedCollection<T>
    {
        private List<T> items;
        private List<float> weights;
        private float totalWeight;

        public void Add(T item, float weight)
        {
            if (items == null)
            {
                items = new List<T>();
                weights = new List<float>();
                weights.Add(0);
            }
            
            items.Add(item);
            totalWeight += weight;
            weights.Add(totalWeight);
        }

        public bool CanGetRandom()
        {
            return items != null && items.Count > 0;
        }

        public T GetRandom()
        {
            float randomValue = UnityEngine.Random.Range(0, totalWeight);
            int index = GetRandomWithBinarySearch(randomValue);
            
            return index >= 0 ? items[index] : items[0]; 
        }

        private int GetRandomWithBinarySearch(float randomValue)
        {
            if(weights == null || weights.Count <= 1) return -1;
            
            int left = 0;
            int right = weights.Count - 2; 

            while (left <= right)
            {
                int mid = left + (right - left) / 2;
                    
                if (randomValue >= weights[mid] && randomValue < weights[mid + 1])  return mid;
            
                if (randomValue < weights[mid]) right = mid - 1;
                else left = mid + 1;
            }

            return -1;
        }
    }
}