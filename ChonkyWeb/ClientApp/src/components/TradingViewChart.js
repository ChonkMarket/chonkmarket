import { createChart } from 'lightweight-charts';
import React, { useEffect, useState } from 'react';
import { areaSeries, lineSeries, candlestickSeries } from './ChartElements/ChartSeries';
import RealTimeButton from './ChartElements/RealTimeButton';
import SeriesLegend from './ChartElements/SeriesLegend';
import Tooltip from './ChartElements/Tooltip';
import styles from './TradingViewChart.module.css';
import { chartLayout, colorPalette, gridSettings, priceScaleSettings } from './utils/chartThemes';
import { formatDate } from './utils/formatUtils';
import setupWebsocket from './utils/websocket';

function TradingViewChart({ data, containerWidth, containerHeight, candlestick }) {
  const ref = React.useRef()
  const tooltipRef = React.useRef()
  const dateRef = React.useRef()
  const buttonRef = React.useRef()

  let priceSeries;
  if (candlestick) {
    priceSeries = new candlestickSeries({
      name: 'Stock Price',
      dataLabel: 'close',
      dataType: 'price',
      priceScaleId: 'right',
    });
  } else {
    priceSeries = new lineSeries({
      name: 'Stock Price',
      dataLabel: 'mark',
      dataType: 'price',
      priceScaleId: 'right'
    })
  }

  const series = {
    'Stock Price': priceSeries,
    'NOPE': new lineSeries({
      name: 'NOPE',
      dataLabel: 'nope',
      dataType: 'nope',
      priceScaleId: 'left',
    }),
    'COPE': new lineSeries({
      name: 'COPE',
      dataLabel: 'cope',
      dataType: 'nope',
      priceScaleId: 'left',
    }),
    'Calls Net Delta Volume': new areaSeries({
      name: 'Calls Net Delta Volume',
      dataLabel: 'totalCallOptionDelta',
      dataType: 'volume',
      priceScaleId: 'volume'
    }),
    'Puts Net Delta Volume': new areaSeries({
      name: 'Puts Net Delta Volume',
      dataLabel: 'totalPutOptionDelta',
      dataType: 'volume',
      priceScaleId: 'volume',
      modifier: -1
    }),
    'Calls Chonky Delta Volume': new areaSeries({
      name: 'Calls Chonky Delta Volume',
      dataLabel: 'totalCCallOptionDelta',
      dataType: 'volume',
      priceScaleId: 'volume'
    }),
    'Puts Chonky Delta Volume': new areaSeries({
      name: 'Puts Chonky Delta Volume',
      dataLabel: 'totalCPutOptionDelta',
      dataType: 'volume',
      priceScaleId: 'volume',
      modifier: -1
    }),
  }

  const volRange = {
    min: Infinity,
    max: 0,
  }

  let quotes = data.quotes || [];
  let info = data.info || {};
  quotes.map((d, i) => {
    for (const sName in series) {
      series[sName].fillData(d)
    }
    volRange.min = Math.min(volRange.min, Math.abs(d['totalCallOptionDelta']), Math.abs(d['totalPutOptionDelta']), Math.abs(d['totalCCallOptionDelta']), Math.abs(d['totalCPutOptionDelta']))
    volRange.max = Math.max(volRange.max, Math.abs(d['totalCallOptionDelta']), Math.abs(d['totalPutOptionDelta']), Math.abs(d['totalCCallOptionDelta']), Math.abs(d['totalCPutOptionDelta']))
  })

  for (const sName in series) {
    series[sName].seedData(quotes)
  }

  const initSeriesVisible = () => {
    const output = JSON.parse(localStorage.getItem('visibleSeries')) || {};
    for (const sName in series) {
      if (!output.hasOwnProperty(sName))
        output[sName] = !sName.includes("Volume")
    }
    return output
  }

  const [visibleRange, setVisibleRange] = useState([0, quotes.length - 1])
  const [btnVisible, setBtnVisible] = useState('none')
  const [realTime, setRealTime] = useState(true)
  const [btnClicks, setBtnClicks] = useState(1)
  const [visibleChange, setVisibleChange] = useState(1)
  const [seriesVisible, setSeriesVisible] = useState(initSeriesVisible())

  const btnClickHandler = () => {
    setBtnClicks(btnClicks * -1)
    setRealTime(true)
  }

  const legendClickHandler = (sName) => {
    const newState = seriesVisible
    newState[sName] = !(seriesVisible[sName])
    localStorage.setItem('visibleSeries', JSON.stringify(newState));
    setSeriesVisible(newState)
    setVisibleChange(visibleChange * -1)
  }

  useEffect(() => {
    // Create chart with baisc settings
    const chart = createChart(ref.current, {
      containerWidth,
      containerHeight,
      localization: {
        timeFormatter: time => formatDate(time), // display in HH:MM AM/PM format
      },
      layout: chartLayout,
      grid: gridSettings,
      rightPriceScale: priceScaleSettings,
      leftPriceScale: priceScaleSettings,
      timeScale: {
        borderColor: colorPalette.base,
        timeVisible: true,
        secondsVisible: false,
        tickMarkFormatter: time => formatDate(time),
        fixLeftEdge: true,
        shiftVisibleRangeOnNewBar: true,
      },

    })

    // Initialize data series

    for (const sName in series) {
      series[sName].initSeries(chart)
      series[sName].setData()
      series[sName].format()
      series[sName].setVisible(seriesVisible[sName]) // Show / hide series based on user toggle 
    }

    if (candlestick) {
      const socketObject = setupWebsocket(data.info.symbol)
      socketObject.push = series['Stock Price'].push.bind(series['Stock Price'])
    }

    series['Calls Net Delta Volume'].setScale(volRange)
    series['Puts Net Delta Volume'].setScale(volRange)
    series['Calls Chonky Delta Volume'].setScale(volRange)
    series['Puts Chonky Delta Volume'].setScale(volRange)

    // Time scale related handlers 

    const timeScale = chart.timeScale()

    timeScale.setVisibleLogicalRange({
      from: visibleRange[0],
      to: visibleRange[1],
    })

    chart.subscribeCrosshairMove(function (param) {

      var toolTipWidth = 120;
      var toolTipHeight = 100;
      var toolTipMargin = 15;

      if (param.point === undefined || !param.time || param.point.x < 0 || param.point.x > containerWidth || param.point.y < 0 || param.point.y > containerHeight) {
        tooltipRef.current.style.display = 'none';
      } else {
        const visibleKeys = [];
        for (const sName in series) {
          series[sName].fillToolTip(param)
          if (seriesVisible[sName])
            visibleKeys.push(sName);
        }
        dateRef.current.textContent = formatDate(param.time, 'long')
        tooltipRef.current.style.display = 'block';


        const yCoordinate = Math.max.apply(Math,
          [...new Array(visibleKeys.length)].map((s, i) => series[visibleKeys[i]].getYCoord()));

        const xCoordinate = param.point.x;

        if (yCoordinate === null) {
          return;
        }
        const newXCoordinate = Math.max(100, Math.min(containerWidth - toolTipWidth, xCoordinate - 150));

        const newYCoordinate =
          yCoordinate - toolTipHeight - toolTipMargin > 0 ?
            yCoordinate - toolTipHeight - toolTipMargin :
            Math.max(20, Math.min(containerHeight - toolTipHeight - toolTipMargin, yCoordinate + toolTipMargin));

        tooltipRef.current.style.left = newXCoordinate + 'px';
        tooltipRef.current.style.top = newYCoordinate + 'px';
      }

    });

    timeScale.subscribeVisibleLogicalRangeChange((newRange) => {
      setVisibleRange([newRange.from, newRange.to])
      if (timeScale.scrollPosition() < 0) {
        setRealTime(false)
        setBtnVisible('block')
      } else {
        setBtnVisible('none')
      }
    });

    if (realTime) {
      timeScale.setVisibleLogicalRange({
        from: visibleRange[0],
        to: quotes.length - 1
      })
    }

    return () => {
      chart.remove()
    }
  }, [quotes.length, containerWidth, containerHeight, btnClicks, visibleChange])

  return (
    <div
      ref={ref}
      style={{
        height: containerHeight - 40,
        width: containerWidth - 40,
        margin: 20,
        display: 'flex',
        justifyContent: 'center',
      }}
    >
      <Tooltip
        ref={{ tooltipRef, dateRef }}
        desc={info.description}
        symbol={info.symbol}
        series={series}
      />
      <RealTimeButton
        ref={buttonRef}
        containerWidth={containerWidth}
        containerHeight={containerHeight}
        btnVisible={btnVisible}
        btnClickHandler={btnClickHandler}
      />
      <div className={styles.legend}>
        {Object.keys(series).map((sName) => (
          <SeriesLegend
            key={sName}
            series={series[sName]}
            legendClickHandler={legendClickHandler}
            ref={series[sName].legendRef}
            visible={seriesVisible[sName]}
          />
        ))}
      </div>
    </div>
  );
}

export default TradingViewChart;
