// Copyright (c) 2026 Jonathan Talcott
// 
// This file is part of EasyConnections.
// 
// EasyConnections is free software: you can redistribute it and/or modify
// it under the terms of the GNU General Public License as published by
// the Free Software Foundation, either version 3 of the License, or
// (at your option) any later version.
// 
// EasyConnections is distributed in the hope that it will be useful,
// but WITHOUT ANY WARRANTY; without even the implied warranty of
// MERCHANTABILITY or FITNESS FOR A PARTICULAR PURPOSE.  See the
// GNU General Public License for more details.
// 
// You should have received a copy of the GNU General Public License
// along with EasyConnections.  If not, see <https://www.gnu.org/licenses/>.

using System;
using System.Collections.Concurrent;
using System.Threading;
using System.Threading.Tasks;

namespace EasyConnections.Utility;

public class AsyncQueue<T> : IDisposable
{
	private readonly ConcurrentQueue<T> backing;
	private readonly Func<T, Task> elementHandler;

	private readonly SemaphoreSlim semaphore;
	private readonly CancellationTokenSource cancellationSource;
	private readonly Task processQueueTask;

	private volatile bool disposed = false;

	/// <param name="elementHandler">Fires for each element in the list sequentially, on a shared thread</param>
	public AsyncQueue(Func<T, Task> elementHandler)
	{
		backing = new ConcurrentQueue<T>();
		this.elementHandler = elementHandler;

		semaphore = new SemaphoreSlim(0);
		cancellationSource = new CancellationTokenSource();
		processQueueTask = Task.Run(ProcessQueueTask);
	}

	public void Add(T item)
	{
		if (disposed)
		{
			throw new ObjectDisposedException(nameof(AsyncQueue<T>));
		}

		backing.Enqueue(item);

		semaphore.Release();
	}

	public void Dispose()
	{
		if (disposed)
		{
			return;
		}

		// Flag as disposed
		disposed = true;

		// Clear
		backing.Clear();

		// Cancel
		cancellationSource.Cancel();

		// Wait for task to complete
		processQueueTask.Wait();

		// Dispose members
		semaphore.Dispose();
		cancellationSource.Dispose();
		processQueueTask.Dispose();

		GC.SuppressFinalize(this);
	}

	// private

	private async Task ProcessQueueTask()
	{
		CancellationToken cancellationToken = cancellationSource.Token;

		// While not cancelled
		while (!cancellationSource.IsCancellationRequested)
		{
			// Wait for semaphore or cancellation
			await semaphore.WaitAsync(cancellationToken);

			// Get element from queue
			if (backing.TryDequeue(out T? element))
			{
				// Do action
				try
				{
					await elementHandler(element);
				}
				catch (Exception)
				{
					// Do nothing
				}
			}
		}
	}
}
