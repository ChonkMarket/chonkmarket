import React from 'react';
import { colorPalette } from '../utils/chartThemes';
import { formatLabels } from '../utils/formatUtils';

export class chartSeries {
  constructor({ name, dataLabel, dataType, modifier = 1, priceScaleId }) {
    this.name = name
    this.ref = React.useRef()
    this.legendRef = React.useRef()
    this.tradingViewSeries = null
    this.color = colorPalette[name]
    this.data = []
    this.dataLabel = dataLabel // e.g., 'nope', 'mark', 'totalCallOptionDelta', 'totalPutOptionDelta'
    this.dataType = dataType // e.g., 'price', 'volume', 'nope'
    this.dataModifier = modifier
    this.latestData = null
    this.currVal = null
    this.priceScaleId = priceScaleId
  }

  seedData(data) {
    const latestData = data[data.length - 1][this.dataLabel]
    this.latestData = formatLabels[this.dataType](latestData * this.dataModifier)
  }

  fillData(d) {
    var time = d['quoteTime'];
    if (time < 2000000000)
      time *= 1000;
    this.data.push({
      time: time,
      value: d[this.dataLabel] * this.dataModifier
    })
  }

  setData() {
    this.tradingViewSeries.setData(this.data)
  }

  fillToolTip(param) {
    this.currVal = param.seriesPrices.get(this.tradingViewSeries)
    if (this.currVal.close != undefined) {
      this.currVal = this.currVal.close;
    }
    this.ref.current.textContent = formatLabels[this.dataType](this.currVal)
  }

  setVisible(v) {
    this.tradingViewSeries.applyOptions({
      visible: v
    })
  }

  format() {
    this.tradingViewSeries.applyOptions({
      priceFormat: {
        type: 'customer',
        formatter: formatLabels[this.dataType]
      }
    })
  }

  getYCoord() {
    return this.tradingViewSeries.priceToCoordinate(this.currVal)
  }
}

export class lineSeries extends chartSeries {
  constructor({ name, dataLabel, dataType, modifier = 1, priceScaleId }) {
    super({ name, dataLabel, dataType, modifier, priceScaleId })
    this.type = 'line'
  }

  initSeries(chart) {
    this.tradingViewSeries = chart.addLineSeries({
      color: this.color,
      lineWidth: 2,
      title: this.name,
      priceScaleId: this.priceScaleId
    })
  }

}

export class candlestickSeries extends chartSeries {
  constructor({ name, dataLabel, dataType, modifier = 1, priceScaleId }) {
    super({ name, dataLabel, dataType, modifier, priceScaleId })
    this.type = 'candlestick'
  }

  initSeries(chart) {
    let purple = '#A083D5'
    let yellow = '#EFCF20'
    this.tradingViewSeries = chart.addCandlestickSeries({
      borderVisible: true,
      upColor: purple,
      wickUpColor: purple,
      borderUpColor: '#b19cd8',
      downColor: yellow,
      wickDownColor: yellow,
      borderDownColor: '#f4e064',
      title: this.name,
      priceScaleId: this.priceScaleId
    })
  }

  push(data) {
    if (this.tradingViewSeries) {
      this.tradingViewSeries.update(data)
    }
  }

  fillData(d) {
    var open = d.open;
    if (this.data.length > 0)
      open = (this.data[this.data.length - 1].open + this.data[this.data.length - 1].close) / 2
    var close = (d.open + d.high + d.low + d.close) / 4;
    this.data.push({
      time: d['quoteTime'] * 1000,
      open: open,
      high: Math.max(d.high, open, close),
      low: Math.min(d.low, open, close),
      close: close
    })
  }
}

export class areaSeries extends chartSeries {
  constructor({ name, dataLabel, dataType, modifier = 1, priceScaleId }) {
    super({ name, dataLabel, dataType, modifier, priceScaleId })
    this.type = 'area'
    this.colorFull = this.color + ' 1)'
  }

  initSeries(chart) {
    this.tradingViewSeries = chart.addAreaSeries({
      topColor: this.color + ' 0.4)',
      bottomColor: this.color + ' 0',
      lineColor: this.colorFull,
      lineWidth: 2,
      crosshairMarkerBackgroundColor: this.color + ' 1)',
      priceLineVisible: false,
      lastValueVisible: false,
      priceScaleId: this.priceScaleId,
    })

  }

  setScale(range) {
    this.tradingViewSeries.applyOptions({
      autoscaleInfoProvider: () => ({
        priceRange: {
          minValue: range.min,
          maxValue: range.max,
        }
      })
    })
  }
}