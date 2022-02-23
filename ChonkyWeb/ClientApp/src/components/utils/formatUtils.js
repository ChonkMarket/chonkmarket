  export function formatDate(time, option='short') {

    const dateOptions = { year: 'numeric', month: 'numeric', day: 'numeric' }
    const timeOptions = { hour12: true, hour: 'numeric', minute: 'numeric' }

    return option === 'short' ?
      new Date(time).toLocaleTimeString([], timeOptions) :
      new Date(time).toLocaleDateString([], dateOptions) + ' ' + new Date(time).toLocaleTimeString([], timeOptions)

  }

  export function formatPrice(price) {
    return '$' + price.toFixed(2)
  }

  export function formatNope(nope) {
    return nope.toFixed(2) 
  }

  export function formatVol(vol) {
    return parseInt(vol.toFixed(0)).toLocaleString()
  }

  export const formatLabels = {
    date: formatDate,
    price: formatPrice,
    nope: formatNope,
    volume: formatVol,
  }