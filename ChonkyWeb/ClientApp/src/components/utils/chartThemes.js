export const colorPalette = {
    'NOPE': 'rgba(241, 86, 59, 1)', // orange-ish
    'COPE': 'rgba(200, 200, 200, 1)', // grey-ish
    'Stock Price': 'steelblue',
    'Calls Net Delta Volume': 'rgba(60, 179, 113, ', // green-ish
    'Puts Net Delta Volume': 'rgba(220, 20, 60, ', // red-ish
    'Calls Chonky Delta Volume': 'rgba(60, 179, 113, ', // green-ish
    'Puts Chonky Delta Volume': 'rgba(220, 20, 60, ', // red-ish
    base: 'rgba(197, 203, 206, 1)' // grey-ish
}

export const chartLayout = {
    fontFamily: '-apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, "Noto Sans", sans-serif, "Apple Color Emoji", "Segoe UI Emoji", "Segoe UI Symbol", "Noto Color Emoji"',
    backgroundColor: 'transparent',
    textColor: colorPalette.base,
    fontSize: 12,
}

export const gridSettings = {
    vertLines: {
        visible: false,
    },
    horzLines: {
        visible: false
    },
}

export const priceScaleSettings = {
    visible: true,
    borderColor: colorPalette.base,
    scaleMargins: {
      top: 0.2,
      bottom: 0.2
    }
}


  // THEME NOT USED YET

  const darkTheme = {
    chart: {
      layout: {
        backgroundColor: 'black',
        lineColor: 'white',
        textColor: 'white',
      },
      grid: {
        vertLines: {
          visible: false,
        }, 
        horzLines: {
          visible: false,
        },
      },
    },
    series: {
      lineColor: 'black'
    }
  }

