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
using System.IO;
using System.Linq;
using NUnit.Framework;
using QuantConnect.Archives;

namespace QuantConnect.Tests.Compression.Archives
{
    [TestFixture]
    public class ArchiveTests
    {
        private const string EntryContents = "entry-message";
        private const string EntryContents2 = "entry-message2";
        private const string UpdatedEntryContents = "updated-entry-message";
        private const string ArchivePath = "./ArchiveTests.zip";
        private const string ArchiveEntryKey = "ArchiveTestsEntryKey.txt";
        private const string ArchiveEntryKey2 = "ArchiveTestsEntryKey2.txt";

        [SetUp]
        public void Initialize()
        {
            File.Delete(ArchivePath);
        }

        [TearDown]
        public void CleanUp()
        {
            File.Delete(ArchivePath);
        }

        [Test]
        [TestCase(ArchiveImplementation.DotNetFramework)]
        [TestCase(ArchiveImplementation.Ionic)]
        [TestCase(ArchiveImplementation.SharpZipLib)]
        public void SavesArchiveOnDispose(ArchiveImplementation impl)
        {
            Archive.OpenWrite(ArchivePath, impl).Dispose();
            Assert.IsTrue(File.Exists(ArchivePath));

            // confirm we can open archive and that it is empty
            using (var archive = Archive.OpenReadOnly(ArchivePath, impl))
            {
                Assert.AreEqual(0, archive.GetEntries().Count);
            }
        }

        [Test]
        [TestCase(ArchiveImplementation.DotNetFramework)]
        [TestCase(ArchiveImplementation.Ionic)]
        [TestCase(ArchiveImplementation.SharpZipLib)]
        public void CreatesAndReadsArchiveWithEntries(ArchiveImplementation impl)
        {
            using (var archive = Archive.OpenWrite(ArchivePath, impl))
            {
                var entry = archive.GetEntry(ArchiveEntryKey);
                entry.WriteString(EntryContents);

                var entry2 = archive.GetEntry(ArchiveEntryKey2);
                entry2.WriteString(EntryContents2);
            }

            using (var archive = Archive.OpenReadOnly(ArchivePath, impl))
            {
                // confirm count and name of entries
                var entries = archive.GetEntries();
                Assert.AreEqual(2, entries.Count);
                Assert.AreEqual(1, entries.Count(e => e.Key == ArchiveEntryKey));
                Assert.AreEqual(1, entries.Count(e => e.Key == ArchiveEntryKey2));

                // confirm entry contents
                var entry = archive.GetEntry(ArchiveEntryKey);
                var contents = entry.ReadAsString();
                Assert.AreEqual(EntryContents, contents);

                var entry2 = archive.GetEntry(ArchiveEntryKey2);
                var contents2 = entry2.ReadAsString();
                Assert.AreEqual(EntryContents2, contents2);
            }
        }

        [Test]
        [TestCase(ArchiveImplementation.DotNetFramework)]
        [TestCase(ArchiveImplementation.Ionic)]
        [TestCase(ArchiveImplementation.SharpZipLib)]
        public void UpdatesArchiveEntry(ArchiveImplementation impl)
        {
            using (var archive = Archive.OpenWrite(ArchivePath, impl))
            {
                var entry = archive.GetEntry(ArchiveEntryKey);
                entry.WriteString(EntryContents);
            }

            using (var archive = Archive.OpenWrite(ArchivePath, impl))
            {
                var entry = archive.GetEntry(ArchiveEntryKey);
                entry.WriteString(UpdatedEntryContents);
            }

            using (var archive = Archive.OpenReadOnly(ArchivePath, impl))
            {
                // confirm count and name of entries
                var entries = archive.GetEntries();
                Assert.AreEqual(1, entries.Count);
                Assert.AreEqual(ArchiveEntryKey, entries.First().Key);

                // confirm entry contents
                var entry = archive.GetEntry(ArchiveEntryKey);
                var contents = entry.ReadAsString();
                Assert.AreEqual(UpdatedEntryContents, contents);
            }
        }
    }
}
