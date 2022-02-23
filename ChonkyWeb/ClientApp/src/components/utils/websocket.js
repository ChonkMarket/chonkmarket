import { load } from 'protobufjs'
import tradeProto from '../../protos/Trade.proto'
let Trade;

function onOpen(evt) {
  console.log("CONNECTED");
}

function onClose(evt) {
  console.log("DISCONNECTED");
}

let processingQueue = []
let stocks = {}
let initialized = false
let socketObject = {
  push: function () { }
};

function onMessage(evt) {
  var buffer = new Uint8Array(evt.data)
  var message = Trade.decode(buffer)
  message.TradeTime = message.TradeTime - (message.TradeTime % 30000);
  processingQueue.push(message)
}

let lasttime = 0;

function process() {
  while (processingQueue.length > 0) {
    var trade = processingQueue.shift();
    if (!stocks[trade.TradeTime])
      if (stocks[lasttime])
        stocks[trade.TradeTime] = { time: trade.TradeTime, open: stocks[lasttime].close, high: trade.Last, low: trade.Last, close: trade.Last }
      else
        stocks[trade.TradeTime] = { time: trade.TradeTime, open: trade.Last, high: trade.Last, low: trade.Last, close: trade.Last };
    else {
      var data = stocks[trade.TradeTime];
      data.high = Math.max(data.high, trade.Last);
      data.low = Math.min(data.low, trade.Last);
      data.close = trade.Last;
    }
    lasttime = trade.TradeTime;
    socketObject.push(stocks[trade.TradeTime])
  }
  window.setTimeout(process, 25);
}

window.setTimeout(process, 25)

function onError(evt) {
  console.log(evt);
}

export default function (symbol) {
  if (initialized)
    return socketObject;
  initialized = true
  load(tradeProto, (err, root) => {
    Trade = root.lookupType("Trade");
    let websocket = new WebSocket(`wss://${window.location.host}/api/nope/${symbol}/ws`);
    websocket.binaryType = "arraybuffer";
    websocket.onopen = function (evt) { onOpen(evt) };
    websocket.onclose = function (evt) { onClose(evt) };
    websocket.onmessage = function (evt) { onMessage(evt) };
    websocket.onerror = function (evt) { onError(evt) };
  })
  return socketObject;
}