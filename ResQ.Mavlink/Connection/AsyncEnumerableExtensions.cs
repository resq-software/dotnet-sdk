/**
 * Copyright 2026 ResQ
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at
 *
 *     http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
 */

using System.Runtime.CompilerServices;

namespace ResQ.Mavlink.Connection;

/// <summary>
/// Extension methods for <see cref="IAsyncEnumerable{T}"/>.
/// </summary>
public static class AsyncEnumerableExtensions
{
    /// <summary>
    /// Filters an async sequence to only elements assignable to <typeparamref name="TResult"/>.
    /// </summary>
    /// <typeparam name="TResult">The target type to filter to.</typeparam>
    /// <param name="source">The source async enumerable.</param>
    /// <param name="ct">Cancellation token.</param>
    /// <returns>An async sequence containing only items of type <typeparamref name="TResult"/>.</returns>
    public static async IAsyncEnumerable<TResult> OfType<TResult>(
        this IAsyncEnumerable<object> source,
        [EnumeratorCancellation] CancellationToken ct = default)
    {
        await foreach (var item in source.WithCancellation(ct).ConfigureAwait(false))
        {
            if (item is TResult typed)
                yield return typed;
        }
    }
}
