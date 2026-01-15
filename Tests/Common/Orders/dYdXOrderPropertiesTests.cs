/*
 * QUANTCONNECT.COM - Democratizing Finance, Empowering Individuals.
 * Lean Algorithmic Trading Engine v2.0. Copyright 2023 QuantConnect Corporation.
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

using NUnit.Framework;
using QuantConnect.Orders;

namespace QuantConnect.Tests.Common.Orders;

[TestFixture]
public class dYdXOrderPropertiesTests
{
    [Test]
    public void PostOnlyPropertyCanBeSetAndRetrieved()
    {
        var properties = new dYdXOrderProperties();

        properties.PostOnly = true;
        Assert.IsTrue(properties.PostOnly);

        properties.PostOnly = false;
        Assert.IsFalse(properties.PostOnly);
    }

    [Test]
    public void IOCPropertyCanBeSetAndRetrieved()
    {
        var properties = new dYdXOrderProperties();

        properties.IOC = true;
        Assert.IsTrue(properties.IOC);

        properties.IOC = false;
        Assert.IsFalse(properties.IOC);
    }

    [Test]
    public void DefaultValuesAreCorrect()
    {
        var properties = new dYdXOrderProperties();

        Assert.IsFalse(properties.PostOnly);
        Assert.IsFalse(properties.IOC);
    }

    [Test]
    public void ThrowsIfSetIOCWhenPostOnlyAlreadySet()
    {
        var properties = new dYdXOrderProperties();

        Assert.Throws<System.InvalidOperationException>(() =>
        {
            properties.PostOnly = true;
            properties.IOC = true;
        });
    }

    [Test]
    public void ThrowsIfSetPostOnlyWhenIOCAlreadySet()
    {
        var properties = new dYdXOrderProperties();

        Assert.Throws<System.InvalidOperationException>(() =>
        {
            properties.IOC = true;
            properties.PostOnly = true;
        });
    }

    [Test]
    public void WhenPostOnlyIsTrueIOCRemainsFalse()
    {
        var properties = new dYdXOrderProperties();

        properties.PostOnly = true;
        Assert.IsTrue(properties.PostOnly);
        Assert.IsFalse(properties.IOC);
    }

    [Test]
    public void WhenIOCIsTruePostOnlyRemainsFalse()
    {
        var properties = new dYdXOrderProperties();

        properties.IOC = true;
        Assert.IsTrue(properties.IOC);
        Assert.IsFalse(properties.PostOnly);
    }
}
