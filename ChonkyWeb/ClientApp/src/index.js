import 'bootstrap/dist/css/bootstrap.css';
import React from 'react';
import ReactDOM from 'react-dom';
import { BrowserRouter } from 'react-router-dom';
import App from './App';
import '@stripe/stripe-js';
import { loadStripe } from '@stripe/stripe-js/pure';

const baseUrl = document.getElementsByTagName('base')[0].getAttribute('href');
const rootElement = document.getElementById('root');

let clientId;
let stripeKey;

if (process.env.NODE_ENV == 'production') {
  stripeKey = "stripe key";
  clientId = "stripe client id";
}
else {
  stripeKey = "stripe key";
  clientId = "stripe client id";
}

async function loadstripe() {
  window.stripe = await loadStripe(stripeKey);
}
loadstripe();

ReactDOM.render(
  <BrowserRouter basename={baseUrl}>
    <App />
  </BrowserRouter>,
  rootElement);

// Uncomment the line above that imports the registerServiceWorker function
// and the line below to register the generated service worker.
// By default create-react-app includes a service worker to improve the
// performance of the application by caching static assets. This service
// worker can interfere with the Identity UI, so it is
// disabled by default when Identity is being used.
//
//registerServiceWorker();

