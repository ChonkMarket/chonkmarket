import React, { useState, useEffect } from 'react'
import ReactMarkdown from 'react-markdown'
import { Jumbotron } from 'reactstrap'
import './AboutUs.css'
const text = `
# what is this

Stuff that we wished existed when we started trading SPY options. Everything in here has been heavily inspired by the NOPE discord community and the discussions therein about how to optimize trading strategies. We are firm believers in helping provide better access to the data that we're collecting and enabling people to become stronger traders together.

# what is nope

Net Option Pricing Effect. Check out the [NOPE discord](https://discord.com/invite/SJNxcgH5hV), [original whitepaper](https://www.scribd.com/document/487296659/Investigating-Delta-Gamma-Hedging-Impact-on-SPY-Returns-2007-2020), and [lily](https://twitter.com/nope_its_lily)'s [guide to nope](https://nope-its-lily.medium.com/interpreting-the-nope-a-brief-users-guide-41c57c1b47a0)

# what is cope

COPE (chonky option pricing effect, for lack of any better name) is an attempt to separate the option deltas from intraday movement in the stock. 

NOPE is calculated using the **total daily volume** of each available option on the chain and the **immediate delta value** for that option. This means that you can calculate a NOPE score at any point with a single option chain. This makes sense since it was originally designed looking interday, where you would only have the end of day option chain. As the day progresses, the delta changes as the underlying price changes, which causes the NOPE score to move with those changes. 

COPE is calculated using the **immediate volume** _(last 30 seconds of change, currently, but really just based on data ingestion rate)_ of each available option on the chain and the **immediate delta value** for that option. Changes in delta does not have an effect on the volume from earlier in the day, since the current delta is multiplied by the incremental volume change (i.e., current volume minus previous volume). Calculating historical COPE is really tricky since you need a LOT of data to be able to slice the day into small enough calculations for this to be meaningfully different from NOPE.

# what else are you planning on doing

Short term goals:

* Enabling backtesting of different option strategies (given an entry point and a direction, plot out various put/call strategies)
* Exploring better data providers, the data feed we have now is flakey at times
* Ingesting historical data feeds
* Adding support for a few more indicators

` +
// # why are you charging me money

// Right now we have this data back to 3/19 for a handful of tickers. Historical data is expensive, and getting full historical option chain state throughout the day is even more expensive. Subscription premiums are there to fund the acquisition of this data and, ideally, to support a broader range of tickers than just the few we are ingesting today.

`# questions?

Feel free to chat any of us up on discord or email us at <feedback@chonk.market>

# disclaimer

The tools contained herein are for informational purposes only. Nothing on this site is intended to as investment advice. Chonk.Market is not a registered investment, legal or tax advisor or a broker/dealer. Best efforts are made for the data presented herein to be factual and accurate, but unintended errors can occur. Investing is risky and you should not invest money you can't afford to lose.
`

const AboutUs = () => {
    const [markdown, setMarkdown] = useState('')

    useEffect(() =>  {
      setMarkdown(text)
    }, [])

    return (
        <div style={{display: 'flex', justifyContent: 'center'}}>
        <Jumbotron
            className="container aboutus"
            style={{
                    backgroundColor:'black',
                    margin: '3rem',
                    paddingTop: '2rem'}}>
            <ReactMarkdown source={markdown}></ReactMarkdown>
        </Jumbotron>
        </div>
    )

}

export default AboutUs