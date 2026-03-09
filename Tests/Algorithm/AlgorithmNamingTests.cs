/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2014 QuantConnect Corporation.
 *
 * Licensed under the Apache License, Version 2.0 (the "License");
 * you may not use this file except in compliance with the License.
 * You may obtain a copy of the License at http://www.apache.org/licenses/LICENSE-2.0
 *
 * Unless required by applicable law or agreed to in writing, software
 * distributed under the License is distributed on an "AS IS" BASIS,
 * WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
 * See the License for the specific language governing permissions and
 * limitations under the License.
*/

using System;
using System.Collections.Generic;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Algorithm;
using QuantConnect.Util;

namespace QuantConnect.Tests.Algorithm
{
    [TestFixture, Parallelizable(ParallelScope.All)]
    public class AlgorithmNamingTest
    {
        #region Naming tests

        private static void SetAlgorithmNameUsingPropertySetter(QCAlgorithm algorithm, string name)
        {
            algorithm.Name = name;
        }

        private static void SetAlgorithmNameUsingSetMethod(QCAlgorithm algorithm, string name)
        {
            algorithm.SetName(name);
        }

        private static void TestSettingAlgorithmName(Action<QCAlgorithm, string> setName)
        {
            var algorithm = new QCAlgorithm();
            var name = "TestName";
            setName(algorithm, name);
            Assert.AreEqual(name, algorithm.Name);
        }

        [Test]
        public void AlgorithmNameIsSetUsingPropertySetter()
        {
            TestSettingAlgorithmName(SetAlgorithmNameUsingPropertySetter);
        }

        [Test]
        public void AlgorithmNameIsSetUsingSetMethod()
        {
            TestSettingAlgorithmName(SetAlgorithmNameUsingSetMethod);
        }

        private static void TestSettingLongAlgorithmName(Action<QCAlgorithm, string> setName)
        {
            var algorithm = new QCAlgorithm();
            var name = new string('a', MaxNameAndTagsLength + 1);
            setName(algorithm, name);
            Assert.AreEqual(name.Substring(0, MaxNameAndTagsLength), algorithm.Name);
        }

        [Test]
        public void AlgorithmNameTruncatedUsingPropertySetter()
        {
            TestSettingLongAlgorithmName(SetAlgorithmNameUsingPropertySetter);
        }

        [Test]
        public void AlgorithmNameTruncatedUsingSetMethod()
        {
            TestSettingLongAlgorithmName(SetAlgorithmNameUsingSetMethod);
        }

        private static void TestSettingAlgorithmNameAfterInitialization(Action<QCAlgorithm, string> setName)
        {
            var algorithm = new QCAlgorithm();
            algorithm.SetLocked();
            Assert.Throws<InvalidOperationException>(() => setName(algorithm, "TestName"));
        }

        [Test]
        public void AlgorithmNameCannotBeSetAfterInitializationUsingPropertySetter()
        {
            TestSettingAlgorithmNameAfterInitialization(SetAlgorithmNameUsingPropertySetter);
        }

        [Test]
        public void AlgorithmNameCannotBeSetAfterInitializationUsingSetMethod()
        {
            TestSettingAlgorithmNameAfterInitialization(SetAlgorithmNameUsingSetMethod);
        }

        #endregion

        #region Tagging tests

        private static void SetAlgorithmTagsUsingPropertySetter(QCAlgorithm algorithm, string[] tags)
        {
            algorithm.Tags = tags?.ToHashSet();
        }

        private static void SetAlgorithmTagsUsingSetMethod(QCAlgorithm algorithm, string[] tags)
        {
            algorithm.SetTags(tags?.ToHashSet());
        }

        private static void TestAlgorithmTagsAreSet(Action<QCAlgorithm, string[]> setTags, QCAlgorithm algorithm, string[] tags)
        {
            setTags(algorithm, tags);
            CollectionAssert.AreEquivalent(tags, algorithm.Tags);
        }

        private static void TestSettingAlgorithmTags(Action<QCAlgorithm, string[]> setTags)
        {
            var algorithm = new QCAlgorithm();
            var tags = new[] { "tag1", "tag2" };
            TestAlgorithmTagsAreSet(setTags, algorithm, tags);
        }

        [Test]
        public void AlgorithmTagsAreSetUsingPropertySetter()
        {
            TestSettingAlgorithmTags(SetAlgorithmTagsUsingPropertySetter);
        }

        [Test]
        public void AlgorithmTagsAreSetUsingSetMethod()
        {
            TestSettingAlgorithmTags(SetAlgorithmTagsUsingSetMethod);
        }

        private static void TestOverridingAlgorithmTags(Action<QCAlgorithm, string[]> setTags)
        {
            var algorithm = new QCAlgorithm();

            var tags = new[] { "tag1", "tag2" };
            TestAlgorithmTagsAreSet(setTags, algorithm, tags);

            var newTags = new[] { "tag3", "tag4" };
            TestAlgorithmTagsAreSet(setTags, algorithm, newTags);
        }

        [Test]
        public void AlgorithmTagsCanBeOverriddenUsingPropertySetter()
        {
            TestOverridingAlgorithmTags(SetAlgorithmTagsUsingPropertySetter);
        }

        [Test]
        public void AlgorithmTagsCanBeOverriddenUsingSetMethod()
        {
            TestOverridingAlgorithmTags(SetAlgorithmTagsUsingSetMethod);
        }

        public static void TestSettingNullAlgorithmTags(Action<QCAlgorithm, string[]> setTags)
        {
            var algorithm = new QCAlgorithm();
            setTags(algorithm, null);
            CollectionAssert.IsEmpty(algorithm.Tags);

            var tags = new[] { "tag1", "tag2" };
            setTags(algorithm, tags);
            setTags(algorithm, null);
            CollectionAssert.AreEquivalent(tags, algorithm.Tags);
        }

        [Test]
        public void AlgorithmTagsAreLeftUntouchedWhenTryingToSetNullUsingPropertySetter()
        {
            TestSettingNullAlgorithmTags(SetAlgorithmTagsUsingPropertySetter);
        }

        [Test]
        public void AlgorithmTagsAreLeftUntouchedWhenTryingToSetNullUsingSetMethod()
        {
            TestSettingNullAlgorithmTags(SetAlgorithmTagsUsingSetMethod);
        }

        private static void TestSettingTooManyAlgorithmTags(Action<QCAlgorithm, string[]> setTags)
        {
            var algorithm = new QCAlgorithm();
            var tags = Enumerable.Range(0, MaxTagsCount + 1).Select(i => $"tag{i}").ToArray();
            setTags(algorithm, tags);
            CollectionAssert.AreEquivalent(tags.ToHashSet().Take(MaxTagsCount), algorithm.Tags);
        }

        [Test]
        public void AlgorithmTagsCollectionIsTruncatedWhenSettingTooManyUsingPropertySetter()
        {
            TestSettingTooManyAlgorithmTags(SetAlgorithmTagsUsingPropertySetter);
        }

        [Test]
        public void AlgorithmTagsCollectionIsTruncatedWhenSettingTooManyUsingSetMethod()
        {
            TestSettingTooManyAlgorithmTags(SetAlgorithmTagsUsingSetMethod);
        }

        private static void TestSettingTagsAreTruncatedWhenTooLong(Action<QCAlgorithm, string[]> setTags)
        {
            var algorithm = new QCAlgorithm();
            var tags = Enumerable.Range(0, MaxTagsCount)
                .Select(i => "tag" + string.Concat(Enumerable.Repeat(i, MaxNameAndTagsLength + 1)))
                .ToArray();
            setTags(algorithm, tags);
            CollectionAssert.AreEquivalent(tags.ToHashSet(t => t.Substring(0, MaxNameAndTagsLength)), algorithm.Tags);
        }

        [Test]
        public void AlgorithmTagsAreTruncatedWhenTooLongUsingPropertySetter()
        {
            TestSettingTagsAreTruncatedWhenTooLong(SetAlgorithmTagsUsingPropertySetter);
        }

        [Test]
        public void AlgorithmTagsAreTruncatedWhenTooLongUsingSetMethod()
        {
            TestSettingTagsAreTruncatedWhenTooLong(SetAlgorithmTagsUsingSetMethod);
        }

        public static void TestSettingEmptyTagsAreIgnored(Action<QCAlgorithm, string[]> setTags)
        {
            var algorithm = new QCAlgorithm();
            var tags = new[] { "tag1", "", "tag2", null, "tag3", " " };
            setTags(algorithm, tags);

            var expectedTags = new[] { "tag1", "tag2", "tag3" };
            CollectionAssert.AreEquivalent(expectedTags, algorithm.Tags);
        }

        [Test]
        public void AlgorithmSetContainingEmptyTagsAreIgnoredUsingPropertySetter()
        {
            TestSettingEmptyTagsAreIgnored(SetAlgorithmTagsUsingPropertySetter);
        }

        [Test]
        public void AlgorithmSetContainingEmptyTagsAreIgnoredUsingSetMethod()
        {
            TestSettingEmptyTagsAreIgnored(SetAlgorithmTagsUsingSetMethod);
        }

        [Test]
        public void AlgorithmTagsAreAddedOneByOne()
        {
            var algorithm = new QCAlgorithm();
            var tags = new List<string>();
            for (var i = 0; i < MaxTagsCount; i++)
            {
                tags.Add($"tag{i}");
                algorithm.AddTag(tags.Last());
                CollectionAssert.AreEquivalent(tags, algorithm.Tags);
            }
        }

        [Test]
        public void DuplicatedTagsAreIgnored()
        {
            var algorithm = new QCAlgorithm();

            var tag = "tag";
            algorithm.AddTag(tag);
            Assert.AreEqual(1, algorithm.Tags.Count);
            Assert.AreEqual(tag, algorithm.Tags.Single());

            algorithm.AddTag(tag);
            Assert.AreEqual(1, algorithm.Tags.Count);
            Assert.AreEqual(tag, algorithm.Tags.Single());
        }

        [TestCase("")]
        [TestCase(" ")]
        [TestCase(null)]
        public void EmptyTagsAreIgnored(string emptyTag)
        {
            var algorithm = new QCAlgorithm();

            algorithm.AddTag(emptyTag);
            CollectionAssert.IsEmpty(algorithm.Tags);
        }

        [Test]
        public void LongTagsAreTruncatedWhenAddedOneByOne()
        {
            var algorithm = new QCAlgorithm();
            var tag = new string('a', MaxNameAndTagsLength + 1);
            algorithm.AddTag(tag);
            Assert.AreEqual(1, algorithm.Tags.Count);
            Assert.AreEqual(tag.Substring(0, MaxNameAndTagsLength), algorithm.Tags.Single());
        }

        [Test]
        public void TagsAreIgnoredWhenAddedOneByOneIfCollectionIsFull()
        {
            var algorithm = new QCAlgorithm();
            var tags = Enumerable.Range(0, MaxTagsCount).Select(i => $"tag{i}").ToArray();
            foreach (var tag in tags)
            {
                algorithm.AddTag(tag);
            }
            CollectionAssert.AreEquivalent(tags, algorithm.Tags);

            // This will not be added
            algorithm.AddTag("LastTag");
            CollectionAssert.AreEquivalent(tags, algorithm.Tags);
        }

        #endregion

        private class TestQCAlgorithm : QCAlgorithm
        {
            public static int PublicMaxNameAndTagsLength => MaxNameAndTagsLength;

            public static int PublicMaxTagsCount => MaxTagsCount;
        }

        private static int MaxNameAndTagsLength => TestQCAlgorithm.PublicMaxNameAndTagsLength;
        private static int MaxTagsCount => TestQCAlgorithm.PublicMaxTagsCount;
    }
}
