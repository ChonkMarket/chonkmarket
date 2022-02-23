import React, { useEffect, useState, useCallback } from 'react';
import { withRouter } from "react-router";
import { useLocation } from "react-router-dom";
import TradingViewChart from './TradingViewChart';
import debounce from './utils/debounce';
import { datePicker, encodeDate, isMarketOpen, tradingDayStart, timeTilMarketOpen } from './utils/tradingDayUtil';
import GraphSpinner from './ChartElements/GraphSpinner'
import CountdownTimers from './ChartElements/CountdownTimers'

function Graph(props) {
  const [loading, setLoading] = useState(true)
  const [evtSource, setEventSource] = useState(null)
  const [height, setHeight] = useState(heightCalc())
  const [width, setWidth] = useState(window.innerWidth)
  const [graphData, setGraphData] = useState({ info: {}, quotes: [] })
  const marketOpenTime = timeTilMarketOpen()

  const useQuery = () => new URLSearchParams(useLocation().search)
  let symbol = useQuery().get("ticker") || 'SPY';
  let currDate = useQuery().get("date") || encodeDate(datePicker(tradingDayStart('dateObj')))

  useEffect(() => {
    const debouncedHandleResize = debounce(() => {
      setHeight(heightCalc())
      setWidth(window.innerWidth)
    }, 500)

    window.addEventListener('resize', debouncedHandleResize)

    return _ => {
      window.removeEventListener('resize', debouncedHandleResize)
    }
  }, [])

  useEffect(() => {
    setGraphData({ info: {}, quotes: [] });
    setLoading(true);

    async function fetchData() {
      const response = await fetch(`api/nope/${symbol}?date=${currDate}`);
      let data = (await response.json()).data;
      if (data && data.quotes && data.quotes.length > 0) {
        buildCope(data.quotes)
      }
      let timeMax = Math.max.apply(Math, [0,...data.quotes.map(o => o.quoteTime)])
      if (evtSource !== null) {
        evtSource.close();
      }
      if (isMarketOpen(currDate)) {
        let source = createSource(timeMax, currDate)
        setEventSource(source)
      }
      setTitle(data.info, data.quotes[data.quotes.length - 1])
      setGraphData(data)
      setLoading(false)
    }
    fetchData();
  }, [symbol, currDate])

  useEffect(() => {
    if (evtSource !== null) {
      if (evtSource.readyState == 1)
        evtSource.onerror = reconnect;
      evtSource.addEventListener('message', updateData);

      return () => {
        evtSource.removeEventListener('message', updateData);
      }
    }
  }, [graphData.quotes.length, evtSource])

  const buildCope = function (quoteSet) {
    let accumulator = {
      putOptionDelta: 0,
      callOptionDelta: 0
    }
    for (const data of quoteSet) {
      updateCopeValues(accumulator, data)
    }
  }

  const addNewData = useCallback((newData) => {
    let accumulator = {
      putOptionDelta: 0,
      callOptionDelta: 0
    }
    if (graphData.quotes.length > 0) {
      var lastElem = graphData.quotes[graphData.quotes.length - 1];
      accumulator = {
        putOptionDelta: lastElem.totalCPutOptionDelta,
        callOptionDelta: lastElem.totalCCallOptionDelta
      };
    }
    if (newData.mark) {
      updateCopeValues(accumulator, newData)
      newData = [newData]
    }
    else {
      for (const data of newData) {
        updateCopeValues(accumulator, data)
      }
    }
    setGraphData({ info: graphData.info, quotes: [...graphData.quotes, ...newData] })
  }, [graphData])

  const updateCopeValues = function (accumulator, data) {
    data.totalCPutOptionDelta = data.localPutOptionDelta + accumulator.putOptionDelta
    accumulator.putOptionDelta = data.totalCPutOptionDelta

    data.totalCCallOptionDelta = data.localCallOptionDelta + accumulator.callOptionDelta
    accumulator.callOptionDelta = data.totalCCallOptionDelta

    data.cope = (data.totalCCallOptionDelta + data.totalCPutOptionDelta) / data.totalVolume * 10000
  }

  useEffect(() => {
    if (evtSource !== null) {
      let timeout;
      evtSource.addEventListener("market_close", function (e) {
        var now = Date.now()
        if (now > e.data) {
          evtSource.close();
        } else {
          timeout = window.setTimeout(_ => { evtSource.close(); }, e.data - now);
        }
      });
      return () => {
        window.clearTimeout(timeout);
      }
    }
  }, [evtSource]);

  function isEmpty(obj) {
    return obj && Object.keys(obj).length === 0 && obj.constructor === Object
  }

  function setTitle(info, quote) {
    if (isEmpty(info) || !quote || isEmpty(quote) || isEmpty(quote.nope)) {
      window.document.title = `NO DATA`
    } else {
      window.document.title = `${info.symbol} ${quote.nope.toFixed(2)} $${quote.mark}`
    }
  }

  function createSource(timeMax, date) {
    var source = new EventSource(`/api/nope/${symbol}/sse?quoteTime=${timeMax}&date=${date}`);
    return source;
  }

  function reconnect() {
    evtSource.close();
    var source = createSource(graphData.quotes[graphData.quotes.length - 1].quoteTime, currDate);
    setEventSource(source)
  }

  function updateData(e) {
    var data = JSON.parse(e.data)
    if (data.info && data.quotes && data.quotes.length > 0) {
      setTitle(data.info, data.quotes[data.quotes.length - 1])
      addNewData(data.quotes)
      setGraphData({ info: graphData.info, quotes: [...graphData.quotes, ...data.quotes] })
    } else if (data.mark) {
      addNewData([data])
      setTitle(graphData.info, data)
      setGraphData({ info: graphData.info, quotes: [...graphData.quotes, data] })
    }
  }

  function heightCalc() {
    var navElement = document.getElementsByTagName('nav')[0];
    if (navElement) {
      var navMargin = window.getComputedStyle(navElement).marginBottom.replace('px', '');
      var navHeight = navElement.offsetHeight;
      return window.innerHeight - navMargin - navHeight;
    } else {
      return window.innerHeight - 73;
    }
  }

  let contents;
  if (loading) {
    contents = <div style={{margin: "20px", display: 'flex', justifyContent: 'center', paddingTop: '20%'}}><GraphSpinner /></div>;
  } else if (graphData.quotes.length === 0) {
    if (marketOpenTime > 0) {
      contents = <CountdownTimers milliseconds={marketOpenTime}/>
    } else {
      contents = <div style={{margin: "20px", display: 'flex', justifyContent: 'center', paddingTop: '20%'}}><em>No data found</em></div>;
    }
  } else {
    contents = <TradingViewChart data={graphData} containerWidth={width} containerHeight={height}/>;
  }

  return (
    <div id="chart">
      {contents}
    </div>
  )

}

export default withRouter(Graph);