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
using QuantConnect.Algorithm.Framework.Alphas;
using QuantConnect.Algorithm.Portfolio;
using QuantConnect.Data.UniverseSelection;
using QuantConnect.Orders;
using QuantConnect.Orders.Fills;
using QuantConnect.Securities;
using QuantConnect.Util;

namespace QuantConnect.Algorithm.Framework.Portfolio
{
    /// <summary>
    /// Provides an implementation of <seealso cref="IPortfolioConstructionModel"/> that works together with the <seealso cref="CompositeAlphaModel"/>.
    /// The user adds multiple alphas via <seealso cref="QCAlgorithm.AddAlpha(IAlphaModel)"/> and this model will select the best alpha model at the
    /// provided times.
    /// </summary>
    public class WalkForwardOptimizationPortfolioConstructionModel : PortfolioConstructionModel
    {
        /// <summary>
        /// Gets the currently elected leader.
        /// </summary>
        public SimulatedPortfolio Leader { get; private set; }

        private DateTime nextElectionTimeUtc;

        // simulated portfolios keyed by alpha model source name
        private readonly Dictionary<string, SimulatedPortfolio> _simulations;

        // factories
        private readonly Func<DateTime, DateTime> _getNextElectionTimeUtc;
        private readonly Func<IAlphaModel, IPortfolioConstructionModel> _portfolioConstructionModelFactory;

        /// <summary>
        /// Initializes a new instance of the <seealso cref="WalkForwardOptimizationPortfolioConstructionModel"/> class
        /// </summary>
        /// <param name="getNextElectionTimeUtc">Function accepting the current algorithm UTC time
        /// and returning the next UTC time when a new leader is to be elected</param>
        /// <param name="portfolioConstructionModelFactory">Function accepting an <seealso cref="IAlphaModel"/>
        /// and returning a new <seealso cref="IPortfolioConstructionModel"/> to model the portfolio of that specific
        /// alpha model</param>
        public WalkForwardOptimizationPortfolioConstructionModel(
            Func<DateTime, DateTime> getNextElectionTimeUtc,
            Func<IAlphaModel, IPortfolioConstructionModel> portfolioConstructionModelFactory
            )
        {
            _getNextElectionTimeUtc = getNextElectionTimeUtc;
            _simulations = new Dictionary<string, SimulatedPortfolio>();
            _portfolioConstructionModelFactory = portfolioConstructionModelFactory;

            Name = "WalkForwardOptimization";
        }


        /// <summary>
        /// Initializes a new instance of the <seealso cref="WalkForwardOptimizationPortfolioConstructionModel"/> class
        /// </summary>
        /// <param name="timeSpan">The re-balancing interval time span</param>
        /// <param name="portfolioConstructionModelFactory">Function accepting an <seealso cref="IAlphaModel"/>
        /// and returning a new <seealso cref="IPortfolioConstructionModel"/> to model the portfolio of that specific
        /// alpha model</param>
        public WalkForwardOptimizationPortfolioConstructionModel(
            TimeSpan timeSpan,
            Func<IAlphaModel, IPortfolioConstructionModel> portfolioConstructionModelFactory = null
        )
            : this(dt => dt.Add(timeSpan), portfolioConstructionModelFactory)
        {
        }


        /// <summary>
        /// Initializes a new instance of the <seealso cref="WalkForwardOptimizationPortfolioConstructionModel"/> class
        /// </summary>
        /// <param name="period">The election period, defaults to one day</param>
        /// <param name="resolution">Resolution defining the step size of <paramref name="period"/></param>
        /// <param name="portfolioConstructionModelFactory">Function accepting an <seealso cref="IAlphaModel"/>
        /// and returning a new <seealso cref="IPortfolioConstructionModel"/> to model the portfolio of that specific
        /// alpha model</param>
        public WalkForwardOptimizationPortfolioConstructionModel(
            int period = 1,
            Resolution resolution = Resolution.Daily,
            Func<IAlphaModel, IPortfolioConstructionModel> portfolioConstructionModelFactory = null
        )
            : this(resolution.ToTimeSpan().Times(period), portfolioConstructionModelFactory)
        {
        }

        /// <summary>
        /// Create portfolio targets from the specified insights
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="insights">The insights to create portfolio targets from</param>
        /// <returns>An enumerable of portfolio targets to be sent to the execution model</returns>
        public override IEnumerable<IPortfolioTarget> CreateTargets(QCAlgorithm algorithm, Insight[] insights)
        {
            // verify we have a simulated portfolio for each source alpha model
            foreach (var insight in insights.DistinctBy(insight => insight.SourceModel))
            {
                SimulatedPortfolio portfolio;
                if (!_simulations.TryGetValue(insight.SourceModel, out portfolio))
                {
                    // resolve the correct IAlphaModel by the insight's source name
                    var alphaModel = algorithm.Alpha.GetByName(insight.SourceModel);

                    // create a new simulated portfolio to track this source model
                    portfolio = CreateSimulatedPortfolio(algorithm, alphaModel);
                    _simulations[insight.SourceModel] = portfolio;
                }
            }

            // push the targets through each of our child PCMs and apply the targets to simulate the portfolio
            foreach (var kvp in _simulations)
            {
                var simulation = kvp.Value;

                // process the incoming insights
                simulation.ProcessInsights(algorithm, insights);
            }

            if (algorithm.UtcTime > nextElectionTimeUtc)
            {
                // the period is likely to be daily yet this method invoked at market open, so ensure we keep it on even intervals
                nextElectionTimeUtc = _getNextElectionTimeUtc(algorithm.UtcTime);

                // perform leader election
                var newLeader = ElectLeader(algorithm, _simulations.Select(kvp => kvp.Value));

                // notify via logs who was picked (even if the same) and note it's objective score
                var objectiveScore = newLeader.GetObjectiveScore(algorithm);
                algorithm.Log($"Elected portfolio model leader: {newLeader.AlphaSourceModel} ({objectiveScore})");
                Leader = newLeader;
            }

            // return the targets from the currently elected leader
            return Leader.GetCurrentTargets();
        }

        /// <summary>
        /// Event fired each time the we add/remove securities from the data feed
        /// </summary>
        /// <param name="algorithm">The algorithm instance that experienced the change in securities</param>
        /// <param name="changes">The security additions and removals from the algorithm</param>
        public override void OnSecuritiesChanged(QCAlgorithm algorithm, SecurityChanges changes)
        {
            foreach (var kvp in _simulations)
            {
                var model = kvp.Value;

                // keep our holdings dictionaries synchronized with the algorithm's selected securities
                NotifiedSecurityChanges.Update(changes,
                    added => model.AddSimulatedHoldings(algorithm, added.Symbol),
                    removed => model.RemoveSimulatedHoldings(algorithm, removed.Symbol)
                );
            }
        }

        /// <summary>
        /// Performs leader election. This default implementation finds the simulated portfolio that has the best
        /// absolute profit/loss metric.
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="portfolios">An enumerable containing all of the simulated portfolios</param>
        /// <returns>The simulated portfolio that has outperformed the others over the period</returns>
        protected virtual SimulatedPortfolio ElectLeader(QCAlgorithm algorithm, IEnumerable<SimulatedPortfolio> portfolios)
        {
            // for now we'll keep it simple and just elect based on the best profit/loss numbers
            return portfolios.OrderByDescending(
                portfolio => portfolio.Holdings.Sum(kvp => kvp.Value.UnrealizedProfit)
            ).First();
        }

        /// <summary>
        /// Creates a new <seealso cref="SimulatedPortfolio"/>. This function is invoked each time we encounter a
        /// new <seealso cref="Insight.SourceModel"/>. This simulated portfolio is used to track the performance of
        /// the alpha model that generated the specified insight and is provided in the method.
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="alphaModel">The alpha model to create the simulated portfolio for</param>
        /// <returns>A new instance of <seealso cref="SimulatedPortfolio"/> that will be used to track/manage</returns>
        protected virtual SimulatedPortfolio CreateSimulatedPortfolio(QCAlgorithm algorithm, IAlphaModel alphaModel)
        {
            // construct a dedicated PCM for this alpha model
            var model = CreatePortfolioConstructionModel(algorithm, alphaModel);

            return new SimulatedPortfolio(alphaModel, model);
        }

        /// <summary>
        /// Creates a new instance of <seealso cref="IPortfolioConstructionModel"/> that will be used to perform
        /// portfolio construction for the specified alpha model
        /// </summary>
        /// <param name="algorithm">The algorithm instance</param>
        /// <param name="alphaModel">The alpha model that this portfolio model will be tracking</param>
        /// <returns>A new portfolio model that will be used to track the specified alpha model</returns>
        protected virtual IPortfolioConstructionModel CreatePortfolioConstructionModel(QCAlgorithm algorithm, IAlphaModel alphaModel)
        {
            return _portfolioConstructionModelFactory(alphaModel);
        }

        /// <summary>
        /// Encapsulates the data and behavior associated with simulating a portfolio powered by an
        /// alpha model and a portfolio construction model
        /// </summary>
        public class SimulatedPortfolio
        {
            private readonly Dictionary<Symbol, SimulatedHoldings> _holdings;

            /// <summary>
            /// Gets the alpha model that is generating insights for this simulated portfolio
            /// </summary>
            public IAlphaModel AlphaModel { get; }
            /// <summary>
            /// Gets the alpha model's source model name
            /// </summary>
            public string AlphaSourceModel => AlphaModel.GetModelName();
            /// <summary>
            /// Gets the portfolio construction model used to create targets from the alpha model's insights
            /// </summary>
            public IPortfolioConstructionModel PortfolioModel { get; }
            /// <summary>
            /// Gets the simulated security holdings managd by this portfolio
            /// </summary>
            public IReadOnlyDictionary<Symbol, SimulatedHoldings> Holdings { get; }

            /// <summary>
            /// Initializes anew instance of the <seealso cref="SimulatedPortfolio"/> class
            /// </summary>
            /// <param name="alphaModel">The alpha model</param>
            /// <param name="portfolioModel">The portfolio construction model</param>
            public SimulatedPortfolio(IAlphaModel alphaModel, IPortfolioConstructionModel portfolioModel)
            {
                AlphaModel = alphaModel;
                PortfolioModel = portfolioModel;
                _holdings = new Dictionary<Symbol, SimulatedHoldings>();
            }

            /// <summary>
            /// Gets the current portfolio targets managed by this simulated portfolio. This is invoked
            /// when this portolio is the leader and is used as the portfolio targets for the algorithm.
            /// </summary>
            /// <returns>The targets generated for this portfolio to be used by the algorithm</returns>
            public IEnumerable<IPortfolioTarget> GetCurrentTargets()
            {
                foreach (var kvp in _holdings)
                {
                    var holdings = kvp.Value;
                    yield return holdings.Target;
                }
            }

            /// <summary>
            /// Computes the current metric score. This score is used to elect a new leader. The highest
            /// score will be chosen as the new leader. This implementation computes the total absolute profit/loss.
            /// </summary>
            /// <param name="algorithm">The algorithm instance</param>
            /// <returns>A scalar objective score used to rank the performance of thi simulated portfolio</returns>
            public virtual decimal GetObjectiveScore(QCAlgorithm algorithm)
            {
                return Holdings.Sum(kvp => kvp.Value.UnrealizedProfit);
            }

            /// <summary>
            /// Process the insights for this model. This includes pushing the insights through to this portfolio's
            /// child PCM as well as producing and applying any simulated fills.
            /// </summary>
            /// <param name="algorithm">The algorithm instance</param>
            /// <param name="insightsForThisModel">The insights to be applied to this portfolio</param>
            public void ProcessInsights(QCAlgorithm algorithm, IEnumerable<Insight> insightsForThisModel)
            {
                foreach (var target in PortfolioModel.CreateTargets(algorithm, insightsForThisModel.ToArray()))
                {
                    SimulatedHoldings holdings;
                    if (!Holdings.TryGetValue(target.Symbol, out holdings))
                    {
                        // ensure our portfolio has a simulated holdings instance for this symbol, this should never
                        // actually happen since we're handling it via OnSecuritiesChanged, but be safe just in case
                        holdings = AddSimulatedHoldings(algorithm, target.Symbol);
                    }

                    // use the security holding for current target storage
                    holdings.Target = target;

                    if (holdings.RequiresFill)
                    {
                        // if we still require more fills to reach our target then perform a simulated fill
                        holdings.SimulateFill(algorithm, AlphaSourceModel);
                    }
                }
            }

            public virtual SimulatedHoldings AddSimulatedHoldings(QCAlgorithm algorithm, Symbol symbol)
            {
                Security security;
                if (!algorithm.Securities.TryGetValue(symbol, out security))
                {
                    throw new InvalidOperationException(
                        $"{nameof(WalkForwardOptimizationPortfolioConstructionModel)} received and insight " +
                        $"for a symbol that does not have a corresponding security in the algorithm: '{symbol.Value}'"
                    );
                }

                // this should never happen since we should have seen this via SecurityChanges
                // log the irregularity and just create the holdings object
                var holdings = new SimulatedHoldings(security, algorithm.Portfolio.CashBook);
                _holdings[symbol] = holdings;
                return holdings;
            }

            public virtual void RemoveSimulatedHoldings(QCAlgorithm algorithm, Symbol symbol)
            {
                _holdings.Remove(symbol);
            }
        }

        public class SimulatedHoldings : SecurityHolding
        {
            // expose protected member
            public new Security Security => base.Security;
            public bool RequiresFill => Quantity != Target.Quantity;
            public Insight CurrentInsight { get; private set; }

            public SimulatedHoldings(Security security, ICurrencyConverter currencyConverter)
                : base(security, currencyConverter)
            {
            }

            public void SetCurrentInsight(Insight insight)
            {
                CurrentInsight = insight;
            }

            public void SimulateFill(QCAlgorithm algorithm, string tag)
            {
                // we'll use market orders for now to keep things simple
                var order = new MarketOrder(
                    Symbol,
                    Target.Quantity,
                    algorithm.UtcTime,
                    tag
                );

                // simulate a fill using the security's settings
                var fill = Security.FillModel.Fill(
                    new FillModelParameters(
                        Security,
                        order,
                        algorithm.SubscriptionManager.SubscriptionDataConfigService,
                        algorithm.Settings.StalePriceTimeSpan
                    )
                ).OrderEvent;

                // apply the fill
                SetHoldings(fill.FillPrice, fill.FillQuantity);
            }
        }
    }
}
