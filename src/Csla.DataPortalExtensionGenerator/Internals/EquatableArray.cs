﻿using System.Collections;
using System.Collections.Immutable;

namespace Ossendorf.Csla.DataPortalExtensionGenerator.Internals;

/// <summary>
/// An immutable, equatable array. This is equivalent to Array{T} but with value equality support.
/// </summary>
/// <typeparam name="T">The type of values in the array.</typeparam>
/// <remarks>Taken from: https://github.com/andrewlock/StronglyTypedId/blob/master/src/StronglyTypedIds/EquatableArray.cs</remarks>
internal readonly struct EquatableArray<T> : IEquatable<EquatableArray<T>>, IEnumerable<T>
    where T : IEquatable<T> {
    public static readonly EquatableArray<T> Empty = new(Array.Empty<T>());

    /// <summary>
    /// The underlying <typeparamref name="T"/> array.
    /// </summary>
    private readonly T[] _array;

    public T this[int i] => _array[i];

    /// <summary>
    /// Creates a new <see cref="EquatableArray{T}"/> instance.
    /// </summary>
    /// <param name="array">The input <see cref="ImmutableArray"/> to wrap.</param>
    public EquatableArray(T[] array) {
        _array = array;
    }

    /// <sinheritdoc/>
    public bool Equals(EquatableArray<T> array) => AsSpan().SequenceEqual(array.AsSpan());

    /// <sinheritdoc/>
    public override bool Equals(object? obj) => obj is EquatableArray<T> array && Equals(array);

    /// <sinheritdoc/>
    public override int GetHashCode() {
        if (_array is not T[] array) {
            return 0;
        }

        HashCode hashCode = default;

        foreach (var item in array) {
            hashCode.Add(item);
        }

        return hashCode.ToHashCode();
    }

    /// <summary>
    /// Returns a <see cref="ReadOnlySpan{T}"/> wrapping the current items.
    /// </summary>
    /// <returns>A <see cref="ReadOnlySpan{T}"/> wrapping the current items.</returns>
    public ReadOnlySpan<T> AsSpan() => _array.AsSpan();

    /// <summary>
    /// Gets the underlying array if there is one
    /// </summary>
    public T[]? GetArray() => _array;

    /// <sinheritdoc/>
    IEnumerator<T> IEnumerable<T>.GetEnumerator() => ((IEnumerable<T>)(_array ?? Array.Empty<T>())).GetEnumerator();

    /// <sinheritdoc/>
    IEnumerator IEnumerable.GetEnumerator() => ((IEnumerable<T>)(_array ?? Array.Empty<T>())).GetEnumerator();

    public int Count => _array?.Length ?? 0;

    /// <summary>
    /// Checks whether two <see cref="EquatableArray{T}"/> values are the same.
    /// </summary>
    /// <param name="left">The first <see cref="EquatableArray{T}"/> value.</param>
    /// <param name="right">The second <see cref="EquatableArray{T}"/> value.</param>
    /// <returns>Whether <paramref name="left"/> and <paramref name="right"/> are equal.</returns>
    public static bool operator ==(EquatableArray<T> left, EquatableArray<T> right) => left.Equals(right);

    /// <summary>
    /// Checks whether two <see cref="EquatableArray{T}"/> values are not the same.
    /// </summary>
    /// <param name="left">The first <see cref="EquatableArray{T}"/> value.</param>
    /// <param name="right">The second <see cref="EquatableArray{T}"/> value.</param>
    /// <returns>Whether <paramref name="left"/> and <paramref name="right"/> are not equal.</returns>
    public static bool operator !=(EquatableArray<T> left, EquatableArray<T> right) => !left.Equals(right);
}
