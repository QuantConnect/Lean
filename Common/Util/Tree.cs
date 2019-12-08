using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace QuantConnect.Util
{
    /// <summary>
    /// Provides a bare-bones implementation of <seealso cref="ITree{T}"/>
    /// </summary>
    public class Tree<T> : ITree<T>
    {
        /// <summary>
        /// Gets the value contained at this tree node
        /// </summary>
        public T Value { get; }

        /// <summary>
        /// Gets an enumerable of child nodes
        /// </summary>
        public IEnumerable<ITree<T>> Children { get; }

        /// <summary>
        /// Initializes a new instance of the <seealso cref="Tree{T}"/> class
        /// </summary>
        /// <param name="value">The value at this tree node</param>
        /// <param name="children">Any child nodes</param>
        public Tree(T value, IEnumerable<ITree<T>> children)
        {
            Value = value;
            Children = children.ToList();
        }
    }

    /// <summary>
    /// Provides methods for creating and traversing tree structures that implement the <seealso cref="ITree{T}"/> interface
    /// </summary>
    public static class Tree
    {
        /// <summary>
        /// Creates a new tree structure by recursively invoking the provided <paramref name="children"/> factory function
        /// </summary>
        /// <typeparam name="T">The tree's node value type</typeparam>
        /// <param name="value">The tree's root node value</param>
        /// <param name="children">A function accepting the value at a node and returning the child values</param>
        /// <returns>A new tree structure adhering to the specified parameters</returns>
        public static ITree<T> Create<T>(T value, Func<T, IEnumerable<T>> children)
        {
            return new Tree<T>(value, children(value).Select(c => Create(c, children)));
        }

        /// <summary>
        /// Performs a projection of the provided tree. The resulting tree will have the same exact topology, but each
        /// node's value will have been projected according to the specified <paramref name="projection"/> function
        /// </summary>
        /// <typeparam name="T">The input tree's node value type</typeparam>
        /// <typeparam name="TResult">The output tree's node value type</typeparam>
        /// <param name="tree">The tree to be projected</param>
        /// <param name="projection">The projection to perform on each node's value</param>
        /// <returns>A tree with the same topology as the input tree, but with each node's value projected
        /// according to the provided function</returns>
        public static ITree<TResult> Select<T, TResult>(this ITree<T> tree, Func<T, TResult> projection)
        {
            return new Tree<TResult>(projection(tree.Value), tree.Children.Select(c => Select(c, projection)));
        }

        /// <summary>
        /// Lazily yield returns all nodes in the tree, including this one, starting at the leaves
        /// and ending at the root
        /// </summary>
        public static IEnumerable<T> SelfAndDescendentsDepthFirst<T>(this ITree<T> node)
        {
            foreach (var child in node.Children)
            {
                foreach (var subChild in SelfAndDescendentsDepthFirst(child))
                {
                    yield return subChild;
                }
            }

            yield return node.Value;
        }

        /// <summary>
        /// Lazily yield returns all nodes in the tree, including this one, starting at the root
        /// and ending at the leaves
        /// </summary>
        public static IEnumerable<T> SelfAndDescendents<T>(this ITree<T> node)
        {
            yield return node.Value;

            foreach (var child in node.Children)
            {
                foreach (var subChild in SelfAndDescendents(child))
                {
                    yield return subChild;
                }
            }
        }

        /// <summary>
        /// Lazily yield returns all nodes in the tree, including except this one,
        /// start at the rooting and ending at the leaves
        /// </summary>
        public static IEnumerable<T> Descendents<T>(this ITree<T> node)
        {
            foreach (var child in node.Children)
            {
                foreach (var descendent in child.SelfAndDescendents())
                {
                    yield return descendent;
                }
            }
        }

        /// <summary>
        /// Lazily yield returns all nodes in the tree, including this one, starting at the leaves
        /// and ending at the root
        /// </summary>
        public static IEnumerable<ITree<T>> SelfAndDescendentsDepthFirstNodes<T>(this ITree<T> node)
        {
            foreach (var child in node.Children)
            {
                foreach (var subChild in SelfAndDescendentsDepthFirstNodes(child))
                {
                    yield return subChild;
                }
            }
            yield return node;
        }

        /// <summary>
        /// Lazily yield returns all nodes in the tree, including this one, start at the root
        /// and ending at the leaves
        /// </summary>
        public static IEnumerable<ITree<T>> SelfAndDescendentsNodes<T>(this ITree<T> node)
        {
            yield return node;
            foreach (var child in node.Children)
            {
                foreach (var subChild in SelfAndDescendentsNodes(child))
                {
                    yield return subChild;
                }
            }
        }

        /// <summary>
        /// Lazily yield returns all nodes in the tree, excluding this one, start at the root
        /// and ending at the leaves
        /// </summary>
        public static IEnumerable<ITree<T>> DescendentsNodes<T>(this ITree<T> node)
        {
            foreach (var child in node.Children)
            {
                foreach (var descendent in child.SelfAndDescendentsNodes())
                {
                    yield return descendent;
                }
            }
        }

        /// <summary>
        /// Writes the provided tree structure to the <paramref name="writer"/>
        /// </summary>
        public static void WriteTo<T>(this ITree<T> tree, TextWriter writer, Func<ITree<T>, string> toString = null)
        {
            tree.WriteTo(writer, "", true, false, toString);
        }

        /// <summary>
        /// Recursively writes the specified tree.
        /// </summary>
        private static void WriteTo<T>(
            this ITree<T> tree,
            TextWriter writer,
            string indent,
            bool first = false,
            bool last = false,
            Func<ITree<T>, string> toString = null
            )
        {
            toString = toString ?? (item => item?.ToString());

            writer.Write(indent);
            if (first)
            {
                writer.Write("┌─");
            }
            else if (last)
            {
                writer.Write("└─");
                indent += "  ";
            }
            else
            {
                writer.Write("├─");
                indent += "| ";
            }
            writer.WriteLine(toString(tree));

            var children = tree.Children.ToList();
            for (var i = 0; i < children.Count; i++)
            {
                children[i].WriteTo(writer, indent, false, i == children.Count - 1, toString);
            }
        }
    }
}
