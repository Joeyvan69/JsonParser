using System;
using System.Collections.Generic;

namespace DesignPattern
{
    public class ObjectPool<T> where T : class
    {
        private Func<T> _createFunc;
        private Stack<T> _pool;
        public int PoolSize => _pool.Count;

        public int CreatedCount { get; private set; }
        public ObjectPool(Func<T> createFunc, int initialSize)
        {
            _createFunc = createFunc;
            _pool = new Stack<T>(initialSize);
            for (int i = 0; i < initialSize; i++)
            {
                _pool.Push(CreateNew());
                CreatedCount++;
            }
        }
        private T CreateNew()
        {
            CreatedCount++;
            return _createFunc();
        }
        public T Get()
        {
            if (_pool.Count > 0)
            {
                return _pool.Pop();
            }
            return CreateNew();
        }
        public void Release(T obj)
        {
            _pool.Push(obj);
        }
    }
}

