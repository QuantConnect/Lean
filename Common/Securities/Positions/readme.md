# Position Groups

## Motivation

The motivation behind the position groups feature is to enable algorithms to submit an order for a logical grouping of securities in a single action. In some cases, the margin required for the group is far less than the sum of the margin required for each piece individually. This happens when the grouping provides some sort of hedge, thereby reducing the overal risk of the position, and in some cases, can be a completely market neutral position. A simple example is a covered call strategy, which nominally consists of 100 shares of the underlying equity and short 1 option contract. Since the short contract position is _covered_ by the account holding the underlying, brokerages reduce the margin requirements. The end goal is to enable LEAN to not only accurately model such groupings, but also submit a multi-leg order so the brokerage can process it as a single order. There are some cases where submitting the legs individually is not possible due to margin requirements, but grouping them together allows the order to be successfully processed.

## Design

The position groups feature introduces some new abstractions and key concepts that will be covered in this section.

### IPosition

A position defines _some_ quantity of a security. It may be all of the security's holdings or only a fraction of the holdings. Each position defines a few key properties listed below:

``` csharp
// See: https://github.com/QuantConnect/Lean/blob/refactor-4065-position-groups/Common/Securities/Positions/IPosition.cs

/// <summary>
/// The symbol
/// </summary>
Symbol Symbol { get; }

/// <summary>
/// The quantity
/// </summary>
decimal Quantity { get; }

/// <summary>
/// The unit quantity. The unit quantities of a group define the group. For example, a covered
/// call has 100 units of stock and -1 units of call contracts.
/// </summary>
decimal UnitQuantity { get; }

```

The `Symbol` property is everything you expect it to be, uniquely identifying which security this position is in and the `Quantity` is likewise uninteresting, denoting the directional (position for long negative for short) quantity of the position. The `UnitQuantity` defines the smallest allowable quantity increment according to the definition of the group the position belongs to. For the default group, `SecurityPositionGroup`, the `UnitQuantity` is equal to the security's lot size (`SymbolProperties.LotSize`). An equity position in a group with option contracts will have a `UnitQuantity` equal to the contract's multiplier (`SymbolProperties.ContractMultiplier`). There's an important relationship between the `Quantity` and the `UnitQuantity` which is that `Quantity/UnitQuantity` **must** always yield a whole number and denotes the number of lots. Using the covered call example from earlier, we may have 5 covered calls. Each contract will have a `UnitQuantity` equal to -1 and the underlying equity will have a `UnitQuantity` normally equal to 100, so 5 covered calls yields -5 contracts and 500 shares in the underlying.

### PositionGroupKey

Before diving into the `IPositionGroup` abstraction, it's important to briefly mention the `PositionGroupKey`. This class uniquely defines a position group within the algorithm and is a deterministic identifier constructed from the contained positions' `Symbol` and `UnitQuantity` properties coupled with an `IPositionGroupBuyingPowerModel`. We'll discuss modelling in a later section, but for now it's enough to understand that if two position group contain the same exact position **but** are modeled differently, then LEAN will treat them as different positions. The `UnitQuantities` list is, under the covers, an `ImmutableSortedSet` which guarantees determinism. In other words, `-1 GOOG CALL; +100 GOOG` is the same as `+100 GOOG; -1 GOOG CALL`. The `PositionGroupKey` can be used to index into collection types containing position groups as well as into the `PositionManager` (to be discussed later). This class also offers a variety of convenience functions for creating empty and unit positions and groups, which is pretty cool as it implies that the `PositionGroupKey` contains sufficient information to create an entire `IPositionGroup`. It's essentially a template for a particular group type with an exact set of symbol. More on group _types_ later.

``` csharp

// See: https://github.com/QuantConnect/Lean/blob/refactor-4065-position-groups/Common/Securities/Positions/PositionGroupKey.cs

/// <summary>
/// Gets whether or not this key defines a default group
/// </summary>
public bool IsDefaultGroup { get; }

/// <summary>
/// Gets the <see cref="IPositionGroupBuyingPowerModel"/> being used by the group
/// </summary>
public IPositionGroupBuyingPowerModel BuyingPowerModel { get; }

/// <summary>
/// Gets the unit quantities defining the ratio between position quantities in the group
/// </summary>
public IReadOnlyList<Tuple<Symbol, decimal>> UnitQuantities { get; }

```


### IPositionGroup

A position group is unsurprisingly a grouping of `IPosition` instances. More importantly though, a position group contains the definition of the group, which includes the ratios between the `UnitQuantity` of its constituent positions and the `IPositionGroupBuyingPowerModel`. These definitional pieces are all contained within the `PositionGroupKey` discussed in the previous section. `IPositionGroup` implements the `IReadOnlyCollection<IPosition>` interface, which allows it to be used as an `IEnumerable<IPosition>`. The `Key` property exposes the deterministic identifier and the `Quantity` property exposes how many _units_ of the group there are. Recalling the earlier discussion in the `IPosition` section, where we showed that `Quantity/UnitQuantity` yields the number of lots; the number of lots is exactly equal to, by definition, the position group's quantity. Further, **every** position within the group **must** have the same exact number of lots, and if not, then something has gone terribly wrong! Position groups have definitions that define the ratios between the positions. We keep using the covered call example, but they can be far more complicated. Due to its simplicity, we'll continue with the covered call example, and more specifically, consider a covered call position group with a `Quantity` equalt to 5. This means that the ratio of the option contract position's `Quantity/UnitQuantity` equals 5 **and** the ratio of the equity position's `Quantity/UnitQuantity` equal 5. As mentioned earlier, modeling is an important part of the position group's definition, and as such, `IPositionGroup` directly exposes its own model via the `BuyingPowerModel` property, and in this way, `IPositionGroup` is analogous to a `Security` object, in that it's the smallest unit of modelling and trading within LEAN with respect to the position group subsystems. There are some extension methods provided that we'll touch on later and only one method is exposed directly by the interface: `TryGetPosition`, which is intended to behave identically to `IDictionary<K, V>.TryGetValue`, returning `true` and a valid `position` instance _or_ `false` and `default(IPosition)` when the group doesn't contain a position with the provided symbol.

``` csharp
// See: https://github.com/QuantConnect/Lean/blob/refactor-4065-position-groups/Common/Securities/Positions/IPositionGroup.cs

/// <summary>
/// Gets the key identifying this group
/// </summary>
PositionGroupKey Key { get; }

/// <summary>
/// Gets the whole number of units in this position group
/// </summary>
decimal Quantity { get; }

/// <summary>
/// Gets the positions in this group
/// </summary>
IEnumerable<IPosition> Positions { get; }

/// <summary>
/// Gets the buying power model defining how margin works in this group
/// </summary>
IPositionGroupBuyingPowerModel BuyingPowerModel { get; }

/// <summary>
/// Attempts to retrieve the position with the specified symbol
/// </summary>
/// <param name="symbol">The symbol</param>
/// <param name="position">The position, if found</param>
/// <returns>True if the position was found, otherwise false</returns>
bool TryGetPosition(Symbol symbol, out IPosition position);

```

### IPositionGroupResolver

The position group resolver is responsible for inspecting an algorithm's security holdings and creating a set of groups that minimizes the margin requirement of the entire portfolio. Some brokerages do this automatically for you, such as IB. The default resolver used to match ungrouped security holdings into the default `SecurityPositionGroup` is the `SecurityPositionGroupResolver`. Each _type_ of group (default/options/futures) will have its own resolver. The options resolver (not yet implemented), will integrate the `OptionStrategyMatcher`. The `OptionStrategyMatcher` looks at a set of security holdings with the same underlying, for example, GOOG and all GOOG option contracts, and attempts to arrange these holdings into groups to minimize the total margin required. When adding futures we'll need to add a `FutureStrategyMatcher`. The matchers work by looking at a set of definitions that define the specific ways in which securities can be grouped, such as covered call, but also more complex groupings such as the `Straddle` and `Strangle`. You can see the complete set of option strategy definitions in the `OptionStrategyDefinitions` class. The `OptionStrategyMatcher` loads all of these definitions and matches them to the algorithm's holdings. Once integrated into the yet-to-be-implemented `OptionStrategyPositionGroupResolver`, the results of the match operation will need to be projected into position group instances. Every time the algorithm's holdings change we need to evaluate at least a subset of the holdings to look for unexpected broken groups and determine whether or not breaking the group pushes the margin requirement up too high. It's entirely possible that the algorithm might **not** be able to sell that single share of GOOG from our earlier example because doing so breaks the covered call group and has the potential to increase the margin requirement beyond the maximum allowed.

In addition to performing matching functions, it also serves as a descriptor for how groups are constructed and therefore, the impacts of breaking a particular group. Since running all resolvers on the entire portfolio is an expensive operation, when holdings change its beneficial to only consider groups impacted by the change. Since each _type_ of group has different rules regarding how the positions relate to one another, the resolver exposes the `GetImpactedGroups` function. The first argument is usually the entire set of groups being maintained by the `PositionManager` and the second argument represents the changes being contemplated. I say contemplated because this is part of the 'what if' analysis that is done _before_ LEAN validates an order as being executable/submittable. We ask the resolver which groups are impacted by the requested change, for example -1 GOOG. In this example, the response would include all position groups that the equity GOOG is a member of in addition to all position groups that contain a GOOG option contract. We can then apply our changed positions to this reduced set of position groups and resolve the new position groups. We can then calculate the margin requirements on this new set of position groups and verify that its within bounds. If so, the order may proceed, if not, the order is flagged as insufficient buying power.

This idea of needing to run 'what if' analysis might take a minute before you're convinced that it's absolutely required, but once you understand how dynamic groups are and likewise how easily they can be broken and particularly how much margin is _saved_ through grouping (sometimes over 75%), it quickly becomes clear that breaking a group can have severe implications on the available margin in the algorithm's portfolio. Despite much effort and ample trying, we were unable to come up with a more performant mechanism and at this point are extremely confident that running 'what if' analysis is actually the only way to confidently and consistently get the correct answer everytime. This is what led to the introduction of the `GetImpactGroups`, which saves a lot of time in algorithms with hundeds of security holdings and potentially hundreds of non-default groups. Consider being long 100 securities and writing a covered call on each. With `GetImpactedGroups`, if you sell one of the underlying shares, we'll only evaluate groups related to that equity and ignore the other 99 equity/option related groups.

``` csharp
// See: https://github.com/QuantConnect/Lean/blob/refactor-4065-position-groups/Common/Securities/Positions/IPositionGroupResolver.cs

/// <summary>
/// Attempts to group the specified positions into a new <see cref="IPositionGroup"/> using an
/// appropriate <see cref="IPositionGroupBuyingPowerModel"/> for position groups created via this
/// resolver.
/// </summary>
/// <param name="positions">The positions to be grouped</param>
/// <param name="group">The grouped positions when this resolver is able to, otherwise null</param>
/// <returns>True if this resolver can group the specified positions, otherwise false</returns>
bool TryGroup(IReadOnlyCollection<IPosition> positions, out IPositionGroup group);

/// <summary>
/// Resolves the position groups that exist within the specified collection of positions.
/// </summary>
/// <param name="positions">The collection of positions</param>
/// <returns>An enumerable of position groups</returns>
PositionGroupCollection Resolve(PositionCollection positions);

/// <summary>
/// Determines the position groups that would be evaluated for grouping of the specified
/// positions were passed into the <see cref="Resolve"/> method.
/// </summary>
/// <remarks>
/// This function allows us to determine a set of impacted groups and run the resolver on just
/// those groups in order to support what-if analysis
/// </remarks>
/// <param name="groups">The existing position groups</param>
/// <param name="positions">The positions being changed</param>
/// <returns>An enumerable containing the position groups that could be impacted by the specified position changes</returns>
IEnumerable<IPositionGroup> GetImpactedGroups(
    PositionGroupCollection groups,
    IReadOnlyCollection<IPosition> positions
    );

```

### IPositionGroupBuyingPowerModel

Another appropriately named abstraction that leaves mystery on the sidelines. This interface aims to be an `IPositionGroup`-centric one-for-one mapping of the `Security`-centric `IBuyingPowerModel`. The only operations that were not ported from the original are the ones focused on getting/setting leverage, which simply doesn't apply to position groups. As you can see from the below excerpt, position groups require their own margin calculations, their own sufficient buying power for order checks and their own functions for determining the maximum quantity for a given delta/target buying power. I won't go into all of the methods as they're identical in purpose as their `IBuyingPower` counterparts, however, there is one _added_ method, and that is the `GetReservedBuyingPowerImpact`. One of the challenges of dealing with position groups is determining how a particular trade will impact the algorithm's groups. Consider our default case of 5 units of GOOG covered call. If we try to sell 1 GOOG share, bringing our total to 499 (500 - 1) shares of GOOG, it will _break_ one of the position group units. This will likely **increase** the total margin requirement of the portfolio. The `GetReservedBuyingPowerImpact` function exists to perform this 'what if' analysis.


``` csharp
// See: https://github.com/QuantConnect/Lean/blob/refactor-4065-position-groups/Common/Securities/Positions/IPositionGroupBuyingPowerModel.cs

/// <summary>
/// Gets the margin currently allocated to the specified holding
/// </summary>
/// <param name="parameters">An object containing the security</param>
/// <returns>The maintenance margin required for the </returns>
MaintenanceMargin GetMaintenanceMargin(PositionGroupMaintenanceMarginParameters parameters);

/// <summary>
/// The margin that must be held in order to increase the position by the provided quantity
/// </summary>
/// <param name="parameters">An object containing the security and quantity</param>
InitialMargin GetInitialMarginRequirement(PositionGroupInitialMarginParameters parameters);

/// <summary>
/// Gets the total margin required to execute the specified order in units of the account currency including fees
/// </summary>
/// <param name="parameters">An object containing the portfolio, the security and the order</param>
/// <returns>The total margin in terms of the currency quoted in the order</returns>
InitialMargin GetInitialMarginRequiredForOrder(PositionGroupInitialMarginForOrderParameters parameters);

/// <summary>
/// Computes the impact on the portfolio's buying power from adding the position group to the portfolio. This is
/// a 'what if' analysis to determine what the state of the portfolio would be if these changes were applied. The
/// delta (before - after) is the margin requirement for adding the positions and if the margin used after the changes
/// are applied is less than the total portfolio value, this indicates sufficient capital.
/// </summary>
/// <param name="parameters">An object containing the portfolio and a position group containing the contemplated
/// changes to the portfolio</param>
/// <returns>Returns the portfolio's total portfolio value and margin used before and after the position changes are applied</returns>
ReservedBuyingPowerImpact GetReservedBuyingPowerImpact(
    ReservedBuyingPowerImpactParameters parameters
    );

/// <summary>
/// Check if there is sufficient buying power for the position group to execute this order.
/// </summary>
/// <param name="parameters">An object containing the portfolio, the position group and the order</param>
/// <returns>Returns buying power information for an order against a position group</returns>
HasSufficientBuyingPowerForOrderResult HasSufficientBuyingPowerForOrder(
    HasSufficientPositionGroupBuyingPowerForOrderParameters parameters
    );

/// <summary>
/// Computes the amount of buying power reserved by the provided position group
/// </summary>
ReservedBuyingPowerForPositionGroup GetReservedBuyingPowerForPositionGroup(
    ReservedBuyingPowerForPositionGroupParameters parameters
    );

/// <summary>
/// Get the maximum position group order quantity to obtain a position with a given buying power
/// percentage. Will not take into account free buying power.
/// </summary>
/// <param name="parameters">An object containing the portfolio, the position group and the target
///     signed buying power percentage</param>
/// <returns>Returns the maximum allowed market order quantity and if zero, also the reason</returns>
GetMaximumLotsResult GetMaximumLotsForTargetBuyingPower(
    GetMaximumLotsForTargetBuyingPowerParameters parameters
    );

/// <summary>
/// Get the maximum market position group order quantity to obtain a delta in the buying power used by a position group.
/// The deltas sign defines the position side to apply it to, positive long, negative short.
/// </summary>
/// <param name="parameters">An object containing the portfolio, the position group and the delta buying power</param>
/// <returns>Returns the maximum allowed market order quantity and if zero, also the reason</returns>
/// <remarks>Used by the margin call model to reduce the position by a delta percent.</remarks>
GetMaximumLotsResult GetMaximumLotsForDeltaBuyingPower(
    GetMaximumLotsForDeltaBuyingPowerParameters parameters
    );

/// <summary>
/// Gets the buying power available for a position group trade
/// </summary>
/// <param name="parameters">A parameters object containing the algorithm's portfolio, security, and order direction</param>
/// <returns>The buying power available for the trade</returns>
PositionGroupBuyingPower GetPositionGroupBuyingPower(PositionGroupBuyingPowerParameters parameters);


```

### PositionGroupBuyingPowerModel

In the spirit of `BuyingPowerModel`, we've provided a base class for position group specific models to extend, as much of the logic is agnostic to the details of the group thanks to the way the various abstractions have been defined. That being said, when integrating options groups we **will** need to add an `OptionStrategyPositionGroupBuyingPowerModel` which subclasses the default base class `PositionGroupBuyingPowerModel`. The option strategy specific type will need to reference the table provided by Interactive Brokers which describes the margin requirements for each type of option strategy. You can find this table on IB's website [here](https://www.interactivebrokers.com/en/index.php?f=26660). This link, along with research notes and heaps of information outlining the general thought pattern and some of the concerns considered, all accumulated during the initial analysis/investigation phase in the github feature request issue [#4065](https://github.com/QuantConnect/Lean/issues/4065). IB seems to be what the users want, but once that's done I think it makes sense to also implement FINRA models. The takeaway from reading all of the regulations is that FINRA provides a baseline and brokers are free make them more strict (and maybe less strict, but IIRC FINRA set a limit), and indeed brokers can decide to _not_ support the concept of grouping at all. In such cases the broker would charge margin as the simple sum of the constituent parts, ie, no savings from groupings. We'll want to have a mechanism to suport this case easily. This can be easily done by configuring the `PositionManager` to only use the `SecurityPositionGroupResolver` (more on that later though).

Back to the `PositionGroupBuyingPowerModel` - you'll notice that these methods make no assumptions as to the type or structure of the position group being evaluated, and as such, uses a lot of 'what if' analysis to make determinations. A particularly interesting bit is the `GetMaximumLotsForTargetBuyingPower` function. This function _should_ be suitable for all subclasses as its phrased in the most general way possible. The `SecurityPositionGroupBuyingPowerModel` _does_ override it for backwards compatibility reasons, particularly because each `IBuyingPowerModel` implementation (I'm looking at you `CashBuyingPowerModel`) implements this functions _slightly_ differently and especially when it comes to how fees are handled. Fees can either be _part_ of the pro-rata operation or fees can be viewed as a fixed cost coming off of the top. It took a while to arrive at a logical reasoning for one or the other, but the `PositionGroupBuyingPowerModel` removes fees off of the top. The reason for this is based on this function's usage, most notably, `QCAlgorithm.SetHoldings`. The purpose of this function is to allocate a particular percentage of the total portfolio value into _something_ (a position group in this case). If the fees were part of the pro-rata allocation then that's like saying we seek a quantity where the **cost** of the order is a particular percentage of total portfolio value, whereas taking the fees off the top is saying that we seek a quantity such that **after** the trade is completed, that position group (or security) will be the requested percentage of the total portfolio value.

Some time was taken to improve the Newton-Rhapson root finding. For some reason all of the `IBuyingPowerModel` implementations of this method have degraded over the years, now requiring two full iterations before returning. This is unnecessary. In a very common case, where fees are directly proportional to the total order value, it all breaks down into a simple linear equation and can by analytically solved exactly _without_ iterating at all (very common in crypto). The time should be taken to refactor the existing `IBuyingPowerModel` implementations by extracting to a single common implementation and parameterizing anything that's special.

Another function worth mentioning is `HasSufficientBuyingPowerForOrder` which has to perform multiple checks now. First we determine that our free buying power is enough to cover the initial margin requirement, then we provide a mechanism via a virtual method `PassesPositionGroupSpecificBuyingPowerForOrderChecks` for subclasses to inject their own checks and finally we perform the 'what if' analysis by invoking `GetChangeInReservedBuyingPower` and verifying that the change isn't greater than the free buying power. The `SecurityPositionGroupBuyingPowerModel` overrides the `PassesPositionGroupSpecificBuyingPowerForOrderChecks` in order to invoke `security.BuyingPowerModel`.

It's worth noting here that **ALL** implementations of `IPositionGroupBuyingPowerModel` should provide reasonable/idiomatic implementations of `GetHashCode` and `Equals`. This is because the model types are used in the `PositionGroupKey`, and as such, equality checks are done against it, but we want to treat these models as value types when it comes to equality checking, i.e, verify private fields are equal and the types are equal - even beter, let a tool like ReSharper implement them for you :)

Also worth mentioning here is that **ALL** derived types will have to provide implementations for `GetInitialMarginRequirement`/`GetMaintenanceMargin`/`GetInitialMarginRequiredForOrder`. These functions enable us to implement the tougher functions in the base class and keeps subclasses laser focused on what makes them special, with an aim of preventing and/or reducing copy pasta. An optional override is the `PassesPositionGroupSpecificBuyingPowerForOrderChecks` which the `SecurityPositionGroupBuyingPowerModel` uses to invoke `security.BuyingPowerModel` functions directly.


``` csharp
// See: https://github.com/QuantConnect/Lean/blob/refactor-4065-position-groups/Common/Securities/Positions/PositionGroupBuyingPowerModel.cs
```

### SecurityPositionGroupBuyingPowerModel

Provides an implementation of `IPositionGroupBuyingPowerModel` that delegates to `security.BuyingPowerModel`. This model is intended to be used with the 'default' group and its aim is to provide 100% backwards compatible behavior. Below are the overriden methods with a _very_ brief comment describing how the delegating to the security's models happens. It's important to note here that `IPositionGroup.Quantity` is, from the security's perspective, a number of lots. In our 5 covered call example, there's 5 lots of (-1 GOOG CALL & 100 GOOG) for a total of -5 GOOG CALL & 500 GOOG shares. This is a position group quantity of 5. There are 5 lots of the GOOG equity and 5 lots of -1 GOOG CALL.

``` csharp
// See: https://github.com/QuantConnect/Lean/blob/refactor-4065-position-groups/Common/Securities/Positions/SecurityPositionGroupBuyingPowerModel.cs

/// <summary>
/// Gets the margin currently allocated to the specified holding
/// </summary>
/// <param name="parameters">An object containing the security</param>
/// <returns>The maintenance margin required for the </returns>
public override MaintenanceMargin GetMaintenanceMargin(PositionGroupMaintenanceMarginParameters parameters)
{
    // simply delegate to security.BuyingPowerModel.GetMaintenanceMargin
}

/// <summary>
/// The margin that must be held in order to increase the position by the provided quantity
/// </summary>
/// <param name="parameters">An object containing the security and quantity</param>
public override InitialMargin GetInitialMarginRequirement(PositionGroupInitialMarginParameters parameters)
{
    // simply delegates to security.BuyingPowerModel.GetInitialMarginRequirement
}

/// <summary>
/// Gets the total margin required to execute the specified order in units of the account currency including fees
/// </summary>
/// <param name="parameters">An object containing the portfolio, the security and the order</param>
/// <returns>The total margin in terms of the currency quoted in the order</returns>
public override InitialMargin GetInitialMarginRequiredForOrder(
    PositionGroupInitialMarginForOrderParameters parameters
    )
{
    // simply delegates to security.BuyingPowerModel.GetInitialMarginRequiredForOrder
}

/// <summary>
/// Get the maximum position group order quantity to obtain a position with a given buying power
/// percentage. Will not take into account free buying power.
/// </summary>
/// <param name="parameters">An object containing the portfolio, the position group and the target
///     signed buying power percentage</param>
/// <returns>Returns the maximum allowed market order quantity and if zero, also the reason</returns>
public override GetMaximumLotsResult GetMaximumLotsForTargetBuyingPower(
    GetMaximumLotsForTargetBuyingPowerParameters parameters
    )
{
    // simply delegates to security.BuyingPowerModel.GetMaximumOrderQuantityForTargetBuyingPower
    // and then converts the result which is in number of lots into a quantity using the lot size
    var quantity = result.Quantity / security.SymbolProperties.LotSize;
}

/// <summary>
/// Get the maximum market position group order quantity to obtain a delta in the buying power used by a position group.
/// The deltas sign defines the position side to apply it to, positive long, negative short.
/// </summary>
/// <param name="parameters">An object containing the portfolio, the position group and the delta buying power</param>
/// <returns>Returns the maximum allowed market order quantity and if zero, also the reason</returns>
/// <remarks>Used by the margin call model to reduce the position by a delta percent.</remarks>
public override GetMaximumLotsResult GetMaximumLotsForDeltaBuyingPower(
    GetMaximumLotsForDeltaBuyingPowerParameters parameters
    )
{
    // simply delegates to security.BuyingPowerModel.GetMaximumOrderQuantityForDeltaBuyingPower
    // and converts the maximum quantity into number of lots using the security's lot size
}

/// <summary>
/// Check if there is sufficient buying power for the position group to execute this order.
/// </summary>
/// <param name="parameters">An object containing the portfolio, the position group and the order</param>
/// <returns>Returns buying power information for an order against a position group</returns>
public override HasSufficientBuyingPowerForOrderResult HasSufficientBuyingPowerForOrder(
    HasSufficientPositionGroupBuyingPowerForOrderParameters parameters
    )
{
    // simply delegates to security.BuyingPowerModel.HasSufficientBuyingPowerForOrder
}

/// <summary>
/// Additionally check initial margin requirements if the algorithm only has default position groups
/// </summary>
protected override HasSufficientBuyingPowerForOrderResult PassesPositionGroupSpecificBuyingPowerForOrderChecks(
    HasSufficientPositionGroupBuyingPowerForOrderParameters parameters,
    decimal availableBuyingPower
    )
{
    // simply delegates to security.BuyingPowerModel.HasSufficientBuyingPowerForOrder for default groups
}

```

## LEAN Integration

The above highlights the main abstractions and key terms used throughout the position groups feature code changes and commit messages. If you don't understand anything written prior to this sentence, stop, and go read it again. The aforementioned concepts are critical to have a firm understanding in before moving forward with how it all integrates into LEAN, and the rest of this document _assumes_ that the reader understands all of the terminology by this point.

### PositionManager

The `PositionManager` provides a mechanism similar to `SecurityPortfolioManager` to manage positions and position groups. Event handlers are wired up such that after _every_ fill event the `PositionManager` is notified and if required, will invoke the configured `IPositionGroupResolver` to determine the latest and greatest set of groups. Any ungrouped holdings get moved into the default `SecurityPositionGroup` -- the group of last resort. ALL HOLDINGS ARE ALWAYS GROUPED.

The `CompositePositionGroupResolver` needs to be added to the manager. The idea behind this guy is he would hold a list of resolvers configured by the algorithm, for example, one for options, one for futures and the last one would be the default (resolver of last resort) `SecurityPositionGroupResolver`. There's a WIP branch (`origin/refactor-4065-position-groups.wip`) that has an implementation of the composite resolver as well as having it all wired up properly in the position manager. Feel free to pull that in, minor edits are required. The order in which the resolvers are invoked is obviously important. If the `SecurityPositionGroupResolver` went _firsT_ then everything would be grouped before the other resolvers ran, so obviously he needs to run last. His entire job is to group everything that didn't get grouped. Foot stomping here. The ordering of invocation matters. **GREATLY**

Once a `CompositePositionGroupResolver` is implemented, should probably start with it just having the single default resolver for simplicity (`SecurityPositionGroupResolver`), make sure all unit/regression tests are passing and that's a clean/solid breakpoint.

### Other Touch Points

`SecurityHolding.QuantityChanged` event was added and the `PositionManager` listens to this. Every time quantities change (fill) we need to know so that we can re-run the resolvers. `PositionManager.ResolveGroups()` is _also_ invoked via `SecurityPortfolioManager.ProcessFill` when it's a partial/completed fill event (qnantity changed). In order for all the margin maths to be correct, we must resolve groups immediately. Consider multiple market orders set to synchronous in the same `OnData` -- if we don't run the resolvers then the buying power models won't even know those orders executed because the buying power models' view of the world is through the lense of position groups, so it's incredibly important that **EVERY** time security holdings change we run the position group resolves to ensure consistent state.
