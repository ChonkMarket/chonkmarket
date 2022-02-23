namespace StockDataLibrary
{
    using StockDataLibrary.Models;
    using System;
    using System.Collections.Generic;

    public class CopeCalculator
    {
        private readonly Dictionary<string, TdaOptionChain> _previousChains = new();
        private readonly Dictionary<string, OratsOptionChain> _previousOratsChains = new();
        private readonly object _locker = new();

        public void SetPrevious(OratsOptionChain chain)
        {
            var date = TradingHours.GetMarketHours(chain.QuoteDate);
            _previousOratsChains[$"{chain.Symbol}-{date.Date}"] = chain;
        }

        public void Calculate(OratsOptionChain chain)
        {
            OratsOptionChain previousChain;
            var date = TradingHours.GetMarketHours(chain.QuoteDate);
            lock (_locker)
            {
                if (!_previousOratsChains.TryGetValue($"{chain.Symbol}-{date.Date}", out previousChain))
                {
                    SetPrevious(chain);
                    if (chain.LocalCallOptionDelta == 0.0 && chain.LocalPutOptionDelta == 0.0)
                    {
                        chain.LocalCallOptionDelta = chain.TotalCallOptionDelta;
                        chain.LocalPutOptionDelta = chain.TotalPutOptionDelta;
                    }
                    return;
                }
            }

            foreach (var option in chain.Options)
            {
                var prevOption = previousChain.Options.Find(o => (o.Strike == option.Strike && o.Expiration == option.Expiration));
                if (prevOption == null)
                {
                    // weird scenario where sometimes an option is missing from the chain
                    // does not seem common
                    // options seem to populate the chain during the day but don't disappear later afaict
                    //
                    continue;
                }
                var newCallVolume = option.CallVolume - prevOption.CallVolume;
                if (newCallVolume > 0)
                    chain.LocalCallOptionDelta += option.Delta * newCallVolume;

                var newPutVolume = option.PutVolume - prevOption.PutVolume;
                if (newPutVolume > 0)
                    chain.LocalPutOptionDelta += (option.Delta - 1) * newPutVolume;
            }
            SetPrevious(chain);
        }

        public void SetPrevious(TdaOptionChain chain)
        {
            var date = TradingHours.GetMarketHours(chain.Underlying.QuoteTime);
            _previousChains[$"{chain.Symbol}-{date.Date}"] = chain;
        }

        public double Calculate(TdaOptionChain chain)
        {
            TdaOptionChain previousChain;
            var date = TradingHours.GetMarketHours(chain.Underlying.QuoteTime);
            lock (_locker)
            {
                if (!_previousChains.TryGetValue($"{chain.Symbol}-{date.Date}", out previousChain))
                {
                    SetPrevious(chain);
                    return chain.Nope;
                }
            }

            var callDelta = 0.0F;
            foreach (var expDateMapEntry in chain.CallExpDateMap)
            {
                var optionMap = expDateMapEntry.Value;
                var key = expDateMapEntry.Key;
                var prevMap = previousChain.CallExpDateMap.GetValueOrDefault(key);
                foreach (var strikePrice in optionMap.Keys)
                {
                    var prevOptionMap = prevMap.GetValueOrDefault(strikePrice);
                    var currOptionMap = optionMap.GetValueOrDefault(strikePrice);
                    var prevOption = prevOptionMap[0];
                    var currOption = currOptionMap[0];

                    if (currOption.TotalVolume > 0 && double.IsFinite(currOption.Delta) && currOption.TotalVolume > prevOption.TotalVolume)
                    {
                        var newVolume = currOption.TotalVolume - prevOption.TotalVolume;
                        callDelta += (currOption.Delta * newVolume);
                    }
                }
            }

            var putDelta = 0.0F;
            foreach (var expDateMapEntry in chain.PutExpDateMap)
            {
                var optionMap = expDateMapEntry.Value;
                var key = expDateMapEntry.Key;
                var prevMap = previousChain.PutExpDateMap.GetValueOrDefault(key);
                foreach (var strikePrice in optionMap.Keys)
                {
                    var prevOptionMap = prevMap.GetValueOrDefault(strikePrice);
                    var currOptionMap = optionMap.GetValueOrDefault(strikePrice);
                    var prevOption = prevOptionMap[0];
                    var currOption = currOptionMap[0];

                    if (currOption.TotalVolume > 0 && double.IsFinite(currOption.Delta))
                    {
                        var newVolume = currOption.TotalVolume - prevOption.TotalVolume;
                        putDelta += (currOption.Delta * newVolume);
                    }
                }
            }

            chain.Underlying.LocalCallOptionDelta = callDelta;
            chain.Underlying.LocalPutOptionDelta = putDelta;
            chain.Underlying.LocalVolume = chain.Underlying.TotalVolume - previousChain.Underlying.TotalVolume;
            SetPrevious(chain);
            return (chain.Underlying.LocalCallOptionDelta + chain.Underlying.LocalPutOptionDelta) * 10000 / chain.Underlying.LocalVolume;
        }
    }
}
