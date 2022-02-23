var moment = require("moment-timezone")

function isRegularTradingDay(date) {
  if (date.weekday() === 0 || date.weekday() === 6)
    return false;
  return true;
}

export function tradingDayStart(format) {
  var now = moment().tz("America/New_York")
  while (!isRegularTradingDay(now))
    now = now.subtract(1, 'days')
  let output;
  switch (format) {
    case 'quoteTime':
      output = now.hour(9).minute(30).seconds(0).milliseconds(0).format("x")
      break
    case 'dateObj':
    default:
      output = now.hour(9).minute(30).seconds(0).milliseconds(0).toDate()
  }
  return output
}

export function getLastDate(dateObj, period=7) {
  return new Date(dateObj.getTime() - (period * 24 * 60 * 60 * 1000))
}

export function isMarketOpen(date) {
  var now = moment().tz("America/New_York")
  var nowTime = encodeURIComponent(now.format("M/D/yyyy"))
  if (nowTime !== date)
    return false;
  return (now.hour() === 9 && now.minute() >= 30) || now.hour() > 9 || now.hour() < 16;
}

export function timeTilMarketOpen() {
  var now = moment().tz("America/New_York")
  var start = tradingDayStart('dateObj');
  return start - now;
}

export function encodeDate(dateObj) {
  return encodeURIComponent(dateObj.toLocaleDateString('en-US', { timeZone: 'America/New_York' }))
}

export function datePicker(dateObj) {
  let now = moment().tz("America/New_York")
  return now.year(dateObj.getYear()+1900).month(dateObj.getMonth()).date(dateObj.getDate()).hour(9).minute(31).seconds(0).milliseconds(0).toDate()
}