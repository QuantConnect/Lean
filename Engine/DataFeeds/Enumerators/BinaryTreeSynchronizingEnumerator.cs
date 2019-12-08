using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using QuantConnect.Data;
using QuantConnect.Util;

namespace QuantConnect.Lean.Engine.DataFeeds.Enumerators
{
    /// <summary>
    /// Synchronizes enumerators by constructing a binary tree. The created tree is largely internalized and
    /// consumers only need to manage the root. Further, the tree is mutable and permits adding and removing
    /// enumerators. Adding and removing enumerators is an O(log(n)) operation, as is the synchronization.
    /// The adding/removing is not as efficient as other methods, but the O(log(n)) sync operation is far better
    /// than existing o(n) operations and becomes increasingly more performant as the number of enumerators increases.
    /// </summary>
    public class BinaryTreeSynchronizingEnumerator : IEnumerator<BaseData>, ITree<IEnumerator<BaseData>>
    {
        /// <summary>
        /// Gets the value contained at this tree node
        /// </summary>
        public IEnumerator<BaseData> Value => this;

        /// <summary>
        /// Gets an enumerable of child nodes
        /// </summary>
        public IEnumerable<ITree<IEnumerator<BaseData>>> Children
        {
            get
            {
                if (_leftEnumerator.Initialized)
                {
                    yield return _leftEnumerator.AsTree();
                }

                if (_rightEnumerator.Initialized)
                {
                    yield return _rightEnumerator.AsTree();
                }
            }
        }

        /// <summary>Gets the element in the collection at the current position of the enumerator.</summary>
        /// <returns>The element in the collection at the current position of the enumerator.</returns>
        public BaseData Current { get; private set; }
        object IEnumerator.Current => Current;

        private Enumerator _leftEnumerator;
        private Enumerator _rightEnumerator;

        public BinaryTreeSynchronizingEnumerator()
            : this(null)
        {
        }

        public BinaryTreeSynchronizingEnumerator(IEnumerator<BaseData> left)
            : this(left, null)
        {
        }

        public BinaryTreeSynchronizingEnumerator(
            IEnumerator<BaseData> left,
            IEnumerator<BaseData> right
            )
        {
            _leftEnumerator = new Enumerator(left);
            _rightEnumerator = new Enumerator(right);
        }

        /// <summary>
        /// Attempts to add the enumerator to the tree WITHOUT modifying the tree structure.
        /// This differs from add, which will generate new root nodes to accomdate the enumerator
        /// in the tree's structure.
        /// </summary>
        public bool TryAdd(IEnumerator<BaseData> enumerator)
        {
            // by convention, keep the tree left heavy
            return _leftEnumerator.TryAddEnumerator(enumerator)
                || _rightEnumerator.TryAddEnumerator(enumerator);
        }

        public BinaryTreeSynchronizingEnumerator Add(IEnumerator<BaseData> enumerator)
        {
            if (!_leftEnumerator.Initialized)
            {
                _leftEnumerator.SetEnumerator(enumerator);
                return this;
            }

            if (!_rightEnumerator.Initialized)
            {
                _rightEnumerator.SetEnumerator(enumerator);
                return this;
            }

            // construct the binary tree
            var child = new BinaryTreeSynchronizingEnumerator(enumerator);
            var parent = new BinaryTreeSynchronizingEnumerator(this, child);
            return parent;
        }

        /// <summary>Advances the enumerator to the next element of the collection.</summary>
        /// <returns>true if the enumerator was successfully advanced to the next element; false if the enumerator has passed the end of the collection.</returns>
        /// <exception cref="T:System.InvalidOperationException">The collection was modified after the enumerator was created. </exception>
        /// <filterpriority>2</filterpriority>
        public bool MoveNext()
        {
            BaseData left, right;
            if (!_leftEnumerator.TryMoveNext(out left))
            {
                if (!_rightEnumerator.TryMoveNext(out right))
                {
                    return false;
                }

                _rightEnumerator._needsMoveNext = true;
                Current = right;
                return true;
            }

            if (!_rightEnumerator.TryMoveNext(out right))
            {
                _leftEnumerator._needsMoveNext = true;
                Current = left;
                return true;
            }

            if (left.EndTime <= right.EndTime)
            {
                Current = left;
                _leftEnumerator._needsMoveNext = true;
            }
            else
            {
                Current = right;
                _rightEnumerator._needsMoveNext = true;
            }

            return true;
        }

        public void Reset()
        {
            throw new NotSupportedException("The reset operation is not currently supported.");
        }

        public void Dispose()
        {
            throw new NotImplementedException();
        }

        private class Enumerator : IDisposable, ITree<IEnumerator<BaseData>>
        {
            public bool Initialized => _enumerator != null;
            public IEnumerator<BaseData> Value => _enumerator;
            public IEnumerable<ITree<IEnumerator<BaseData>>> Children
            {
                get
                {
                    var binary = _enumerator as BinaryTreeSynchronizingEnumerator;
                    if (binary != null)
                    {
                        // if the child is binary then yield directly as it implements ITree<T>
                        // otherwise, create a new child-less tree node for the yield
                        if (binary._leftEnumerator.Initialized)
                        {
                            yield return binary._leftEnumerator.AsTree();
                        }
                        if (binary._rightEnumerator.Initialized)
                        {
                            yield return binary._rightEnumerator.AsTree();
                        }
                    }
                }
            }

            internal bool _needsMoveNext;
            private IEnumerator<BaseData> _enumerator;

            public Enumerator(IEnumerator<BaseData> enumerator)
            {
                _needsMoveNext = true;
                _enumerator = enumerator;
            }

            public bool TryMoveNext(out BaseData current)
            {
                if (_enumerator == null)
                {
                    current = null;
                    return false;
                }

                if (!_needsMoveNext)
                {
                    current = _enumerator.Current;
                    return true;
                }

                if (!_enumerator.MoveNext())
                {
                    current = null;
                    return false;
                }

                _needsMoveNext = false;
                current = _enumerator.Current;
                return true;
            }

            /// <summary>
            /// Attempts to add the enumerator to this enumerator's children IF this enumerator is a binary tree synchronizer
            /// </summary>
            public bool TryAddEnumerator(IEnumerator<BaseData> enumerator)
            {
                // attempts to
                var binary = _enumerator as BinaryTreeSynchronizingEnumerator;
                if (binary != null)
                {
                    if (binary._leftEnumerator.TryAddEnumerator(enumerator))
                    {
                        return true;
                    }

                    if (binary._rightEnumerator.TryAddEnumerator(enumerator))
                    {
                        return true;
                    }
                }

                return false;
            }

            public void SetEnumerator(IEnumerator<BaseData> enumerator)
            {
                if (_enumerator != null)
                {
                    throw new InvalidOperationException("Enumerator has already been initialized.");
                }

                _needsMoveNext = true;
                _enumerator = enumerator;
            }

            public void Dispose()
            {
                _enumerator?.Dispose();
            }

            public ITree<IEnumerator<BaseData>> AsTree()
            {
                var self = _enumerator;
                var binary = self as BinaryTreeSynchronizingEnumerator;
                return binary ?? Tree.Create(self, v => Enumerable.Empty<IEnumerator<BaseData>>());
            }
        }
    }
}
