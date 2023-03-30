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
using NUnit.Framework;
using QuantConnect.Indicators;

namespace QuantConnect.Tests.Indicators;

[TestFixture]
public class HilbertTransformIndicatorTests : CommonIndicatorTests<IndicatorDataPoint>
{
    protected override IndicatorBase<IndicatorDataPoint> CreateIndicator()
    {
        return new HilbertTransformIndicator();
    }

    protected override string TestFileName => "spy_with_hilbert.csv";

    protected override string TestColumnName => "Close";

    [Test]
    public void ComparesAgainstExternalDataInPhase()
    {
        var hilbertTransformIndicator = new HilbertTransformIndicator();
        TestHelper.TestIndicator(
            hilbertTransformIndicator,
            TestFileName,
            "InPhase",
            (_, expected) =>
                Assert.AreEqual(expected, (double)hilbertTransformIndicator.InPhase.Current.Value, 1e-3));
    }

    [Test]
    public void ComparesAgainstExternalDataQuadrature()
    {
        var hilbertTransformIndicator = new HilbertTransformIndicator();
        TestHelper.TestIndicator(
            hilbertTransformIndicator,
            TestFileName,
            "Quadrature",
            (actual, expected) =>
                Assert.AreEqual(expected, (double)hilbertTransformIndicator.Quadrature.Current.Value, 1e-3));
    }

    [Test]
    public override void ResetsProperly()
    {
        var hti = new HilbertTransformIndicator(length: 2);
        hti.Update(DateTime.Today, 1m);
        hti.Update(DateTime.Today.AddSeconds(1), 2m);
        Assert.IsFalse(hti.IsReady);

        hti.Reset();
        TestHelper.AssertIndicatorIsInDefaultState(hti);
        TestHelper.AssertIndicatorIsInDefaultState(hti.Quadrature);
        TestHelper.AssertIndicatorIsInDefaultState(hti.InPhase);
    }
}
