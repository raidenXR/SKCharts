using System;
using System.Collections;
using System.Collections.Generic;

namespace SKCharts;


/// <summary>
/// Not the most appropriate name... It locks the access to during enumeration to
/// ensure that no Exception is thrown for modofying the list from a different thread
///</summary>
public class ThreadedList<T> : IList<T>
{
	int count;
	T[] items = new T[20];

	readonly object threadlock = new object();

	public ref struct Enumerator
	{
		int index = -1;
		T current;
		ThreadedList<T> list;

		public Enumerator(ThreadedList<T> list)
		{
			this.list = list;
		}

		public T Current => current;

		public bool MoveNext()
		{
			lock(list.threadlock)
			{
				current = list.items[++index];

				return index < list.count;
			}
		}
	}

	public Enumerator GetEnumerator() => new Enumerator(this);

	public void Add(T item)
	{
		if(count >= items.Length) throw new IndexOutOfRangeException();
		items[count] = item;
		count += 1;
	}

	public void Insert(int index, T item)
	{
		if(index >= items.Length || index < 0) throw new IndexOutOfRangeException();
		items[index] = item;
	}
	
	public bool Remove(T item)
	{
		for(int i = 0; i < count; i++)
		{
			if(items[i]!.Equals(item))
			{
				if(i < count - 1) 
				{
					for(int j = i; j < count - 1; j++)
					{
						items[j] = items[j + 1];
					}					
					items[count - 1] = default;
					count -= 1;
				}
				return true;
			}
		}
		return false;
	}

	public void RemoveAt(int index)
	{
		if(index >= count || index < 0) throw new IndexOutOfRangeException();
		
		if(index < count - 1)
		{
			for(int i = index; i < count - 1; i++)
			{
				items[i] = items[i + 1];
			}
		}
		items[count - 1] = default;
		count -= 1;
	}

	public int IndexOf(T item)
	{
		for(int i = 0; i < count; i++)
		{
			if(items[i]!.Equals(item))
			{
				return i;
			}
		}
		return -1;
	}

	public void Clear()
	{
		count = 0;
	}

	public bool Contains(T item)
	{
		for(int i = 0; i < count; i++)
		{
			if(items[i]!.Equals(item))
			{
				return true;
			}
		}
		return false;
	}

	public bool IsReadOnly => false;

	public int Count => count; 

	public T this[int index]
	{
		get => items[index];
		set => items[index] = value;
	}


	public void CopyTo(T[] items, int index)
	{
		if(index >= items.Length) throw new IndexOutOfRangeException();
		
		for(int i = index; i < this.items.Length; i++)
		{
			this.items[i] = items[i];
		}
	}

	IEnumerator<T> IEnumerable<T>.GetEnumerator() => throw new NotImplementedException();

	IEnumerator IEnumerable.GetEnumerator() => throw new NotImplementedException();
}
