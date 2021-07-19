using System.Collections.Generic;

namespace barmud
{
    public class Entity
    {
        private int MaxHealth = 100;
        public int Health = 100;
        public int Money = 0;
        public uint Level = 0;
        public List<int> Inventory = new();
    }
}